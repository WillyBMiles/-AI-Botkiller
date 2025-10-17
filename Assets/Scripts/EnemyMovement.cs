using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyMovement : MonoBehaviour
{
    public enum MovementStrategy
    {
        Wander,
        Stationary
    }
    
    [Header("Movement Settings")]
    [SerializeField] private MovementStrategy movementStrategy = MovementStrategy.Wander;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderChangeInterval = 3f;
    [SerializeField] private float arrivalDistance = 0.5f;
    
    [Header("Surface Detection")]
    [SerializeField] private float surfaceCheckDistance = 1f;
    [SerializeField] private float edgeCheckDistance = 0.5f;
    [SerializeField] private LayerMask surfaceLayerMask = -1;
    
    [Header("Components")]
    [SerializeField] private Animator animator;
    
    private CharacterController characterController;
    private Vector3 surfaceNormal = Vector3.up;
    private Vector3 spawnPosition;
    private Vector3 currentWanderTarget;
    private float nextWanderTime = 0f;
    private float currentSpeed = 0f;
    private Collider spawnSurfaceCollider; // The specific surface we spawned on
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        spawnPosition = transform.position;
        
        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("EnemyMovement: No Animator found on " + gameObject.name);
            }
        }
    }
    
    private void Start()
    {
        // Detect initial surface
        DetectSurface();
        
        // Set initial wander target
        if (movementStrategy == MovementStrategy.Wander)
        {
            PickNewWanderTarget();
        }
    }
    
    private void Update()
    {
        // Always check and align to surface
        DetectSurface();
        AlignToSurface();
        
        // Handle movement based on strategy
        switch (movementStrategy)
        {
            case MovementStrategy.Wander:
                HandleWanderMovement();
                break;
            case MovementStrategy.Stationary:
                currentSpeed = 0f;
                break;
        }
        
        // Update animation
        UpdateAnimation();
    }
    
    private void DetectSurface()
    {
        // Raycast downward in local space to find surface
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, surfaceCheckDistance, surfaceLayerMask))
        {
            // Only update surface if it's the same collider we spawned on, or if we don't have a spawn surface yet
            if (spawnSurfaceCollider == null || hit.collider == spawnSurfaceCollider)
            {
                surfaceNormal = hit.normal;
                
                // Snap to surface to prevent floating
                float distanceToSurface = hit.distance;
                if (distanceToSurface > 0.1f)
                {
                    // Too far from surface, move down
                    Vector3 correction = -transform.up * (distanceToSurface - 0.05f);
                    characterController.Move(correction);
                }
            }
        }
    }
    
    private void AlignToSurface()
    {
        // Calculate target rotation based on surface normal
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;
        
        // Smoothly rotate to align with surface
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
    
    private void HandleWanderMovement()
    {
        // Check if we need a new wander target
        if (Time.time >= nextWanderTime || Vector3.Distance(transform.position, currentWanderTarget) < arrivalDistance)
        {
            PickNewWanderTarget();
        }
        
        // Move towards wander target
        MoveTowardsTarget(currentWanderTarget);
    }
    
    private void PickNewWanderTarget()
    {
        // Pick a random point on the surface within wander radius
        Vector3 randomDirection = Random.insideUnitCircle.normalized;
        
        // Convert 2D circle to 3D plane aligned with surface
        Vector3 surfaceRight = Vector3.Cross(surfaceNormal, transform.forward);
        if (surfaceRight.sqrMagnitude < 0.001f)
        {
            surfaceRight = Vector3.Cross(surfaceNormal, Vector3.up);
        }
        surfaceRight.Normalize();
        
        Vector3 surfaceForward = Vector3.Cross(surfaceRight, surfaceNormal).normalized;
        
        // Calculate target position on surface
        Vector3 offset = (surfaceRight * randomDirection.x + surfaceForward * randomDirection.y) * wanderRadius;
        Vector3 targetPosition = spawnPosition + offset;
        
        // Raycast to find actual surface point
        RaycastHit hit;
        Vector3 rayOrigin = targetPosition + surfaceNormal * surfaceCheckDistance;
        if (Physics.Raycast(rayOrigin, -surfaceNormal, out hit, surfaceCheckDistance * 2f, surfaceLayerMask))
        {
            currentWanderTarget = hit.point;
        }
        else
        {
            // Fallback to calculated position
            currentWanderTarget = targetPosition;
        }
        
        nextWanderTime = Time.time + wanderChangeInterval;
    }
    
    private void MoveTowardsTarget(Vector3 target)
    {
        // Calculate direction to target in surface plane
        Vector3 directionToTarget = target - transform.position;
        
        // Project direction onto surface plane
        directionToTarget = Vector3.ProjectOnPlane(directionToTarget, surfaceNormal).normalized;
        
        if (directionToTarget.sqrMagnitude < 0.001f)
        {
            currentSpeed = 0f;
            return;
        }
        
        // Check for edge before moving
        if (!CheckForEdge(directionToTarget))
        {
            // Edge detected, pick new target
            PickNewWanderTarget();
            currentSpeed = 0f;
            return;
        }
        
        // Rotate towards target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, surfaceNormal);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        
        // Move forward
        Vector3 movement = transform.forward * moveSpeed * Time.deltaTime;
        characterController.Move(movement);
        
        // Track current speed for animation
        currentSpeed = characterController.velocity.magnitude;
    }
    
    private bool CheckForEdge(Vector3 moveDirection)
    {
        // Check if there's surface ahead in the movement direction
        Vector3 checkPosition = transform.position + moveDirection * edgeCheckDistance;
        
        RaycastHit hit;
        if (Physics.Raycast(checkPosition, -transform.up, out hit, surfaceCheckDistance * 2f, surfaceLayerMask))
        {
            // Check if it's the same surface we spawned on
            if (spawnSurfaceCollider != null && hit.collider != spawnSurfaceCollider)
            {
                // Different surface, treat as edge
                return false;
            }
            
            // Same surface found ahead, safe to move
            return true;
        }
        
        // No surface found, at edge
        return false;
    }
    
    /// <summary>
    /// Sets the surface normal for this enemy (called by spawn system)
    /// </summary>
    public void SetSurfaceNormal(Vector3 normal)
    {
        surfaceNormal = normal;
        
        // Immediately align to surface
        transform.rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal) * transform.rotation;
    }
    
    /// <summary>
    /// Sets the surface collider this enemy is locked to (called by spawn system)
    /// </summary>
    public void SetSpawnSurface(Collider surfaceCollider)
    {
        spawnSurfaceCollider = surfaceCollider;
    }
    
    /// <summary>
    /// Sets the spawn position (used as center for wandering)
    /// </summary>
    public void SetSpawnPosition(Vector3 position)
    {
        spawnPosition = position;
    }
    
    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        // Calculate normalized walk value (0 to 1)
        float normalizedSpeed = Mathf.Clamp01(currentSpeed / moveSpeed);
        
        // Set animator parameter
        animator.SetFloat("Walk", normalizedSpeed);
    }
    
    // Visualize wander area in editor
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Draw wander radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPosition, wanderRadius);
        
        // Draw current target
        if (movementStrategy == MovementStrategy.Wander)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentWanderTarget, 0.3f);
            Gizmos.DrawLine(transform.position, currentWanderTarget);
        }
        
        // Draw surface normal
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, surfaceNormal * 2f);
    }
}
