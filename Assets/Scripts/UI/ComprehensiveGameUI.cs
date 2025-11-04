using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// Comprehensive UI system for tracking all player and AI metrics
/// </summary>
public class ComprehensiveGameUI : MonoBehaviour
{
    [Header("=== PLAYER TRACKING ===")]
    [Header("Player Score Panel")]
    public TextMeshProUGUI playerCurrentTurnScore;
    public TextMeshProUGUI playerTotalScore;
    public TextMeshProUGUI playerTurnNumber;
    public TextMeshProUGUI playerTurnMultiplier;
    public TextMeshProUGUI playerConsecutiveStreaks;
    public TextMeshProUGUI playerProjectedScore;
    
    [Header("Player Turn Progress")]
    public Slider playerTurnProgressBar;
    public TextMeshProUGUI playerCombinationsCount;
    public TextMeshProUGUI playerDiceRemaining;
    public Transform playerCombinationHistory;
    public GameObject combinationEntryPrefab;
    
    [Header("=== AI TRACKING ===")]
    [Header("AI Score Panel")]
    public TextMeshProUGUI aiCurrentTurnScore;
    public TextMeshProUGUI aiTotalScore;
    public TextMeshProUGUI aiScoreDifference; // vs player
    public TextMeshProUGUI aiBehaviorMode;
    public Image aiBehaviorModeIndicator;
    
    [Header("AI Strategy Panel")]
    public TextMeshProUGUI aiIterationCount;
    public TextMeshProUGUI aiMaxIterations;
    public TextMeshProUGUI aiPointsCap;
    public TextMeshProUGUI aiSuccessfulCombinations;
    public Slider aiIterationProgressBar;
    
    [Header("AI Decision Analysis")]
    public TextMeshProUGUI aiCurrentDecision;
    public TextMeshProUGUI aiDecisionReason;
    public TextMeshProUGUI aiZonkProbability;
    public TextMeshProUGUI aiMomentumStopChance;
    public TextMeshProUGUI aiCapStopChance;
    public TextMeshProUGUI aiCombinedStopChance;
    
    [Header("AI Risk Assessment")]
    public Slider aiZonkRiskSlider;
    public Slider aiMomentumRiskSlider;
    public Slider aiCapRiskSlider;
    public Image aiOverallRiskIndicator;
    
    [Header("=== TURN COMPARISON ===")]
    [Header("Head-to-Head Display")]
    public TextMeshProUGUI turnWinner;
    public TextMeshProUGUI scoreDifference;
    public Transform turnHistoryPanel;
    public GameObject turnHistoryEntryPrefab;
    
    [Header("=== REAL-TIME TRACKING ===")]
    [Header("Live Action Feed")]
    public Transform actionFeedPanel;
    public GameObject actionFeedEntryPrefab;
    public ScrollRect actionFeedScrollRect;
    
    [Header("Current Action Display")]
    public TextMeshProUGUI currentPlayerAction;
    public TextMeshProUGUI currentAIAction;
    public TextMeshProUGUI gamePhaseIndicator;
    
    [Header("=== STATISTICS ===")]
    [Header("Game Statistics")]
    public TextMeshProUGUI totalTurnsPlayed;
    public TextMeshProUGUI averagePlayerScore;
    public TextMeshProUGUI averageAIScore;
    public TextMeshProUGUI playerWinRate;
    public TextMeshProUGUI longestPlayerStreak;
    public TextMeshProUGUI longestAIStreak;
    
    [Header("=== VISUAL SETTINGS ===")]
    [Header("Color Coding")]
    public Color playerColor = Color.blue;
    public Color aiColor = Color.red;
    public Color aggressiveColor = Color.orange;
    public Color passiveColor = Color.green;
    public Color winningColor = Color.gold;
    public Color losingColor = Color.gray;
    
    [Header("Animation")]
    public float updateSpeed = 2f;
    public bool enableAnimations = true;
    
    // Data tracking
    private TurnScoreManager scoreManager;
    private GameTurnManager turnManager;
    private AITurnExecutor aiExecutor;
    private List<string> actionHistory = new List<string>();
    private List<TurnResult> turnResults = new List<TurnResult>();
    
    [System.Serializable]
    public class TurnResult
    {
        public int turnNumber;
        public int playerScore;
        public int aiScore;
        public string winner;
        public int scoreDifference;
    }
    
