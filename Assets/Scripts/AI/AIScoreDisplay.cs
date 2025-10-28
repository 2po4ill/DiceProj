using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using HybridEnemyAI;

/// <summary>
/// AI Score Display component - handles AI-specific score visualization and tracking
/// Requirements: 10.4
/// </summary>
public class AIScoreDisplay : MonoBehaviour
{
    [Header("AI Score Elements")]
    [SerializeField] private TextMeshProUGUI aiTotalScoreText;
    [SerializeField] private TextMeshProUGUI aiTurnScoreText;
    [SerializeField] private TextMeshProUGUI aiProjectedScoreText;
    [SerializeField] private TextMeshProUGUI aiScoreDifferenceText;
    
    [Header("Behavior Mode Display")]
    [SerializeField] private TextMeshProUGUI behaviorModeText;
    [SerializeField] private Image behaviorModeIndicator;
    [SerializeField] private TextMeshProUGUI pointsCapText;
    [SerializeField] private TextMeshProUGUI bufferCapText;
    
    [Header("Progress Indicators")]
    [SerializeField] private Slider turnProgressSlider;
    [SerializeField] private TextMeshProUGUI turnProgressText;
    [SerializeField] private Slider gameProgressSlider;
    [SerializeField] private TextMeshProUGUI gameProgressText;
    
    [Header("Visual Settings")]
    [SerializeField] private Color aiScoreColor = Color.cyan;
    [SerializeField] private Color playerScoreColor = Color.white;
    [SerializeField] private Color positiveScoreColor = Color.green;
    [SerializeField] private Color negativeScoreColor = Color.red;
    [SerializeField] private Color aggressiveColor = Color.red;
    [SerializeField] private Color passiveColor = Color.blue;
    
    [Header("Animation Settings")]
    [SerializeField] private float scoreAnimationSpeed = 2f;
    [SerializeField] private bool enableScoreAnimations = true;
    [SerializeField] private bool enableColorTransitions = true;
    [SerializeField] private float colorTransitionSpeed = 1f;
    
    [Header("Score Tracking")]
    [SerializeField] private int targetGameScore = 10000; // Target score for game completion
    [SerializeField] private bool showProjectedScore = true;
    [SerializeField] private bool showScoreDifference = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Component references
    private AIGameStateAnalyzer gameStateAnalyzer;
    private TurnScoreManager scoreManager;
    private GameTurnManager turnManager;
    
    // Score tracking
    private int currentAITotalScore = 0;
    private int currentPlayerScore = 0;
    private int currentTurnScore = 0;
    private int displayedAIScore = 0;
    private int displayedTurnScore = 0;
    
    // State tracking
    private BehaviorMode currentBehaviorMode = BehaviorMode.AGGRESSIVE;
    private int currentPointsCap = 0;
    private int currentBufferCap = 0;
    
    // Animation coroutines
    private Coroutine scoreAnimationCoroutine;
    private Coroutine colorTransitionCoroutine;
    
    void Start()
    {
        InitializeComponents();
        InitializeDisplay();
    }
    
    void Update()
    {
        if (enableScoreAnimations)
        {
            UpdateAnimatedScores();
        }
        else
        {
            UpdateScoresImmediate();
        }
    }
    
