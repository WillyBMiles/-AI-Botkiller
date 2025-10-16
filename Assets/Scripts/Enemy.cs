using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private int pointValue = 1;
    
    [Header("Death Settings")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionLifetime = 2f;
    
    private bool isDead = false;
    
    private void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
    }
    
    /// <summary>
    /// Damages the enemy by the specified amount
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);
        
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Check if enemy died
        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Heals the enemy by the specified amount
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        Debug.Log($"{gameObject.name} healed {amount}. Health: {currentHealth}/{maxHealth}");
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        Debug.Log($"{gameObject.name} died!");
        
        // Spawn explosion if prefab is assigned
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, explosionLifetime);
        }
        
        // Destroy this enemy
        Destroy(gameObject);
    }
    
    // Public getters
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public int GetPointValue() => pointValue;
}
