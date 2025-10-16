using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private float spawnRadius = 1f;
    
    /// <summary>
    /// Spawns enemies at this spawn point with a total point value approximately equal to the specified amount.
    /// </summary>
    /// <param name="pointValue">The target total point value of enemies to spawn</param>
    public void SpawnEnemies(int pointValue)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogWarning($"EnemySpawnPoint on {gameObject.name}: No enemy prefabs assigned!");
            return;
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
            return;
        }
        
        // Spawn enemies until we reach or exceed the target point value
        int currentPoints = 0;
        int enemiesSpawned = 0;
        
        while (currentPoints < pointValue)
        {
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
            
            // Calculate spawn position with random offset within radius
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            
            // Spawn the enemy
            GameObject spawnedEnemy = Instantiate(selectedPrefab, spawnPosition, transform.rotation);
            
            // Add to current points
            currentPoints += selectedPointValue;
            enemiesSpawned++;
            
            // Safety check to prevent infinite loops
            if (enemiesSpawned > 100)
            {
                Debug.LogWarning($"EnemySpawnPoint: Spawned 100 enemies, stopping to prevent infinite loop. Target: {pointValue}, Current: {currentPoints}");
                break;
            }
        }
        
        Debug.Log($"EnemySpawnPoint spawned {enemiesSpawned} enemies with total point value {currentPoints} (target: {pointValue})");
    }
    
    // Visualize spawn radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