    void InitializeComponents()
    {
        // Find required components
        if (gameStateAnalyzer == null)
            gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
        
        if (scoreManager == null)
            scoreManager = FindObjectOfType<TurnScoreManager>();
        
        if (turnManager == null)
            turnManager = FindObjectOfType<GameTurnManager>();
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIScoreDisplay: Components initialized - " +
                     $"GameStateAnalyzer: {gameStateAnalyzer != null}, " +
                     $"ScoreManager: {scoreManager != null}, " +
                     $"TurnManager: {turnManager != null}");
        }
    }
    
    void InitializeDisplay()
    {
        // Set initial values
        UpdateAITotalScore(0);
        UpdateAITurnScore(0);
        UpdateScoreDifference(0);
        UpdateBehaviorMode(BehaviorMode.AGGRESSIVE);
        UpdatePointsCap(400);
        UpdateBufferCap(200);
        UpdateTurnProgress(0, 400);
        UpdateGameProgress(0, targetGameScore);
        
        if (enableDebugLogs)
            Debug.Log("AIScoreDisplay: Display initialized");
    }
    
    #region Score Update Methods
    
    /// <summary>
    /// Update AI total score display
    /// </summary>
    public void UpdateAITotalScore(int totalScore)
    {
        currentAITotalScore = totalScore;
        
        if (aiTotalScoreText != null)
        {
            if (enableScoreAnimations)
            {
                // Animation will be handled in Update()
            }
            else
            {
                aiTotalScoreText.text = $"AI: {totalScore:N0}";
                aiTotalScoreText.color = aiScoreColor;
                displayedAIScore = totalScore;
            }
        }
        
        // Update game progress
        UpdateGameProgress(totalScore, targetGameScore);
    }
    
    /// <summary>
    /// Update AI turn score display
    /// </summary>
    public void UpdateAITurnScore(int turnScore)
    {
        currentTurnScore = turnScore;
        
        if (aiTurnScoreText != null)
        {
            if (enableScoreAnimations)
            {
                // Animation will be handled in Update()
            }
            else
            {
                aiTurnScoreText.text = $"Turn: {turnScore:N0}";
                displayedTurnScore = turnScore;
            }
        }
        
        // Update turn progress
        UpdateTurnProgress(turnScore, currentPointsCap);
    }
    
    /// <summary>
    /// Update projected final score
    /// </summary>
    public void UpdateProjectedScore(int projectedScore)
    {
        if (aiProjectedScoreText != null && showProjectedScore)
        {
            aiProjectedScoreText.text = $"Projected: {projectedScore:N0}";
            
            // Color based on whether projected is higher than current
            if (projectedScore > currentAITotalScore)
                aiProjectedScoreText.color = positiveScoreColor;
            else
                aiProjectedScoreText.color = aiScoreColor;
        }
    }
    
    /// <summary>
    /// Update score difference display
    /// </summary>
    public void UpdateScoreDifference(int scoreDifference)
    {
        if (aiScoreDifferenceText != null && showScoreDifference)
        {
            string prefix = scoreDifference >= 0 ? "+" : "";
            aiScoreDifferenceText.text = $"{prefix}{scoreDifference:N0}";
            
            // Color based on positive/negative difference
            Color targetColor = scoreDifference >= 0 ? positiveScoreColor : negativeScoreColor;
            
            if (enableColorTransitions)
            {
                StartColorTransition(aiScoreDifferenceText, targetColor);
            }
            else
            {
                aiScoreDifferenceText.color = targetColor;
            }
        }
    }
    
    /// <summary>
    /// Update behavior mode display
    /// </summary>
    public void UpdateBehaviorMode(BehaviorMode mode)
    {
        currentBehaviorMode = mode;
        
        if (behaviorModeText != null)
        {
            behaviorModeText.text = $"Mode: {mode}";
            
            Color modeColor = mode == BehaviorMode.AGGRESSIVE ? aggressiveColor : passiveColor;
            
            if (enableColorTransitions)
            {
                StartColorTransition(behaviorModeText, modeColor);
            }
            else
            {
                behaviorModeText.color = modeColor;
            }
        }
        
        if (behaviorModeIndicator != null)
        {
            Color indicatorColor = mode == BehaviorMode.AGGRESSIVE ? aggressiveColor : passiveColor;
            
            if (enableColorTransitions)
            {
                StartColorTransition(behaviorModeIndicator, indicatorColor);
            }
            else
            {
                behaviorModeIndicator.color = indicatorColor;
            }
        }
    }
    
    /// <summary>
    /// Update points per turn cap display
    /// </summary>
    public void UpdatePointsCap(int pointsCap)
    {
        currentPointsCap = pointsCap;
        
        if (pointsCapText != null)
        {
            pointsCapText.text = $"Cap: {pointsCap:N0}";
        }
        
        // Update turn progress with new cap
        UpdateTurnProgress(currentTurnScore, pointsCap);
    }
    
    /// <summary>
    /// Update buffer cap display
    /// </summary>
    public void UpdateBufferCap(int bufferCap)
    {
        currentBufferCap = bufferCap;
        
        if (bufferCapText != null)
        {
            bufferCapText.text = $"Buffer: ±{bufferCap}";
        }
    }
    
    /// <summary>
    /// Update turn progress slider and text
    /// </summary>
    public void UpdateTurnProgress(int currentScore, int targetScore)
    {
        if (turnProgressSlider != null)
        {
            float progress = targetScore > 0 ? (float)currentScore / targetScore : 0f;
            turnProgressSlider.value = Mathf.Clamp01(progress);
            
            // Color the slider based on progress
            Image fillImage = turnProgressSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                Color fillColor;
                if (progress >= 1f)
                    fillColor = Color.yellow;      // Over cap
                else if (progress >= 0.8f)
                    fillColor = positiveScoreColor; // Good progress
                else if (progress >= 0.5f)
                    fillColor = Color.blue;        // Moderate progress
                else
                    fillColor = Color.gray;        // Low progress
                
                if (enableColorTransitions)
                {
                    StartColorTransition(fillImage, fillColor);
                }
                else
                {
                    fillImage.color = fillColor;
                }
            }
        }
        
        if (turnProgressText != null)
        {
            float percentage = targetScore > 0 ? (float)currentScore / targetScore * 100f : 0f;
            turnProgressText.text = $"{percentage:F0}%";
        }
    }
    
    /// <summary>
    /// Update game progress slider and text
    /// </summary>
    public void UpdateGameProgress(int currentScore, int targetScore)
    {
        if (gameProgressSlider != null)
        {
            float progress = targetScore > 0 ? (float)currentScore / targetScore : 0f;
            gameProgressSlider.value = Mathf.Clamp01(progress);
        }
        
        if (gameProgressText != null)
        {
            float percentage = targetScore > 0 ? (float)currentScore / targetScore * 100f : 0f;
            gameProgressText.text = $"Game: {percentage:F0}%";
        }
    }
    
    #endregion
    
    #region Animation Methods
    
    void UpdateAnimatedScores()
    {
        // Animate AI total score
        if (displayedAIScore != currentAITotalScore)
        {
            displayedAIScore = Mathf.RoundToInt(Mathf.Lerp(displayedAIScore, currentAITotalScore, Time.deltaTime * scoreAnimationSpeed));
            
            if (aiTotalScoreText != null)
            {
                aiTotalScoreText.text = $"AI: {displayedAIScore:N0}";
                aiTotalScoreText.color = aiScoreColor;
            }
        }
        
        // Animate turn score
        if (displayedTurnScore != currentTurnScore)
        {
            displayedTurnScore = Mathf.RoundToInt(Mathf.Lerp(displayedTurnScore, currentTurnScore, Time.deltaTime * scoreAnimationSpeed));
            
            if (aiTurnScoreText != null)
            {
                aiTurnScoreText.text = $"Turn: {displayedTurnScore:N0}";
            }
        }
    }
    
    void UpdateScoresImmediate()
    {
        displayedAIScore = currentAITotalScore;
        displayedTurnScore = currentTurnScore;
        
        if (aiTotalScoreText != null)
        {
            aiTotalScoreText.text = $"AI: {currentAITotalScore:N0}";
            aiTotalScoreText.color = aiScoreColor;
        }
        
        if (aiTurnScoreText != null)
        {
            aiTurnScoreText.text = $"Turn: {currentTurnScore:N0}";
        }
    }
    
    void StartColorTransition(TextMeshProUGUI textComponent, Color targetColor)
    {
        if (textComponent != null)
        {
            StartCoroutine(AnimateColorTransition(textComponent, targetColor));
        }
    }
    
    void StartColorTransition(Image imageComponent, Color targetColor)
    {
        if (imageComponent != null)
        {
            StartCoroutine(AnimateColorTransition(imageComponent, targetColor));
        }
    }
    
    IEnumerator AnimateColorTransition(TextMeshProUGUI textComponent, Color targetColor)
    {
        if (textComponent == null) yield break;
        
        Color startColor = textComponent.color;
        float duration = 1f / colorTransitionSpeed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            textComponent.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        textComponent.color = targetColor;
    }
    
    IEnumerator AnimateColorTransition(Image imageComponent, Color targetColor)
    {
        if (imageComponent == null) yield break;
        
        Color startColor = imageComponent.color;
        float duration = 1f / colorTransitionSpeed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            imageComponent.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        imageComponent.color = targetColor;
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Update all AI score information at once
    /// </summary>
    public void UpdateAllScores(int aiTotalScore, int playerScore, int turnScore, BehaviorMode mode, int pointsCap, int bufferCap)
    {
        currentPlayerScore = playerScore;
        
        UpdateAITotalScore(aiTotalScore);
        UpdateAITurnScore(turnScore);
        UpdateScoreDifference(aiTotalScore - playerScore);
        UpdateBehaviorMode(mode);
        UpdatePointsCap(pointsCap);
        UpdateBufferCap(bufferCap);
        
        // Update projected score
        int projectedScore = aiTotalScore + turnScore;
        UpdateProjectedScore(projectedScore);
    }
    
    /// <summary>
    /// Update from AI turn state
    /// </summary>
    public void UpdateFromTurnState(AITurnState turnState, int aiTotalScore, int playerScore)
    {
        if (turnState == null) return;
        
        UpdateAllScores(
            aiTotalScore,
            playerScore,
            turnState.CurrentTurnScore,
            turnState.CurrentMode,
            turnState.PointsPerTurnCap,
            gameStateAnalyzer?.GetCurrentBufferCap() ?? 200
        );
    }
    
    /// <summary>
    /// Set target game score
    /// </summary>
    public void SetTargetGameScore(int targetScore)
    {
        targetGameScore = targetScore;
        UpdateGameProgress(currentAITotalScore, targetScore);
        
        if (enableDebugLogs)
            Debug.Log($"AIScoreDisplay: Target game score set to {targetScore}");
    }
    
    /// <summary>
    /// Toggle score animations
    /// </summary>
    public void SetEnableScoreAnimations(bool enable)
    {
        enableScoreAnimations = enable;
        
        if (!enable)
        {
            // Update immediately if animations disabled
            UpdateScoresImmediate();
        }
        
        if (enableDebugLogs)
            Debug.Log($"AIScoreDisplay: Score animations set to {enable}");
    }
    
    /// <summary>
    /// Toggle color transitions
    /// </summary>
    public void SetEnableColorTransitions(bool enable)
    {
        enableColorTransitions = enable;
        
        if (enableDebugLogs)
            Debug.Log($"AIScoreDisplay: Color transitions set to {enable}");
    }
    
    /// <summary>
    /// Set animation speeds
    /// </summary>
    public void SetAnimationSpeeds(float scoreSpeed, float colorSpeed)
    {
        scoreAnimationSpeed = Mathf.Clamp(scoreSpeed, 0.1f, 10f);
        colorTransitionSpeed = Mathf.Clamp(colorSpeed, 0.1f, 5f);
        
        if (enableDebugLogs)
            Debug.Log($"AIScoreDisplay: Animation speeds set - Score: {scoreSpeed}, Color: {colorSpeed}");
    }
    
    /// <summary>
    /// Get current displayed scores
    /// </summary>
    public (int aiScore, int turnScore, int scoreDifference) GetCurrentScores()
    {
        return (displayedAIScore, displayedTurnScore, displayedAIScore - currentPlayerScore);
    }
    
    /// <summary>
    /// Reset all displays to initial state
    /// </summary>
    public void ResetDisplay()
    {
        currentAITotalScore = 0;
        currentPlayerScore = 0;
        currentTurnScore = 0;
        displayedAIScore = 0;
        displayedTurnScore = 0;
        
        InitializeDisplay();
        
        if (enableDebugLogs)
            Debug.Log("AIScoreDisplay: Display reset");
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Stop any running coroutines
        StopAllCoroutines();
    }
}