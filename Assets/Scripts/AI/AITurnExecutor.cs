using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public DiceController diceController;
    
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
    private bool isTurnCompleted = false;
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
        
        // Reset completion flag for new turn
        isTurnCompleted = false;
        
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
        
        // Spawn visual AI dice
        if (diceController != null)
        {
            diceController.SpawnAIDice(currentTurnState.CurrentDice);
        }
        
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
        
        // Only complete if not already completed (e.g., by zonk)
        if (!isTurnCompleted)
        {
            CompleteTurn();
        }
    }
    
    /// <summary>
    /// Executes aggressive turn flow using aggressive reroll strategy
    /// </summary>
    IEnumerator ExecuteAggressiveTurnFlow()
    {
        if (enableDebugLogs)
            Debug.Log("=== EXECUTING AGGRESSIVE TURN FLOW ===");
        
        // Initial delay to show AI dice
        if (enableDebugLogs)
            Debug.Log("AI (Aggressive) starting - showing initial dice...");
        yield return new WaitForSeconds(3.0f);
        
        // Set up dual probability cap system
        if (dualProbabilityCapSystem != null)
        {
            dualProbabilityCapSystem.SetDynamicCap(BehaviorMode.AGGRESSIVE);
        }
        
        // Show AI thinking
        if (enableDebugLogs)
            Debug.Log("AI (Aggressive) analyzing all possibilities...");
        yield return new WaitForSeconds(2.0f);
        
        // Execute aggressive reroll strategy
        var aggressiveResult = aggressiveRerollStrategy.ExecuteAggressiveReroll(
            currentTurnState.CurrentDice,
            currentTurnState.CurrentMode,
            currentTurnState.CurrentTurnScore,
            currentTurnState.PointsPerTurnCap
        );
        
        // Visualize each iteration from the aggressive result
        if (aggressiveResult != null && aggressiveResult.Iterations != null)
        {
            yield return StartCoroutine(VisualizeAggressiveIterations(aggressiveResult));
        }
        
        // Process aggressive reroll results (final cleanup)
        ProcessAggressiveRerollResult(aggressiveResult);
        
        // Final delay to show results
        if (enableDebugLogs)
            Debug.Log("Aggressive turn complete - showing final result...");
        yield return new WaitForSeconds(2.0f);
    }
    
    /// <summary>
    /// Executes standard turn flow for non-aggressive modes
    /// </summary>
    IEnumerator ExecuteStandardTurnFlow()
    {
        // Initial delay to let player see the AI dice
        if (enableDebugLogs)
            Debug.Log("AI turn starting - showing initial dice...");
        yield return new WaitForSeconds(3.0f);
        
        while (isTurnActive && currentTurnState.IterationCount < currentTurnState.MaxIterations)
        {
            yield return StartCoroutine(ExecuteFullDiceSetIteration());
        }
        
        // Check if we hit iteration limit
        if (currentTurnState.IterationCount >= currentTurnState.MaxIterations)
        {
            if (enableDebugLogs)
                Debug.Log($"AI reached maximum iterations ({currentTurnState.MaxIterations}). Ending turn.");
        }
    }
    
    /// <summary>
    /// Executes a full dice set iteration (6 dice → combinations → reroll until all used or stop)
    /// </summary>
    IEnumerator ExecuteFullDiceSetIteration()
    {
        currentTurnState.IterationCount++;
        
        if (enableDebugLogs)
            Debug.Log($"=== ITERATION {currentTurnState.IterationCount}: STARTING WITH FRESH DICE SET ===");
        
        // Continue making combinations until all dice used or AI decides to stop
        while (isTurnActive && GetRemainingDiceCount() > 0)
        {
            yield return StartCoroutine(ExecuteSingleCombinationStep());
            
            // Check if AI decided to stop
            if (!isTurnActive) break;
        }
        
        // Check if all dice were used (hot streak)
        if (GetRemainingDiceCount() == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"=== ITERATION {currentTurnState.IterationCount} COMPLETE: HOT STREAK! ===");
                Debug.Log("All dice used! Starting new iteration with fresh dice...");
            }
            
            // Generate fresh dice for next iteration
            currentTurnState.CurrentDice = diceGenerator.GenerateRandomDice(6);
            
            // Show fresh dice
            if (diceController != null)
            {
                diceController.SpawnAIDice(currentTurnState.CurrentDice);
            }
            
            yield return new WaitForSeconds(2.5f);
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"=== ITERATION {currentTurnState.IterationCount} COMPLETE: AI STOPPED ===");
        }
    }
    
    /// <summary>
    /// Executes a single combination selection step within an iteration
    /// </summary>
    IEnumerator ExecuteSingleCombinationStep()
    {
        if (enableDebugLogs)
            Debug.Log($"--- Combination Step within Iteration {currentTurnState.IterationCount} ---");
        
        // Step 1: AI thinking delay
        if (enableDebugLogs)
            Debug.Log("AI is analyzing dice...");
        yield return new WaitForSeconds(2.0f);
            
        // Step 2: Check for Zonk
        if (!combinationDetector.HasAnyCombination(currentTurnState.CurrentDice))
        {
            HandleZonk();
            yield break;
        }
        
        // Step 3: Find and select best combination
        var selectedCombination = SelectBestCombination();
        if (selectedCombination == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("No valid combination found despite HasAnyCombination returning true!");
            HandleZonk();
            yield break;
        }
        
        // Step 4: Show what AI is about to select
        if (enableDebugLogs)
        {
            Debug.Log($"=== AI SELECTING COMBINATION ===");
            Debug.Log($"Combination: {selectedCombination.description}");
            Debug.Log($"Points: +{selectedCombination.points}");
            Debug.Log($"Dice used: {GetDiceUsedForCombination(selectedCombination.rule)}");
        }
        yield return new WaitForSeconds(1.5f);
        
        // Step 5: Process the combination (dice disappear here)
        ProcessCombination(selectedCombination);
        
        // Step 6: Show result of selection
        if (enableDebugLogs)
        {
            Debug.Log($"Selected dice removed! Remaining dice: {GetRemainingDiceCount()}");
        }
        yield return new WaitForSeconds(2.0f);
            
        // Step 6: Check if all dice used (will be handled by parent iteration)
        if (GetRemainingDiceCount() == 0)
        {
            if (enableDebugLogs)
                Debug.Log("All dice used in this combination step!");
            yield break; // Exit this combination step, parent will handle hot streak
        }
        
        // Step 7: Make continue/stop decision
        var stopDecision = MakeStopDecision();
        
        if (stopDecision.ShouldStop)
        {
            if (enableDebugLogs)
                Debug.Log($"AI decides to STOP: {stopDecision.DecisionReason}");
            
            // Add delay to show stop decision
            yield return new WaitForSeconds(1.0f);
            
            // End the turn by setting isTurnActive to false
            isTurnActive = false;
            yield break;
        }
        
        // Step 8: AI continues - show decision
        if (enableDebugLogs)
            Debug.Log($"AI decides to CONTINUE: {stopDecision.DecisionReason}");
        
        yield return new WaitForSeconds(1.0f);
        
        // Step 9: Generate new dice for remaining positions
        RegenerateRemainingDice();
        
        // Step 10: Update visual dice after reroll
        if (diceController != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"=== AI REROLLS REMAINING DICE ===");
                Debug.Log($"Remaining dice count: {GetRemainingDiceCount()}");
                Debug.Log($"New dice values: [{string.Join(",", currentTurnState.CurrentDice)}]");
                Debug.Log($"Watch as new dice appear in empty positions...");
            }
            // Spawn all current dice (this will show the rerolled dice)
            diceController.SpawnAIDice(currentTurnState.CurrentDice);
        }
        
        // Step 11: Final delay before next combination step
        if (enableDebugLogs)
            Debug.Log($"Combination step complete. Continuing iteration {currentTurnState.IterationCount}...");
        yield return new WaitForSeconds(2.0f);
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
            // Get indices of dice to remove (specific dice that match the combination)
            List<int> indicesToRemove = GetDiceIndicesForCombination(combination, diceUsed);
            
            if (enableDebugLogs)
                Debug.Log($"Removing dice at indices: [{string.Join(",", indicesToRemove)}] for combination {combination.rule}");
            
            // Visually remove AI dice
            if (diceController != null && indicesToRemove.Count > 0)
            {
                diceController.RemoveAIDice(indicesToRemove);
            }
            
            // Remove the same dice from the internal array (by indices, not count)
            RemoveUsedDiceByIndices(indicesToRemove);
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
    
    void RemoveUsedDiceByIndices(List<int> indices)
    {
        // Remove dice at specific indices from current dice list
        // Sort indices in descending order to avoid index shifting issues
        var sortedIndices = indices.OrderByDescending(x => x).ToList();
        
        if (enableDebugLogs)
        {
            Debug.Log($"Before removal: [{string.Join(",", currentTurnState.CurrentDice)}]");
            Debug.Log($"Removing indices: [{string.Join(",", sortedIndices)}]");
        }
        
        foreach (int index in sortedIndices)
        {
            if (index >= 0 && index < currentTurnState.CurrentDice.Count)
            {
                int removedValue = currentTurnState.CurrentDice[index];
                currentTurnState.CurrentDice.RemoveAt(index);
                
                if (enableDebugLogs)
                    Debug.Log($"Removed dice value {removedValue} at index {index}");
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"After removal: [{string.Join(",", currentTurnState.CurrentDice)}]");
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
        // Prevent double completion
        if (isTurnCompleted || currentTurnState == null) return;
        
        isTurnActive = false;
        isTurnCompleted = true;
        
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
        // Prevent double completion (same as CompleteTurn)
        if (isTurnCompleted || currentTurnState == null) return;
        
        isTurnActive = false;
        isTurnCompleted = true;
        
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
    
    /// <summary>
    /// Gets the indices of dice to remove for a specific combination
    /// </summary>
    List<int> GetDiceIndicesForCombination(CombinationResult combination, int diceCount)
    {
        List<int> indices = new List<int>();
        List<int> currentDice = new List<int>(currentTurnState.CurrentDice);
        
        if (enableDebugLogs)
            Debug.Log($"Finding dice for combination: {combination.rule} from dice: [{string.Join(",", currentDice)}]");
        
        // Identify specific dice based on combination type
        switch (combination.rule)
        {
            case Rule.One:
                // Single 1 or 5 - determine which based on dice values
                if (currentDice.Contains(1))
                    indices.AddRange(FindDiceWithValue(currentDice, 1, 1));
                else if (currentDice.Contains(5))
                    indices.AddRange(FindDiceWithValue(currentDice, 5, 1));
                break;
            case Rule.ThreeOfKind:
                indices.AddRange(FindThreeOfAKindDice(currentDice));
                break;
            case Rule.FourOfKind:
                indices.AddRange(FindFourOfAKindDice(currentDice));
                break;
            case Rule.ThreePairs:
                indices.AddRange(FindThreePairsDice(currentDice));
                break;
            case Rule.Straight:
                indices.AddRange(FindStraightDice(currentDice, 5));
                break;
            case Rule.MaxStraight:
                indices.AddRange(FindStraightDice(currentDice, 6));
                break;
            case Rule.TwoSets:
                indices.AddRange(FindTwoSetsDice(currentDice));
                break;
            case Rule.Pair:
                indices.AddRange(FindPairDice(currentDice));
                break;
            case Rule.TwoPair:
                indices.AddRange(FindTwoPairDice(currentDice));
                break;
            case Rule.LowStraight:
                indices.AddRange(FindStraightDice(currentDice, 3));
                break;
            case Rule.MiddleStraight:
                indices.AddRange(FindStraightDice(currentDice, 4));
                break;
            case Rule.FullHouse:
                indices.AddRange(FindFullHouseDice(currentDice));
                break;
            default:
                // Fallback to first N dice
                for (int i = 0; i < diceCount && i < currentDice.Count; i++)
                {
                    indices.Add(i);
                }
                break;
        }
        
        if (enableDebugLogs)
        {
            var selectedValues = indices.Select(i => currentDice[i]).ToList();
            Debug.Log($"Selected dice at indices [{string.Join(",", indices)}] with values [{string.Join(",", selectedValues)}]");
        }
        
        return indices;
    }
    
    List<int> FindDiceWithValue(List<int> dice, int value, int count)
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < dice.Count && indices.Count < count; i++)
        {
            if (dice[i] == value)
                indices.Add(i);
        }
        return indices;
    }
    
    List<int> FindThreeOfAKindDice(List<int> dice)
    {
        var groups = dice.Select((value, index) => new { value, index })
                         .GroupBy(x => x.value)
                         .Where(g => g.Count() >= 3)
                         .FirstOrDefault();
        
        return groups?.Take(3).Select(x => x.index).ToList() ?? new List<int>();
    }
    
    List<int> FindFourOfAKindDice(List<int> dice)
    {
        var groups = dice.Select((value, index) => new { value, index })
                         .GroupBy(x => x.value)
                         .Where(g => g.Count() >= 4)
                         .FirstOrDefault();
        
        return groups?.Take(4).Select(x => x.index).ToList() ?? new List<int>();
    }
    
    List<int> FindPairDice(List<int> dice)
    {
        var groups = dice.Select((value, index) => new { value, index })
                         .GroupBy(x => x.value)
                         .Where(g => g.Count() >= 2)
                         .FirstOrDefault();
        
        return groups?.Take(2).Select(x => x.index).ToList() ?? new List<int>();
    }
    
    List<int> FindTwoPairDice(List<int> dice)
    {
        var groups = dice.Select((value, index) => new { value, index })
                         .GroupBy(x => x.value)
                         .Where(g => g.Count() >= 2)
                         .Take(2);
        
        List<int> indices = new List<int>();
        foreach (var group in groups)
        {
            indices.AddRange(group.Take(2).Select(x => x.index));
        }
        return indices;
    }
    
    List<int> FindFullHouseDice(List<int> dice)
    {
        var groups = dice.Select((value, index) => new { value, index })
                         .GroupBy(x => x.value)
                         .OrderByDescending(g => g.Count())
                         .ToList();
        
        List<int> indices = new List<int>();
        
        // Find three of a kind
        var threeOfKind = groups.FirstOrDefault(g => g.Count() >= 3);
        if (threeOfKind != null)
        {
            indices.AddRange(threeOfKind.Take(3).Select(x => x.index));
        }
        
        // Find pair (different from three of a kind)
        var pair = groups.FirstOrDefault(g => g.Key != threeOfKind?.Key && g.Count() >= 2);
        if (pair != null)
        {
            indices.AddRange(pair.Take(2).Select(x => x.index));
        }
        
        return indices;
    }
    
    List<int> FindThreePairsDice(List<int> dice)
    {
        var groups = dice.Select((value, index) => new { value, index })
                         .GroupBy(x => x.value)
                         .Where(g => g.Count() >= 2)
                         .Take(3);
        
        List<int> indices = new List<int>();
        foreach (var group in groups)
        {
            indices.AddRange(group.Take(2).Select(x => x.index));
        }
        return indices;
    }
    
    List<int> FindStraightDice(List<int> dice, int length)
    {
        List<int> indices = new List<int>();
        List<int> targetValues = length == 5 ? new List<int> { 1, 2, 3, 4, 5 } : new List<int> { 1, 2, 3, 4, 5, 6 };
        
        foreach (int value in targetValues)
        {
            int index = dice.FindIndex(d => d == value);
            if (index >= 0)
                indices.Add(index);
        }
        
        return indices.Count == length ? indices : new List<int>();
    }
    

    
    List<int> FindTwoSetsDice(List<int> dice)
    {
        var groups = dice.Select((value, index) => new { value, index })
                         .GroupBy(x => x.value)
                         .Where(g => g.Count() >= 3)
                         .Take(2);
        
        List<int> indices = new List<int>();
        foreach (var group in groups)
        {
            indices.AddRange(group.Take(3).Select(x => x.index));
        }
        return indices;
    }
    
    /// <summary>
    /// Test method to verify delays are working
    /// </summary>
    [ContextMenu("Test AI Delays")]
    public void TestDelays()
    {
        Debug.Log("=== DELAY TEST START ===");
        Debug.Log($"GameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"Component enabled: {enabled}");
        Debug.Log($"Time.timeScale: {Time.timeScale}");
        Debug.Log($"Application.isPlaying: {Application.isPlaying}");
        
        if (!Application.isPlaying)
        {
            Debug.LogError("❌ Game must be PLAYING for coroutines to work!");
            return;
        }
        
        if (Time.timeScale == 0)
        {
            Debug.LogError("❌ Time.timeScale is 0! Set it to 1.0");
            return;
        }
        
        StartCoroutine(TestDelayCoroutine());
    }
    
    IEnumerator TestDelayCoroutine()
    {
        Debug.Log("Coroutine started - waiting 2 seconds...");
        yield return new WaitForSeconds(2.0f);
        Debug.Log("✅ 2 seconds passed");
        yield return new WaitForSeconds(2.0f);
        Debug.Log("✅ 4 seconds passed");
        yield return new WaitForSeconds(2.0f);
        Debug.Log("✅ 6 seconds passed - delay test complete");
    }
    
    /// <summary>
    /// Alternative test using Invoke (doesn't require coroutines)
    /// </summary>
    [ContextMenu("Test Invoke Delays")]
    public void TestInvokeDelays()
    {
        Debug.Log("=== INVOKE TEST START ===");
        Invoke(nameof(InvokeTest1), 2.0f);
        Invoke(nameof(InvokeTest2), 4.0f);
        Invoke(nameof(InvokeTest3), 6.0f);
    }
    
    void InvokeTest1() { Debug.Log("✅ Invoke: 2 seconds passed"); }
    void InvokeTest2() { Debug.Log("✅ Invoke: 4 seconds passed"); }
    void InvokeTest3() { Debug.Log("✅ Invoke: 6 seconds passed - invoke test complete"); }
    
    /// <summary>
    /// Visualizes each iteration from aggressive strategy results with delays
    /// </summary>
    IEnumerator VisualizeAggressiveIterations(AggressiveRerollResult result)
    {
        if (enableDebugLogs)
            Debug.Log($"=== VISUALIZING {result.Iterations.Count} AGGRESSIVE ITERATIONS ===");
        
        for (int i = 0; i < result.Iterations.Count; i++)
        {
            var iteration = result.Iterations[i];
            
            if (enableDebugLogs)
            {
                Debug.Log($"--- Aggressive Iteration {i + 1} ---");
                Debug.Log($"Starting dice: [{string.Join(",", iteration.InitialDice)}]");
            }
            
            // Show AI dice for this iteration
            if (diceController != null)
            {
                diceController.SpawnAIDice(iteration.InitialDice);
            }
            
            // AI thinking delay
            yield return new WaitForSeconds(1.5f);
            
            if (iteration.SelectedCombination != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"=== AI SELECTED (Aggressive) ===");
                    Debug.Log($"Combination: {iteration.SelectedCombination.description}");
                    Debug.Log($"Points: +{iteration.SelectedCombination.points}");
                }
                
                // Show combination selection
                yield return new WaitForSeconds(1.5f);
                
                // Simulate dice removal (we don't have exact indices from aggressive result)
                if (diceController != null && iteration.RemainingDice < iteration.InitialDice.Count)
                {
                    // Remove some dice to show selection
                    int diceToRemove = iteration.InitialDice.Count - iteration.RemainingDice;
                    List<int> indicesToRemove = new List<int>();
                    for (int j = 0; j < diceToRemove; j++)
                    {
                        indicesToRemove.Add(j);
                    }
                    diceController.RemoveAIDice(indicesToRemove);
                }
                
                // Show dice removal result
                yield return new WaitForSeconds(1.0f);
            }
            
            // Check for hot streak
            if (iteration.RemainingDice == 0 && i < result.Iterations.Count - 1)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("=== AGGRESSIVE HOT STREAK! ===");
                    Debug.Log("All dice used! Fresh dice incoming...");
                }
                yield return new WaitForSeconds(1.5f);
            }
            else if (iteration.RemainingDice > 0)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"AI (Aggressive) continues with {iteration.RemainingDice} dice...");
                }
                yield return new WaitForSeconds(1.0f);
            }
        }
        
        // Show final result
        if (enableDebugLogs)
        {
            Debug.Log($"=== AGGRESSIVE STRATEGY COMPLETE ===");
            Debug.Log($"Total iterations: {result.Iterations.Count}");
            Debug.Log($"Total points: {result.TotalPointsScored}");
            Debug.Log($"Final reason: {result.FinalReason}");
        }
        yield return new WaitForSeconds(2.0f);
    }
}