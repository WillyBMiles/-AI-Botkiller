using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private float spawnRadius = 1f;
    [SerializeField] private float surfaceCheckDistance = 5f;
    [SerializeField] private LayerMask surfaceLayerMask = -1;
    [SerializeField] private float spaceCheckRadius = 1f;
    [SerializeField] private bool checkPlayerVisibility = true;
    
    private int enemiesSpawnedThisWave = 0;
    private Transform playerTransform;
    
    /// <summary>
    /// Checks if there's room to spawn more enemies at this spawn point
    /// </summary>
    public bool HasSpaceAvailable(GameObject prefab)
    {
        // Get character controller size from prefab
        CharacterController controller = prefab.GetComponent<CharacterController>();
        if (controller == null) return true; // If no controller, assume it fits
        
        // Account for prefab scale
        Vector3 scale = prefab.transform.lossyScale;
        float maxScale = Mathf.Max(scale.x, scale.z); // Use max of X/Z for radius
        float scaledRadius = controller.radius * maxScale;
        
        float checkRadius = Mathf.Max(scaledRadius, spaceCheckRadius);
        
        // Check for overlapping enemies in the spawn area
        Collider[] overlaps = Physics.OverlapSphere(transform.position, spawnRadius + checkRadius);
        
        // Count how many enemies are already here
        int enemyCount = 0;
        foreach (Collider col in overlaps)
        {
            if (col.GetComponent<Enemy>() != null)
            {
                enemyCount++;
            }
        }
        
        // Allow some enemies but not too many (based on area)
        float spawnArea = Mathf.PI * spawnRadius * spawnRadius;
        float enemyArea = Mathf.PI * scaledRadius * scaledRadius;
        int maxEnemies = Mathf.Max(1, Mathf.FloorToInt(spawnArea / enemyArea * 0.5f)); // 50% density
        
        return enemyCount < maxEnemies;
    }
    
    /// <summary>
    /// Resets the spawn counter for a new wave
    /// </summary>
    public void ResetWaveCounter()
    {
        enemiesSpawnedThisWave = 0;
    }
    
    /// <summary>
    /// Spawns enemies at this spawn point with a total point value approximately equal to the specified amount.
    /// </summary>
    /// <param name="pointValue">The target total point value of enemies to spawn</param>
    /// <returns>True if enemies were spawned, false if no space available</returns>
    public bool SpawnEnemies(int pointValue)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogWarning($"EnemySpawnPoint on {gameObject.name}: No enemy prefabs assigned!");
            return false;
        }
        
        // Filter out null prefabs and validate they have Enemy component
        List<GameObject> validPrefabs = new List<GameObject>();
        List<int> prefabPointValues = new List<int>();
        
        foreach (GameObject prefab in enemyPrefabs)
        {
            if (prefab != null)
            {
                Enemy enemyComponent = prefab.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    validPrefabs.Add(prefab);
                    prefabPointValues.Add(enemyComponent.GetPointValue());
                }
                else
                {
                    Debug.LogWarning($"EnemySpawnPoint: Prefab {prefab.name} does not have an Enemy component!");
                }
            }
        }
        
        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning($"EnemySpawnPoint on {gameObject.name}: No valid enemy prefabs found!");
            return false;
        }
        
        // Spawn enemies until we reach or exceed the target point value
        int currentPoints = 0;
        int enemiesSpawned = 0;
        int failedAttempts = 0;
        int maxFailedAttempts = 50; // Prevent infinite loops
        
        while (currentPoints < pointValue && failedAttempts < maxFailedAttempts)
        {
            // Check if there's space before spawning
            if (!HasSpaceAvailable(validPrefabs[0]))
            {
                Debug.Log($"EnemySpawnPoint on {gameObject.name}: No space available for more enemies.");
                return enemiesSpawned > 0;
            }
            
            // Select a random enemy prefab
            int randomIndex = Random.Range(0, validPrefabs.Count);
            GameObject selectedPrefab = validPrefabs[randomIndex];
            int selectedPointValue = prefabPointValues[randomIndex];
            
            // Check if adding this enemy would exceed the point value too much
            // If we're close to the target, prefer enemies that fit better
            if (currentPoints > 0 && currentPoints + selectedPointValue > pointValue * 1.5f)
            {
                // Try to find a better fitting enemy
                GameObject bestFit = null;
                int bestFitValue = int.MaxValue;
                int bestFitIndex = -1;
                
                for (int i = 0; i < validPrefabs.Count; i++)
                {
                    int prefabValue = prefabPointValues[i];
                    int remaining = pointValue - currentPoints;
                    
                    // Find the enemy that gets us closest to the target without going too far over
                    if (prefabValue <= remaining || Mathf.Abs(prefabValue - remaining) < bestFitValue)
                    {
                        bestFit = validPrefabs[i];
                        bestFitValue = Mathf.Abs(prefabValue - remaining);
                        bestFitIndex = i;
                    }
                }
                
                if (bestFit != null)
                {
                    selectedPrefab = bestFit;
                    selectedPointValue = prefabPointValues[bestFitIndex];
                }
            }
            
            // Calculate spawn position with random offset within radius (in local XZ plane)
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 localOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 worldOffset = transform.TransformDirection(localOffset);
            Vector3 spawnPosition = transform.position + worldOffset;
            
            // Raycast downward in local space to find surface
            Vector3 finalSpawnPosition = spawnPosition;
            Vector3 surfaceNormal = transform.up; // Default to spawn point's up direction
            Quaternion spawnRotation = transform.rotation;
            
            // Raycast in local -Y direction (downward relative to spawn point)
            Vector3 rayDirection = -transform.up;
            RaycastHit hit;
            bool foundSurface = Physics.Raycast(spawnPosition, rayDirection, out hit, surfaceCheckDistance, surfaceLayerMask);
            
            if (!foundSurface)
            {
                // No surface found, skip this spawn attempt
                failedAttempts++;
                Debug.LogWarning($"EnemySpawnPoint: No surface found at spawn position, skipping enemy spawn. (Attempt {failedAttempts}/{maxFailedAttempts})");
                continue;
            }
            
            // Use the hit surface
            finalSpawnPosition = hit.point + hit.normal * 0.1f; // Offset slightly from surface
            surfaceNormal = hit.normal;
            
            // Check if player can see this spawn position
            if (checkPlayerVisibility && IsVisibleToPlayer(finalSpawnPosition))
            {
                // Player can see this position, skip this spawn attempt
                failedAttempts++;
                continue;
            }
            
            // Check for obstacles at spawn position
            CharacterController controller = selectedPrefab.GetComponent<CharacterController>();
            if (controller != null)
            {
                // Check if there's enough space for the enemy
                // Account for prefab scale
                Vector3 scale = selectedPrefab.transform.lossyScale;
                float maxRadiusScale = Mathf.Max(scale.x, scale.z);
                float checkRadius = controller.radius * maxRadiusScale;
                float checkHeight = controller.height * scale.y;
                
                // Position the capsule check above the surface, not overlapping it
                Vector3 capsuleBottom = finalSpawnPosition + surfaceNormal * checkRadius;
                Vector3 capsuleTop = finalSpawnPosition + surfaceNormal * (checkHeight - checkRadius);
                
                // Check for overlaps at the spawn position
                Collider[] overlaps = Physics.OverlapCapsule(
                    capsuleBottom,
                    capsuleTop,
                    checkRadius
                );
                
                // Filter out triggers, enemies, and the surface we're spawning on
                bool hasObstacle = false;
                foreach (Collider col in overlaps)
                {
                    // Ignore triggers, enemies, and the surface collider we hit
                    if (!col.isTrigger && (col.attachedRigidbody == null || col.attachedRigidbody.GetComponent<Enemy>() == null) && col != hit.collider)
                    {
                        hasObstacle = true;
                        break;
                    }
                }
                
                if (hasObstacle)
                {
                    // Obstacle detected, skip this spawn attempt
                    failedAttempts++;
                    Debug.LogWarning($"EnemySpawnPoint: Obstacle detected at spawn position, skipping enemy spawn. (Attempt {failedAttempts}/{maxFailedAttempts})");
                    continue;
                }
            }
            
            // Spawn the enemy
            GameObject spawnedEnemy = Instantiate(selectedPrefab, finalSpawnPosition, spawnRotation);
            
            // Set surface normal, spawn position, and surface collider on movement component if it exists
            EnemyMovement movement = spawnedEnemy.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.SetSurfaceNormal(surfaceNormal);
                movement.SetSpawnPosition(finalSpawnPosition);
                movement.SetSpawnSurface(hit.collider); // Lock enemy to this specific surface
            }
            
            // Add to current points
            currentPoints += selectedPointValue;
            enemiesSpawned++;
            enemiesSpawnedThisWave++;
            
            // Reset failed attempts counter on successful spawn
            failedAttempts = 0;
            
            // Safety check to prevent infinite loops
            if (enemiesSpawned > 100)
            {
                Debug.LogWarning($"EnemySpawnPoint: Spawned 100 enemies, stopping to prevent infinite loop. Target: {pointValue}, Current: {currentPoints}");
                break;
            }
        }
        
        if (failedAttempts >= maxFailedAttempts)
        {
            Debug.LogWarning($"EnemySpawnPoint on {gameObject.name}: Stopped spawning after {maxFailedAttempts} failed attempts. Spawned {enemiesSpawned} enemies with {currentPoints} points (target: {pointValue})");
        }
        else
        {
            Debug.Log($"EnemySpawnPoint spawned {enemiesSpawned} enemies with total point value {currentPoints} (target: {pointValue})");
        }
        
        return enemiesSpawned > 0;
    }
    
    private bool IsVisibleToPlayer(Vector3 position)
    {
        // Find player if not cached
        if (playerTransform == null)
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                return false; // No player found, assume not visible
            }
        }
        
        // Check if position is in front of player
        Vector3 directionToSpawn = position - playerTransform.position;
        float distanceToPlayer = directionToSpawn.magnitude;
        
        // Get player's camera direction
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return false;
        
        Vector3 cameraForward = playerCamera.transform.forward;
        float dotProduct = Vector3.Dot(directionToSpawn.normalized, cameraForward);
        
        // If behind player or far to the side, not visible
        if (dotProduct < 0.3f) return false; // ~70 degree FOV check
        
        // Check if there's line of sight
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, directionToSpawn.normalized, out hit, distanceToPlayer))
        {
            // If raycast hits something before reaching spawn position, it's occluded
            if (hit.distance < distanceToPlayer - 0.5f)
            {
                return false; // Occluded
            }
        }
        
        // Player can see this position
        return true;
    }
    
    // Visualize spawn radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