    void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        InitializeUI();
    }
    
    void InitializeComponents()
    {
        scoreManager = FindObjectOfType<TurnScoreManager>();
        turnManager = FindObjectOfType<GameTurnManager>();
        aiExecutor = FindObjectOfType<AITurnExecutor>();
        
        if (scoreManager == null) Debug.LogError("TurnScoreManager not found!");
        if (turnManager == null) Debug.LogError("GameTurnManager not found!");
        if (aiExecutor == null) Debug.LogError("AITurnExecutor not found!");
    }
    
    void SetupEventListeners()
    {
        if (aiExecutor != null)
        {
            aiExecutor.OnTurnStarted += UpdateAITurnStart;
            aiExecutor.OnCombinationSelected += UpdateAICombination;
            aiExecutor.OnDecisionMade += UpdateAIDecision;
            aiExecutor.OnTurnCompleted += UpdateAITurnComplete;
        }
    }
    
    void Update()
    {
        UpdatePlayerUI();
        UpdateAIUI();
        UpdateComparisonUI();
        UpdateRealTimeTracking();
    }
    
    // === PLAYER UI UPDATES ===
    void UpdatePlayerUI()
    {
        if (scoreManager == null || turnManager == null) return;
        
        // Basic scores
        UpdateText(playerCurrentTurnScore, scoreManager.GetCurrentTurnScore().ToString());
        UpdateText(playerTotalScore, turnManager.playerScore.ToString());
        UpdateText(playerTurnNumber, turnManager.currentTurn.ToString());
        UpdateText(playerTurnMultiplier, $"{scoreManager.GetCurrentTurnMultiplier():F2}x");
        UpdateText(playerConsecutiveStreaks, scoreManager.consecutiveSuccessfulTurns.ToString());
        UpdateText(playerProjectedScore, scoreManager.GetProjectedFinalScore().ToString());
        
        // Turn progress
        if (playerTurnProgressBar != null)
        {
            float progress = Mathf.Clamp01(scoreManager.GetCurrentTurnScore() / 500f);
            playerTurnProgressBar.value = progress;
        }
        
        UpdateText(playerCombinationsCount, scoreManager.currentTurn?.combinations?.Count.ToString() ?? "0");
        UpdateText(playerDiceRemaining, turnManager.diceController?.GetRemainingDice()?.Count.ToString() ?? "6");
    }
    
    // === AI UI UPDATES ===
    void UpdateAIUI()
    {
        if (aiExecutor == null) return;
        
        var aiState = aiExecutor.GetCurrentTurnState();
        if (aiState == null) return;
        
        // Basic AI scores
        UpdateText(aiCurrentTurnScore, aiState.CurrentTurnScore.ToString());
        UpdateText(aiTotalScore, turnManager.aiScore.ToString());
        
        // Score difference
        int difference = turnManager.aiScore - turnManager.playerScore;
        UpdateText(aiScoreDifference, $"{(difference >= 0 ? "+" : "")}{difference}");
        
        // Behavior mode
        UpdateText(aiBehaviorMode, aiState.CurrentMode.ToString());
        if (aiBehaviorModeIndicator != null)
        {
            aiBehaviorModeIndicator.color = aiState.CurrentMode == BehaviorMode.AGGRESSIVE ? aggressiveColor : passiveColor;
        }
        
        // Strategy info
        UpdateText(aiIterationCount, aiState.IterationCount.ToString());
        UpdateText(aiMaxIterations, aiState.MaxIterations.ToString());
        UpdateText(aiPointsCap, aiState.PointsPerTurnCap.ToString());
        UpdateText(aiSuccessfulCombinations, aiState.SuccessfulCombinationsCount.ToString());
        
        // Progress bar
        if (aiIterationProgressBar != null)
        {
            float progress = (float)aiState.IterationCount / aiState.MaxIterations;
            aiIterationProgressBar.value = progress;
        }
        
        // Risk analysis
        UpdateText(aiZonkProbability, $"{aiState.ZonkProbability:P1}");
        UpdateText(aiMomentumStopChance, $"{aiState.CurrentMomentumStopChance:P1}");
        UpdateText(aiCapStopChance, $"{aiState.CurrentCapStopChance:P1}");
        UpdateText(aiCombinedStopChance, $"{aiState.CombinedStopChance:P1}");
        
        // Risk sliders
        UpdateSlider(aiZonkRiskSlider, aiState.ZonkProbability);
        UpdateSlider(aiMomentumRiskSlider, aiState.CurrentMomentumStopChance);
        UpdateSlider(aiCapRiskSlider, aiState.CurrentCapStopChance);
        
        // Overall risk indicator
        if (aiOverallRiskIndicator != null)
        {
            float risk = aiState.CombinedStopChance;
            aiOverallRiskIndicator.color = Color.Lerp(Color.green, Color.red, risk);
        }
    }
    
    // === COMPARISON UI ===
    void UpdateComparisonUI()
    {
        if (turnManager == null) return;
        
        // Determine current leader
        string leader = turnManager.playerScore > turnManager.aiScore ? "PLAYER" : 
                       turnManager.aiScore > turnManager.playerScore ? "AI" : "TIE";
        UpdateText(turnWinner, leader);
        
        // Score difference
        int diff = Mathf.Abs(turnManager.playerScore - turnManager.aiScore);
        UpdateText(scoreDifference, diff.ToString());
    }
    
    // === REAL-TIME TRACKING ===
    void UpdateRealTimeTracking()
    {
        // Update current phase
        string phase = turnManager.isAITurn ? "AI Turn" : "Player Turn";
        UpdateText(gamePhaseIndicator, phase);
        
        // Update current actions
        if (turnManager.isAITurn)
        {
            UpdateText(currentAIAction, GetCurrentAIAction());
            UpdateText(currentPlayerAction, "Waiting...");
        }
        else
        {
            UpdateText(currentPlayerAction, GetCurrentPlayerAction());
            UpdateText(currentAIAction, "Waiting...");
        }
    }
    
    // === EVENT HANDLERS ===
    void UpdateAITurnStart(AITurnState state)
    {
        AddActionFeedEntry($"AI Turn Started - Mode: {state.CurrentMode}");
    }
    
    void UpdateAICombination(CombinationResult combination)
    {
        AddActionFeedEntry($"AI Selected: {combination.description} (+{combination.points})");
    }
    
    void UpdateAIDecision(AIStopDecision decision)
    {
        string action = decision.ShouldStop ? "STOP" : "CONTINUE";
        AddActionFeedEntry($"AI Decision: {action} - {decision.DecisionReason}");
        UpdateText(aiCurrentDecision, action);
        UpdateText(aiDecisionReason, decision.DecisionReason);
    }
    
    void UpdateAITurnComplete(AITurnState state)
    {
        AddActionFeedEntry($"AI Turn Complete - Final Score: {state.CurrentTurnScore}");
    }
    
    // === UTILITY METHODS ===
    void UpdateText(TextMeshProUGUI textComponent, string value)
    {
        if (textComponent != null) textComponent.text = value;
    }
    
    void UpdateSlider(Slider slider, float value)
    {
        if (slider != null) slider.value = Mathf.Clamp01(value);
    }
    
    void AddActionFeedEntry(string message)
    {
        actionHistory.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        
        // Keep only last 20 entries
        if (actionHistory.Count > 20)
            actionHistory.RemoveAt(0);
        
        UpdateActionFeed();
    }
    
    void UpdateActionFeed()
    {
        // Implementation would create UI entries for each action
        // This would populate the actionFeedPanel with recent actions
    }
    
    string GetCurrentAIAction()
    {
        if (aiExecutor == null || !aiExecutor.IsTurnActive()) return "Idle";
        
        var state = aiExecutor.GetCurrentTurnState();
        return $"Iteration {state.IterationCount}/{state.MaxIterations} - Analyzing...";
    }
    
    string GetCurrentPlayerAction()
    {
        if (turnManager.isAITurn) return "Waiting for AI";
        
        // Check game state to determine player action
        // This would need to be connected to the actual game state
        return "Rolling dice...";
    }
    
    void InitializeUI()
    {
        // Set initial values
        UpdateText(gamePhaseIndicator, "Game Starting...");
        
        // Initialize sliders
        if (aiZonkRiskSlider != null) aiZonkRiskSlider.value = 0;
        if (aiMomentumRiskSlider != null) aiMomentumRiskSlider.value = 0;
        if (aiCapRiskSlider != null) aiCapRiskSlider.value = 0;
    }
}