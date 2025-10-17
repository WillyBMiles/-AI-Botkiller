using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();
    
    [Header("Wave Settings")]
    [SerializeField] private int initialWavePoints = 5;
    [SerializeField] private float pointIncreasePerWave = 2f;
    [SerializeField] private float delayBetweenWaves = 5f;
    [SerializeField] private float minDistanceFromPlayer = 20f;
    [SerializeField] private float campingTimeout = 30f; // Force new wave if no shots for this long
    
    [Header("Spawn Point Selection")]
    [SerializeField] private int minSpawnPointsPerWave = 1;
    [SerializeField] private int maxSpawnPointsPerWave = 3;
    [SerializeField] private float spawnPointClusterRadius = 30f;
    
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    
    private int currentWave = 0;
    private float waveTimer = 0f;
    private bool isWaitingForNextWave = false;
    private List<Enemy> activeEnemies = new List<Enemy>();
    private float lastCampingCheckTime = 0f;
    
    private void Awake()
    {
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("EnemySpawnManager: No Player found in scene!");
            }
        }
        
        // Auto-find spawn points if not assigned
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            spawnPoints = new List<EnemySpawnPoint>(FindObjectsOfType<EnemySpawnPoint>());
            if (spawnPoints.Count == 0)
            {
                Debug.LogError("EnemySpawnManager: No spawn points found in scene!");
            }
            else
            {
                // Debug.Log($"EnemySpawnManager: Auto-found {spawnPoints.Count} spawn points");
            }
        }
    }
    
    private void Start()
    {
        // Start the spawning cycle
        isWaitingForNextWave = true;
        waveTimer = delayBetweenWaves;
    }
    
    private void Update()
    {
        // Clean up destroyed enemies from the active list
        activeEnemies.RemoveAll(enemy => enemy == null);
        
        // Check for camping every 30 seconds
        if (Time.time - lastCampingCheckTime >= campingTimeout)
        {
            lastCampingCheckTime = Time.time;
            
            if (activeEnemies.Count > 0 && CheckForCamping())
            {
                // Debug.Log("No enemy activity detected for 30 seconds - forcing new wave to prevent camping.");
                // Force new wave
                isWaitingForNextWave = true;
                waveTimer = 0f;
            }
        }
        
        // Check if we should start a new wave
        if (isWaitingForNextWave)
        {
            waveTimer -= Time.deltaTime;
            
            // Only spawn if no enemies remain and timer has elapsed
            if (waveTimer <= 0f)
            {
                SpawnNewWave();
                isWaitingForNextWave = false;
                waveTimer = delayBetweenWaves;
            }
        }
        else
        {
            // Check if all enemies are dead
            if (activeEnemies.Count == 0)
            {
                // Start waiting for next wave
                isWaitingForNextWave = true;
                waveTimer = delayBetweenWaves;
            }
        }
    }
    
    private void SpawnNewWave()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("EnemySpawnManager: Cannot spawn wave, no spawn points available!");
            return;
        }
        
        currentWave++;
        
        // Calculate total points for this wave
        int totalPoints = Mathf.RoundToInt(initialWavePoints + (currentWave - 1) * pointIncreasePerWave);
        
        // Debug.Log($"Starting Wave {currentWave} with {totalPoints} points");
        
        // Select spawn points for this wave
        List<EnemySpawnPoint> selectedSpawnPoints = SelectSpawnPoints();
        
        if (selectedSpawnPoints.Count == 0)
        {
            Debug.LogWarning("EnemySpawnManager: No valid spawn points selected!");
            return;
        }
        
        // Distribute points among selected spawn points
        List<int> pointDistribution = DistributePoints(totalPoints, selectedSpawnPoints.Count);
        
        // Reset wave counters for all spawn points
        foreach (EnemySpawnPoint sp in selectedSpawnPoints)
        {
            sp.ResetWaveCounter();
        }
        
        // Spawn enemies at each selected spawn point
        List<EnemySpawnPoint> availableSpawnPoints = new List<EnemySpawnPoint>(selectedSpawnPoints);
        
        for (int i = 0; i < selectedSpawnPoints.Count; i++)
        {
            int pointsForThisSpawn = pointDistribution[i];
            if (pointsForThisSpawn > 0)
            {
                // Track enemies before spawning
                int enemiesBefore = FindObjectsOfType<Enemy>().Length;
                
                // Try to spawn at this point
                bool spawned = selectedSpawnPoints[i].SpawnEnemies(pointsForThisSpawn);
                
                // If spawn failed due to space, try other available spawn points
                if (!spawned && availableSpawnPoints.Count > 1)
                {
                    availableSpawnPoints.Remove(selectedSpawnPoints[i]);
                    
                    // Try redistributing to other spawn points
                    foreach (EnemySpawnPoint alternatePoint in availableSpawnPoints)
                    {
                        if (alternatePoint.SpawnEnemies(pointsForThisSpawn))
                        {
                            spawned = true;
                            break;
                        }
                    }
                    
                    if (!spawned)
                    {
                        // Debug.Log($"Could not spawn {pointsForThisSpawn} points worth of enemies - all spawn points full");
                    }
                }
                
                // Track newly spawned enemies
                Enemy[] allEnemies = FindObjectsOfType<Enemy>();
                for (int j = enemiesBefore; j < allEnemies.Length; j++)
                {
                    if (!activeEnemies.Contains(allEnemies[j]))
                    {
                        activeEnemies.Add(allEnemies[j]);
                    }
                }
            }
        }
        
        // Debug.Log($"Wave {currentWave} spawned at {selectedSpawnPoints.Count} spawn points. Total active enemies: {activeEnemies.Count}");
    }
    
    private bool CheckForCamping()
    {
        // Sample up to 5 random enemies
        int samplesToCheck = Mathf.Min(5, activeEnemies.Count);
        
        // Create a list of random indices
        List<int> randomIndices = new List<int>();
        for (int i = 0; i < samplesToCheck; i++)
        {
            randomIndices.Add(Random.Range(0, activeEnemies.Count));
        }
        
        // Check the sampled enemies
        for (int i = 0; i < samplesToCheck; i++)
        {
            Enemy enemy = activeEnemies[randomIndices[i]];
            if (enemy == null) continue;
            
            EnemyShooting shooting = enemy.GetComponent<EnemyShooting>();
            if (shooting != null)
            {
                float timeSinceLastShot = shooting.GetTimeSinceLastShot();
                if (timeSinceLastShot < campingTimeout)
                {
                    // At least one sampled enemy has shot recently
                    return false;
                }
            }
        }
        
        // None of the sampled enemies have shot in the timeout period
        return true;
    }
    
    private List<EnemySpawnPoint> SelectSpawnPoints()
    {
        // Filter spawn points that are far enough from the player
        List<EnemySpawnPoint> validSpawnPoints = new List<EnemySpawnPoint>();
        
        foreach (EnemySpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint != null && playerTransform != null)
            {
                float distance = Vector3.Distance(spawnPoint.transform.position, playerTransform.position);
                if (distance >= minDistanceFromPlayer)
                {
                    validSpawnPoints.Add(spawnPoint);
                }
            }
        }
        
        if (validSpawnPoints.Count == 0)
        {
            Debug.LogWarning("EnemySpawnManager: No spawn points far enough from player, using all spawn points");
            validSpawnPoints = new List<EnemySpawnPoint>(spawnPoints);
        }
        
        // Determine how many spawn points to use
        int numSpawnPoints = Random.Range(minSpawnPointsPerWave, Mathf.Min(maxSpawnPointsPerWave, validSpawnPoints.Count) + 1);
        numSpawnPoints = Mathf.Max(1, numSpawnPoints);
        
        if (validSpawnPoints.Count <= numSpawnPoints)
        {
            // Use all valid spawn points
            return validSpawnPoints;
        }
        
        // Select spawn points that are clustered together
        List<EnemySpawnPoint> selectedSpawnPoints = new List<EnemySpawnPoint>();
        
        // Pick a random starting spawn point
        EnemySpawnPoint firstSpawnPoint = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
        selectedSpawnPoints.Add(firstSpawnPoint);
        validSpawnPoints.Remove(firstSpawnPoint);
        
        // Select remaining spawn points, preferring ones close to already selected points
        while (selectedSpawnPoints.Count < numSpawnPoints && validSpawnPoints.Count > 0)
        {
            // Calculate weighted probabilities based on distance to selected spawn points
            List<float> weights = new List<float>();
            
            foreach (EnemySpawnPoint candidate in validSpawnPoints)
            {
                // Find minimum distance to any selected spawn point
                float minDistance = float.MaxValue;
                foreach (EnemySpawnPoint selected in selectedSpawnPoints)
                {
                    float distance = Vector3.Distance(candidate.transform.position, selected.transform.position);
                    minDistance = Mathf.Min(minDistance, distance);
                }
                
                // Weight is higher for closer spawn points, but add randomness
                // Use inverse distance with a cap to prevent division issues
                float weight = 1f / Mathf.Max(minDistance, 1f);
                
                // Add randomness factor (50% weight from distance, 50% random)
                weight = weight * 0.5f + Random.Range(0f, 1f) * 0.5f;
                
                // Bonus weight if within cluster radius
                if (minDistance <= spawnPointClusterRadius)
                {
                    weight *= 2f;
                }
                
                weights.Add(weight);
            }
            
            // Select spawn point based on weighted random selection
            EnemySpawnPoint selectedPoint = WeightedRandomSelection(validSpawnPoints, weights);
            selectedSpawnPoints.Add(selectedPoint);
            validSpawnPoints.Remove(selectedPoint);
        }
        
        return selectedSpawnPoints;
    }
    
    private EnemySpawnPoint WeightedRandomSelection(List<EnemySpawnPoint> options, List<float> weights)
    {
        if (options.Count == 0) return null;
        if (options.Count == 1) return options[0];
        
        // Calculate total weight
        float totalWeight = 0f;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }
        
        // Pick a random value
        float randomValue = Random.Range(0f, totalWeight);
        
        // Find the selected option
        float currentWeight = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return options[i];
            }
        }
        
        // Fallback (should never reach here)
        return options[options.Count - 1];
    }
    
    private List<int> DistributePoints(int totalPoints, int numSpawnPoints)
    {
        List<int> distribution = new List<int>();
        
        if (numSpawnPoints <= 0)
        {
            return distribution;
        }
        
        // Start with equal distribution
        int basePoints = totalPoints / numSpawnPoints;
        int remainder = totalPoints % numSpawnPoints;
        
        for (int i = 0; i < numSpawnPoints; i++)
        {
            int points = basePoints;
            
            // Distribute remainder randomly
            if (remainder > 0 && Random.value > 0.5f)
            {
                points++;
                remainder--;
            }
            
            distribution.Add(points);
        }
        
        // Distribute any remaining points
        while (remainder > 0)
        {
            int randomIndex = Random.Range(0, distribution.Count);
            distribution[randomIndex]++;
            remainder--;
        }
        
        // Add some randomness by redistributing points
        int redistributions = Random.Range(0, totalPoints / 4);
        for (int i = 0; i < redistributions; i++)
        {
            int fromIndex = Random.Range(0, distribution.Count);
            int toIndex = Random.Range(0, distribution.Count);
            
            if (fromIndex != toIndex && distribution[fromIndex] > 1)
            {
                distribution[fromIndex]--;
                distribution[toIndex]++;
            }
        }
        
        return distribution;
    }
    
    // Public methods for external control
    public int GetCurrentWave() => currentWave;
    public int GetActiveEnemyCount() => activeEnemies.Count;
    public bool IsWaitingForNextWave() => isWaitingForNextWave;
    public float GetTimeUntilNextWave() => Mathf.Max(0f, waveTimer);
}
