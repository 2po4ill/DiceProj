using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// Main AI UI Manager that handles all AI-specific UI elements and visual feedback
/// Requirements: 10.4, 8.5
/// </summary>
public class AIUIManager : MonoBehaviour
{
    [Header("AI Score Display")]
    [SerializeField] private TextMeshProUGUI aiTotalScoreText;
    [SerializeField] private TextMeshProUGUI aiTurnScoreText;
    [SerializeField] private TextMeshProUGUI aiScoreDifferenceText;
    [SerializeField] private TextMeshProUGUI aiBehaviorModeText;
    
    [Header("AI Turn Status")]
    [SerializeField] private TextMeshProUGUI aiTurnStatusText;
    [SerializeField] private TextMeshProUGUI aiIterationText;
    [SerializeField] private TextMeshProUGUI aiDecisionText;
    [SerializeField] private Slider aiTurnProgressSlider;
    
    [Header("AI Dice Display")]
    [SerializeField] private Transform aiDiceContainer;
    [SerializeField] private GameObject aiDicePrefab;
    [SerializeField] private AIDiceDisplaySystem diceDisplaySystem;
    
    [Header("AI Decision Feedback")]
    [SerializeField] private TextMeshProUGUI aiCombinationText;
    [SerializeField] private TextMeshProUGUI aiRiskAssessmentText;
    [SerializeField] private TextMeshProUGUI aiMomentumText;
    [SerializeField] private Image aiDecisionIndicator;
    
    [Header("Visual Settings")]
    [SerializeField] private Color aiActiveColor = Color.cyan;
    [SerializeField] private Color aiInactiveColor = Color.gray;
    [SerializeField] private Color aiAggressiveColor = Color.red;
    [SerializeField] private Color aiPassiveColor = Color.blue;
    [SerializeField] private Color aiContinueColor = Color.green;
    [SerializeField] private Color aiStopColor = Color.orange;
    
    [Header("Animation Settings")]
    [SerializeField] private float scoreUpdateSpeed = 2f;
    [SerializeField] private bool enableAnimations = true;
    [SerializeField] private float decisionFeedbackDuration = 2f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showDetailedDecisions = false;
    
    // Component references
    private AITurnExecutor aiTurnExecutor;
    private AIGameStateAnalyzer gameStateAnalyzer;
    private TurnScoreManager scoreManager;
    
    // Current state tracking
    private AITurnState currentTurnState;
    private int displayedAIScore = 0;
    private int displayedTurnScore = 0;
    private bool isAITurnActive = false;
    
    // Animation tracking
    private UnityEngine.Coroutine scoreAnimationCoroutine;
    private UnityEngine.Coroutine decisionFeedbackCoroutine;
    
