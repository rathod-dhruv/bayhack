// using System.Collections.Generic;
// using System.Linq;
// using Meta.XR.MRUtilityKit;
// using UnityEngine;

// public class SpawnAroundObject : MonoBehaviour
// {
//     public List<Vector3> SpawnPositions { get; private set; } = new List<Vector3>();

//     [Header("Prefab to Spawn")]
//     [Tooltip("Prefab to spawn on the floor around the table.")]
//     public GameObject spawnPrefab;

//     [Header("How Many To Spawn")]
//     [Tooltip("Number of prefabs to spawn around the table.")]
//     [Min(1)]
//     public int prefabCount = 2;

//     [Header("Spacing")]
//     [Tooltip("Minimum distance between any two spawned prefabs.")]
//     public float minSeparation = 0.5f;

//     [Header("Raycast Settings")]
//     [Tooltip("Height above the table center from which we start horizontal scans.")]
//     public float castHeight = 10f;

//     [Tooltip("Maximum horizontal distance from the table center to search for spawn points.")]
//     public float maxDistance = 5f;

//     [Tooltip("Step size (meters) when scanning outward along each direction.")]
//     public float step = 0.25f;

//     [Tooltip("Small lift above the floor to avoid z-fighting.")]
//     public float floorOffset = 0.01f;

//     [Header("Floor Detection")]
//     [Tooltip("How strictly we require the hit normal to point upward (1 = perfectly up, 0 = any direction). 0.7 is a good 'floor-ish' threshold.")]
//     [Range(0f, 1f)]
//     public float floorNormalDotThreshold = 0.7f;

//     [Tooltip("Tolerance (meters) above the table's bottom Y to still consider something as floor (prevents hitting top surfaces).")]
//     public float floorHeightTolerance = 0.1f;

//     private void Start()
//     {
//         if (MRUK.Instance == null)
//         {
//             Debug.LogError("[SpawnAroundTable] MRUK.Instance is null – make sure the MRUK prefab is in the scene.");
//             return;
//         }

//         MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
//     }

//     // -------------------------------------------------------------
//     // 1. Called once MRUK has loaded the scanned scene/room.
//     // -------------------------------------------------------------
//     private void OnSceneLoaded()
//     {
//         var room = MRUK.Instance.GetCurrentRoom();
//         if (room == null)
//         {
//             Debug.LogError("[SpawnAroundTable] No current MRUK room found.");
//             return;
//         }

//         if (spawnPrefab == null)
//         {
//             Debug.LogError("[SpawnAroundTable] spawnPrefab is not assigned.");
//             return;
//         }

//         // Find all TABLE anchors
//         List<MRUKAnchor> tableAnchors = room.Anchors
//             .Where(a => a.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
//             .ToList();

//         Debug.Log($"[SpawnAroundTable] Found {tableAnchors.Count} TABLE anchors in room.");

//         if (tableAnchors.Count == 0)
//         {
//             Debug.LogWarning("[SpawnAroundTable] No TABLE anchors found. Nothing to spawn around.");
//             return;
//         }

//         // Use only the first table anchor
//         var tableAnchor = tableAnchors[0];
//         ProcessTable(room, tableAnchor);
//     }

//     // -------------------------------------------------------------
//     // 2. For a given TABLE anchor, find its collider and spawn around it
//     // -------------------------------------------------------------
//     private void ProcessTable(MRUKRoom room, MRUKAnchor tableAnchor)
//     {
//         // Try to find a BoxCollider in the anchor's hierarchy
//         BoxCollider box = tableAnchor.GetComponentInChildren<BoxCollider>();
//         if (box == null)
//         {
//             Debug.LogError("[SpawnAroundTable] No BoxCollider found under TABLE anchor.");
//             return;
//         }

//         Bounds b = box.bounds;

//         Debug.Log(
//             "[SpawnAroundTable] Table Collider Bounds:\n" +
//             $"   Center : {b.center}\n" +
//             $"   Min    : {b.min}\n" +
//             $"   Max    : {b.max}\n" +
//             $"   Size   : {b.size}\n" +
//             $"   Extents: {b.extents}\n" +
//             $"   Collider Object: {box.gameObject.name}"
//         );

//         SpawnConfiguredCountAroundTable(room, b);
//     }

//     // -------------------------------------------------------------
//     // 3. Spawn EXACTLY 'prefabCount' objects on the floor around the table,
//     //    ensuring:
//     //      - on floor,
//     //      - inside room bounds,
//     //      - at least minSeparation apart.
//     // -------------------------------------------------------------
//     private void SpawnConfiguredCountAroundTable(MRUKRoom room, Bounds b)
//     {
//         int targetCount = Mathf.Max(1, prefabCount);

