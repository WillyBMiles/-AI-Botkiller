using System;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifetime = 5f;
    
    [Header("Impact Settings")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float trailFadeDelay = 0.5f;
    
    private Vector3 direction;
    private bool hasHit = false;
    private float spawnTime;
    
    // Event for when projectile hits player
    public Action onHitPlayer;
    
    private void Start()
    {
        spawnTime = Time.time;
    }
    
    private void Update()
    {
        // Don't move if we've already hit something
        if (hasHit) return;
        
        // Check if projectile has exceeded lifetime
        if (Time.time - spawnTime > lifetime)
        {
            DestroyProjectile();
            return;
        }
        
        // Move projectile forward
        float distance = speed * Time.deltaTime;
        
        // Raycast to check for collisions
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, distance))
        {
            // Hit something
            HandleHit(hit);
        }
        else
        {
            // No collision, move forward
            transform.position += direction * distance;
        }
    }
    
    /// <summary>
    /// Sets the direction the projectile should travel
    /// </summary>
    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection.normalized;
        
        // Rotate projectile to face direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    /// <summary>
    /// Sets the damage this projectile deals
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
    
    private void HandleHit(RaycastHit hit)
    {
        hasHit = true;
        
        // Move to hit point
        transform.position = hit.point;
        
        // Check if we hit the player
        Player player = hit.collider.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(damage);
            Debug.Log($"Projectile hit player for {damage} damage");
            
            // Invoke hit event
            onHitPlayer?.Invoke();
        }
        
        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 2f);
        }
        
        // Destroy projectile after delay to allow trail to fade
        Destroy(gameObject, trailFadeDelay);
    }
    
    private void DestroyProjectile()
    {
        // Destroy immediately if no trail delay needed
        Destroy(gameObject, hasHit ? 0f : trailFadeDelay);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Backup collision detection using trigger
        // This handles cases where raycast might miss
        if (hasHit) return;
        
        // Ignore other enemy projectiles and enemies
        if (other.attachedRigidbody != null && (other.attachedRigidbody.GetComponent<EnemyProjectile>() != null || other.attachedRigidbody.GetComponent<Enemy>() != null))
        {
            return;
        }
        
        // Create a fake hit for trigger collision
        RaycastHit hit = new RaycastHit();
        hit.point = transform.position;
        hit.normal = -direction;
        
        // Check if we hit the player
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            hasHit = true;
            player.TakeDamage(damage);
            Debug.Log($"Projectile hit player (trigger) for {damage} damage");
            
            // Invoke hit event
            onHitPlayer?.Invoke();
            
            // Spawn impact effect
            if (impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(impactEffectPrefab, transform.position, Quaternion.LookRotation(-direction));
                Destroy(impact, 2f);
            }
            
            // Destroy projectile after delay
            Destroy(gameObject, trailFadeDelay);
        }
        else
        {
            // Hit something else (wall, obstacle)
            hasHit = true;
            
            // Spawn impact effect
            if (impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(impactEffectPrefab, transform.position, Quaternion.LookRotation(-direction));
                Destroy(impact, 2f);
            }
            
            // Destroy projectile after delay
            Destroy(gameObject, trailFadeDelay);
        }
    }
}
