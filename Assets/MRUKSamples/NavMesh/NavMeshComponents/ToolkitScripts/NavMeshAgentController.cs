// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using UnityEngine.AI;

namespace MRUtilityKitSample.NavMesh
{
    [MetaCodeSample("MRUKSample-NavMesh")]
    public class NavMeshAgentController : MonoBehaviour
    {
        [Header("Distance Settings")]
        public float minDistanceFromUser = 3f; // Minimum distance for spawn
        public float maxDistanceFromUser = 8f; // Maximum distance for spawn
        
        [Header("Path Visualization")]
        public GameObject pathPrefab; // Assign your path marker prefab
        public float pathSpacing = 0.5f; // Distance between path markers
        public bool spawnPathMarkers = true;
        
        [Header("Original Settings")]
        private UnityEngine.AI.NavMeshAgent agent;
        private GameObject positionIndicator;
        public bool VisualizeTargetPosition = false;
        
        public List<GameObject> currentPathMarkers = new List<GameObject>();
        private Transform userTransform; // Will use camera
        private bool hasSpawnedPath = false; // Flag to spawn only once

        void OnEnable()
        {
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            var childTransform = transform.Find("PositionIndicator");
            positionIndicator = childTransform?.gameObject;
            
            // Get user camera position
            userTransform = Camera.main.transform;
        }

        void Start()
        {
            // Spawn path ONCE on start
            SpawnDestinationAndPath();
        }

        void SpawnDestinationAndPath()
        {
            if (hasSpawnedPath)
            {
                Debug.Log("Path already spawned, skipping...");
                return;
            }

            // Generate random position FAR from user
            var newPos = GetRandomFarPosition();

            var room = MRUK.Instance?.GetCurrentRoom();
            if (!room)
            {
                Debug.LogError("No room found!");
                return;
            }

            var test = room.IsPositionInRoom(newPos, false);

            if (!test)
            {
                Debug.Log("[NavMeshAgent] [Error]: destination is outside the room bounds, retrying...");
                Invoke(nameof(SpawnDestinationAndPath), 0.5f); // Retry after delay
                return;
            }

            if (VisualizeTargetPosition && positionIndicator != null)
            {
                positionIndicator.transform.parent = null;
                positionIndicator.transform.position = newPos;
            }

            // Set destination ONCE
            agent.SetDestination(newPos);
            
            // Set speed
            agent.speed = Random.Range(1.2f, 1.6f);
            
            // Wait for path calculation then spawn markers
            StartCoroutine(SpawnPathAfterCalculation());
            
            hasSpawnedPath = true;
            Debug.Log($"Destination set at {newPos}, distance: {Vector3.Distance(transform.position, newPos):F2}m");
        }

        // Generate random position that is FAR from user
        Vector3 GetRandomFarPosition()
        {
            Vector3 userPos = userTransform.position;
            Vector3 candidatePos = Vector3.zero;
            int maxAttempts = 20;
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                candidatePos = RandomNavPoint();
                
                float distance = Vector3.Distance(userPos, candidatePos);
                
                // Check if distance is within desired range
                if (distance >= minDistanceFromUser && distance <= maxDistanceFromUser)
                {
                    Debug.Log($"Found position {distance:F2}m away from user");
                    return candidatePos;
                }
            }
            
            // Fallback: just use random point
            Debug.LogWarning("Could not find point in distance range, using random point");
            return candidatePos;
        }

        // Wait for NavMesh path calculation then spawn markers
        IEnumerator SpawnPathAfterCalculation()
        {
            // Wait for path to be calculated
            while (agent.pathPending)
            {
                yield return null;
            }
            
            if (spawnPathMarkers && agent.hasPath)
            {
                SpawnPathMarkers();
            }

            DisableAll();
        }

