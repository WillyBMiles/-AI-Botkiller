using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private int ammoAmount = 30;
    [SerializeField] private float respawnCooldown = 10f;
    [SerializeField] private float respawnDistance = 15f;
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject pickupVisual;
    
    private Transform playerTransform;
    private Player player;
    private bool isAvailable = true;
    private float cooldownTimer = 0f;
    
    private void Awake()
    {
        // Auto-find the pickup visual if not assigned (use the first child or self)
        if (pickupVisual == null)
        {
            // Try to find a child object, otherwise use self
            if (transform.childCount > 0)
            {
                pickupVisual = transform.GetChild(0).gameObject;
            }
            else
            {
                Debug.LogWarning($"AmmoPickup on {gameObject.name}: No pickup visual assigned. Assign a child GameObject for the visual representation.");
            }
        }
    }
    
    private void Start()
    {
        // Find the player in the scene
        player = FindObjectOfType<Player>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("AmmoPickup: No Player found in scene!");
        }
        
        // Ensure pickup is visible at start
        SetPickupVisible(true);
    }
    
    private void Update()
    {
        // Handle respawn logic
        if (!isAvailable)
        {
            cooldownTimer += Time.deltaTime;
            
            // Check if cooldown has passed and player is far enough
            if (cooldownTimer >= respawnCooldown && IsPlayerFarEnough())
            {
                Respawn();
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if player touched the pickup
        if (isAvailable)
        {
            Player playerComponent = other.GetComponent<Player>();
            if (playerComponent != null)
            {
                // Check if player can benefit from ammo
                if (playerComponent.GetCurrentAmmo() < playerComponent.GetMaxAmmo())
                {
                    // Give ammo to player
                    playerComponent.AddAmmo(ammoAmount);
                    
                    // Hide pickup and start cooldown
                    PickupCollected();
                }
            }
        }
    }
    
    private void PickupCollected()
    {
        isAvailable = false;
        cooldownTimer = 0f;
        SetPickupVisible(false);
    }
    
    private void Respawn()
    {
        isAvailable = true;
        cooldownTimer = 0f;
        SetPickupVisible(true);
    }
    
    private void SetPickupVisible(bool visible)
    {
        if (pickupVisual != null)
        {
            pickupVisual.SetActive(visible);
        }
    }
    
    private bool IsPlayerFarEnough()
    {
        if (playerTransform == null) return true;
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance >= respawnDistance;
    }
}
