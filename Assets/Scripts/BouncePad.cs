using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BouncePad : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 20f;
    [SerializeField] private bool preserveHorizontalVelocity = true;
    [SerializeField] private float horizontalBoostMultiplier = 1.2f;
    
    [Header("Cooldown")]
    [SerializeField] private bool useCooldown = false;
    [SerializeField] private float cooldownDuration = 0.5f;
    private float lastBounceTime = -999f;
    
    private void Awake()
    {
        // Ensure the collider is set to trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"BouncePad on {gameObject.name}: Collider should be set as trigger. Setting it now.");
            col.isTrigger = true;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check cooldown
        if (useCooldown && Time.time - lastBounceTime < cooldownDuration)
        {
            return;
        }
        
        // Check if the colliding object is the player
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController != null)
        {
            ApplyBounce(playerController, other.transform.position);
            lastBounceTime = Time.time;
        }
    }
    
    private void ApplyBounce(PlayerController playerController, Vector3 contactPoint)
    {
        // Get the bounce direction (up from the bounce pad's orientation)
        Vector3 bounceDirection = transform.up;
        
        // Calculate the bounce velocity
        Vector3 bounceVelocity = bounceDirection * bounceForce;
        
        if (preserveHorizontalVelocity)
        {
            // Get player's current velocity
            Vector3 currentVelocity = playerController.GetCurrentVelocity();
            
            // Project current velocity onto horizontal plane
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
            
            // Apply horizontal boost multiplier
            horizontalVelocity *= horizontalBoostMultiplier;
            
            // Combine horizontal velocity with vertical bounce
            bounceVelocity = new Vector3(horizontalVelocity.x, bounceVelocity.y, horizontalVelocity.z);
        }
        
        // Apply the bounce to the player
        playerController.SetVelocity(bounceVelocity);
        
        // Play bounce pad sound
        AudioManager.PlayBouncePadSound(contactPoint);
        
        // Notify scoring system if available
        Scoring scoring = playerController.GetComponent<Scoring>();
        if (scoring != null)
        {
            scoring.OnBouncePadUsed();
        }
    }
   
    
    // Visualize bounce direction in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + transform.up * bounceForce * 0.2f;
        
        // Draw arrow showing bounce direction and force
        Gizmos.DrawLine(start, end);
        
        // Draw arrow head
        Vector3 right = Quaternion.LookRotation(transform.up) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(transform.up) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        Gizmos.DrawRay(end, right * 0.5f);
        Gizmos.DrawRay(end, left * 0.5f);
        
        // Draw sphere at base
        Gizmos.DrawWireSphere(start, 0.3f);
    }
}
