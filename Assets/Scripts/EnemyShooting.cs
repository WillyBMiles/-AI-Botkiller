using System.Collections.Generic;
using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform lookTarget;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private LayerMask wallLayerMask;
    
    [Header("Neck Rotation Settings")]
    [SerializeField] private Transform neckTransform;
    [SerializeField] private float neckRotationSpeed = 5f;
    [SerializeField] private float maxNeckAngle = 80f;
    
    [Header("Gun Rotation Settings")]
    [SerializeField] private Transform leftGunTransform;
    [SerializeField] private Transform rightGunTransform;
    [SerializeField] private float gunRotationSpeed = 5f;
    [SerializeField] private float maxGunAngle = 45f;
    
    [Header("Gun Barrel Settings")]
    [SerializeField] private Transform leftBarrelTransform;
    [SerializeField] private Transform rightBarrelTransform;
    [SerializeField] private GameObject leftMuzzleFlare;
    [SerializeField] private GameObject rightMuzzleFlare;
    
    [Header("Shooting Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int shotsPerBurst = 3;
    [SerializeField] private float timeBetweenShots = 0.1f;
    [SerializeField] private float timeBetweenBursts = 2f;
    [SerializeField] private float projectileSpread = 2f;
    [SerializeField] private bool alternateBetweenGuns = true;
    
    [Header("Targeting Strategy Settings")]
    [SerializeField] private float averageTrackingDuration = 2f;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float randomTargetRadius = 3f;
    [SerializeField] private int burstsBeforeStrategyChange = 3;
    
    [Header("Components")]
    [SerializeField] private Animator animator;
    
    // Shooting state
    private float nextBurstTime = 0f;
    private bool isFiring = false;
    private int shotsInCurrentBurst = 0;
    private float nextShotTime = 0f;
    private bool useLeftGun = true;
    private bool hasLineOfSight = false;
    
    // Targeting strategy state
    private enum TargetingStrategy { AverageTracking, LeadTarget, DirectTracking, RandomOffset }
    private TargetingStrategy currentStrategy = TargetingStrategy.DirectTracking;
    private Queue<Vector3> playerPositionHistory = new Queue<Vector3>();
    private Vector3 lastPlayerPosition;
    private int burstsWithoutHit = 0;
    private bool hitThisBurst = false;
    
    private void Awake()
    {
        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("EnemyShooting: No Animator found on " + gameObject.name);
            }
        }
        
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        // Hide muzzle flares at start
        if (leftMuzzleFlare != null)
        {
            leftMuzzleFlare.SetActive(false);
        }
        if (rightMuzzleFlare != null)
        {
            rightMuzzleFlare.SetActive(false);
        }
        
        // Initialize last player position
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
        }
    }
    
    private void Update()
    {
        UpdatePlayerTracking();
        UpdateTargetPosition();
        CheckLineOfSight();
        HandleNeckRotation();
        HandleGunRotation();
        HandleShooting();
    }
    
    private void HandleNeckRotation()
    {
        if (neckTransform == null || lookTarget == null) return;
        
        // Calculate direction to target
        Vector3 directionToTarget = lookTarget.position - neckTransform.position;
        
        // Project direction onto the horizontal plane (ignore Y difference)
        // This ensures we only rotate around the Y axis
        directionToTarget.y = 0f;
        
        // Check if direction is valid
        if (directionToTarget.sqrMagnitude < 0.001f) return;
        
        // Calculate the target rotation (only Y axis)
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        
        // Get only the Y rotation from the target
        Vector3 targetEuler = targetRotation.eulerAngles;
        Quaternion yOnlyRotation = Quaternion.Euler(0f, targetEuler.y, 0f);
        
        // Calculate the angle difference from the neck's parent forward direction
        // This allows us to clamp the rotation relative to the body
        Transform parentTransform = neckTransform.parent != null ? neckTransform.parent : transform;
        float angleToTarget = Vector3.SignedAngle(parentTransform.forward, directionToTarget, Vector3.up);
        
        // Clamp the angle to max neck rotation
        float clampedAngle = Mathf.Clamp(angleToTarget, -maxNeckAngle, maxNeckAngle);
        
        // Calculate the final rotation
        Quaternion clampedRotation = parentTransform.rotation * Quaternion.Euler(0f, clampedAngle, 0f);
        
        // Smoothly rotate the neck towards the target
        neckTransform.rotation = Quaternion.Slerp(
            neckTransform.rotation,
            clampedRotation,
            Time.deltaTime * neckRotationSpeed
        );
    }
    
    private void HandleGunRotation()
    {
        if (lookTarget == null) return;
        
        // Rotate left gun
        if (leftGunTransform != null)
        {
            RotateGunOnXAxis(leftGunTransform);
        }
        
        // Rotate right gun
        if (rightGunTransform != null)
        {
            RotateGunOnXAxis(rightGunTransform);
        }
    }
    
    private void RotateGunOnXAxis(Transform gunTransform)
    {
        // Calculate direction to target
        Vector3 directionToTarget = lookTarget.position - gunTransform.position;
        
        // Check if direction is valid
        if (directionToTarget.sqrMagnitude < 0.001f) return;
        
        // Transform direction to local space to work with local axes
        Vector3 localDirection = gunTransform.parent != null 
            ? gunTransform.parent.InverseTransformDirection(directionToTarget) 
            : gunTransform.InverseTransformDirection(directionToTarget);
        
        // Calculate the pitch angle (rotation around X axis)
        // We want to rotate around X to aim up/down
        float targetPitch = -Mathf.Atan2(localDirection.y, new Vector2(localDirection.x, localDirection.z).magnitude) * Mathf.Rad2Deg;
        
        // Clamp the pitch angle
        targetPitch = Mathf.Clamp(targetPitch, -maxGunAngle, maxGunAngle);
        
        // Get current local rotation
        Vector3 currentLocalEuler = gunTransform.localEulerAngles;
        
        // Normalize the current X rotation to -180 to 180 range
        float currentPitch = currentLocalEuler.x;
        if (currentPitch > 180f) currentPitch -= 360f;
        
        // Smoothly interpolate to target pitch
        float newPitch = Mathf.LerpAngle(currentPitch, targetPitch, Time.deltaTime * gunRotationSpeed);
        
        // Apply only X rotation, keep Y and Z unchanged
        gunTransform.localRotation = Quaternion.Euler(newPitch, currentLocalEuler.y, currentLocalEuler.z);
    }
    
    private void UpdatePlayerTracking()
    {
        if (playerTransform == null) return;
        
        // Track player position history for averaging
        playerPositionHistory.Enqueue(playerTransform.position);
        
        // Keep only positions from the last few seconds
        int maxHistorySize = Mathf.CeilToInt(averageTrackingDuration / Time.deltaTime);
        while (playerPositionHistory.Count > maxHistorySize)
        {
            playerPositionHistory.Dequeue();
        }
        
        lastPlayerPosition = playerTransform.position;
    }
    
    private void UpdateTargetPosition()
    {
        if (lookTarget == null || playerTransform == null) return;
        
        Vector3 targetPosition = playerTransform.position;
        
        switch (currentStrategy)
        {
            case TargetingStrategy.AverageTracking:
                targetPosition = CalculateAveragePosition();
                break;
                
            case TargetingStrategy.LeadTarget:
                targetPosition = CalculateLeadPosition();
                break;
                
            case TargetingStrategy.DirectTracking:
                targetPosition = playerTransform.position;
                break;
                
            case TargetingStrategy.RandomOffset:
                targetPosition = CalculateRandomOffset();
                break;
        }
        
        lookTarget.position = targetPosition;
    }
    
    private Vector3 CalculateAveragePosition()
    {
        if (playerPositionHistory.Count == 0) return playerTransform.position;
        
        Vector3 sum = Vector3.zero;
        foreach (Vector3 pos in playerPositionHistory)
        {
            sum += pos;
        }
        return sum / playerPositionHistory.Count;
    }
    
    private Vector3 CalculateLeadPosition()
    {
        if (neckTransform == null) return playerTransform.position;
        
        // Calculate player velocity
        Vector3 playerVelocity = (playerTransform.position - lastPlayerPosition) / Time.deltaTime;
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(neckTransform.position, playerTransform.position);
        
        // Calculate time for projectile to reach player based on projectile speed
        float timeToReach = distanceToPlayer / projectileSpeed;
        
        // Predict where player will be when projectile arrives
        Vector3 predictedPosition = playerTransform.position + playerVelocity * timeToReach;
        
        return predictedPosition;
    }
    
    private Vector3 CalculateRandomOffset()
    {
        // Add random offset around player
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomTargetRadius, randomTargetRadius),
            Random.Range(-randomTargetRadius * 0.5f, randomTargetRadius * 0.5f),
            Random.Range(-randomTargetRadius, randomTargetRadius)
        );
        return playerTransform.position + randomOffset;
    }
    
    private void CheckLineOfSight()
    {
        if (neckTransform == null || lookTarget == null) return;
        
        Vector3 directionToTarget = lookTarget.position - neckTransform.position;
        float distanceToTarget = directionToTarget.magnitude;
        
        // Raycast to check for walls
        RaycastHit hit;
        if (Physics.Raycast(neckTransform.position, directionToTarget.normalized, out hit, distanceToTarget, wallLayerMask))
        {
            // Hit a wall, no line of sight
            hasLineOfSight = false;
        }
        else
        {
            // Clear line of sight
            hasLineOfSight = true;
        }
    }
    
    private void HandleShooting()
    {
        if (projectilePrefab == null || lookTarget == null) return;
        
        // Only shoot if we have line of sight
        if (!hasLineOfSight)
        {
            // Pause burst but don't cancel it - just wait for line of sight to return
            return;
        }
        
        // Check if we should start a new burst
        if (!isFiring && Time.time >= nextBurstTime)
        {
            StartBurst();
        }
        
        // Handle burst firing
        if (isFiring && Time.time >= nextShotTime)
        {
            FireProjectile();
            shotsInCurrentBurst++;
            
            // Check if burst is complete
            if (shotsInCurrentBurst >= shotsPerBurst)
            {
                EndBurst();
            }
            else
            {
                nextShotTime = Time.time + timeBetweenShots;
            }
        }
    }
    
    private void StartBurst()
    {
        isFiring = true;
        shotsInCurrentBurst = 0;
        nextShotTime = Time.time;
        hitThisBurst = false;
    }
    
    private void EndBurst()
    {
        isFiring = false;
        nextBurstTime = Time.time + timeBetweenBursts;
        
        // Track if we hit during this burst
        if (!hitThisBurst)
        {
            burstsWithoutHit++;
        }
        else
        {
            burstsWithoutHit = 0;
        }
        
        // Change strategy if we haven't hit in several bursts
        if (burstsWithoutHit >= burstsBeforeStrategyChange)
        {
            ChangeStrategy();
            burstsWithoutHit = 0;
        }
    }
    
    private void ChangeStrategy()
    {
        TargetingStrategy newStrategy;
        
        // Always favor LeadTarget strategy unless already using it
        if (currentStrategy != TargetingStrategy.LeadTarget)
        {
            newStrategy = TargetingStrategy.LeadTarget;
        }
        else
        {
            // If already using LeadTarget, randomly pick one of the others
            do
            {
                newStrategy = (TargetingStrategy)Random.Range(0, 4);
            }
            while (newStrategy == TargetingStrategy.LeadTarget);
        }
        
        currentStrategy = newStrategy;
        Debug.Log($"Enemy changed targeting strategy to: {currentStrategy}");
    }
    
    private void FireProjectile()
    {
        // Determine which barrel to use
        Transform barrelToUse = null;
        
        if (alternateBetweenGuns)
        {
            // Alternate between left and right guns
            barrelToUse = useLeftGun ? leftBarrelTransform : rightBarrelTransform;
            useLeftGun = !useLeftGun;
        }
        else
        {
            // Fire from both guns simultaneously
            if (leftBarrelTransform != null)
            {
                FireFromBarrel(leftBarrelTransform);
            }
            if (rightBarrelTransform != null)
            {
                FireFromBarrel(rightBarrelTransform);
            }
            return;
        }
        
        // Fire from selected barrel
        if (barrelToUse != null)
        {
            FireFromBarrel(barrelToUse);
        }
    }
    
    private void FireFromBarrel(Transform barrel)
    {
        if (barrel == null) return;
        
        // Trigger animation and muzzle flare based on which barrel is firing
        if (barrel == leftBarrelTransform)
        {
            if (animator != null)
            {
                animator.SetTrigger("Left");
            }
            ShowMuzzleFlare(leftMuzzleFlare);
        }
        else if (barrel == rightBarrelTransform)
        {
            if (animator != null)
            {
                animator.SetTrigger("Right");
            }
            ShowMuzzleFlare(rightMuzzleFlare);
        }
        
        // Spawn projectile at barrel position
        GameObject projectile = Instantiate(projectilePrefab, barrel.position, barrel.rotation);
        
        // Get projectile component
        EnemyProjectile projectileScript = projectile.GetComponent<EnemyProjectile>();
        if (projectileScript != null)
        {
            // Calculate direction with spread
            Vector3 direction = barrel.forward;
            
            // Add random spread
            if (projectileSpread > 0f)
            {
                Quaternion spread = Quaternion.Euler(
                    Random.Range(-projectileSpread, projectileSpread),
                    Random.Range(-projectileSpread, projectileSpread),
                    0f
                );
                direction = spread * direction;
            }
            
            // Set projectile direction
            projectileScript.SetDirection(direction);
            
            // Subscribe to hit event (we'll need to add this to EnemyProjectile)
            projectileScript.onHitPlayer += OnProjectileHitPlayer;
        }
    }
    
    private void OnProjectileHitPlayer()
    {
        hitThisBurst = true;
    }
    
    private void ShowMuzzleFlare(GameObject muzzleFlare)
    {
        if (muzzleFlare == null) return;
        
        // Show muzzle flare with random Z rotation
        muzzleFlare.SetActive(true);
        muzzleFlare.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        
        // Hide muzzle flare after a short delay
        Invoke(nameof(HideMuzzleFlares), 0.02f);
    }
    
    private void HideMuzzleFlares()
    {
        if (leftMuzzleFlare != null)
        {
            leftMuzzleFlare.SetActive(false);
        }
        if (rightMuzzleFlare != null)
        {
            rightMuzzleFlare.SetActive(false);
        }
    }
    
    // Visualize the look direction in the editor
    private void OnDrawGizmosSelected()
    {
        if (lookTarget == null) return;
        
        // Draw line from neck to target
        if (neckTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(neckTransform.position, lookTarget.position);
            
            // Draw neck forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(neckTransform.position, neckTransform.forward * 3f);
        }
        
        // Draw gun aim lines
        if (leftGunTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftGunTransform.position, lookTarget.position);
            Gizmos.DrawRay(leftGunTransform.position, leftGunTransform.forward * 2f);
        }
        
        if (rightGunTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(rightGunTransform.position, lookTarget.position);
            Gizmos.DrawRay(rightGunTransform.position, rightGunTransform.forward * 2f);
        }
    }
}
