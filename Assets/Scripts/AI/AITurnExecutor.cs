using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// Manages complete AI turn flow with momentum tracking and iteration control
/// Handles turn execution from start to finish including cleanup and state management
/// </summary>
public class AITurnExecutor : MonoBehaviour
{
    [Header("AI Components")]
    public AIGameStateAnalyzer gameStateAnalyzer;
    public AICombinationStrategy combinationStrategy;
    public AIDecisionEngine decisionEngine;
    public AIDiceGenerator diceGenerator;
    public AIRiskCalculator riskCalculator;
    public AIAggressiveRerollStrategy aggressiveRerollStrategy;
    public AIDualProbabilityCapSystem dualProbabilityCapSystem;
    
    [Header("Game Components")]
    public TurnScoreManager scoreManager;
    public DiceCombinationDetector combinationDetector;
    
    [Header("AI Configuration")]
    public AIConfiguration aiConfig;
    
    [Header("Current Turn State")]
    public AITurnState currentTurnState;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showDetailedDecisions = false;
    
    // Events for UI and game integration
    public System.Action<AITurnState> OnTurnStarted;
    public System.Action<CombinationResult> OnCombinationSelected;
    public System.Action<AIStopDecision> OnDecisionMade;
    public System.Action<AITurnState> OnTurnCompleted;
    public System.Action OnZonkOccurred;
    
    private bool isTurnActive = false;
    private Coroutine currentTurnCoroutine;
    
    void Awake()
    {
        // Initialize turn state
        if (currentTurnState == null)
            currentTurnState = new AITurnState();
            
        // Initialize AI configuration if not set
        if (aiConfig == null)
            aiConfig = new AIConfiguration();
    }
    
    void Start()
    {
        // Get components if not assigned
        InitializeComponents();
    }
    
    void InitializeComponents()
    {
        if (gameStateAnalyzer == null) gameStateAnalyzer = GetComponent<AIGameStateAnalyzer>();
        if (combinationStrategy == null) combinationStrategy = GetComponent<AICombinationStrategy>();
        if (decisionEngine == null) decisionEngine = GetComponent<AIDecisionEngine>();
        if (diceGenerator == null) diceGenerator = GetComponent<AIDiceGenerator>();
        if (riskCalculator == null) riskCalculator = GetComponent<AIRiskCalculator>();
        if (aggressiveRerollStrategy == null) aggressiveRerollStrategy = GetComponent<AIAggressiveRerollStrategy>();
        if (dualProbabilityCapSystem == null) dualProbabilityCapSystem = GetComponent<AIDualProbabilityCapSystem>();
        if (scoreManager == null) scoreManager = FindObjectOfType<TurnScoreManager>();
        if (combinationDetector == null) combinationDetector = FindObjectOfType<DiceCombinationDetector>();
        
        // Validate critical components
        if (gameStateAnalyzer == null || combinationStrategy == null || decisionEngine == null)
        {
            Debug.LogError("AITurnExecutor: Missing critical AI components!");
        }
    }
    
    /// <summary>
    /// Starts a complete AI turn with momentum tracking
    /// </summary>
    public void StartAITurn(int turnNumber)
    {
        if (isTurnActive)
        {
            Debug.LogWarning("AI turn already active! Cannot start new turn.");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"=== AI TURN {turnNumber} START ===");
        
        // Initialize turn state
        InitializeTurnState(turnNumber);
        
        // Start turn execution coroutine
        currentTurnCoroutine = StartCoroutine(ExecuteTurnFlow());
    }
    
    /// <summary>
    /// Forces AI turn to end (for emergency stops)
    /// </summary>
    public void ForceEndTurn()
    {
        if (currentTurnCoroutine != null)
        {
            StopCoroutine(currentTurnCoroutine);
            currentTurnCoroutine = null;
        }
        
        CompleteTurn();
    }
    
