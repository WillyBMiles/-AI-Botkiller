using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private int maxAmmo = 100;
    [SerializeField] private int currentAmmo;
    
    [Header("UI References")]
    [SerializeField] private CanvasGroup loseScreenCanvasGroup;
    [SerializeField] private Image healthBarFillImage;
    [SerializeField] private TextMeshProUGUI ammoCountText;
    
    [Header("Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Shooting shooting;
    
    private bool isDead = false;
    
    private void Awake()
    {
        // Initialize stats
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
        
        // Auto-find components if not assigned
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
        
        if (shooting == null)
        {
            shooting = GetComponent<Shooting>();
        }
        
        // Hide lose screen at start
        if (loseScreenCanvasGroup != null)
        {
            loseScreenCanvasGroup.alpha = 0f;
            loseScreenCanvasGroup.interactable = false;
            loseScreenCanvasGroup.blocksRaycasts = false;
        }
        
        // Initialize health bar
        UpdateHealthBar();
        
        // Initialize ammo display
        UpdateAmmoDisplay();
    }
    
    
    /// <summary>
    /// Reduces player health by the specified amount
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);
        
        // Update health bar
        UpdateHealthBar();
        
        // Check if player died
        if (currentHealth <= 0f)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Heals the player by the specified amount (from health packs)
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        // Update health bar
        UpdateHealthBar();
    }
    
    /// <summary>
    /// Adds ammo to the player (from ammo packs)
    /// </summary>
    public void AddAmmo(int amount)
    {
        if (isDead) return;
        
        currentAmmo += amount;
        currentAmmo = Mathf.Min(currentAmmo, maxAmmo);
        
        // Update ammo display
        UpdateAmmoDisplay();
    }
    
    /// <summary>
    /// Consumes ammo when shooting. Returns true if ammo was available.
    /// </summary>
    public bool UseAmmo(int amount = 1)
    {
        if (isDead) return false;
        
        if (currentAmmo >= amount)
        {
            currentAmmo -= amount;
            
            // Update ammo display
            UpdateAmmoDisplay();
            
            return true;
        }
        
        Debug.Log("Out of ammo!");
        return false;
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        Debug.Log("Player died!");
        
        // Disable player controller
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Show lose screen
        ShowLoseScreen();
        
        // Unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void ShowLoseScreen()
    {
        if (loseScreenCanvasGroup != null)
        {
            loseScreenCanvasGroup.alpha = 1f;
            loseScreenCanvasGroup.interactable = true;
            loseScreenCanvasGroup.blocksRaycasts = true;
        }
        
        // Show game over score
        if (shooting != null)
        {
            shooting.ShowGameOverScore();
        }
    }
    
    public void OnRestart(InputAction.CallbackContext context)
    {
        if (context.performed && isDead)
        {
            RestartGame();
        }
    }
    
    private void RestartGame()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    private void UpdateHealthBar()
    {
        if (healthBarFillImage != null)
        {
            healthBarFillImage.fillAmount = GetHealthPercentage();
        }
    }
    
    private void UpdateAmmoDisplay()
    {
        if (ammoCountText != null)
        {
            ammoCountText.text = $"{currentAmmo}";
        }
    }
    
    // Public getters for UI or other systems
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool IsDead() => isDead;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetAmmoPercentage() => (float)currentAmmo / maxAmmo;
}
