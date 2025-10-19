using UnityEngine;
using TMPro;

public class Scoring : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int currentScore = 0;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI trickText;
    
    [Header("Trick Point Values")]
    [SerializeField] private int killTrickPoints = 10;
    [SerializeField] private int wallShotTrickPoints = 50;
    
    [Header("Trick Display Animation")]
    [SerializeField] private float popScaleMultiplier = 1.5f;
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 5f;
    
    [Header("Components")]
    [SerializeField] private PlayerController playerController;
    
    // Animation state
    private Vector3 trickTextOriginalScale;
    private Vector2 trickTextOriginalAnchoredPosition;
    private RectTransform trickTextRectTransform;
    private bool isAnimating = false;
    
    private void Awake()
    {
        // Auto-find player controller if not assigned
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
        
        // Store original trick text transform
        if (trickText != null)
        {
            trickTextRectTransform = trickText.GetComponent<RectTransform>();
            if (trickTextRectTransform != null)
            {
                trickTextOriginalScale = trickTextRectTransform.localScale;
                trickTextOriginalAnchoredPosition = trickTextRectTransform.anchoredPosition;
            }
            trickText.text = ""; // Start empty
        }
        
        // Initialize score display
        UpdateScoreDisplay();
    }
    
    /// <summary>
    /// Awards points for performing a trick
    /// </summary>
    /// <param name="trickName">Name of the trick performed</param>
    /// <param name="points">Points to award</param>
    public void AwardTrick(string trickName, int points)
    {
        currentScore += points;
        UpdateScoreDisplay();
        UpdateTrickDisplay(trickName, points);
        
        Debug.Log($"Trick: {trickName} (+{points} points) - Total Score: {currentScore}");
    }
    
    /// <summary>
    /// Called when an enemy is killed. Determines which trick(s) apply and awards the highest value one.
    /// </summary>
    public void OnEnemyKilled()
    {
        // Check all possible tricks and find the highest value one
        string bestTrickName = "Kill";
        int bestTrickPoints = killTrickPoints;
        
        // Check for wall shot
        if (playerController != null && playerController.IsWallRunning())
        {
            if (wallShotTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Wall Shot";
                bestTrickPoints = wallShotTrickPoints;
            }
        }
        
        // Award the best trick
        AwardTrick(bestTrickName, bestTrickPoints);
    }
    
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
    }
    
    private void UpdateTrickDisplay(string trickName, int points)
    {
        if (trickText == null) return;
        
        // Update text
        trickText.text = $"{trickName} +{points}";
        
        // Start pop animation
        if (!isAnimating)
        {
            StartCoroutine(TrickPopAnimation());
        }
    }
    
    private System.Collections.IEnumerator TrickPopAnimation()
    {
        if (trickTextRectTransform == null) yield break;
        
        isAnimating = true;
        float elapsed = 0f;
        
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            
            // Scale animation (pop out then back)
            float scale = 1f + (popScaleMultiplier - 1f) * Mathf.Sin(t * Mathf.PI);
            trickTextRectTransform.localScale = trickTextOriginalScale * scale;
            
            // Shake animation (random offset that decreases over time)
            float shakeAmount = shakeIntensity * (1f - t);
            Vector2 shake = new Vector2(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount)
            );
            trickTextRectTransform.anchoredPosition = trickTextOriginalAnchoredPosition + shake;
            
            yield return null;
        }
        
        // Reset to original transform
        trickTextRectTransform.localScale = trickTextOriginalScale;
        trickTextRectTransform.anchoredPosition = trickTextOriginalAnchoredPosition;
        
        isAnimating = false;
    }
    
    // Public getters
    public int GetCurrentScore() => currentScore;
    
    /// <summary>
    /// Resets the score to zero
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreDisplay();
    }
}