        // Spawn prefabs along the NavMesh path
        void SpawnPathMarkers()
        {
            // Clear old markers
            ClearPathMarkers();
            
            if (pathPrefab == null)
            {
                Debug.LogWarning("Path Prefab not assigned!");
                return;
            }
            
            NavMeshPath path = agent.path;
            
            if (path.corners.Length < 2)
            {
                Debug.LogWarning("Path has less than 2 corners");
                return;
            }
            
            // Calculate total path length
            float totalLength = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                totalLength += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            
            Debug.Log($"Path length: {totalLength:F2}m, Spawning markers every {pathSpacing}m");
            
            // Spawn markers along path
            float accumulatedDistance = 0f;
            
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Vector3 segmentStart = path.corners[i];
                Vector3 segmentEnd = path.corners[i + 1];
                float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
                
                // Spawn markers along this segment
                while (accumulatedDistance < segmentLength)
                {
                    float t = accumulatedDistance / segmentLength;
                    Vector3 markerPos = Vector3.Lerp(segmentStart, segmentEnd, t);
                    
                    // Calculate rotation to face next point
                    Vector3 direction = (segmentEnd - segmentStart).normalized;
                    Quaternion rotation = Quaternion.LookRotation(direction);
                    
                    // Spawn marker
                    GameObject marker = Instantiate(pathPrefab, markerPos, rotation);
                    currentPathMarkers.Add(marker);
                    
                    accumulatedDistance += pathSpacing;
                }
                
                accumulatedDistance -= segmentLength;
            }
            
            // Always add final corner
            GameObject finalMarker = Instantiate(pathPrefab, path.corners[path.corners.Length - 1], Quaternion.identity);
            currentPathMarkers.Add(finalMarker);
            
            Debug.Log($"Spawned {currentPathMarkers.Count} path markers");
            
        }

        // Clear all existing path markers
        void ClearPathMarkers()
        {
            foreach (var marker in currentPathMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }
            currentPathMarkers.Clear();
        }

        // Generate a new position on the NavMesh
        public static Vector3 RandomNavPoint()
        {
            var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();

            if (triangulation.indices.Length == 0)
            {
                return Vector3.zero;
            }

            float totalArea = 0.0f;
            List<float> areas = new List<float>();
            for (int i = 0; i < triangulation.indices.Length;)
            {
                var i0 = triangulation.indices[i];
                var i1 = triangulation.indices[i + 1];
                var i2 = triangulation.indices[i + 2];
                var v0 = triangulation.vertices[i0];
                var v1 = triangulation.vertices[i1];
                var v2 = triangulation.vertices[i2];
                var cross = Vector3.Cross(v1 - v0, v2 - v0);
                float area = cross.magnitude * 0.5f;
                totalArea += area;
                areas.Add(area);
                i += 3;
            }

            var rand = Random.Range(0, totalArea);
            int triangleIndex = 0;
            for (; triangleIndex < areas.Count - 1; ++triangleIndex)
            {
                rand -= areas[triangleIndex];
                if (rand <= 0.0f)
                {
                    break;
                }
            }

            {
                var i0 = triangulation.indices[triangleIndex * 3];
                var i1 = triangulation.indices[triangleIndex * 3 + 1];
                var i2 = triangulation.indices[triangleIndex * 3 + 2];
                var v0 = triangulation.vertices[i0];
                var v1 = triangulation.vertices[i1];
                var v2 = triangulation.vertices[i2];

                float u = Random.Range(0.0f, 1.0f);
                float v = Random.Range(0.0f, 1.0f);
                if (u + v > 1.0f)
                {
                    if (u > v)
                    {
                        u = 1.0f - u;
                    }
                    else
                    {
                        v = 1.0f - v;
                    }
                }

                return v0 + u * (v1 - v0) + v * (v2 - v0);
            }
        }

        public void DisableAll()
        {
            foreach (var x in currentPathMarkers)
            {
                x.SetActive(false);
            }
        }
        void OnDisable()
        {
            ClearPathMarkers();
        }
    }
}