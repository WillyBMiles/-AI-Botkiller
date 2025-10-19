using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Shooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private float fireRate = 10f; // Shots per second
    [SerializeField] private LayerMask hitLayers;
    
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform gunBarrelTransform;
    [SerializeField] private GameObject muzzleFlare;
    [SerializeField] private GameObject bulletHitPrefab;
    [SerializeField] private LineRenderer bulletTrailPrefab;
    
    [Header("Bullet Hit Pool Settings")]
    [SerializeField] private int maxBulletHits = 20;
    
    [Header("Bullet Trail Settings")]
    [SerializeField] private float bulletTrailDuration = 0.1f;
    [SerializeField] private float bulletTrailFadeSpeed = 5f;
    
    [Header("Camera Recoil Settings")]
    [SerializeField] private float cameraRecoilVertical = 1f;
    [SerializeField] private float cameraRecoilHorizontal = 0.5f;
    
    [Header("Components")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerController playerController;
    
    [Header("Score UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    
    // Shooting state
    private bool isShooting = false;
    private float nextFireTime = 0f;
    
    // Score tracking
    private int score = 0;
    
    // Bullet hit pool
    private Queue<GameObject> bulletHitPool = new Queue<GameObject>();
    
    // Camera recoil (no variables needed, applied directly)
    
    private void Awake()
    {
        // Auto-find camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("Shooting: No camera found! Please assign a camera.");
            }
        }
        
        // Auto-find player if not assigned
        if (player == null)
        {
            player = GetComponent<Player>();
        }
        
        // Auto-find player controller if not assigned
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
        
        // Hide muzzle flare at start
        if (muzzleFlare != null)
        {
            muzzleFlare.SetActive(false);
        }
        
        // Initialize score display
        UpdateScoreDisplay();
    }
    
    private void Update()
    {
        // Handle automatic fire
        if (isShooting && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + (1f / fireRate);
        }
    }
    
    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isShooting = true;
            // Shoot immediately on first press
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + (1f / fireRate);
            }
        }
        else if (context.canceled)
        {
            isShooting = false;
        }
    }
    
    private void Shoot()
    {
        // Check if player has ammo
        if (player != null && !player.UseAmmo(1))
        {
            // Out of ammo, don't shoot
            return;
        }
        
        // Show muzzle flare with random rotation
        if (muzzleFlare != null)
        {
            muzzleFlare.SetActive(true);
            muzzleFlare.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            // Disable muzzle flare next frame
            Invoke(nameof(HideMuzzleFlare), 0.02f);
        }
        
        // Trigger gun recoil
        if (playerController != null)
        {
            playerController.TriggerGunRecoil();
        }
        
        // Apply camera recoil
        ApplyCameraRecoil();
        
        // Raycast from camera center
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        // Determine bullet trail start and end points
        Vector3 trailStart = gunBarrelTransform != null ? gunBarrelTransform.position : playerCamera.transform.position;
        Vector3 trailEnd;
        
        if (Physics.Raycast(ray, out hit, maxRange, hitLayers))
        {
            trailEnd = hit.point;
            
            // Check if we hit an enemy
            Enemy enemy = hit.rigidbody?.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Check if enemy will die from this damage
                bool willDie = enemy.GetCurrentHealth() <= damage;
                
                enemy.TakeDamage(damage);
                
                // Increment score if enemy died
                if (willDie)
                {
                    score++;
                    UpdateScoreDisplay();
                }
            }
            
            // Instantiate bullet hit effect at hit point
            if (bulletHitPrefab != null)
            {
                // Create bullet hit as child of hit object
                GameObject bulletHit = Instantiate(bulletHitPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                
                // Parent to the hit transform if available
                if (hit.transform != null)
                {
                    bulletHit.transform.SetParent(hit.transform);
                }
                
                // Add to pool and manage pool size
                bulletHitPool.Enqueue(bulletHit);
                
                // If pool exceeds max size, destroy oldest
                if (bulletHitPool.Count > maxBulletHits)
                {
                    GameObject oldestHit = bulletHitPool.Dequeue();
                    if (oldestHit != null)
                    {
                        Destroy(oldestHit);
                    }
                }
            }
        }
        else
        {
            // No hit, trail goes to max range
            trailEnd = ray.origin + ray.direction * maxRange;
        }
        
        // Create bullet trail
        if (bulletTrailPrefab != null)
        {
            CreateBulletTrail(trailStart, trailEnd);
        }
    }
    
    private void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        // Instantiate line renderer in world space (not parented)
        LineRenderer trail = Instantiate(bulletTrailPrefab, Vector3.zero, Quaternion.identity);
        
        // Set positions
        trail.positionCount = 2;
        trail.SetPosition(0, start);
        trail.SetPosition(1, end);
        
        // Start fade coroutine
        StartCoroutine(FadeBulletTrail(trail));
    }
    
    private System.Collections.IEnumerator FadeBulletTrail(LineRenderer trail)
    {
        if (trail == null) yield break;
        
        float elapsed = 0f;
        Color startColor = trail.startColor;
        Color endColor = trail.endColor;
        
        while (elapsed < bulletTrailDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / bulletTrailDuration);
            
            // Fade out both start and end colors
            Color fadeStart = startColor;
            fadeStart.a = startColor.a * alpha;
            Color fadeEnd = endColor;
            fadeEnd.a = endColor.a * alpha;
            
            trail.startColor = fadeStart;
            trail.endColor = fadeEnd;
            
            yield return null;
        }
        
        // Destroy trail after fade
        Destroy(trail.gameObject);
    }
    
    private void HideMuzzleFlare()
    {
        if (muzzleFlare != null)
        {
            muzzleFlare.SetActive(false);
        }
    }
    
    private void ApplyCameraRecoil()
    {
        if (playerController == null) return;
        
        // Apply recoil by directly modifying the PlayerController's camera rotation
        // This permanently affects aim and requires player to compensate
        
        // Vertical recoil (kick up)
        float verticalKick = -cameraRecoilVertical;
        
        // Horizontal recoil (random left/right)
        float horizontalKick = Random.Range(-cameraRecoilHorizontal, cameraRecoilHorizontal);
        
        // Apply recoil to the player's look rotation
        playerController.ApplyCameraRecoil(verticalKick, horizontalKick);
    }
    
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
    
    public void ShowGameOverScore()
    {
        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = "Your score is: " + score;
        }
    }
    
    // Public getter for score
    public int GetScore() => score;
}
