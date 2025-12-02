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
    public GameTurnManager gameTurnManager; // For accessing player/AI total scores
    public AIActionLog actionLog; // Optional: for UI logging
    
    [Header("AI Configuration")]
    public AIConfiguration aiConfig;
    
    [Header("Turn Timing Settings")]
    [Tooltip("Time to show initial dice roll")]
    public float initialSetupDelay = 1.0f;
    [Tooltip("Time for AI to analyze dice")]
    public float analyzingDelay = 0.5f;
    [Tooltip("Time to show dice removal")]
    public float diceRemovalDelay = 0.5f;
    [Tooltip("Time to show continue decision")]
    public float continueDecisionDelay = 0.5f;
    [Tooltip("Time between iteration steps")]
    public float stepCompleteDelay = 0.5f;
    [Tooltip("Time after hot streak before next iteration")]
    public float hotStreakDelay = 0.5f;
    [Tooltip("Time for aggressive mode initial setup")]
    public float aggressiveSetupDelay = 3.0f;
    [Tooltip("Time for aggressive mode analysis")]
    public float aggressiveAnalysisDelay = 2.0f;
    [Tooltip("Time for aggressive mode final result")]
    public float aggressiveFinalDelay = 2.0f;
    [Tooltip("Time for aggressive iteration thinking")]
    public float aggressiveIterationThinkDelay = 1.5f;
    [Tooltip("Time for aggressive combination selection")]
    public float aggressiveCombinationDelay = 1.5f;
    [Tooltip("Time for aggressive dice removal")]
    public float aggressiveDiceRemovalDelay = 1.0f;
    [Tooltip("Time for aggressive hot streak")]
    public float aggressiveHotStreakDelay = 1.5f;
    [Tooltip("Time for aggressive continue decision")]
    public float aggressiveContinueDelay = 1.0f;
    [Tooltip("Time for aggressive final visualization")]
    public float aggressiveVisualizationDelay = 2.0f;
    
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
        if (gameTurnManager == null) gameTurnManager = FindObjectOfType<GameTurnManager>();
        
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
        
        // Hardcode AI to always use aggressive mode
        currentTurnState.CurrentMode = BehaviorMode.AGGRESSIVE;
        
        // Set points per turn cap based on behavior mode
        currentTurnState.PointsPerTurnCap = gameStateAnalyzer.GetPointsPerTurnCap(currentTurnState.CurrentMode);
        
        // Set max iterations based on behavior mode
        currentTurnState.MaxIterations = currentTurnState.CurrentMode == BehaviorMode.AGGRESSIVE ? 5 : 2;
        
        // Decide which stop logic to use at turn start
        int playerTotalScore = gameTurnManager != null ? gameTurnManager.playerScore : 0;
        int aiGameScore = gameTurnManager != null ? gameTurnManager.aiScore : 0;
        int scoreDifference = aiGameScore - playerTotalScore;
        
        bool playerLeadingByMuch = scoreDifference < -5000; // Player leading by >5000
        bool playerHasHighScore = playerTotalScore > 8000;
        
        // Use custom logic unless player is dominating
        currentTurnState.UseCustomStopLogic = !(playerLeadingByMuch || playerHasHighScore);
        
        // Log turn separator and AI mode to action log
        if (actionLog != null)
        {
            actionLog.LogAITurnStart(turnNumber);
            
            if (currentTurnState.UseCustomStopLogic)
            {
                actionLog.LogAIAction("ИИ в пассивном режиме");
            }
            else
            {
                actionLog.LogAIAction("ИИ в агрессивном режиме");
            }
        }
        
        // Generate initial dice (no rigged dice for custom logic)
        bool useRiggedDice = !currentTurnState.UseCustomStopLogic;
        
        if (enableDebugLogs)
            Debug.Log($"🎲 Initial dice generation: UseCustomLogic={currentTurnState.UseCustomStopLogic}, useRiggedDice={useRiggedDice}");
        
        currentTurnState.CurrentDice = diceGenerator.GenerateRandomDice(6, useRiggedDice);
        
        // Log initial dice to action log
        if (actionLog != null)
        {
            actionLog.LogAIDiceRoll(currentTurnState.CurrentDice);
        }
        
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
            Debug.Log($"  Stop Logic: {(currentTurnState.UseCustomStopLogic ? "CUSTOM" : "PROBABILITY-BASED")}");
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
        // Use standard turn flow for all modes
        // (Aggressive reroll strategy has its own broken logic - disabled for now)
        yield return StartCoroutine(ExecuteStandardTurnFlow());
        
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
        yield return new WaitForSeconds(aggressiveSetupDelay);
        
        // Set up dual probability cap system
        if (dualProbabilityCapSystem != null)
        {
            dualProbabilityCapSystem.SetDynamicCap(BehaviorMode.AGGRESSIVE);
        }
        
        // Show AI thinking
        if (enableDebugLogs)
            Debug.Log("AI (Aggressive) analyzing all possibilities...");
        yield return new WaitForSeconds(aggressiveAnalysisDelay);
        
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
        yield return new WaitForSeconds(aggressiveFinalDelay);
    }
    
    /// <summary>
    /// Executes standard turn flow for non-aggressive modes
    /// </summary>
    IEnumerator ExecuteStandardTurnFlow()
    {
        // Initial delay to let player see the AI dice
        if (enableDebugLogs)
            Debug.Log("AI turn starting - showing initial dice...");
        yield return new WaitForSeconds(initialSetupDelay);
        
        if (currentTurnState.UseCustomStopLogic)
        {
            // Custom logic: No iteration limit, only stops on zonk or dice-based conditions
            while (isTurnActive)
            {
                yield return StartCoroutine(ExecuteFullDiceSetIteration());
            }
        }
        else
        {
            // Old logic: Use iteration limit
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
    }
    
    /// <summary>
    /// Executes a full dice set iteration (6 dice → combinations → reroll until all used)
    /// Stop decisions only happen AFTER hot streaks (all dice used)
    /// </summary>
    IEnumerator ExecuteFullDiceSetIteration()
    {
        currentTurnState.IterationCount++;
        
        if (enableDebugLogs)
            Debug.Log($"=== ITERATION {currentTurnState.IterationCount}: STARTING WITH FRESH DICE SET ===");
        
        // Continue making combinations until all dice used (NO stop decisions during iteration)
        while (isTurnActive && GetRemainingDiceCount() > 0)
        {
            yield return StartCoroutine(ExecuteSingleCombinationStep());
            
            // Only break if zonk occurred (handled in ExecuteSingleCombinationStep)
            if (!isTurnActive) break;
        }
        
        // Check if all dice were used (hot streak)
        if (GetRemainingDiceCount() == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"=== ITERATION {currentTurnState.IterationCount} COMPLETE: HOT STREAK! ===");
                Debug.Log("All dice used! Checking if AI wants to continue...");
            }
            
            // Log hot streak to action log
            if (actionLog != null)
            {
                actionLog.LogAIHotStreak();
            }
            
            // HOT STREAK DECISION POINT - Should AI continue or stop?
            if (enableDebugLogs)
            {
                Debug.Log($"HOT STREAK DECISION CHECK:");
                Debug.Log($"  Using Logic: {(currentTurnState.UseCustomStopLogic ? "CUSTOM" : "PROBABILITY")}");
                Debug.Log($"  Current Turn Score: {currentTurnState.CurrentTurnScore}");
                Debug.Log($"  Points Per Turn Cap: {currentTurnState.PointsPerTurnCap}");
                Debug.Log($"  Iteration Count: {currentTurnState.IterationCount}/{currentTurnState.MaxIterations}");
                Debug.Log($"  Successful Combinations: {currentTurnState.SuccessfulCombinationsCount}");
            }
            
            AIStopDecision stopDecision;
            
            if (currentTurnState.UseCustomStopLogic)
            {
                // Custom logic: With 6 fresh dice (>4), never stop after hot streak
                stopDecision = MakeCustomStopDecision(6);
            }
            else
            {
                // Old probability-based logic
                stopDecision = MakeStopDecision();
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"STOP DECISION RESULT: {(stopDecision.ShouldStop ? "STOP" : "CONTINUE")}");
                Debug.Log($"  Reason: {stopDecision.DecisionReason}");
            }
            
            if (stopDecision.ShouldStop)
            {
                if (enableDebugLogs)
                    Debug.Log($"AI decides to STOP after hot streak: {stopDecision.DecisionReason}");
                
                // Log stop decision to action log
                if (actionLog != null)
                {
                    actionLog.LogAIDecision(false, stopDecision.DecisionReason);
                }
                
                yield return new WaitForSeconds(continueDecisionDelay);
                
                // Roll fresh 6 dice for final combination
                if (enableDebugLogs)
                    Debug.Log("Rolling fresh dice for final combination...");
                
                bool useRiggedDice = !currentTurnState.UseCustomStopLogic;
                currentTurnState.CurrentDice = diceGenerator.GenerateRandomDice(6, useRiggedDice);
                
                // Log fresh dice to action log
                if (actionLog != null)
                {
                    actionLog.LogAIDiceRoll(currentTurnState.CurrentDice);
                }
                
                // Show fresh dice
                if (diceController != null)
                {
                    diceController.SpawnAIDice(currentTurnState.CurrentDice);
                }
                
                yield return new WaitForSeconds(initialSetupDelay);
                
                // Check for zonk on final roll
                if (!combinationDetector.HasAnyCombination(currentTurnState.CurrentDice))
                {
                    if (enableDebugLogs)
                        Debug.Log("ZONK on final roll!");
                    HandleZonk();
                    yield break;
                }
                
                // Select and process best combination from fresh 6 dice
                // For final combination, just maximize points (no risk, turn is ending)
                if (enableDebugLogs)
                    Debug.Log("AI selecting final combination (maximize points)...");
                
                yield return new WaitForSeconds(analyzingDelay);
                
                var finalCombination = SelectHighestPointsCombination();
                if (finalCombination != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"=== FINAL COMBINATION ===");
                        Debug.Log($"Combination: {finalCombination.description}");
                        Debug.Log($"Points: +{finalCombination.points}");
                    }
                    
                    // Log final combination to action log
                    if (actionLog != null)
                    {
                        actionLog.LogAICombination(
                            finalCombination.description,
                            finalCombination.points,
                            GetDiceUsedForCombination(finalCombination.rule)
                        );
                    }
                    
                    ProcessCombination(finalCombination);
                    yield return new WaitForSeconds(diceRemovalDelay);
                }
                
                // End turn
                isTurnActive = false;
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"AI decides to CONTINUE after hot streak: {stopDecision.DecisionReason}");
                
                // Check if we've reached max iterations before continuing (only for old logic)
                if (!currentTurnState.UseCustomStopLogic && currentTurnState.IterationCount >= currentTurnState.MaxIterations)
                {
                    if (enableDebugLogs)
                        Debug.Log($"Cannot continue - reached maximum iterations ({currentTurnState.MaxIterations}). Ending turn.");
                    isTurnActive = false;
                    yield break;
                }
                
                yield return new WaitForSeconds(continueDecisionDelay);
                
                // Generate fresh dice for next iteration
                bool useRiggedDice = !currentTurnState.UseCustomStopLogic;
                currentTurnState.CurrentDice = diceGenerator.GenerateRandomDice(6, useRiggedDice);
                
                // Log fresh dice to action log
                if (actionLog != null)
                {
                    actionLog.LogAIDiceRoll(currentTurnState.CurrentDice);
                }
                
                // Show fresh dice
                if (diceController != null)
                {
                    diceController.SpawnAIDice(currentTurnState.CurrentDice);
                }
                
                yield return new WaitForSeconds(hotStreakDelay);
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"=== ITERATION {currentTurnState.IterationCount} INCOMPLETE: STOPPED MID-ITERATION (shouldn't happen) ===");
        }
    }
    
    /// <summary>
    /// Executes a single combination selection step within an iteration
    /// NO stop decisions here - AI commits to using all dice in the iteration
    /// </summary>
    IEnumerator ExecuteSingleCombinationStep()
    {
        if (enableDebugLogs)
            Debug.Log($"--- Combination Step within Iteration {currentTurnState.IterationCount} ---");
        
        // Step 1: AI thinking delay
        if (enableDebugLogs)
            Debug.Log("AI is analyzing dice...");
        yield return new WaitForSeconds(analyzingDelay);
            
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
        
        // Step 5: Save state before processing (for logging)
        List<int> rolledDiceBeforeProcessing = new List<int>(currentTurnState.CurrentDice);
        int previousScore = currentTurnState.CurrentTurnScore;
        
        // Process the combination (dice disappear here)
        ProcessCombination(selectedCombination);
        int diceRemaining = GetRemainingDiceCount();
        
        // Step 6: Show result of selection
        if (enableDebugLogs)
        {
            Debug.Log($"Selected dice removed! Remaining dice: {diceRemaining}");
        }
        yield return new WaitForSeconds(diceRemovalDelay);
            
        // Step 7: Check if all dice used (will be handled by parent iteration)
        bool allDiceUsed = diceRemaining == 0;
        
        // Log complete iteration to action log (combined format)
        if (actionLog != null)
        {
            // Convert dice indices to actual dice values for display
            List<int> selectedDiceValues = new List<int>();
            if (selectedCombination.diceIndices != null)
            {
                foreach (int index in selectedCombination.diceIndices)
                {
                    if (index >= 0 && index < rolledDiceBeforeProcessing.Count)
                    {
                        selectedDiceValues.Add(rolledDiceBeforeProcessing[index]);
                    }
                }
            }
            
            actionLog.LogAIIteration(
                rolledDice: rolledDiceBeforeProcessing,
                selectedDice: selectedDiceValues,
                combinationName: selectedCombination.description,
                combinationPoints: selectedCombination.points,
                previousScore: previousScore,
                totalScore: currentTurnState.CurrentTurnScore,
                continues: !allDiceUsed,
                diceRemaining: diceRemaining
            );
        }
        
        // Step 7.5: Check if AI has reached victory condition (for both logic systems)
        int victoryScore = gameTurnManager != null ? gameTurnManager.victoryScore : 5000;
        int aiGameScore = gameTurnManager != null ? gameTurnManager.aiScore : 0;
        int aiAccumulatedPoints = aiGameScore + currentTurnState.CurrentTurnScore;
        
        if (aiAccumulatedPoints >= victoryScore)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"=== VICTORY CONDITION MET ===");
                Debug.Log($"AI Accumulated Points: {aiAccumulatedPoints} >= Victory Score: {victoryScore}");
                Debug.Log($"Stopping immediately to win the game!");
            }
            
            // Log victory decision to action log
            if (actionLog != null)
            {
                actionLog.LogAIDecision(false, $"Victory! Reached {aiAccumulatedPoints} points");
            }
            
            yield return new WaitForSeconds(continueDecisionDelay);
            
            // End the turn immediately - AI wins!
            isTurnActive = false;
            yield break;
        }
        
        if (allDiceUsed)
        {
            if (enableDebugLogs)
                Debug.Log("All dice used in this combination step! Hot streak achieved.");
            yield break; // Exit this combination step, parent will handle hot streak decision
        }
        
        // Step 8: Check stop decision based on which logic system is active
        if (currentTurnState.UseCustomStopLogic)
        {
            // Use custom stop logic
            var customStopDecision = MakeCustomStopDecision(diceRemaining);
            
            if (customStopDecision.ShouldStop)
            {
                if (enableDebugLogs)
                    Debug.Log($"AI decides to STOP mid-iteration: {customStopDecision.DecisionReason}");
                
                // Log stop decision to action log
                if (actionLog != null)
                {
                    actionLog.LogAIDecision(false, customStopDecision.DecisionReason);
                }
                
                yield return new WaitForSeconds(continueDecisionDelay);
                
                // End the turn - bank the points
                isTurnActive = false;
                yield break;
            }
        }
        // If using old logic, no mid-iteration stops (only after hot streaks)
        
        // Step 9: AI continues with remaining dice
        if (enableDebugLogs)
            Debug.Log($"AI continues with {diceRemaining} dice remaining...");
        
        yield return new WaitForSeconds(continueDecisionDelay);
        
        // Step 10: Generate new dice for remaining positions
        RegenerateRemainingDice();
        
        // Step 11: Update visual dice after reroll
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
        
        // Step 12: Final delay before next combination step
        if (enableDebugLogs)
            Debug.Log($"Combination step complete. Continuing iteration {currentTurnState.IterationCount}...");
        yield return new WaitForSeconds(stepCompleteDelay);
    }
    
    CombinationResult SelectBestCombination()
    {
        // Use combination strategy to find best combination based on current AI state
        var availableCombinations = combinationStrategy.FindAllValidCombinations(currentTurnState.CurrentDice);
        
        if (availableCombinations.Count == 0)
            return null;
        
        int remainingDiceCount = GetRemainingDiceCount();
        
        // CUSTOM LOGIC OVERRIDE: When far behind with 3 dice, pick highest points
        if (currentTurnState.UseCustomStopLogic && remainingDiceCount == 3)
        {
            int playerTotalScore = gameTurnManager != null ? gameTurnManager.playerScore : 0;
            int aiGameScore = gameTurnManager != null ? gameTurnManager.aiScore : 0;
            int aiAccumulatedPoints = aiGameScore + currentTurnState.CurrentTurnScore;
            bool playerLeadingByMuch = (playerTotalScore - aiAccumulatedPoints) > 3000;
            
            if (playerLeadingByMuch)
            {
                // Override strategy: pick highest points combination
                var highestPointsCombination = availableCombinations.OrderByDescending(c => c.points).First();
                
                if (enableDebugLogs)
                {
                    Debug.Log($"🎯 OVERRIDE: Player leading by {playerTotalScore - aiAccumulatedPoints}, picking highest points with 3 dice");
                    Debug.Log($"Selected Combination: {highestPointsCombination.rule} - {highestPointsCombination.description}");
                    Debug.Log($"Points: {highestPointsCombination.points}, Dice Used: {GetDiceUsedForCombination(highestPointsCombination.rule)}");
                }
                
                return highestPointsCombination;
            }
        }
        
        // Normal strategy selection
        var selectedCombination = combinationStrategy.SelectOptimalCombination(
            availableCombinations, 
            currentTurnState.CurrentMode,
            remainingDiceCount,
            currentTurnState.IterationCount
        );
        
        if (enableDebugLogs && selectedCombination != null)
        {
            Debug.Log($"Selected Combination: {selectedCombination.rule} - {selectedCombination.description}");
            Debug.Log($"Points: {selectedCombination.points}, Dice Used: {GetDiceUsedForCombination(selectedCombination.rule)}");
        }
        
        return selectedCombination;
    }
    
    CombinationResult SelectHighestPointsCombination()
    {
        // For final combination after stop decision - just maximize points
        var availableCombinations = combinationStrategy.FindAllValidCombinations(currentTurnState.CurrentDice);
        
        if (availableCombinations.Count == 0)
            return null;
        
        // Select combination with highest points (no strategy, no risk)
        var selectedCombination = availableCombinations.OrderByDescending(c => c.points).First();
        
        if (enableDebugLogs && selectedCombination != null)
        {
            Debug.Log($"Final Combination (max points): {selectedCombination.rule} - {selectedCombination.description}");
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
    
    /// <summary>
    /// Custom stop decision based on remaining dice count and game state
    /// Called after processing a combination to decide if AI should stop mid-iteration
    /// </summary>
    AIStopDecision MakeCustomStopDecision(int remainingDiceCount)
    {
        // Get game scores
        int playerTotalScore = gameTurnManager != null ? gameTurnManager.playerScore : 0;
        int aiGameScore = gameTurnManager != null ? gameTurnManager.aiScore : 0;
        
        // Use turn score for stop decisions (not accumulated)
        int aiTurnScore = currentTurnState.CurrentTurnScore;
        int aiAccumulatedPoints = aiGameScore + aiTurnScore;
        int scoreDifference = aiAccumulatedPoints - playerTotalScore;
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== CUSTOM STOP DECISION ===");
            Debug.Log($"Player Total Score: {playerTotalScore}");
            Debug.Log($"AI Game Score: {aiGameScore}");
            Debug.Log($"AI Turn Score: {aiTurnScore}");
            Debug.Log($"AI Accumulated Points: {aiAccumulatedPoints} (game + turn)");
            Debug.Log($"Score Difference: {scoreDifference} (AI accumulated - Player)");
            Debug.Log($"Remaining Dice: {remainingDiceCount}");
        }
        
        // CUSTOM LOGIC: Stop decision based on remaining dice and TURN SCORE
        
        // 1. If >4 dice remaining, never stop (continue iteration)
        if (remainingDiceCount > 4)
        {
            if (enableDebugLogs)
                Debug.Log("Decision: CONTINUE (>4 dice remaining - never stop mid-iteration)");
            return new AIStopDecision 
            { 
                ShouldStop = false, 
                DecisionReason = ">4 dice remaining - continue iteration" 
            };
        }
        
        // 2. If 1 dice left
        if (remainingDiceCount == 1)
        {
            // Exception: if player is leading by >3000, take the risk and roll
            bool playerLeadingByMuch = (playerTotalScore - aiAccumulatedPoints) > 3000;
            
            if (playerLeadingByMuch)
            {
                if (enableDebugLogs)
                    Debug.Log($"Decision: CONTINUE (1 dice left but player leading by {playerTotalScore - aiAccumulatedPoints} - taking risk)");
                return new AIStopDecision 
                { 
                    ShouldStop = false, 
                    DecisionReason = "1 dice left, player leading by >3000 - taking risk" 
                };
            }
            
            if (enableDebugLogs)
                Debug.Log("Decision: STOP (1 dice left - banking points)");
            return new AIStopDecision 
            { 
                ShouldStop = true, 
                DecisionReason = "1 dice left - banking points" 
            };
        }
        
        // 3. If 3 dice left
        if (remainingDiceCount == 3)
        {
            bool isLeading = scoreDifference > 1500;
            bool isEqual = scoreDifference >= -1500 && scoreDifference <= 1500;
            bool isLosing = scoreDifference < -1500;
            
            if (isLeading && aiTurnScore > 3500)
            {
                if (enableDebugLogs)
                    Debug.Log($"Decision: STOP (3 dice, leading by {scoreDifference}, turn score {aiTurnScore} points)");
                return new AIStopDecision 
                { 
                    ShouldStop = true, 
                    DecisionReason = $"3 dice left, leading with {aiTurnScore} turn points" 
                };
            }
            else if (isEqual && aiTurnScore > 1300)
            {
                if (enableDebugLogs)
                    Debug.Log($"Decision: STOP (3 dice, equal game, turn score {aiTurnScore} points)");
                return new AIStopDecision 
                { 
                    ShouldStop = true, 
                    DecisionReason = $"3 dice left, equal game with {aiTurnScore} turn points" 
                };
            }
            else if (isLosing && aiTurnScore > 2500)
            {
                if (enableDebugLogs)
                    Debug.Log($"Decision: STOP (3 dice, losing by {-scoreDifference}, turn score {aiTurnScore} points)");
                return new AIStopDecision 
                { 
                    ShouldStop = true, 
                    DecisionReason = $"3 dice left, losing but have {aiTurnScore} turn points" 
                };
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"Decision: CONTINUE (3 dice, conditions not met for stopping)");
                return new AIStopDecision 
                { 
                    ShouldStop = false, 
                    DecisionReason = "3 dice left, need more points" 
                };
            }
        }
        
        // 4. If 2 dice left
        if (remainingDiceCount == 2)
        {
            bool isNotLosing = scoreDifference >= -1500;
            bool isLosing = scoreDifference < -1500;
            
            if (aiTurnScore > 550 && isNotLosing)
            {
                if (enableDebugLogs)
                    Debug.Log($"Decision: STOP (2 dice, turn score {aiTurnScore} points and not losing)");
                return new AIStopDecision 
                { 
                    ShouldStop = true, 
                    DecisionReason = $"2 dice left, have {aiTurnScore} turn points and not losing" 
                };
            }
            else if (aiTurnScore > 1000 && isLosing)
            {
                if (enableDebugLogs)
                    Debug.Log($"Decision: STOP (2 dice, turn score {aiTurnScore} points despite losing)");
                return new AIStopDecision 
                { 
                    ShouldStop = true, 
                    DecisionReason = $"2 dice left, have {aiTurnScore} turn points" 
                };
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"Decision: CONTINUE (2 dice, conditions not met for stopping)");
                return new AIStopDecision 
                { 
                    ShouldStop = false, 
                    DecisionReason = "2 dice left, need more points" 
                };
            }
        }
        
        // Default: continue (shouldn't reach here with current logic)
        if (enableDebugLogs)
            Debug.Log($"Decision: CONTINUE (default fallback)");
        return new AIStopDecision 
        { 
            ShouldStop = false, 
            DecisionReason = "Default - continue" 
        };
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
            bool useRiggedDice = !currentTurnState.UseCustomStopLogic;
            
            if (enableDebugLogs)
                Debug.Log($"🎲 RegenerateRemainingDice: count={remainingCount}, UseCustomLogic={currentTurnState.UseCustomStopLogic}, useRiggedDice={useRiggedDice}");
            
            var newDice = diceGenerator.GenerateRandomDice(remainingCount, useRiggedDice);
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
        
        // Log zonk to UI
        if (actionLog != null)
        {
            actionLog.LogAIZonk();
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
        
        // CRITICAL DEBUG: Show what array we're actually searching
        Debug.Log($"🔍 GetDiceIndicesForCombination: Combination={combination.rule}, Points={combination.points}");
        Debug.Log($"🔍 Internal dice array: [{string.Join(",", currentDice)}]");
        Debug.Log($"🔍 Looking for dice to match this combination...");
        
        // First, check if the combination already has stored dice indices (from when it was selected)
        // These are the correct indices calculated by the combination strategy
        if (combination.diceIndices != null && combination.diceIndices.Count > 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"✓ Using stored dice indices: [{string.Join(",", combination.diceIndices)}]");
                var storedValues = combination.diceIndices.Select(i => i < currentDice.Count ? currentDice[i] : -1).ToList();
                Debug.Log($"  Stored indices map to values: [{string.Join(",", storedValues)}]");
            }
            return new List<int>(combination.diceIndices);
        }
        
        if (enableDebugLogs)
            Debug.Log("⚠ No stored dice indices found, calculating from scratch...");
        
        // Identify specific dice based on combination type
        switch (combination.rule)
        {
            case Rule.One:
                // Single 1 or 5 - determine which based on points
                // Single 1 = 100 points, Single 5 = 50 points
                if (combination.points == 100)
                    indices.AddRange(FindDiceWithValue(currentDice, 1, 1));
                else if (combination.points == 50)
                    indices.AddRange(FindDiceWithValue(currentDice, 5, 1));
                else
                {
                    // Fallback: check what's available
                    if (currentDice.Contains(1))
                        indices.AddRange(FindDiceWithValue(currentDice, 1, 1));
                    else if (currentDice.Contains(5))
                        indices.AddRange(FindDiceWithValue(currentDice, 5, 1));
                }
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
        // For length 3, 4, 5, or 6, find any consecutive sequence
        // Sort dice with their indices, then find consecutive values
        var sorted = dice.Select((value, index) => new { value, index })
                         .OrderBy(x => x.value)
                         .ToList();
        
        List<int> indices = new List<int>();
        int consecutiveCount = 1;
        int lastValue = sorted[0].value;
        indices.Add(sorted[0].index);
        
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].value == lastValue + 1)
            {
                consecutiveCount++;
                indices.Add(sorted[i].index);
                lastValue = sorted[i].value;
                
                if (consecutiveCount >= length)
                {
                    return indices.Take(length).ToList();
                }
            }
            else if (sorted[i].value != lastValue)
            {
                // Start a new sequence
                consecutiveCount = 1;
                indices.Clear();
                indices.Add(sorted[i].index);
                lastValue = sorted[i].value;
            }
            // If sorted[i].value == lastValue, skip duplicates
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
            
            // Log dice roll to UI
            if (actionLog != null)
            {
                actionLog.LogAIDiceRoll(iteration.InitialDice);
            }
            
            // Show AI dice for this iteration
            if (diceController != null)
            {
                diceController.SpawnAIDice(iteration.InitialDice);
            }
            
            // AI thinking delay
            yield return new WaitForSeconds(aggressiveIterationThinkDelay);
            
            if (iteration.SelectedCombination != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"=== AI SELECTED (Aggressive) ===");
                    Debug.Log($"Combination: {iteration.SelectedCombination.description}");
                    Debug.Log($"Points: +{iteration.SelectedCombination.points}");
                }
                
                // Log combination selection to UI
                if (actionLog != null)
                {
                    actionLog.LogAICombination(
                        iteration.SelectedCombination.description,
                        iteration.SelectedCombination.points,
                        iteration.DiceUsed
                    );
                }
                
                // Show combination selection
                yield return new WaitForSeconds(aggressiveCombinationDelay);
                
                // Remove dice using actual indices from the combination
                if (diceController != null && iteration.DiceIndicesUsed != null && iteration.DiceIndicesUsed.Count > 0)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"Removing dice at indices: [{string.Join(",", iteration.DiceIndicesUsed)}]");
                    }
                    diceController.RemoveAIDice(iteration.DiceIndicesUsed);
                }
                else if (diceController != null && iteration.RemainingDice < iteration.InitialDice.Count)
                {
                    // Fallback: remove from start if indices not available
                    int diceToRemove = iteration.InitialDice.Count - iteration.RemainingDice;
                    List<int> indicesToRemove = new List<int>();
                    for (int j = 0; j < diceToRemove; j++)
                    {
                        indicesToRemove.Add(j);
                    }
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning($"No dice indices available, using fallback removal: [{string.Join(",", indicesToRemove)}]");
                    }
                    diceController.RemoveAIDice(indicesToRemove);
                }
                
                // Show dice removal result
                yield return new WaitForSeconds(aggressiveDiceRemovalDelay);
            }
            
            // Check for hot streak
            if (iteration.RemainingDice == 0 && i < result.Iterations.Count - 1)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("=== AGGRESSIVE HOT STREAK! ===");
                    Debug.Log("All dice used! Fresh dice incoming...");
                }
                
                // Log hot streak to UI
                if (actionLog != null)
                {
                    actionLog.LogAIHotStreak();
                }
                
                yield return new WaitForSeconds(aggressiveHotStreakDelay);
            }
            else if (iteration.RemainingDice > 0)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"AI (Aggressive) continues with {iteration.RemainingDice} dice...");
                }
                
                // Log decision to UI
                if (actionLog != null && iteration.ContinueDecision)
                {
                    actionLog.LogAIDecision(true, $"{iteration.RemainingDice} dice remaining");
                }
                
                yield return new WaitForSeconds(aggressiveContinueDelay);
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
        
        // Log zonk if it occurred
        if (result.ZonkOccurred && actionLog != null)
        {
            // First log the dice that caused the zonk
            if (result.ZonkDice != null && result.ZonkDice.Count > 0)
            {
                actionLog.LogAIDiceRoll(result.ZonkDice);
            }
            // Then log the zonk message
            actionLog.LogAIZonk();
        }
        
        // Log turn end
        if (actionLog != null && !result.ZonkOccurred)
        {
            actionLog.LogAITurnEnd(result.TotalPointsScored, result.HotStreakCount + 1);
        }
        
        yield return new WaitForSeconds(aggressiveVisualizationDelay);
    }
}