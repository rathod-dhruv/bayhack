using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshPathVisualizer : MonoBehaviour
{
    [Header("NavMesh Agent")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Path Settings")]
    [SerializeField] private float waypointDistance = 2f; // Distance between waypoints
    [SerializeField] private GameObject pathPrefab; // Prefab to spawn at each waypoint
    [SerializeField] private bool visualizePath = true;
    [SerializeField] private Color pathColor = Color.green;

    [Header("Path Management")]
    [SerializeField] private bool clearPreviousPath = true; // Clear old waypoints when setting new destination

    private List<Vector3> pathPoints = new List<Vector3>();
    private List<GameObject> spawnedPathObjects = new List<GameObject>();
    private NavMeshPath currentPath;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        currentPath = new NavMeshPath();
    }

   

    /// <summary>
    /// Set a destination and create waypoints along the path
    /// </summary>
    public void SetDestinationAndCreatePath(Vector3 destination)
    {
        if (agent == null)
        {
            Debug.LogError("[PathVisualizer] NavMeshAgent is null!");
            return;
        }

        // Clear previous path if needed
        if (clearPreviousPath)
        {
            ClearPath();
        }

        // Calculate path
        if (agent.CalculatePath(destination, currentPath))
        {
            if (currentPath.status == NavMeshPathStatus.PathComplete)
            {
                // Generate waypoints along the path
                GenerateWaypointsAlongPath(currentPath);

                // Actually set the agent's destination
                agent.SetDestination(destination);

                Debug.Log($"[PathVisualizer] Path created with {pathPoints.Count} waypoints");
            }
            else
            {
                Debug.LogWarning($"[PathVisualizer] Path incomplete! Status: {currentPath.status}");
            }
        }
        else
        {
            Debug.LogWarning("[PathVisualizer] Failed to calculate path!");
        }
    }

    /// <summary>
    /// Generate waypoints at fixed intervals along the NavMesh path
    /// </summary>
    private void GenerateWaypointsAlongPath(NavMeshPath path)
    {
        pathPoints.Clear();

        if (path.corners.Length < 2)
        {
            Debug.LogWarning("[PathVisualizer] Path has less than 2 corners");
            return;
        }

        // Start from agent's current position
        Vector3 currentPoint = agent.transform.position;
        pathPoints.Add(currentPoint);

        // Spawn prefab at start
        if (pathPrefab != null)
        {
            SpawnPathPrefab(currentPoint, 0);
        }

        float accumulatedDistance = 0f;
        int waypointIndex = 1;

        // Iterate through path corners
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 segmentStart = (i == 0) ? currentPoint : path.corners[i];
            Vector3 segmentEnd = path.corners[i + 1];
            
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            Vector3 segmentDirection = (segmentEnd - segmentStart).normalized;

            float distanceAlongSegment = 0f;

            // Place waypoints along this segment
            while (distanceAlongSegment < segmentLength)
            {
                float remainingDistance = waypointDistance - accumulatedDistance;

                if (distanceAlongSegment + remainingDistance <= segmentLength)
                {
                    // Place waypoint
                    distanceAlongSegment += remainingDistance;
                    Vector3 waypointPos = segmentStart + segmentDirection * distanceAlongSegment;
                    
                    pathPoints.Add(waypointPos);

                    // Spawn prefab at waypoint
                    if (pathPrefab != null)
                    {
                        SpawnPathPrefab(waypointPos, waypointIndex);
                    }

                    waypointIndex++;
                    accumulatedDistance = 0f;
                }
                else
                {
                    // Move to next segment
                    accumulatedDistance += (segmentLength - distanceAlongSegment);
                    break;
                }
            }
        }

        // Add final destination point
        Vector3 finalPoint = path.corners[path.corners.Length - 1];
        if (Vector3.Distance(pathPoints[pathPoints.Count - 1], finalPoint) > 0.1f)
        {
            pathPoints.Add(finalPoint);
            
            if (pathPrefab != null)
            {
                SpawnPathPrefab(finalPoint, waypointIndex);
            }
        }
    }

    /// <summary>
    /// Spawn prefab at waypoint location
    /// </summary>
    private void SpawnPathPrefab(Vector3 position, int index)
    {
        GameObject spawnedObj = Instantiate(pathPrefab, position, Quaternion.identity);
        spawnedObj.name = $"PathWaypoint_{index}";
        spawnedObj.transform.SetParent(transform); // Optional: parent to this object
        spawnedPathObjects.Add(spawnedObj);
    }

    /// <summary>
    /// Clear all spawned path objects and path points
    /// </summary>
    public void ClearPath()
    {
        // Destroy all spawned objects
        foreach (GameObject obj in spawnedPathObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedPathObjects.Clear();
        pathPoints.Clear();

        Debug.Log("[PathVisualizer] Path cleared");
    }

    /// <summary>
    /// Get all waypoint positions
    /// </summary>
    public List<Vector3> GetPathPoints()
    {
        return new List<Vector3>(pathPoints);
    }

    /// <summary>
    /// Get all spawned path objects
    /// </summary>
    public List<GameObject> GetSpawnedPathObjects()
    {
        return new List<GameObject>(spawnedPathObjects);
    }

    // Visualize the path in the editor
    private void OnDrawGizmos()
    {
        if (!visualizePath || pathPoints.Count < 2)
            return;

        Gizmos.color = pathColor;

        // Draw lines between waypoints
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
            Gizmos.DrawSphere(pathPoints[i], 0.1f);
        }

        // Draw final point
        if (pathPoints.Count > 0)
        {
            Gizmos.DrawSphere(pathPoints[pathPoints.Count - 1], 0.15f);
        }
    }

    private void OnDestroy()
    {
        ClearPath();
    }
}