//         Vector3 center = b.center;
//         float floorYApprox = b.min.y; // approx floor level near the table

//         // Directions we’ll try sampling around the table
//         List<Vector3> directions = new List<Vector3>
//         {
//             Vector3.right,                    // +X
//             -Vector3.right,                   // -X
//             Vector3.forward,                  // +Z
//             -Vector3.forward,                 // -Z
//             (Vector3.right + Vector3.forward).normalized,
//             (Vector3.right - Vector3.forward).normalized,
//             (-Vector3.right + Vector3.forward).normalized,
//             (-Vector3.right - Vector3.forward).normalized
//         };

//         List<Vector3> validPoints = new List<Vector3>();

//         // 1) Collect raycast-based floor positions, enforcing minSeparation AND room bounds
//         foreach (var dir in directions)
//         {
//             if (validPoints.Count >= targetCount)
//                 break;

//             if (TryFindFloorPointAlongDirection(
//                     room,
//                     center,
//                     dir,
//                     castHeight,
//                     maxDistance,
//                     step,
//                     floorOffset,
//                     floorYApprox,
//                     out Vector3 spawnPos))
//             {
//                 if ((room == null || room.IsPositionInRoom(spawnPos)) &&
//                     IsFarFromExisting(spawnPos, validPoints, minSeparation))
//                 {
//                     validPoints.Add(spawnPos);
//                     Debug.Log($"[SpawnAroundTable] Candidate floor point: {spawnPos} (dir {dir})");
//                 }

//                 var pathSpawner = FindObjectOfType<NavMeshPathSpawner>();
//                 if (pathSpawner != null)
//                 {
//                     Debug.Log("[SpawnAroundTable] Calling NavMeshPathSpawner.ShowPathToClosestSpawn()");
//                     pathSpawner.ShowPathToClosestSpawn();
//                 }
//                 else
//                 {
//                     Debug.LogWarning("[SpawnAroundTable] No NavMeshPathSpawner found in the scene.");
//                 }
//             }
//         }

//         // 2) If we did not get enough, synthesize more positions near the table,
//         //    still respecting minSeparation AND room bounds.
//         if (validPoints.Count == 0)
//         {
//             Debug.LogWarning("[SpawnAroundTable] No valid floor points found via raycast. Using fallback near table.");

//             int safety = 0;
//             while (validPoints.Count < targetCount && safety < targetCount * 4)
//             {
//                 float angle = ((float)safety / Mathf.Max(1, targetCount)) * Mathf.PI * 2f;
//                 Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
//                 Vector3 fallback = center + dir * 0.5f;
//                 fallback.y = floorYApprox + floorOffset;

//                 if ((room == null || room.IsPositionInRoom(fallback)) &&
//                     IsFarFromExisting(fallback, validPoints, minSeparation))
//                 {
//                     validPoints.Add(fallback);
//                 }

//                 safety++;
//             }
//         }
//         else if (validPoints.Count < targetCount)
//         {
//             Debug.LogWarning($"[SpawnAroundTable] Only {validPoints.Count} valid floor point(s) found. Synthesizing {targetCount - validPoints.Count} more.");

//             int safety = 0;
//             while (validPoints.Count < targetCount && safety < targetCount * 8)
//             {
//                 int idx = safety % validPoints.Count;
//                 Vector3 basePoint = validPoints[idx];

//                 Vector3 horiz = basePoint - center;
//                 horiz.y = 0f;
//                 if (horiz.sqrMagnitude < 0.0001f)
//                 {
//                     horiz = Vector3.right;
//                 }

//                 float dist = horiz.magnitude;
//                 horiz.Normalize();

//                 // Try opposite side
//                 Vector3 candidate = center - horiz * dist;
//                 candidate.y = floorYApprox + floorOffset;

//                 // If outside room, try perpendicular
//                 if (room != null && !room.IsPositionInRoom(candidate))
//                 {
//                     Vector3 perp = new Vector3(-horiz.z, 0f, horiz.x);
//                     candidate = center + perp * dist;
//                     candidate.y = floorYApprox + floorOffset;
//                 }

//                 if ((room == null || room.IsPositionInRoom(candidate)) &&
//                     IsFarFromExisting(candidate, validPoints, minSeparation))
//                 {
//                     validPoints.Add(candidate);
//                 }

//                 safety++;
//             }

//             // If still not enough (extreme edge case), fill in a small radial pattern
//             int safety2 = 0;
//             while (validPoints.Count < targetCount && safety2 < targetCount * 4)
//             {
//                 float angle = ((float)safety2 / Mathf.Max(1, targetCount)) * Mathf.PI * 2f;
//                 Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
//                 Vector3 fallback = center + dir * 0.75f;
//                 fallback.y = floorYApprox + floorOffset;

