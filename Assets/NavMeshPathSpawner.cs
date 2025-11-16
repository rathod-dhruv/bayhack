// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.AI;

// public class NavMeshPathSpawner : MonoBehaviour
// {
//     [Header("References")]
//     public Transform xrCamera;
//     public SpawnAroundObject spawnAroundObject;

//     [Header("Prefab Settings")]
//     public GameObject pathPrefab;           // The breadcrumb object
//     public float spacing = 0.25f;           // Distance between breadcrumbs
//     public float heightOffset = 0.02f;      // Lift slightly above ground

//     [Header("NavMesh")]
//     public float sampleRadius = 1f;

//     private NavMeshPath _path;
//     private readonly List<GameObject> spawnedObjects = new();

//     private void Awake()
//     {
//         _path = new NavMeshPath();
//         if (!xrCamera) xrCamera = Camera.main.transform;
//     }

//     /// <summary>
//     /// Clears old breadcrumbs and spawns a new path.
//     /// </summary>
//     public void ShowPathToClosestSpawn()
//     {
//         Clear();

//         if (xrCamera == null || spawnAroundObject == null || spawnAroundObject.SpawnPositions.Count == 0)
//         {
//             Debug.LogWarning("[NavMeshPathSpawner] Missing camera or spawn positions.");
//             return;
//         }

//         // START — sample camera position onto NavMesh
//         if (!NavMesh.SamplePosition(xrCamera.position, out NavMeshHit startHit, sampleRadius, NavMesh.AllAreas))
//         {
//             Debug.LogWarning("[NavMeshPathSpawner] Camera not on NavMesh.");
//             return;
//         }

//         // TARGET — closest spawn point
//         Vector3 target = GetClosestSpawn(startHit.position);

//         if (!NavMesh.SamplePosition(target, out NavMeshHit targetHit, sampleRadius, NavMesh.AllAreas))
//         {
//             Debug.LogWarning("[NavMeshPathSpawner] Target not on NavMesh.");
//             return;
//         }

//         // Compute path
//         if (!NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, _path)
//             || _path.status != NavMeshPathStatus.PathComplete)
//         {
//             Debug.LogWarning("[NavMeshPathSpawner] No valid path.");
//             return;
//         }

//         SpawnAlongPath(_path);
//     }

//     private Vector3 GetClosestSpawn(Vector3 from)
//     {
//         float bestDist = float.MaxValue;
//         Vector3 bestPos = Vector3.zero;

//         foreach (Vector3 p in spawnAroundObject.SpawnPositions)
//         {
//             float d = (p - from).sqrMagnitude;
//             if (d < bestDist)
//             {
//                 bestDist = d;
//                 bestPos = p;
//             }
//         }
//         return bestPos;
//     }

//     /// <summary>
//     /// Spawns breadcrumbs along entire NavMesh path.
//     /// </summary>
//     private void SpawnAlongPath(NavMeshPath path)
//     {
//         for (int i = 0; i < path.corners.Length - 1; i++)
//         {
//             Vector3 start = path.corners[i];
//             Vector3 end = path.corners[i + 1];

//             float dist = Vector3.Distance(start, end);
//             int steps = Mathf.CeilToInt(dist / spacing);

//             for (int s = 0; s <= steps; s++)
//             {
//                 float t = s / (float)steps;
//                 Vector3 pos = Vector3.Lerp(start, end, t);
//                 pos.y += heightOffset;

//                 GameObject obj = Instantiate(pathPrefab, pos, Quaternion.identity);
//                 spawnedObjects.Add(obj);
//             }
//         }
//     }

