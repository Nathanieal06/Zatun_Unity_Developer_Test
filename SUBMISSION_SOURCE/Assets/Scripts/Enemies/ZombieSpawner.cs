using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class ZombieSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The zombie prefabs to spawn from.")]
    public GameObject[] zombiePrefabs;
    
    [Tooltip("How many zombies to spawn immediately on start.")]
    public int initialZombies = 5;
    
    [Tooltip("The maximum number of zombies that can be active at once.")]
    public int maxZombies = 20;
    
    [Tooltip("Time in seconds between spawn attempts.")]
    public float spawnInterval = 5f;
    
    [Header("Placement Validation")]
    [Tooltip("Layers that block zombie spawning (e.g. walls, props).")]
    public LayerMask obstacleMask;
    
    [Tooltip("Radius around the spawn point to check for obstacles.")]
    public float obstacleCheckRadius = 0.5f;

    [Header("Exceptions")]
    [Tooltip("Specific objects that should NOT block spawning, even if they are on an obstacle layer.")]
    public List<GameObject> ignoreList = new List<GameObject>();

    private BoxCollider spawnArea;
    private List<GameObject> activeZombies = new List<GameObject>();

    private void Awake()
    {
        spawnArea = GetComponent<BoxCollider>();
        // Ensure it's a trigger so it doesn't physically block things
        spawnArea.isTrigger = true; 
    }

    private void Start()
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
        {
            Debug.LogError("[ZombieSpawner] No Zombie Prefabs are assigned!");
            return;
        }

        // Scatter initial zombies
        for (int i = 0; i < initialZombies; i++)
        {
            TrySpawnZombie();
        }

        StartCoroutine(SpawnRoutine());
    }

    private void Update()
    {
        // Clean up the list to remove destroyed zombies
        // Unity's overloaded == handles destroyed GameObjects returning null
        activeZombies.RemoveAll(z => z == null);
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (activeZombies.Count < maxZombies)
            {
                TrySpawnZombie();
            }
        }
    }

    private void TrySpawnZombie()
    {
        // Check if there are any valid prefabs left (handles the case where user assigned scene objects that got destroyed)
        bool hasValidPrefab = false;
        foreach (var prefab in zombiePrefabs)
        {
            if (prefab != null)
            {
                hasValidPrefab = true;
                break;
            }
        }

        if (!hasValidPrefab)
        {
            Debug.LogError("[ZombieSpawner] All assigned zombie prefabs have been destroyed! Make sure you assign Prefabs from the Project window, NOT objects from the Scene hierarchy.");
            return;
        }

        Vector3 spawnPoint;
        if (GetValidSpawnPoint(out spawnPoint))
        {
            GameObject prefabToSpawn = null;
            // Loop until we randomly pick a prefab that hasn't been destroyed
            while (prefabToSpawn == null)
            {
                prefabToSpawn = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
            }

            GameObject newZombie = Instantiate(prefabToSpawn, spawnPoint, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            activeZombies.Add(newZombie);
            Debug.Log("[ZombieSpawner] Successfully spawned zombie at " + spawnPoint);
        }
    }

    private bool GetValidSpawnPoint(out Vector3 validPoint)
    {
        validPoint = Vector3.zero;
        
        // Increase attempts to handle crowded or complex areas
        int maxAttempts = 30;
        for (int attempts = 0; attempts < maxAttempts; attempts++)
        {
            Vector3 randomPos = GetRandomPointInBounds(spawnArea.bounds);

            // Raycast down from above the box to find the floor
            RaycastHit hit;
            // Increased raycast distance and offset to be more robust
            if (Physics.Raycast(randomPos + Vector3.up * 20f, Vector3.down, out hit, 40f))
            {
                Vector3 groundPos = hit.point;

                // Check if it's on the NavMesh
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(groundPos, out navHit, 2f, NavMesh.AllAreas))
                {
                    Vector3 finalPos = navHit.position;

                    // Check for obstacles overlapping the spot. 
                    // We lift the sphere center slightly more to avoid hitting the ground if the ground is in the mask.
                    float lift = obstacleCheckRadius + 0.2f; 
                    Vector3 sphereCenter = finalPos + Vector3.up * lift;
                    
                    // Use OverlapSphere instead of CheckSphere so we can identify WHAT is blocking
                    Collider[] colliders = Physics.OverlapSphere(sphereCenter, obstacleCheckRadius, obstacleMask);
                    
                    // Filter out this spawner and any objects in the ignore list
                    List<Collider> validBlockers = new List<Collider>();
                    foreach (var col in colliders)
                    {
                        if (col.gameObject != gameObject && !ignoreList.Contains(col.gameObject))
                        {
                            validBlockers.Add(col);
                        }
                    }

                    if (validBlockers.Count == 0)
                    {
                        validPoint = finalPos;
                        return true;
                    }
                    // Silent failure for individual attempts to avoid console spam
                }
            }
        }

        // Only log if we failed every single attempt
        Debug.LogError($"[ZombieSpawner] Failed to find valid spawn point after {maxAttempts} attempts! Ensure the spawn area is clear and 'Obstacle Mask' is configured correctly.");
        return false;
    }

    private Vector3 GetRandomPointInBounds(Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.center.y, // Base Y off the center, we raycast anyway
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnArea == null) spawnArea = GetComponent<BoxCollider>();
        if (spawnArea != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(spawnArea.center, spawnArea.size);
        }
    }
}