    void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        InitializeUI();
    }
    
    void Update()
    {
        if (enableAnimations)
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
        // Find AI components
        if (aiTurnExecutor == null)
            aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        
        if (gameStateAnalyzer == null)
            gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
        
        if (scoreManager == null)
            scoreManager = FindObjectOfType<TurnScoreManager>();
        
        // Initialize dice display system
        if (diceDisplaySystem == null)
            diceDisplaySystem = GetComponent<AIDiceDisplaySystem>();
        
        if (diceDisplaySystem == null && aiDiceContainer != null)
        {
            diceDisplaySystem = gameObject.AddComponent<AIDiceDisplaySystem>();
            diceDisplaySystem.SetAIDiceContainer(aiDiceContainer);
            if (aiDicePrefab != null)
                diceDisplaySystem.SetAIDicePrefab(aiDicePrefab);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIManager: Components initialized - " +
                     $"TurnExecutor: {aiTurnExecutor != null}, " +
                     $"GameStateAnalyzer: {gameStateAnalyzer != null}, " +
                     $"ScoreManager: {scoreManager != null}");
        }
    }
    
    void SetupEventListeners()
    {
        if (aiTurnExecutor != null)
        {
            aiTurnExecutor.OnTurnStarted += HandleAITurnStarted;
            aiTurnExecutor.OnCombinationSelected += HandleCombinationSelected;
            aiTurnExecutor.OnDecisionMade += HandleDecisionMade;
            aiTurnExecutor.OnTurnCompleted += HandleAITurnCompleted;
            aiTurnExecutor.OnZonkOccurred += HandleZonkOccurred;
            
            if (enableDebugLogs)
                Debug.Log("AIUIManager: Event listeners set up");
        }
        else
        {
            Debug.LogWarning("AIUIManager: No AITurnExecutor found - events will not be handled");
        }
    }
    
    void InitializeUI()
    {
        // Set initial UI state
        SetAITurnActive(false);
        UpdateAIScoreDisplay(0, 0);
        UpdateBehaviorModeDisplay(BehaviorMode.AGGRESSIVE);
        UpdateTurnStatusDisplay("Waiting for turn...", 0, 0);
        ClearDecisionFeedback();
        
        if (enableDebugLogs)
            Debug.Log("AIUIManager: UI initialized");
    }
    
    #region Event Handlers
    
    void HandleAITurnStarted(AITurnState turnState)
    {
        currentTurnState = turnState;
        isAITurnActive = true;
        
        SetAITurnActive(true);
        UpdateBehaviorModeDisplay(turnState.CurrentMode);
        UpdateTurnStatusDisplay("AI Turn Active", turnState.IterationCount, turnState.MaxIterations);
        UpdateTurnProgressDisplay(turnState);
        
        // Display initial dice
        if (diceDisplaySystem != null && turnState.CurrentDice != null)
        {
            diceDisplaySystem.DisplayAIDice(turnState.CurrentDice);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIManager: AI turn started - Mode: {turnState.CurrentMode}, " +
                     $"Cap: {turnState.PointsPerTurnCap}, Dice: {turnState.CurrentDice?.Count ?? 0}");
        }
    }
    
    void HandleCombinationSelected(CombinationResult combination)
    {
        if (combination == null) return;
        
        // Update combination display
        UpdateCombinationDisplay(combination);
        
        // Update turn score
        if (currentTurnState != null)
        {
            UpdateTurnScoreDisplay(currentTurnState.CurrentTurnScore);
            UpdateTurnProgressDisplay(currentTurnState);
        }
        
        // Update dice display to show remaining dice
        if (diceDisplaySystem != null && currentTurnState?.CurrentDice != null)
        {
            diceDisplaySystem.UpdateRemainingDice(currentTurnState.CurrentDice);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIManager: Combination selected - {combination.rule}: {combination.points} pts");
        }
    }
    
    void HandleDecisionMade(AIStopDecision decision)
    {
        if (decision == null) return;
        
        // Update decision feedback
        UpdateDecisionFeedback(decision);
        
        // Update risk assessment display
        UpdateRiskAssessmentDisplay(decision);
        
        // Update momentum display
        if (currentTurnState != null)
        {
            UpdateMomentumDisplay(currentTurnState.SuccessfulCombinationsCount, decision);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIManager: Decision made - {(decision.ShouldStop ? "STOP" : "CONTINUE")}, " +
                     $"Combined: {decision.CombinedStopChance:P1}");
        }
    }
    
    void HandleAITurnCompleted(AITurnState turnState)
    {
        currentTurnState = turnState;
        isAITurnActive = false;
        
        SetAITurnActive(false);
        UpdateTurnStatusDisplay("Turn Complete", turnState.IterationCount, turnState.MaxIterations);
        UpdateTurnProgressDisplay(turnState);
        
        // Final score update
        UpdateTurnScoreDisplay(turnState.CurrentTurnScore);
        
        // Clear dice display after a delay
        if (diceDisplaySystem != null)
        {
            StartCoroutine(ClearDiceDisplayDelayed(2f));
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIManager: AI turn completed - Final Score: {turnState.CurrentTurnScore}, " +
                     $"Iterations: {turnState.IterationCount}");
        }
    }
    
    void HandleZonkOccurred()
    {
        // Show Zonk feedback
        UpdateCombinationDisplay(new CombinationResult(Rule.Zonk, 0, "ZONK - All progress lost!", 0f));
        UpdateDecisionText("ZONK!", aiStopColor);
        
        // Reset turn score display
        UpdateTurnScoreDisplay(0);
        
        if (enableDebugLogs)
            Debug.Log("AIUIManager: Zonk occurred");
    }
    
    #endregion
    
    #region UI Update Methods
    
    void SetAITurnActive(bool active)
    {
        Color targetColor = active ? aiActiveColor : aiInactiveColor;
        
        // Update all AI UI elements with active/inactive color
        if (aiTurnStatusText != null)
            aiTurnStatusText.color = targetColor;
        
        if (aiBehaviorModeText != null)
            aiBehaviorModeText.color = targetColor;
        
        if (aiIterationText != null)
            aiIterationText.color = targetColor;
    }
    
    void UpdateAIScoreDisplay(int totalScore, int scoreDifference)
    {
        if (aiTotalScoreText != null)
            aiTotalScoreText.text = $"AI Score: {totalScore}";
        
        if (aiScoreDifferenceText != null)
        {
            string prefix = scoreDifference >= 0 ? "+" : "";
            Color diffColor = scoreDifference >= 0 ? Color.green : Color.red;
            
            aiScoreDifferenceText.text = $"{prefix}{scoreDifference}";
            aiScoreDifferenceText.color = diffColor;
        }
    }
    
    void UpdateTurnScoreDisplay(int turnScore)
    {
        if (aiTurnScoreText != null)
            aiTurnScoreText.text = $"Turn: {turnScore}";
    }
    
    void UpdateBehaviorModeDisplay(BehaviorMode mode)
    {
        if (aiBehaviorModeText != null)
        {
            aiBehaviorModeText.text = $"Mode: {mode}";
            aiBehaviorModeText.color = mode == BehaviorMode.AGGRESSIVE ? aiAggressiveColor : aiPassiveColor;
        }
    }
    
    void UpdateTurnStatusDisplay(string status, int currentIteration, int maxIterations)
    {
        if (aiTurnStatusText != null)
            aiTurnStatusText.text = status;
        
        if (aiIterationText != null)
            aiIterationText.text = $"Iteration: {currentIteration}/{maxIterations}";
    }
    
    void UpdateTurnProgressDisplay(AITurnState turnState)
    {
        if (aiTurnProgressSlider != null && turnState != null)
        {
            float progress = turnState.PointsPerTurnCap > 0 ? 
                (float)turnState.CurrentTurnScore / turnState.PointsPerTurnCap : 0f;
            
            aiTurnProgressSlider.value = Mathf.Clamp01(progress);
            
            // Color code the slider based on progress
            Image fillImage = aiTurnProgressSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                if (progress >= 1f)
                    fillImage.color = Color.yellow; // Over cap
                else if (progress >= 0.8f)
                    fillImage.color = Color.green;  // Good progress
                else if (progress >= 0.5f)
                    fillImage.color = Color.blue;   // Moderate progress
                else
                    fillImage.color = Color.gray;   // Low progress
            }
        }
    }
    
    void UpdateCombinationDisplay(CombinationResult combination)
    {
        if (aiCombinationText != null && combination != null)
        {
            if (combination.rule == Rule.Zonk)
            {
                aiCombinationText.text = "ZONK!";
                aiCombinationText.color = Color.red;
            }
            else
            {
                aiCombinationText.text = $"{combination.rule}: {combination.points} pts";
                aiCombinationText.color = Color.white;
            }
        }
    }
    
    void UpdateDecisionFeedback(AIStopDecision decision)
    {
        if (decision == null) return;
        
        string decisionText = decision.ShouldStop ? "STOP" : "CONTINUE";
        Color decisionColor = decision.ShouldStop ? aiStopColor : aiContinueColor;
        
        UpdateDecisionText(decisionText, decisionColor);
        UpdateDecisionIndicator(decision.ShouldStop);
        
        // Show detailed decision info if enabled
        if (showDetailedDecisions && aiDecisionText != null)
        {
            aiDecisionText.text = $"{decisionText}\n" +
                                 $"Momentum: {decision.MomentumStopChance:P1}\n" +
                                 $"Cap: {decision.CapStopChance:P1}\n" +
                                 $"Combined: {decision.CombinedStopChance:P1}";
        }
        
        // Start feedback duration timer
        if (decisionFeedbackCoroutine != null)
            StopCoroutine(decisionFeedbackCoroutine);
        
        decisionFeedbackCoroutine = StartCoroutine(ClearDecisionFeedbackDelayed(decisionFeedbackDuration));
    }
    
    void UpdateDecisionText(string text, Color color)
    {
        if (aiDecisionText != null)
        {
            aiDecisionText.text = text;
            aiDecisionText.color = color;
        }
    }
    
    void UpdateDecisionIndicator(bool shouldStop)
    {
        if (aiDecisionIndicator != null)
        {
            aiDecisionIndicator.color = shouldStop ? aiStopColor : aiContinueColor;
            
            // Add pulsing effect
            StartCoroutine(PulseDecisionIndicator());
        }
    }
    
    void UpdateRiskAssessmentDisplay(AIStopDecision decision)
    {
        if (aiRiskAssessmentText != null && decision != null)
        {
            string riskText = $"Risk Analysis:\n" +
                             $"Momentum: {decision.MomentumStopChance:P0}\n" +
                             $"Cap: {decision.CapStopChance:P0}\n" +
                             $"Combined: {decision.CombinedStopChance:P0}";
            
            aiRiskAssessmentText.text = riskText;
            
            // Color based on combined risk
            if (decision.CombinedStopChance > 0.7f)
                aiRiskAssessmentText.color = Color.red;
            else if (decision.CombinedStopChance > 0.4f)
                aiRiskAssessmentText.color = Color.yellow;
            else
                aiRiskAssessmentText.color = Color.green;
        }
    }
    
    void UpdateMomentumDisplay(int successCount, AIStopDecision decision)
    {
        if (aiMomentumText != null)
        {
            string momentumText = $"Momentum:\n" +
                                 $"Successes: {successCount}\n" +
                                 $"Hot Streak: {(successCount >= 3 ? "YES" : "NO")}";
            
            if (decision != null)
            {
                momentumText += $"\nMomentum Effect: {(1f - decision.MomentumStopChance):P0}";
            }
            
            aiMomentumText.text = momentumText;
            
            // Color based on momentum strength
            if (successCount >= 3)
                aiMomentumText.color = Color.green;
            else if (successCount >= 1)
                aiMomentumText.color = Color.yellow;
            else
                aiMomentumText.color = Color.gray;
        }
    }
    
    void ClearDecisionFeedback()
    {
        if (aiDecisionText != null)
        {
            aiDecisionText.text = "";
            aiDecisionText.color = Color.white;
        }
        
        if (aiDecisionIndicator != null)
            aiDecisionIndicator.color = aiInactiveColor;
        
        if (aiRiskAssessmentText != null)
        {
            aiRiskAssessmentText.text = "Risk Analysis:\nWaiting...";
            aiRiskAssessmentText.color = Color.gray;
        }
        
        if (aiMomentumText != null)
        {
            aiMomentumText.text = "Momentum:\nWaiting...";
            aiMomentumText.color = Color.gray;
        }
    }
    
    #endregion
    
    #region Animation Methods
    
    void UpdateAnimatedScores()
    {
        // Get target scores
        int targetAIScore = GetCurrentAIScore();
        int targetTurnScore = currentTurnState?.CurrentTurnScore ?? 0;
        
        // Animate AI total score
        if (displayedAIScore != targetAIScore)
        {
            displayedAIScore = Mathf.RoundToInt(Mathf.Lerp(displayedAIScore, targetAIScore, Time.deltaTime * scoreUpdateSpeed));
        }
        
        // Animate turn score
        if (displayedTurnScore != targetTurnScore)
        {
            displayedTurnScore = Mathf.RoundToInt(Mathf.Lerp(displayedTurnScore, targetTurnScore, Time.deltaTime * scoreUpdateSpeed));
        }
        
        // Update displays with animated values
        int playerScore = GetCurrentPlayerScore();
        int scoreDifference = displayedAIScore - playerScore;
        
        UpdateAIScoreDisplay(displayedAIScore, scoreDifference);
        UpdateTurnScoreDisplay(displayedTurnScore);
    }
    
    void UpdateScoresImmediate()
    {
        int aiScore = GetCurrentAIScore();
        int playerScore = GetCurrentPlayerScore();
        int turnScore = currentTurnState?.CurrentTurnScore ?? 0;
        int scoreDifference = aiScore - playerScore;
        
        displayedAIScore = aiScore;
        displayedTurnScore = turnScore;
        
        UpdateAIScoreDisplay(aiScore, scoreDifference);
        UpdateTurnScoreDisplay(turnScore);
    }
    
    System.Collections.IEnumerator PulseDecisionIndicator()
    {
        if (aiDecisionIndicator == null) yield break;
        
        Color originalColor = aiDecisionIndicator.color;
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0.3f, Mathf.PingPong(elapsed * 4f, 1f));
            
            Color pulsedColor = originalColor;
            pulsedColor.a = alpha;
            aiDecisionIndicator.color = pulsedColor;
            
            yield return null;
        }
        
        aiDecisionIndicator.color = originalColor;
    }
    
    System.Collections.IEnumerator ClearDecisionFeedbackDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearDecisionFeedback();
    }
    
    System.Collections.IEnumerator ClearDiceDisplayDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (diceDisplaySystem != null)
            diceDisplaySystem.ClearAIDiceDisplay();
    }
    
    #endregion
    
    #region Utility Methods
    
    int GetCurrentAIScore()
    {
        // This would typically come from a game manager or score tracking system
        // For now, return a placeholder or get from score manager
        if (scoreManager != null)
        {
            // Assuming AI score is tracked separately or we need to implement AI score tracking
            return 0; // Placeholder - needs integration with actual AI score tracking
        }
        return 0;
    }
    
    int GetCurrentPlayerScore()
    {
        if (scoreManager != null)
            return scoreManager.totalGameScore;
        return 0;
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Manually update AI score display (for external integration)
    /// </summary>
    public void SetAIScore(int totalScore, int playerScore)
    {
        int scoreDifference = totalScore - playerScore;
        UpdateAIScoreDisplay(totalScore, scoreDifference);
        displayedAIScore = totalScore;
    }
    
    /// <summary>
    /// Toggle detailed decision display
    /// </summary>
    public void SetShowDetailedDecisions(bool show)
    {
        showDetailedDecisions = show;
        if (enableDebugLogs)
            Debug.Log($"AIUIManager: Detailed decisions display set to {show}");
    }
    
    /// <summary>
    /// Toggle animations
    /// </summary>
    public void SetEnableAnimations(bool enable)
    {
        enableAnimations = enable;
        if (enableDebugLogs)
            Debug.Log($"AIUIManager: Animations set to {enable}");
    }
    
    /// <summary>
    /// Get current AI turn state for external monitoring
    /// </summary>
    public AITurnState GetCurrentTurnState()
    {
        return currentTurnState;
    }
    
    /// <summary>
    /// Check if AI turn is currently active
    /// </summary>
    public bool IsAITurnActive()
    {
        return isAITurnActive;
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (aiTurnExecutor != null)
        {
            aiTurnExecutor.OnTurnStarted -= HandleAITurnStarted;
            aiTurnExecutor.OnCombinationSelected -= HandleCombinationSelected;
            aiTurnExecutor.OnDecisionMade -= HandleDecisionMade;
            aiTurnExecutor.OnTurnCompleted -= HandleAITurnCompleted;
            aiTurnExecutor.OnZonkOccurred -= HandleZonkOccurred;
        }
        
        // Stop any running coroutines
        if (scoreAnimationCoroutine != null)
            StopCoroutine(scoreAnimationCoroutine);
        
        if (decisionFeedbackCoroutine != null)
            StopCoroutine(decisionFeedbackCoroutine);
    }
}