//     /// <summary>
//     /// Removes existing breadcrumbs from the scene.
//     /// </summary>
//     public void Clear()
//     {
//         foreach (var obj in spawnedObjects)
//         {
//             if (obj) Destroy(obj);
//         }
//         spawnedObjects.Clear();
//     }
// }


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshPathSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("XR Center Eye / Main Camera. If null, will try Camera.main in Awake.")]
    public Transform xrCamera;

    [Tooltip("Script that contains the spawn positions on the floor.")]
    public SpawnAroundObject spawnAroundObject;

    [Header("Debug Target (Optional)")]
    [Tooltip("If set, you can test paths directly to this transform instead of using SpawnPositions.")]
    public Transform debugTarget;

    [Header("Prefab Settings")]
    [Tooltip("The breadcrumb object to spawn along the path.")]
    public GameObject pathPrefab;

    [Tooltip("Distance between breadcrumbs along the path.")]
    public float spacing = 0.25f;

    [Tooltip("Lift breadcrumbs slightly above the floor to avoid z-fighting.")]
    public float heightOffset = 0.02f;

    [Header("NavMesh")]
    [Tooltip("Radius used by NavMesh.SamplePosition for camera & target.")]
    public float sampleRadius = 1f;

    [Header("Debug")]
    public bool autoRunOnStart = false;
    public bool logDetails = true;

    private NavMeshPath _path;
    public readonly List<GameObject> spawnedObjects = new();

    private void Awake()
    {
        _path = new NavMeshPath();

        if (!xrCamera && Camera.main != null)
        {
            xrCamera = Camera.main.transform;
            if (logDetails) Debug.Log("[NavMeshPathSpawner] Using Camera.main as xrCamera.");
        }
    }

    private void Start()
    {
        if (autoRunOnStart)
        {
            if (debugTarget != null)
            {
                if (logDetails) Debug.Log("[NavMeshPathSpawner] autoRunOnStart → ShowPathToDebugTarget");
                ShowPathToDebugTarget();
            }
            else
            {
                if (logDetails) Debug.Log("[NavMeshPathSpawner] autoRunOnStart → ShowPathToClosestSpawn");
                ShowPathToClosestSpawn();
            }
        }
    }

    /// <summary>
    /// Clears old breadcrumbs and spawns a new path to the closest spawn point.
    /// </summary>
    public void ShowPathToClosestSpawn()
    {
        Clear();

        if (xrCamera == null)
        {
            Debug.LogWarning("[NavMeshPathSpawner] xrCamera is null. Assign it or tag a camera as MainCamera.");
            return;
        }

        if (spawnAroundObject == null)
        {
            Debug.LogWarning("[NavMeshPathSpawner] spawnAroundObject is null. Assign it in the inspector.");
            return;
        }

        if (spawnAroundObject.SpawnPositions == null || spawnAroundObject.SpawnPositions.Count == 0)
        {
            Debug.LogWarning("[NavMeshPathSpawner] SpawnPositions is empty. Did you spawn around the object yet?");
            return;
        }

        // 1) Sample camera position onto NavMesh
        Vector3 camPos = xrCamera.position;
        if (!NavMesh.SamplePosition(camPos, out NavMeshHit startHit, sampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[NavMeshPathSpawner] Camera at {camPos} not on NavMesh (SamplePosition failed).");
            return;
        }
        if (logDetails) Debug.Log($"[NavMeshPathSpawner] Start NavMesh pos = {startHit.position}");

        // 2) Find closest spawn
        Vector3 target = GetClosestSpawn(startHit.position);
        if (logDetails) Debug.Log($"[NavMeshPathSpawner] Closest spawn (raw) = {target}");

        // 3) Sample target onto NavMesh
        if (!NavMesh.SamplePosition(target, out NavMeshHit targetHit, sampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[NavMeshPathSpawner] Target at {target} not on NavMesh (SamplePosition failed).");
            return;
        }
        if (logDetails) Debug.Log($"[NavMeshPathSpawner] Target NavMesh pos = {targetHit.position}");

        // 4) Compute path
        if (!NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, _path))
        {
            Debug.LogWarning("[NavMeshPathSpawner] NavMesh.CalculatePath returned false.");
            return;
        }

        if (_path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning($"[NavMeshPathSpawner] Path status = {_path.status}, no complete path.");
            return;
        }

        if (logDetails)
        {
            Debug.Log($"[NavMeshPathSpawner] Path found with {_path.corners.Length} corners.");
        }

        // 5) Spawn breadcrumbs along path
        SpawnAlongPath(_path);
    }

    /// <summary>
    /// For debugging: compute a path to a manual Transform target.
    /// </summary>
    public void ShowPathToDebugTarget()
    {
        Clear();

        if (xrCamera == null || debugTarget == null)
        {
            Debug.LogWarning("[NavMeshPathSpawner] Missing xrCamera or debugTarget.");
            return;
        }

        if (!NavMesh.SamplePosition(xrCamera.position, out NavMeshHit startHit, sampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning("[NavMeshPathSpawner] Camera not on NavMesh for debug target.");
            return;
        }

        if (!NavMesh.SamplePosition(debugTarget.position, out NavMeshHit targetHit, sampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning("[NavMeshPathSpawner] Debug target not on NavMesh.");
            return;
        }

        if (!NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, _path)
            || _path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning("[NavMeshPathSpawner] No valid path to debug target.");
            return;
        }

        if (logDetails)
            Debug.Log($"[NavMeshPathSpawner] Debug path found with {_path.corners.Length} corners.");

        SpawnAlongPath(_path);
    }

    private Vector3 GetClosestSpawn(Vector3 from)
    {
        float bestDist = float.MaxValue;
        Vector3 bestPos = Vector3.zero;

        foreach (Vector3 p in spawnAroundObject.SpawnPositions)
        {
            float d = (p - from).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                bestPos = p;
            }
        }

        if (logDetails)
            Debug.Log($"[NavMeshPathSpawner] Closest spawn chosen = {bestPos}, dist^2 = {bestDist}");

        return bestPos;
    }

    /// <summary>
    /// Spawns breadcrumbs along entire NavMesh path.
    /// </summary>
    private void SpawnAlongPath(NavMeshPath path)
    {
        if (pathPrefab == null)
        {
            Debug.LogWarning("[NavMeshPathSpawner] pathPrefab is null, cannot spawn breadcrumbs.");
            return;
        }

        if (path.corners == null || path.corners.Length < 2)
        {
            Debug.LogWarning("[NavMeshPathSpawner] Path has insufficient corners to spawn along.");
            return;
        }

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 start = path.corners[i];
            Vector3 end = path.corners[i + 1];

            float dist = Vector3.Distance(start, end);
            int steps = Mathf.CeilToInt(dist / spacing);
            if (steps <= 0) steps = 1;

            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector3 pos = Vector3.Lerp(start, end, t);
                pos.y += heightOffset;

                GameObject obj = Instantiate(pathPrefab, pos, Quaternion.identity);
                spawnedObjects.Add(obj);
            }
        }

        if (logDetails)
            Debug.Log($"[NavMeshPathSpawner] Spawned {spawnedObjects.Count} prefabs along the path.");

        DisableAll();

    }

    public void DisableAll()
    {
        
        foreach(var x in spawnedObjects)
            x.gameObject.SetActive(false);
    }
    /// <summary>
    /// Removes existing breadcrumbs from the scene.
    /// </summary>
    public void Clear()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj) Destroy(obj);
        }
        spawnedObjects.Clear();
    }
}