//                 if ((room == null || room.IsPositionInRoom(fallback)) &&
//                     IsFarFromExisting(fallback, validPoints, minSeparation))
//                 {
//                     validPoints.Add(fallback);
//                 }

//                 safety2++;
//             }
//         }

//         // 3) Spawn exactly 'targetCount' prefabs at the first N points
//         if (validPoints.Count < targetCount)
//         {
//             Debug.LogWarning($"[SpawnAroundTable] Could only generate {validPoints.Count} unique in-room positions " +
//                              $"with minSeparation={minSeparation}. Spawning that many.");
//             targetCount = validPoints.Count;
//         }

//         for (int i = 0; i < targetCount; i++)
//         {
//             Vector3 pos = validPoints[i];

//             // var go = Instantiate(spawnPrefab, pos, Quaternion.identity);
//             SpawnAt(pos);
//             // go.name = $"TableSpawn_{i + 1}";
//             Debug.Log($"[SpawnAroundTable] Spawned TableSpawn_{i + 1} at {pos}");
//         }
//     }

//     public void SpawnAt(Vector3 position)
//     {
//         // Spawn the prefab
//         Instantiate(spawnPrefab, position, Quaternion.identity);

//         // Record this position for pathfinding
//         SpawnPositions.Add(position);
//     }

//     // -------------------------------------------------------------
//     // 4. Try to find a FLOOR-ONLY point via raycasts outward from the table
//     //    (also checked for room bounds by the caller before accepting).
//     // -------------------------------------------------------------
//     private bool TryFindFloorPointAlongDirection(
//         MRUKRoom room,
//         Vector3 center,
//         Vector3 direction,
//         float height,
//         float maxDistance,
//         float step,
//         float floorOffset,
//         float floorYApprox,
//         out Vector3 result)
//     {
//         result = Vector3.zero;

//         // We only care about horizontal direction
//         direction.y = 0f;
//         if (direction.sqrMagnitude < 0.0001f)
//             return false;

//         direction.Normalize();

//         // Start from a point above the table center
//         Vector3 top = center + Vector3.up * height;

//         for (float d = 0.5f; d <= maxDistance; d += step)
//         {
//             // This is the horizontal sample point at this distance
//             Vector3 scanPos = top + direction * d;

//             // Raycast straight down from this point to find floor
//             if (Physics.Raycast(scanPos, Vector3.down, out RaycastHit hit, height * 2f))
//             {
//                 // 1) Ensure the surface normal is sufficiently "upward" (floor-like)
//                 float upDot = Vector3.Dot(hit.normal, Vector3.up);
//                 if (upDot < floorNormalDotThreshold)
//                     continue;

//                 // 2) Ensure the hit Y is at or slightly above the table bottom (floor-ish),
//                 //    not some surface much higher (like table top).
//                 if (hit.point.y > floorYApprox + floorHeightTolerance)
//                     continue;

//                 Vector3 floorPoint = hit.point + Vector3.up * floorOffset;

//                 // 3) Caller will also check IsPositionInRoom, but we can early-filter too
//                 if (room != null && !room.IsPositionInRoom(floorPoint))
//                     continue;

//                 result = floorPoint;
//                 return true;
//             }
//         }

//         return false;
//     }

//     // -------------------------------------------------------------
//     // 5. Helper: ensure candidate is far enough from all existing points
//     // -------------------------------------------------------------
//     private bool IsFarFromExisting(Vector3 candidate, List<Vector3> existing, float minDist)
//     {
//         float minDistSq = minDist * minDist;
//         foreach (var p in existing)
//         {
//             if ((candidate - p).sqrMagnitude < minDistSq)
//                 return false;
//         }
//         return true;
//     }
// }

