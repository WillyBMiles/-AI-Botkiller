using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Scoring : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int currentScore = 0;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI trickText;
    [SerializeField] private TextMeshProUGUI comboMultiplierText;
    [SerializeField] private TextMeshProUGUI comboScoreText;
    [SerializeField] private Image comboTimerFillImage;
    
    [Header("Trick Point Values")]
    [SerializeField] private int killTrickPoints = 10;
    [SerializeField] private int wallShotTrickPoints = 50;
    [SerializeField] private int wallHopTrickPoints = 100;
    [SerializeField] private int doubleUpTrickPoints = 25;
    [SerializeField] private int sliderTrickPoints = 100;
    [SerializeField] private int highJumpTrickPoints = 150;
    [SerializeField] private int bounceBounceTrickPoints = 200;
    [SerializeField] private int diveBombTrickPoints = 125;
    [SerializeField] private int longTripTrickPoints = 75;
    [SerializeField] private int savedFallTrickPoints = 125;
    [SerializeField] private int weakSpotTrickPoints = 300;
    [SerializeField] private int deathFromAboveTrickPoints = 175;
    [SerializeField] private int meleeTrickPoints = 25;
    [SerializeField] private int spreeTrickPoints = 125;
    [SerializeField] private int bHopTrickPoints = 90;
    [SerializeField] private int plummetTrickPoints = 170;
    
    [Header("Trick Timing")]
    [SerializeField] private float trickTimeWindow = 1f; // Time window for timed tricks
    [SerializeField] private float bounceBounceTrickWindow = 2f; // Time window for double wall jump
    [SerializeField] private float longTripTimeThreshold = 10f; // Time airborne for long trip
    [SerializeField] private float spreeTimeWindow = 2f; // Time window for kill spree
    [SerializeField] private float bHopTimeWindow = 5f; // Time window for b-hop jumps
    [SerializeField] private int bHopJumpCount = 5; // Number of jumps required for b-hop
    [SerializeField] private float plummetTimeThreshold = 3f; // Time without touching ground/wall
    
    [Header("Trick Distance Thresholds")]
    [SerializeField] private float meleeDistance = 2f;
    [SerializeField] private float weakSpotDistance = 3f;
    [SerializeField] private float deathFromAboveAngle = 60f; // Degrees from vertical
    
    [Header("Combo Settings")]
    [SerializeField] private float comboTimerDuration = 3f;
    
    [Header("Trick Display Animation")]
    [SerializeField] private float popScaleMultiplier = 1.5f;
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 5f;
    [SerializeField] private float multiplierPopScale = 2.0f; // Bigger pop for multiplier
    
    [Header("Combo Visual Effects")]
    [SerializeField] private float flashThreshold = 0.2f; // Flash when below 20% time
    [SerializeField] private float flashSpeed = 10f;
    [SerializeField] private float countUpDuration = 1.5f; // Time to count up in seconds
    
    [Header("Components")]
    [SerializeField] private PlayerController playerController;
    
    // Animation state
    private Vector3 trickTextOriginalScale;
    private Vector2 trickTextOriginalAnchoredPosition;
    private RectTransform trickTextRectTransform;
    private bool isAnimating = false;
    
    // Combo text animation
    private Vector3 comboScoreTextOriginalScale;
    private RectTransform comboScoreTextRectTransform;
    private bool isComboTextAnimating = false;
    
    // Multiplier text animation
    private Vector3 comboMultiplierTextOriginalScale;
    private RectTransform comboMultiplierTextRectTransform;
    private bool isMultiplierTextAnimating = false;
    
    // Count-up animation
    private int displayedScore = 0;
    private int targetScore = 0;
    private float scoreCountTimer = 0f;
    private int scoreCountStartValue = 0;
    private bool isCountingScore = false;
    
    private int displayedComboScore = 0;
    private int targetComboScore = 0;
    private float comboScoreCountTimer = 0f;
    private int comboScoreCountStartValue = 0;
    private bool isCountingComboScore = false;
    
    // Combo state
    private bool isComboActive = false;
    private float comboTimer = 0f;
    private int comboTotalPoints = 0;
    private HashSet<string> uniqueTricksInCombo = new HashSet<string>();
    private List<int> trickPointsInCombo = new List<int>();
    
    // Trick timing tracking
    private float lastWallJumpTime = -999f;
    private float lastDoubleJumpTime = -999f;
    private float lastSlideBoostJumpTime = -999f;
    private float secondToLastWallJumpTime = -999f; // For bounce bounce tracking
    private float lastGroundedTime = 0f; // For long trip tracking
    private float firstKillTime = -999f; // For spree tracking
    private int killsInSpreeWindow = 0; // For spree tracking
    private float firstJumpTime = -999f; // For b-hop tracking
    private int jumpsInBHopWindow = 0; // For b-hop tracking
    private float lastGroundOrWallTouchTime = -999f; // For plummet tracking
    
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
        
        // Store original combo score text transform
        if (comboScoreText != null)
        {
            comboScoreTextRectTransform = comboScoreText.GetComponent<RectTransform>();
            if (comboScoreTextRectTransform != null)
            {
                comboScoreTextOriginalScale = comboScoreTextRectTransform.localScale;
            }
        }
        
        // Store original combo multiplier text transform
        if (comboMultiplierText != null)
        {
            comboMultiplierTextRectTransform = comboMultiplierText.GetComponent<RectTransform>();
            if (comboMultiplierTextRectTransform != null)
            {
                comboMultiplierTextOriginalScale = comboMultiplierTextRectTransform.localScale;
            }
        }
        
        // Initialize score display
        UpdateScoreDisplay();
        UpdateComboUI();
    }
    
    private void Update()
    {
        // Update combo timer
        if (isComboActive)
        {
            comboTimer -= Time.deltaTime;
            
            float fillAmount = comboTimer / comboTimerDuration;
            
            // Update timer fill image
            if (comboTimerFillImage != null)
            {
                comboTimerFillImage.fillAmount = fillAmount;
                
                // Flash when below threshold
                if (fillAmount < flashThreshold)
                {
                    float alpha = 0.5f + 0.5f * Mathf.Sin(Time.time * flashSpeed);
                    Color color = comboTimerFillImage.color;
                    color.a = alpha;
                    comboTimerFillImage.color = color;
                }
                else
                {
                    // Reset alpha when above threshold
                    Color color = comboTimerFillImage.color;
                    color.a = 1f;
                    comboTimerFillImage.color = color;
                }
            }
            
            // Check if combo expired
            if (comboTimer <= 0f)
            {
                EndCombo();
            }
        }
        
        // Count up displayed scores (time-based)
        if (isCountingScore)
        {
            scoreCountTimer += Time.deltaTime;
            float t = Mathf.Clamp01(scoreCountTimer / countUpDuration);
            displayedScore = Mathf.RoundToInt(Mathf.Lerp(scoreCountStartValue, targetScore, t));
            
            if (scoreText != null)
            {
                scoreText.text = displayedScore.ToString();
            }
            
            if (t >= 1f)
            {
                displayedScore = targetScore;
                isCountingScore = false;
            }
        }
        
        if (isCountingComboScore)
        {
            comboScoreCountTimer += Time.deltaTime;
            float t = Mathf.Clamp01(comboScoreCountTimer / countUpDuration);
            displayedComboScore = Mathf.RoundToInt(Mathf.Lerp(comboScoreCountStartValue, targetComboScore, t));
            
            if (comboScoreText != null && isComboActive)
            {
                int multiplier = uniqueTricksInCombo.Count;
                int multipliedScore = displayedComboScore * multiplier;
                comboScoreText.text = multipliedScore.ToString();
            }
            
            if (t >= 1f)
            {
                displayedComboScore = targetComboScore;
                isCountingComboScore = false;
            }
        }
    }
    
    /// <summary>
    /// Awards points for performing a trick
    /// </summary>
    /// <param name="trickName">Name of the trick performed</param>
    /// <param name="points">Points to award</param>
    public void AwardTrick(string trickName, int points)
    {
        // Add to combo
        if (!isComboActive)
        {
            StartCombo();
        }
        
        // Add trick to combo
        int previousUniqueCount = uniqueTricksInCombo.Count;
        uniqueTricksInCombo.Add(trickName);
        trickPointsInCombo.Add(points);
        comboTotalPoints += points;
        
        // Check if multiplier increased
        bool multiplierIncreased = uniqueTricksInCombo.Count > previousUniqueCount;
        
        // Reset combo timer
        comboTimer = comboTimerDuration;
        
        // Start count-up animation for combo score
        comboScoreCountStartValue = displayedComboScore;
        targetComboScore = comboTotalPoints;
        comboScoreCountTimer = 0f;
        isCountingComboScore = true;
        
        // Update displays
        UpdateTrickDisplay(trickName, points);
        UpdateComboUI();
        
        // Play trick completed sound
        if (playerController != null)
        {
            AudioManager.PlayTrickCompletedSound(playerController.transform.position);
        }
        
        // Pop combo text if multiplier increased
        if (multiplierIncreased)
        {
            if (!isComboTextAnimating)
            {
                StartCoroutine(ComboTextPopAnimation());
            }
            if (!isMultiplierTextAnimating)
            {
                StartCoroutine(MultiplierTextPopAnimation());
            }
            
            // Play multiplier increase sound
            if (playerController != null)
            {
                AudioManager.PlayComboMultiplierIncreaseSound(playerController.transform.position);
            }
        }
        
        Debug.Log($"Trick: {trickName} (+{points} points) - Combo: {uniqueTricksInCombo.Count}x multiplier, {comboTotalPoints} total");
    }
    
    /// <summary>
    /// Called when an enemy is killed. Determines which trick(s) apply and awards the highest value one.
    /// </summary>
    public void OnEnemyKilled(Vector3 enemyPosition)
    {
        // Check all possible tricks and find the highest value one
        string bestTrickName = "Kill";
        int bestTrickPoints = killTrickPoints;
        
        float currentTime = Time.time;
        Vector3 playerPosition = playerController != null ? playerController.transform.position : Vector3.zero;
        float distanceToEnemy = Vector3.Distance(playerPosition, enemyPosition);
        Vector3 directionToEnemy = (enemyPosition - playerPosition).normalized;
        float verticalAngle = Vector3.Angle(Vector3.down, directionToEnemy); // Angle from straight down
        
        // Check for weak spot (below enemy, close, and sliding)
        bool isBelow = enemyPosition.y > playerPosition.y;
        if (isBelow && distanceToEnemy <= weakSpotDistance && playerController != null && playerController.IsSliding())
        {
            if (weakSpotTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Weak Spot";
                bestTrickPoints = weakSpotTrickPoints;
            }
        }
        
        // Check for bounce bounce (two wall jumps within window, kill within 1s of second jump)
        bool isBounceBounceTrick = (currentTime - lastWallJumpTime <= trickTimeWindow) &&
                                    (lastWallJumpTime - secondToLastWallJumpTime <= bounceBounceTrickWindow);
        if (isBounceBounceTrick)
        {
            if (bounceBounceTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Bounce Bounce";
                bestTrickPoints = bounceBounceTrickPoints;
            }
        }
        
        // Check for death from above (almost directly above enemy)
        if (verticalAngle <= deathFromAboveAngle)
        {
            if (deathFromAboveTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Death from Above";
                bestTrickPoints = deathFromAboveTrickPoints;
            }
        }
        
        // Check for saved fall (double jump while fast falling)
        bool isFastFalling = playerController != null && playerController.IsFastFalling();
        if (isFastFalling && (currentTime - lastDoubleJumpTime <= trickTimeWindow))
        {
            if (savedFallTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Saved Fall";
                bestTrickPoints = savedFallTrickPoints;
            }
        }
        
        // Check for dive bomb (active fast falling)
        if (isFastFalling)
        {
            if (diveBombTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Dive Bomb";
                bestTrickPoints = diveBombTrickPoints;
            }
        }
        
        // Check for spree (3rd kill within time window)
        if (currentTime - firstKillTime <= spreeTimeWindow)
        {
            killsInSpreeWindow++;
            if (killsInSpreeWindow >= 3)
            {
                if (spreeTrickPoints > bestTrickPoints)
                {
                    bestTrickName = "Spree";
                    bestTrickPoints = spreeTrickPoints;
                }
            }
        }
        else
        {
            // Reset spree tracking
            firstKillTime = currentTime;
            killsInSpreeWindow = 1;
        }
        
        // Check for plummet (no ground or wall touch for extended time)
        // Only award if we have a valid last touch time and enough time has passed
        if (lastGroundOrWallTouchTime > 0f)
        {
            float timeSinceGroundOrWall = currentTime - lastGroundOrWallTouchTime;
            if (timeSinceGroundOrWall >= plummetTimeThreshold)
            {
                if (plummetTrickPoints > bestTrickPoints)
                {
                    bestTrickName = "Plummet";
                    bestTrickPoints = plummetTrickPoints;
                }
            }
        }
        
        // Check for long trip (airborne for extended time)
        float timeAirborne = currentTime - lastGroundedTime;
        if (timeAirborne >= longTripTimeThreshold)
        {
            if (longTripTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Long Trip";
                bestTrickPoints = longTripTrickPoints;
            }
        }
        
        // Check for b-hop (5 jumps within time window)
        if (jumpsInBHopWindow >= bHopJumpCount)
        {
            if (bHopTrickPoints > bestTrickPoints)
            {
                bestTrickName = "B-Hop";
                bestTrickPoints = bHopTrickPoints;
            }
        }
        
        // Check for wall shot (active wall running)
        if (playerController != null && playerController.IsWallRunning())
        {
            if (wallShotTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Wall Shot";
                bestTrickPoints = wallShotTrickPoints;
            }
        }
        
        // Check for slider (active sliding)
        if (playerController != null && playerController.IsSliding())
        {
            if (sliderTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Slider";
                bestTrickPoints = sliderTrickPoints;
            }
        }
        
        // Check for high jump (within time window of slide-boosted jump)
        if (currentTime - lastSlideBoostJumpTime <= trickTimeWindow)
        {
            if (highJumpTrickPoints > bestTrickPoints)
            {
                bestTrickName = "High Jump";
                bestTrickPoints = highJumpTrickPoints;
            }
        }
        
        // Check for wall hop (within time window of wall jump)
        if (currentTime - lastWallJumpTime <= trickTimeWindow)
        {
            if (wallHopTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Wall Hop";
                bestTrickPoints = wallHopTrickPoints;
            }
        }
        
        // Check for melee (very close to enemy)
        if (distanceToEnemy <= meleeDistance)
        {
            if (meleeTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Melee";
                bestTrickPoints = meleeTrickPoints;
            }
        }
        
        // Check for double up (within time window of double jump)
        if (currentTime - lastDoubleJumpTime <= trickTimeWindow)
        {
            if (doubleUpTrickPoints > bestTrickPoints)
            {
                bestTrickName = "Double Up";
                bestTrickPoints = doubleUpTrickPoints;
            }
        }
        
        // Award the best trick
        AwardTrick(bestTrickName, bestTrickPoints);
    }
    
    /// <summary>
    /// Called when player performs a wall jump
    /// </summary>
    public void OnWallJump()
    {
        secondToLastWallJumpTime = lastWallJumpTime; // Track previous wall jump
        lastWallJumpTime = Time.time;
    }
    
    /// <summary>
    /// Called when player performs a double jump
    /// </summary>
    public void OnDoubleJump()
    {
        lastDoubleJumpTime = Time.time;
    }
    
    /// <summary>
    /// Called when player performs a slide-boosted jump
    /// </summary>
    public void OnSlideBoostJump()
    {
        lastSlideBoostJumpTime = Time.time;
    }
    
    /// <summary>
    /// Called when player touches the ground
    /// </summary>
    public void OnGrounded()
    {
        lastGroundedTime = Time.time;
        lastGroundOrWallTouchTime = Time.time;
    }
    
    /// <summary>
    /// Called when player performs any type of jump (for b-hop tracking)
    /// </summary>
    public void OnAnyJump()
    {
        float currentTime = Time.time;
        
        // Track jumps for b-hop
        if (currentTime - firstJumpTime <= bHopTimeWindow)
        {
            jumpsInBHopWindow++;
        }
        else
        {
            // Reset b-hop tracking
            firstJumpTime = currentTime;
            jumpsInBHopWindow = 1;
        }
    }
    
    /// <summary>
    /// Called when player uses a bounce pad (optional hook for future tricks)
    /// </summary>
    public void OnBouncePadUsed()
    {
        // Currently no specific trick for bounce pads, but this hook is here for future expansion
        // You could add tricks like "Bounce Master" for using multiple bounce pads in succession
    }
    
    private void StartCombo()
    {
        isComboActive = true;
        comboTimer = comboTimerDuration;
        comboTotalPoints = 0;
        uniqueTricksInCombo.Clear();
        trickPointsInCombo.Clear();
    }
    
    private void EndCombo()
    {
        if (!isComboActive) return;
        
        // Calculate final combo score
        int multiplier = uniqueTricksInCombo.Count;
        int finalComboScore = comboTotalPoints * multiplier;
        
        // Award points to total score
        scoreCountStartValue = displayedScore;
        targetScore = currentScore + finalComboScore;
        currentScore = targetScore;
        scoreCountTimer = 0f;
        isCountingScore = true; // Start count-up animation
        
        Debug.Log($"Combo ended! {uniqueTricksInCombo.Count}x multiplier, {comboTotalPoints} base points = {finalComboScore} total points awarded");
        
        // Play combo finish sound
        if (playerController != null)
        {
            AudioManager.PlayComboFinishSound(playerController.transform.position);
        }
        
        // Reset combo state
        isComboActive = false;
        comboTotalPoints = 0;
        displayedComboScore = 0;
        uniqueTricksInCombo.Clear();
        trickPointsInCombo.Clear();
        
        // Clear trick text
        if (trickText != null)
        {
            trickText.text = "";
        }
        
        UpdateComboUI();
    }
    
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = displayedScore.ToString();
        }
    }
    
    private void UpdateComboUI()
    {
        int multiplier = isComboActive ? uniqueTricksInCombo.Count : 0;
        
        // Update multiplier text
        if (comboMultiplierText != null)
        {
            if (isComboActive)
            {
                comboMultiplierText.text = $"x{multiplier}";
            }
            else
            {
                comboMultiplierText.text = "";
            }
        }
        
        // Update combo score text (with multiplier applied for display only)
        if (comboScoreText != null)
        {
            if (isComboActive)
            {
                int multipliedScore = displayedComboScore * multiplier;
                comboScoreText.text = multipliedScore.ToString();
            }
            else
            {
                comboScoreText.text = "";
            }
        }
        
        // Update timer fill
        if (comboTimerFillImage != null)
        {
            if (isComboActive)
            {
                comboTimerFillImage.fillAmount = comboTimer / comboTimerDuration;
            }
            else
            {
                comboTimerFillImage.fillAmount = 0f;
            }
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
    
    private System.Collections.IEnumerator ComboTextPopAnimation()
    {
        if (comboScoreTextRectTransform == null) yield break;
        
        isComboTextAnimating = true;
        float elapsed = 0f;
        
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            
            // Scale animation (pop out then back)
            float scale = 1f + (popScaleMultiplier - 1f) * Mathf.Sin(t * Mathf.PI);
            comboScoreTextRectTransform.localScale = comboScoreTextOriginalScale * scale;
            
            yield return null;
        }
        
        // Reset to original scale
        comboScoreTextRectTransform.localScale = comboScoreTextOriginalScale;
        
        isComboTextAnimating = false;
    }
    
    private System.Collections.IEnumerator MultiplierTextPopAnimation()
    {
        if (comboMultiplierTextRectTransform == null) yield break;
        
        isMultiplierTextAnimating = true;
        float elapsed = 0f;
        
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            
            // Scale animation (bigger pop for multiplier!)
            float scale = 1f + (multiplierPopScale - 1f) * Mathf.Sin(t * Mathf.PI);
            comboMultiplierTextRectTransform.localScale = comboMultiplierTextOriginalScale * scale;
            
            yield return null;
        }
        
        // Reset to original scale
        comboMultiplierTextRectTransform.localScale = comboMultiplierTextOriginalScale;
        
        isMultiplierTextAnimating = false;
    }
    
    // Public getters
    public int GetCurrentScore() => currentScore;
    
    /// <summary>
    /// Resets the score to zero
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        displayedScore = 0;
        UpdateScoreDisplay();
    }
    
    /// <summary>
    /// Called when player dies - ends combo without awarding points
    /// </summary>
    public void OnPlayerDeath()
    {
        if (isComboActive)
        {
            Debug.Log("Player died - combo lost!");
            isComboActive = false;
            comboTotalPoints = 0;
            displayedComboScore = 0;
            uniqueTricksInCombo.Clear();
            trickPointsInCombo.Clear();
            
            // Clear trick text
            if (trickText != null)
            {
                trickText.text = "";
            }
            
            UpdateComboUI();
        }
        
        // Reset trick timing
        lastWallJumpTime = -999f;
        lastDoubleJumpTime = -999f;
        lastSlideBoostJumpTime = -999f;
        secondToLastWallJumpTime = -999f;
        lastGroundedTime = Time.time;
        lastGroundOrWallTouchTime = Time.time;
        firstKillTime = -999f;
        killsInSpreeWindow = 0;
        firstJumpTime = -999f;
        jumpsInBHopWindow = 0;
    }
}