    void InitializeTurnState(int turnNumber)
    {
        // Reset turn state
        currentTurnState.Reset();
        
        // Analyze game state and set behavior mode
        // Get current scores from turn manager
        var turnManager = FindObjectOfType<GameTurnManager>();
        int aiScore = turnManager != null ? turnManager.aiScore : 0;
        int playerScore = turnManager != null ? turnManager.playerScore : 0;
        
        currentTurnState.CurrentMode = gameStateAnalyzer.AnalyzeGameState(aiScore, playerScore);
        
        // Set points per turn cap based on behavior mode
        currentTurnState.PointsPerTurnCap = gameStateAnalyzer.GetPointsPerTurnCap(currentTurnState.CurrentMode);
        
        // Set max iterations based on behavior mode
        currentTurnState.MaxIterations = currentTurnState.CurrentMode == BehaviorMode.AGGRESSIVE ? 5 : 2;
        
        // Generate initial dice
        currentTurnState.CurrentDice = diceGenerator.GenerateRandomDice(6);
        
        isTurnActive = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"AI Turn State Initialized:");
            Debug.Log($"  Behavior Mode: {currentTurnState.CurrentMode}");
            Debug.Log($"  Points Cap: {currentTurnState.PointsPerTurnCap}");
            Debug.Log($"  Max Iterations: {currentTurnState.MaxIterations}");
            Debug.Log($"  Initial Dice: [{string.Join(",", currentTurnState.CurrentDice)}]");
        }
        
        // Notify listeners
        OnTurnStarted?.Invoke(currentTurnState);
    }
    
    /// <summary>
    /// Main turn execution flow with momentum tracking
    /// </summary>
    IEnumerator ExecuteTurnFlow()
    {
        // Check if we should use aggressive reroll strategy
        if (currentTurnState.CurrentMode == BehaviorMode.AGGRESSIVE && aggressiveRerollStrategy != null)
        {
            yield return StartCoroutine(ExecuteAggressiveTurnFlow());
        }
        else
        {
            yield return StartCoroutine(ExecuteStandardTurnFlow());
        }
        
        CompleteTurn();
    }
    
    /// <summary>
    /// Executes aggressive turn flow using aggressive reroll strategy
    /// </summary>
    IEnumerator ExecuteAggressiveTurnFlow()
    {
        if (enableDebugLogs)
            Debug.Log("=== EXECUTING AGGRESSIVE TURN FLOW ===");
        
        // Set up dual probability cap system
        if (dualProbabilityCapSystem != null)
        {
            dualProbabilityCapSystem.SetDynamicCap(BehaviorMode.AGGRESSIVE);
        }
        
        // Execute aggressive reroll strategy
        var aggressiveResult = aggressiveRerollStrategy.ExecuteAggressiveReroll(
            currentTurnState.CurrentDice,
            currentTurnState.CurrentMode,
            currentTurnState.CurrentTurnScore,
            currentTurnState.PointsPerTurnCap
        );
        
        // Process aggressive reroll results
        ProcessAggressiveRerollResult(aggressiveResult);
        
        // Small delay for visual feedback
        yield return new WaitForSeconds(0.5f);
    }
    
    /// <summary>
    /// Executes standard turn flow for non-aggressive modes
    /// </summary>
    IEnumerator ExecuteStandardTurnFlow()
    {
        while (isTurnActive && currentTurnState.IterationCount < currentTurnState.MaxIterations)
        {
            currentTurnState.IterationCount++;
            
            if (enableDebugLogs)
                Debug.Log($"--- AI Iteration {currentTurnState.IterationCount} ---");
            
            // Check for Zonk first
            if (!combinationDetector.HasAnyCombination(currentTurnState.CurrentDice))
            {
                HandleZonk();
                yield break;
            }
            
            // Find and select best combination
            var selectedCombination = SelectBestCombination();
            if (selectedCombination == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("No valid combination found despite HasAnyCombination returning true!");
                HandleZonk();
                yield break;
            }
            
            // Process the combination
            ProcessCombination(selectedCombination);
            
            // Check if all dice used (hot streak)
            if (GetRemainingDiceCount() == 0)
            {
                if (enableDebugLogs)
                    Debug.Log("All dice used! Hot streak - generating new dice set.");
                
                // Generate new full set of dice for hot streak
                currentTurnState.CurrentDice = diceGenerator.GenerateRandomDice(6);
                
                // Small delay for dramatic effect
                yield return new WaitForSeconds(0.5f);
                continue;
            }
            
            // Make continue/stop decision using dual probability system
            var stopDecision = MakeStopDecision();
            
            if (stopDecision.ShouldStop)
            {
                if (enableDebugLogs)
                    Debug.Log($"AI decides to STOP: {stopDecision.DecisionReason}");
                break;
            }
            
            if (enableDebugLogs)
                Debug.Log($"AI decides to CONTINUE: {stopDecision.DecisionReason}");
            
            // Generate new dice for remaining positions
            RegenerateRemainingDice();
            
            // Small delay between iterations for visual clarity
            yield return new WaitForSeconds(0.3f);
        }
        
        // Check if we hit iteration limit
        if (currentTurnState.IterationCount >= currentTurnState.MaxIterations)
        {
            if (enableDebugLogs)
                Debug.Log($"AI reached maximum iterations ({currentTurnState.MaxIterations}). Ending turn.");
        }
    }
    
    CombinationResult SelectBestCombination()
    {
        // Use combination strategy to find best combination based on current AI state
        var availableCombinations = combinationStrategy.FindAllValidCombinations(currentTurnState.CurrentDice);
        
        if (availableCombinations.Count == 0)
            return null;
        
        // Select combination based on current behavior mode and iteration
        var selectedCombination = combinationStrategy.SelectOptimalCombination(
            availableCombinations, 
            currentTurnState.CurrentMode,
            GetRemainingDiceCount(),
            currentTurnState.IterationCount
        );
        
        if (enableDebugLogs && selectedCombination != null)
        {
            Debug.Log($"Selected Combination: {selectedCombination.rule} - {selectedCombination.description}");
            Debug.Log($"Points: {selectedCombination.points}, Dice Used: {GetDiceUsedForCombination(selectedCombination.rule)}");
        }
        
        return selectedCombination;
    }
    
    void ProcessCombination(CombinationResult combination)
    {
        // Add combination to turn state
        currentTurnState.AddCombination(combination);
        
        // Remove used dice from current dice list
        int diceUsed = GetDiceUsedForCombination(combination.rule);
        if (diceUsed > 0)
        {
            RemoveUsedDice(diceUsed);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Combination processed: +{combination.points} points");
            Debug.Log($"Turn Score: {currentTurnState.CurrentTurnScore}");
            Debug.Log($"Remaining Dice: {GetRemainingDiceCount()}");
            Debug.Log($"Successful Combinations: {currentTurnState.SuccessfulCombinationsCount}");
        }
        
        // Notify listeners
        OnCombinationSelected?.Invoke(combination);
    }
    
    AIStopDecision MakeStopDecision()
    {
        // Calculate dual probability stop decision
        var stopDecision = riskCalculator.CalculateStopDecision(
            currentTurnState.IterationCount,
            GetRemainingDiceCount(),
            currentTurnState.SuccessfulCombinationsCount,
            currentTurnState.CurrentTurnScore,
            currentTurnState.PointsPerTurnCap,
            currentTurnState.CurrentMode == BehaviorMode.AGGRESSIVE
        );
        
        // Update turn state with probability information
        currentTurnState.CurrentMomentumStopChance = stopDecision.MomentumStopChance;
        currentTurnState.CurrentCapStopChance = stopDecision.CapStopChance;
        currentTurnState.CombinedStopChance = stopDecision.CombinedStopChance;
        
        if (showDetailedDecisions)
        {
            Debug.Log($"Stop Decision Analysis:");
            Debug.Log($"  Momentum Stop Chance: {stopDecision.MomentumStopChance:P1}");
            Debug.Log($"  Cap Stop Chance: {stopDecision.CapStopChance:P1}");
            Debug.Log($"  Combined Stop Chance: {stopDecision.CombinedStopChance:P1}");
            Debug.Log($"  Momentum Roll: {stopDecision.MomentumRollResult}");
            Debug.Log($"  Cap Roll: {stopDecision.CapRollResult}");
            Debug.Log($"  Final Decision: {(stopDecision.ShouldStop ? "STOP" : "CONTINUE")}");
        }
        
        // Notify listeners
        OnDecisionMade?.Invoke(stopDecision);
        
        return stopDecision;
    }
    
    void RemoveUsedDice(List<int> usedDice)
    {
        // Remove dice values from current dice list
        // This is a simplified approach - in a real implementation you might need more sophisticated tracking
        foreach (int diceValue in usedDice)
        {
            if (currentTurnState.CurrentDice.Contains(diceValue))
            {
                currentTurnState.CurrentDice.Remove(diceValue);
            }
        }
    }
    
    void RemoveUsedDice(int diceCount)
    {
        // Remove the specified number of dice from current dice list
        for (int i = 0; i < diceCount && currentTurnState.CurrentDice.Count > 0; i++)
        {
            currentTurnState.CurrentDice.RemoveAt(0);
        }
    }
    
    void RegenerateRemainingDice()
    {
        int remainingCount = GetRemainingDiceCount();
        if (remainingCount > 0)
        {
            var newDice = diceGenerator.GenerateRandomDice(remainingCount);
            currentTurnState.CurrentDice = newDice;
            
            if (enableDebugLogs)
                Debug.Log($"Regenerated {remainingCount} dice: [{string.Join(",", newDice)}]");
        }
    }
    
    int GetRemainingDiceCount()
    {
        return currentTurnState.CurrentDice.Count;
    }
    
    void HandleZonk()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== AI ZONK ===");
            Debug.Log($"All progress lost! Turn score was: {currentTurnState.CurrentTurnScore}");
        }
        
        // Create Zonk result
        var zonkResult = new CombinationResult(Rule.Zonk, 0, "ZONK - All progress lost!", 0f);
        currentTurnState.CompletedCombinations.Add(zonkResult);
        
        // Reset turn score and momentum
        currentTurnState.CurrentTurnScore = 0;
        currentTurnState.SuccessfulCombinationsCount = 0;
        
        // Notify listeners
        OnZonkOccurred?.Invoke();
        
        // Complete turn with Zonk
        CompleteTurnWithZonk();
    }
    
    void CompleteTurn()
    {
        if (!isTurnActive) return;
        
        isTurnActive = false;
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== AI TURN COMPLETE ===");
            Debug.Log($"Final Turn Score: {currentTurnState.CurrentTurnScore}");
            Debug.Log($"Iterations Used: {currentTurnState.IterationCount}/{currentTurnState.MaxIterations}");
            Debug.Log($"Successful Combinations: {currentTurnState.SuccessfulCombinationsCount}");
            Debug.Log($"Combinations: {string.Join(", ", currentTurnState.CompletedCombinations.ConvertAll(c => c.rule.ToString()))}");
        }
        
        // Add combinations to score manager if available
        if (scoreManager != null)
        {
            foreach (var combination in currentTurnState.CompletedCombinations)
            {
                scoreManager.AddCombination(combination);
            }
        }
        
        // Notify listeners
        OnTurnCompleted?.Invoke(currentTurnState);
        
        // Clean up
        currentTurnCoroutine = null;
    }
    
    void CompleteTurnWithZonk()
    {
        if (!isTurnActive) return;
        
        isTurnActive = false;
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== AI TURN COMPLETE (ZONK) ===");
            Debug.Log($"Turn ended with Zonk - no points scored");
            Debug.Log($"Iterations before Zonk: {currentTurnState.IterationCount}");
        }
        
        // Handle Zonk in score manager if available
        if (scoreManager != null)
        {
            // Add the Zonk result to show in UI
            var zonkResult = new CombinationResult(Rule.Zonk, 0, "ZONK - All progress lost!", 0f);
            scoreManager.AddCombination(zonkResult);
        }
        
        // Notify listeners
        OnTurnCompleted?.Invoke(currentTurnState);
        
        // Clean up
        currentTurnCoroutine = null;
    }
    
    /// <summary>
    /// Gets current turn state for external monitoring
    /// </summary>
    public AITurnState GetCurrentTurnState()
    {
        return currentTurnState;
    }
    
    /// <summary>
    /// Checks if AI turn is currently active
    /// </summary>
    public bool IsTurnActive()
    {
        return isTurnActive;
    }
    
    /// <summary>
    /// Processes results from aggressive reroll strategy execution
    /// </summary>
    void ProcessAggressiveRerollResult(AggressiveRerollResult result)
    {
        if (result == null)
        {
            if (enableDebugLogs)
                Debug.LogError("AITurnExecutor: Aggressive reroll result is null!");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Processing aggressive reroll result:");
            Debug.Log($"  Total Points: {result.TotalPointsScored}");
            Debug.Log($"  Iterations: {result.Iterations.Count}");
            Debug.Log($"  Hot Streaks: {result.HotStreakCount}");
            Debug.Log($"  Final Reason: {result.FinalReason}");
        }
        
        // Update turn state with results
        currentTurnState.CurrentTurnScore += result.TotalPointsScored;
        currentTurnState.IterationCount = result.Iterations.Count;
        
        // Add all combinations to turn state
        foreach (var iteration in result.Iterations)
        {
            if (iteration.SelectedCombination != null)
            {
                currentTurnState.CompletedCombinations.Add(iteration.SelectedCombination);
                currentTurnState.SuccessfulCombinationsCount++;
            }
        }
        
        // Handle Zonk if occurred
        if (result.ZonkOccurred)
        {
            HandleZonk();
            return;
        }
        
        // Update remaining dice count
        currentTurnState.CurrentDice.Clear();
        for (int i = 0; i < result.FinalDiceCount; i++)
        {
            currentTurnState.CurrentDice.Add(0); // Placeholder - actual dice values not needed for completion
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Aggressive reroll complete - Turn Score: {currentTurnState.CurrentTurnScore}, " +
                     $"Combinations: {currentTurnState.SuccessfulCombinationsCount}");
        }
    }
    
    /// <summary>
    /// Updates AI configuration at runtime
    /// </summary>
    public void UpdateConfiguration(AIConfiguration newConfig)
    {
        aiConfig = newConfig;
        
        // Update dependent components
        if (riskCalculator != null)
            riskCalculator.UpdateConfiguration(aiConfig);
        if (combinationStrategy != null)
            combinationStrategy.UpdateConfiguration(aiConfig);
        if (decisionEngine != null)
            decisionEngine.UpdateConfiguration(aiConfig);
        if (aggressiveRerollStrategy != null)
            aggressiveRerollStrategy.UpdateConfiguration(aiConfig);
        if (dualProbabilityCapSystem != null)
            dualProbabilityCapSystem.UpdateConfiguration(aiConfig);
    }
    
    /// <summary>
    /// Determines how many dice are used for a specific combination type
    /// </summary>
    int GetDiceUsedForCombination(Rule rule)
    {
        switch (rule)
        {
            case Rule.One:
                return 1;
            case Rule.Pair:
                return 2;
            case Rule.LowStraight:
                return 3;
            case Rule.ThreeOfKind:
                return 3;
            case Rule.TwoPair:
                return 4;
            case Rule.MiddleStraight:
                return 4;
            case Rule.FullHouse:
                return 5;
            case Rule.Straight:
                return 5;
            case Rule.FourOfKind:
                return 4;
            case Rule.ThreePairs:
                return 6;
            case Rule.MaxStraight:
                return 6;
            case Rule.TwoSets:
                return 6;
            case Rule.Zonk:
                return 0;
            default:
                return 1;
        }
    }
}