using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class SpawnAroundObject : MonoBehaviour
{
    public List<Vector3> SpawnPositions { get; private set; } = new List<Vector3>();

    [Header("Prefab to Spawn")]
    [Tooltip("Prefab to spawn on the floor around the table.")]
    public GameObject spawnPrefab;

    [Header("How Many To Spawn")]
    [Tooltip("Number of prefabs to spawn around the table.")]
    [Min(1)]
    public int prefabCount = 2;

    [Header("Spacing")]
    [Tooltip("Minimum distance between any two spawned prefabs.")]
    public float minSeparation = 0.5f;

    [Header("Raycast Settings")]
    [Tooltip("Height above the table center from which we start horizontal scans.")]
    public float castHeight = 10f;

    [Tooltip("Maximum horizontal distance from the table center to search for spawn points.")]
    public float maxDistance = 5f;

    [Tooltip("Step size (meters) when scanning outward along each direction.")]
    public float step = 0.25f;

    [Tooltip("Small lift above the floor to avoid z-fighting.")]
    public float floorOffset = 0.01f;

    [Header("Floor Detection")]
    [Tooltip("How strictly we require the hit normal to point upward (1 = perfectly up, 0 = any direction). 0.7 is a good 'floor-ish' threshold.")]
    [Range(0f, 1f)]
    public float floorNormalDotThreshold = 0.7f;

    [Tooltip("Tolerance (meters) above the table's bottom Y to still consider something as floor (prevents hitting top surfaces).")]
    public float floorHeightTolerance = 0.1f;

    private void Start()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogError("[SpawnAroundTable] MRUK.Instance is null – make sure the MRUK prefab is in the scene.");
            return;
        }

        MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
    }

    private void OnSceneLoaded()
    {
        var room = MRUK.Instance.GetCurrentRoom();
        if (room == null)
        {
            Debug.LogError("[SpawnAroundTable] No current MRUK room found.");
            return;
        }

        if (spawnPrefab == null)
        {
            Debug.LogError("[SpawnAroundTable] spawnPrefab is not assigned.");
            return;
        }

        List<MRUKAnchor> tableAnchors = room.Anchors
            .Where(a => a.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE))
            .ToList();

        Debug.Log($"[SpawnAroundTable] Found {tableAnchors.Count} TABLE anchors in room.");

        if (tableAnchors.Count == 0)
        {
            Debug.LogWarning("[SpawnAroundTable] No TABLE anchors found. Nothing to spawn around.");
            return;
        }

        var tableAnchor = tableAnchors[0];
        ProcessTable(room, tableAnchor);
    }

    private void ProcessTable(MRUKRoom room, MRUKAnchor tableAnchor)
    {
        BoxCollider box = tableAnchor.GetComponentInChildren<BoxCollider>();
        if (box == null)
        {
            Debug.LogError("[SpawnAroundTable] No BoxCollider found under TABLE anchor.");
            return;
        }

        Bounds b = box.bounds;

        Debug.Log(
            "[SpawnAroundTable] Table Collider Bounds:\n" +
            $"   Center : {b.center}\n" +
            $"   Min    : {b.min}\n" +
            $"   Max    : {b.max}\n" +
            $"   Size   : {b.size}\n" +
            $"   Extents: {b.extents}\n" +
            $"   Collider Object: {box.gameObject.name}"
        );

        SpawnConfiguredCountAroundTable(room, b);
    }

    private void SpawnConfiguredCountAroundTable(MRUKRoom room, Bounds b)
    {
        int targetCount = Mathf.Max(1, prefabCount);

        Vector3 center = b.center;
        float floorYApprox = b.min.y;

        List<Vector3> directions = new List<Vector3>
        {
            Vector3.right,
            -Vector3.right,
            Vector3.forward,
            -Vector3.forward,
            (Vector3.right + Vector3.forward).normalized,
            (Vector3.right - Vector3.forward).normalized,
            (-Vector3.right + Vector3.forward).normalized,
            (-Vector3.right - Vector3.forward).normalized
        };

        List<Vector3> validPoints = new List<Vector3>();

        // 1) Collect raycast-based floor positions
        foreach (var dir in directions)
        {
            if (validPoints.Count >= targetCount)
                break;

            if (TryFindFloorPointAlongDirection(
                    room,
                    center,
                    dir,
                    castHeight,
                    maxDistance,
                    step,
                    floorOffset,
                    floorYApprox,
                    out Vector3 spawnPos))
            {
                if ((room == null || room.IsPositionInRoom(spawnPos)) &&
                    IsFarFromExisting(spawnPos, validPoints, minSeparation))
                {
                    validPoints.Add(spawnPos);
                    Debug.Log($"[SpawnAroundTable] Candidate floor point: {spawnPos} (dir {dir})");
                }
            }
        }

        // 2) Fallback / synthesize points if needed (unchanged)
        if (validPoints.Count == 0)
        {
            Debug.LogWarning("[SpawnAroundTable] No valid floor points found via raycast. Using fallback near table.");

            int safety = 0;
            while (validPoints.Count < targetCount && safety < targetCount * 4)
            {
                float angle = ((float)safety / Mathf.Max(1, targetCount)) * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 fallback = center + dir * 0.5f;
                fallback.y = floorYApprox + floorOffset;

                if ((room == null || room.IsPositionInRoom(fallback)) &&
                    IsFarFromExisting(fallback, validPoints, minSeparation))
                {
                    validPoints.Add(fallback);
                }

                safety++;
            }
        }
        else if (validPoints.Count < targetCount)
        {
            Debug.LogWarning($"[SpawnAroundTable] Only {validPoints.Count} valid floor point(s) found. Synthesizing {targetCount - validPoints.Count} more.");

            int safety = 0;
            while (validPoints.Count < targetCount && safety < targetCount * 8)
            {
                int idx = safety % validPoints.Count;
                Vector3 basePoint = validPoints[idx];

                Vector3 horiz = basePoint - center;
                horiz.y = 0f;
                if (horiz.sqrMagnitude < 0.0001f)
                {
                    horiz = Vector3.right;
                }

                float dist = horiz.magnitude;
                horiz.Normalize();

                Vector3 candidate = center - horiz * dist;
                candidate.y = floorYApprox + floorOffset;

                if (room != null && !room.IsPositionInRoom(candidate))
                {
                    Vector3 perp = new Vector3(-horiz.z, 0f, horiz.x);
                    candidate = center + perp * dist;
                    candidate.y = floorYApprox + floorOffset;
                }

                if ((room == null || room.IsPositionInRoom(candidate)) &&
                    IsFarFromExisting(candidate, validPoints, minSeparation))
                {
                    validPoints.Add(candidate);
                }

                safety++;
            }

            int safety2 = 0;
            while (validPoints.Count < targetCount && safety2 < targetCount * 4)
            {
                float angle = ((float)safety2 / Mathf.Max(1, targetCount)) * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 fallback = center + dir * 0.75f;
                fallback.y = floorYApprox + floorOffset;

                if ((room == null || room.IsPositionInRoom(fallback)) &&
                    IsFarFromExisting(fallback, validPoints, minSeparation))
                {
                    validPoints.Add(fallback);
                }

                safety2++;
            }
        }

        // 3) Spawn prefabs & fill SpawnPositions
        if (validPoints.Count < targetCount)
        {
            Debug.LogWarning($"[SpawnAroundTable] Could only generate {validPoints.Count} unique in-room positions " +
                             $"with minSeparation={minSeparation}. Spawning that many.");
            targetCount = validPoints.Count;
        }

        for (int i = 0; i < targetCount; i++)
        {
            Vector3 pos = validPoints[i];
            SpawnAt(pos);
            Debug.Log($"[SpawnAroundTable] Spawned TableSpawn_{i + 1} at {pos}");
        }

        Debug.Log($"[SpawnAroundTable] Total SpawnPositions after spawning = {SpawnPositions.Count}");

        // 🔹 NOW call NavMeshPathSpawner, AFTER SpawnPositions is populated
        var pathSpawner = FindObjectOfType<NavMeshPathSpawner>();
        if (pathSpawner != null)
        {
            Debug.Log("[SpawnAroundTable] Calling NavMeshPathSpawner.ShowPathToClosestSpawn()");
            pathSpawner.ShowPathToClosestSpawn();
        }
        else
        {
            Debug.LogWarning("[SpawnAroundTable] No NavMeshPathSpawner found in the scene.");
        }
    }

    public void SpawnAt(Vector3 position)
    {
        Instantiate(spawnPrefab, position, Quaternion.identity);
        SpawnPositions.Add(position);
    }

    private bool TryFindFloorPointAlongDirection(
        MRUKRoom room,
        Vector3 center,
        Vector3 direction,
        float height,
        float maxDistance,
        float step,
        float floorOffset,
        float floorYApprox,
        out Vector3 result)
    {
        result = Vector3.zero;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();

        Vector3 top = center + Vector3.up * height;

        for (float d = 0.5f; d <= maxDistance; d += step)
        {
            Vector3 scanPos = top + direction * d;

            if (Physics.Raycast(scanPos, Vector3.down, out RaycastHit hit, height * 2f))
            {
                float upDot = Vector3.Dot(hit.normal, Vector3.up);
                if (upDot < floorNormalDotThreshold)
                    continue;

                if (hit.point.y > floorYApprox + floorHeightTolerance)
                    continue;

                Vector3 floorPoint = hit.point + Vector3.up * floorOffset;

                if (room != null && !room.IsPositionInRoom(floorPoint))
                    continue;

                result = floorPoint;
                return true;
            }
        }

        return false;
    }

    private bool IsFarFromExisting(Vector3 candidate, List<Vector3> existing, float minDist)
    {
        float minDistSq = minDist * minDist;
        foreach (var p in existing)
        {
            if ((candidate - p).sqrMagnitude < minDistSq)
                return false;
        }
        return true;
    }
}