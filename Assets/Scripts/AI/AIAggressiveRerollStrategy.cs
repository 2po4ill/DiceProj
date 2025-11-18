using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HybridEnemyAI;

/// <summary>
/// Implements aggressive reroll strategy for maximum point pursuit
/// Focuses on minimum dice usage and iterative rerolling with remaining dice
/// Requirements: 3.1, 3.2, 3.3, 5.4, 5.5
/// </summary>
public class AIAggressiveRerollStrategy : MonoBehaviour
{
    [Header("Dependencies")]
    public AICombinationStrategy combinationStrategy;
    public AIRiskCalculator riskCalculator;
    public AIGameStateAnalyzer gameStateAnalyzer;
    
    [Header("Configuration")]
    public AIConfiguration config;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool showRerollDetails = false;
    
    [Header("Strategy Tracking")]
    [SerializeField] private AggressiveRerollState currentRerollState;
    [SerializeField] private List<RerollIteration> rerollHistory = new List<RerollIteration>();
    
    /// <summary>
    /// State tracking for aggressive reroll strategy
    /// </summary>
    [System.Serializable]
    public class AggressiveRerollState
    {
        public int TotalIterations; // Number of hot streaks (full 6-dice clears)
        public int MaxIterationsAllowed; // Max hot streaks allowed (5)
        public int TotalSelections; // Total number of dice selections made
        public int TotalPointsScored;
        public int PointsPerTurnCap;
        public int RemainingDiceCount;
        public bool CapEnforcementActive;
        public List<CombinationResult> SelectedCombinations = new List<CombinationResult>();
        
        public void Reset()
        {
            TotalIterations = 0;
            MaxIterationsAllowed = 5;
            TotalSelections = 0;
            TotalPointsScored = 0;
            PointsPerTurnCap = 500;
            RemainingDiceCount = 6;
            CapEnforcementActive = false;
            SelectedCombinations.Clear();
        }
    }
    
    /// <summary>
    /// Record of a single dice selection (not a full iteration)
    /// </summary>
    [System.Serializable]
    public class RerollIteration
    {
        public int IterationNumber; // Which hot streak cycle (1-5)
        public int SelectionNumber; // Which selection within this cycle
        public List<int> InitialDice;
        public CombinationResult SelectedCombination;
        public List<int> DiceIndicesUsed; // Actual indices of dice that were selected
        public int DiceUsed;
        public int RemainingDice;
        public int PointsGained;
        public string SelectionReason;
        public bool ContinueDecision;
        public string DecisionReason;
        public bool IsHotStreak; // True if this selection cleared all dice
        
        public override string ToString()
        {
            return $"Iter {IterationNumber}.{SelectionNumber}: {SelectedCombination?.rule} ({DiceUsed} dice, {PointsGained} pts) -> {RemainingDice} remaining";
        }
    }
    
    void Start()
    {
        ValidateComponents();
        InitializeConfiguration();
        ResetRerollState();
    }
    
    /// <summary>
    /// Main entry point for aggressive reroll strategy
    /// </summary>
    public AggressiveRerollResult ExecuteAggressiveReroll(List<int> initialDice, BehaviorMode mode, 
                                                         int currentTurnScore, int pointsPerTurnCap)
    {
        if (mode != BehaviorMode.AGGRESSIVE)
        {
            if (enableDebugLogs)
                Debug.LogWarning("AIAggressiveRerollStrategy: Called with non-aggressive mode");
        }
        
        // Initialize reroll state
        InitializeRerollState(initialDice, currentTurnScore, pointsPerTurnCap);
        
        var result = new AggressiveRerollResult();
        result.InitialDice = new List<int>(initialDice);
        result.StartTime = System.DateTime.Now;
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== AGGRESSIVE REROLL STRATEGY START ===");
            Debug.Log($"Initial Dice: [{string.Join(",", initialDice)}]");
            Debug.Log($"Current Turn Score: {currentTurnScore}");
            Debug.Log($"Points Per Turn Cap: {pointsPerTurnCap}");
        }
        
        // Execute iterative rerolling
        List<int> currentDice = new List<int>(initialDice);
        currentRerollState.TotalIterations = 1; // Start at iteration 1 (first set of 6 dice)
        
        while (ShouldContinueRerolling(currentDice, result))
        {
            var selection = ExecuteRerollIteration(currentDice, result);
            
            if (selection == null || selection.SelectedCombination == null)
            {
                // No valid combination found (Zonk)
                // Store the dice that caused the zonk
                result.ZonkOccurred = true;
                result.ZonkDice = new List<int>(currentDice);
                result.FinalReason = "Zonk - no valid combinations found";
                break;
            }
            
            // Process the selection
            ProcessRerollIteration(selection, result);
            
            // Check for hot streak (all dice used)
            if (selection.RemainingDice == 0)
            {
                selection.IsHotStreak = true;
                currentRerollState.TotalIterations++; // Increment iteration on hot streak
                
                if (enableDebugLogs)
                    Debug.Log($"Hot streak! All dice used - starting iteration {currentRerollState.TotalIterations}");
                
                // Check iteration limit BEFORE generating new dice
                if (currentRerollState.TotalIterations > currentRerollState.MaxIterationsAllowed)
                {
                    result.FinalReason = $"Iteration limit reached ({currentRerollState.MaxIterationsAllowed} hot streaks)";
                    break;
                }
                
                currentDice = GenerateNewDiceSet(6);
                result.HotStreakCount++;
            }
            else
            {
                // Generate new dice for remaining positions (same iteration)
                currentDice = GenerateNewDiceSet(selection.RemainingDice);
            }
        }
        
        // Finalize result
        FinalizeAggressiveRerollResult(result);
        
        if (enableDebugLogs)
        {
            LogAggressiveRerollSummary(result);
        }
        
        return result;
    }
    
    /// <summary>
    /// Executes a single dice selection with minimum dice strategy
    /// </summary>
    RerollIteration ExecuteRerollIteration(List<int> currentDice, AggressiveRerollResult result)
    {
        currentRerollState.TotalSelections++;
        
        var selection = new RerollIteration
        {
            IterationNumber = currentRerollState.TotalIterations,
            SelectionNumber = currentRerollState.TotalSelections,
            InitialDice = new List<int>(currentDice),
            IsHotStreak = false
        };
        
        if (enableDebugLogs)
        {
            Debug.Log($"--- Aggressive Selection {selection.IterationNumber}.{selection.SelectionNumber} ---");
            Debug.Log($"Current Dice: [{string.Join(",", currentDice)}]");
        }
        
        // Find minimum dice combination using hierarchical strategy
        var selectedCombination = SelectMinimumDiceCombination(currentDice);
        
        if (selectedCombination == null)
        {
            if (enableDebugLogs)
                Debug.Log("No valid combination found - Zonk!");
            return null;
        }
        
        // Fill selection details
        selection.SelectedCombination = selectedCombination;
        selection.DiceIndicesUsed = selectedCombination.diceIndices != null ? 
            new List<int>(selectedCombination.diceIndices) : new List<int>();
        selection.DiceUsed = GetDiceUsedForCombination(selectedCombination.rule);
        selection.RemainingDice = currentDice.Count - selection.DiceUsed;
        selection.PointsGained = selectedCombination.points;
        selection.SelectionReason = GenerateSelectionReason(selectedCombination, currentDice.Count);
        
        // Make continue/stop decision
        var continueDecision = MakeContinueDecision(selection, result);
        selection.ContinueDecision = continueDecision.ShouldContinue;
        selection.DecisionReason = continueDecision.Reason;
        
        if (showRerollDetails)
        {
            Debug.Log($"Selected: {selectedCombination.rule} ({selection.DiceUsed} dice, {selection.PointsGained} pts)");
            Debug.Log($"Remaining: {selection.RemainingDice} dice");
            Debug.Log($"Decision: {(selection.ContinueDecision ? "CONTINUE" : "STOP")} - {selection.DecisionReason}");
        }
        
        return selection;
    }
    
    /// <summary>
    /// Selects combination using two-phase aggressive strategy:
    /// Phase 1 (4-6 dice): Accumulate points (aim for 500+ total)
    /// Phase 2 (1-3 dice): Full clear mode (try to use all dice)
    /// </summary>
    CombinationResult SelectMinimumDiceCombination(List<int> diceValues)
    {
        if (diceValues == null || diceValues.Count == 0)
            return null;
        
        int diceCount = diceValues.Count;
        int currentScore = currentRerollState.TotalPointsScored;
        
        // PHASE 1: Accumulation (4-6 dice) - Build up to 500+ points
        if (diceCount >= 4 && currentScore < 500)
        {
            if (enableDebugLogs)
                Debug.Log($"Phase 1 (Accumulation): {diceCount} dice, {currentScore} pts - seeking high-value combinations");
            
            return SelectAccumulationCombination(diceValues);
        }
        
        // PHASE 2: Full Clear (1-3 dice OR 500+ points reached) - Try to clear all dice
        if (enableDebugLogs)
            Debug.Log($"Phase 2 (Full Clear): {diceCount} dice, {currentScore} pts - prioritizing hot streaks");
        
        // Use standard minimum dice strategy (already prioritizes hot streaks)
        var strategyResult = combinationStrategy.FindMinimumDiceCombination(diceValues, BehaviorMode.AGGRESSIVE);
        
        if (strategyResult == null)
            return null;
        
        var combination = strategyResult.combination;
        
        // Validate combination meets aggressive criteria
        if (ValidateAggressiveCombination(combination, diceValues.Count))
        {
            return combination;
        }
        
        // Fallback: find any valid combination
        var allCombinations = combinationStrategy.FindAllValidCombinations(diceValues);
        if (allCombinations.Count > 0)
        {
            return allCombinations.OrderByDescending(c => 
                GetDiceUsedForCombination(c.rule) > 0 ? (float)c.points / GetDiceUsedForCombination(c.rule) : 0f).First();
        }
        
        return null;
    }
    
    /// <summary>
    /// Selects combination for accumulation phase - prioritizes high points over minimum dice
    /// </summary>
    CombinationResult SelectAccumulationCombination(List<int> diceValues)
    {
        var allCombinations = combinationStrategy.FindAllValidCombinations(diceValues);
        
        if (allCombinations.Count == 0)
            return null;
        
        // Filter to combinations worth 200+ points (meaningful accumulation)
        var highValueCombos = allCombinations.Where(c => c.points >= 200).ToList();
        
        if (highValueCombos.Count > 0)
        {
            // Select highest point combination (not minimum dice!)
            var bestCombo = highValueCombos.OrderByDescending(c => c.points).First();
            
            if (enableDebugLogs)
                Debug.Log($"Accumulation: Selected {bestCombo.rule} for {bestCombo.points} pts (high value strategy)");
            
            return bestCombo;
        }
        
        // No high-value combos available, fall back to best available
        var fallbackCombo = allCombinations.OrderByDescending(c => c.points).First();
        
        if (enableDebugLogs)
            Debug.Log($"Accumulation: No high-value combos, taking {fallbackCombo.rule} for {fallbackCombo.points} pts");
        
        return fallbackCombo;
    }
    
    /// <summary>
    /// Validates that combination meets aggressive strategy criteria
    /// </summary>
    bool ValidateAggressiveCombination(CombinationResult combination, int totalDiceCount)
    {
        if (combination == null || combination.points <= 0)
            return false;
        
        int diceUsed = GetDiceUsedForCombination(combination.rule);
        if (diceUsed <= 0 || diceUsed > totalDiceCount)
            return false;
        
        // Aggressive validation criteria
        float pointsPerDice = (float)combination.points / diceUsed;
        
        // Minimum efficiency threshold for aggressive mode
        float minEfficiency = totalDiceCount <= 2 ? 25f : 40f; // Lower threshold for risky situations
        
        if (pointsPerDice < minEfficiency)
        {
            if (enableDebugLogs)
                Debug.Log($"Combination rejected: efficiency {pointsPerDice:F1} below threshold {minEfficiency}");
            return false;
        }
        
        // Avoid using all dice for low points (unless forced)
        if (diceUsed == totalDiceCount && combination.points < 300 && totalDiceCount > 3)
        {
            if (enableDebugLogs)
                Debug.Log($"Combination rejected: uses all {diceUsed} dice for only {combination.points} points");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Makes continue/stop decision for current selection
    /// </summary>
    ContinueDecision MakeContinueDecision(RerollIteration selection, AggressiveRerollResult result)
    {
        var decision = new ContinueDecision();
        
        // Update turn state for decision making
        int projectedTurnScore = currentRerollState.TotalPointsScored + selection.PointsGained;
        
        // ALWAYS CONTINUE on iteration 1 (first set of 6 dice)
        if (currentRerollState.TotalIterations == 1)
        {
            decision.ShouldContinue = true;
            decision.Reason = "Iteration 1 - always continue with initial dice";
            return decision;
        }
        
        // Hot streak check - always continue if all dice used
        if (selection.RemainingDice <= 0)
        {
            decision.ShouldContinue = true;
            decision.Reason = "Hot streak - all dice used, continuing with new set";
            return decision;
        }
        
        // Use dual probability system for decision
        // NOTE: Pass TotalIterations (hot streak count) not selection count
        var stopDecision = riskCalculator.CalculateStopDecision(
            currentRerollState.TotalIterations, // Hot streak count, not selection count
            selection.RemainingDice,
            currentRerollState.SelectedCombinations.Count,
            projectedTurnScore,
            currentRerollState.PointsPerTurnCap,
            true // isAggressive
        );
        
        decision.ShouldContinue = !stopDecision.ShouldStop;
        decision.Reason = stopDecision.DecisionReason;
        decision.StopDecision = stopDecision;
        
        return decision;
    }
    
    /// <summary>
    /// Processes a completed reroll iteration
    /// </summary>
    void ProcessRerollIteration(RerollIteration iteration, AggressiveRerollResult result)
    {
        // Add to history
        rerollHistory.Add(iteration);
        result.Iterations.Add(iteration);
        
        // Update state
        currentRerollState.SelectedCombinations.Add(iteration.SelectedCombination);
        currentRerollState.TotalPointsScored += iteration.PointsGained;
        currentRerollState.RemainingDiceCount = iteration.RemainingDice;
        
        // Update result
        result.TotalPointsScored += iteration.PointsGained;
        result.TotalCombinations++;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Iteration {iteration.IterationNumber} processed: +{iteration.PointsGained} points " +
                     $"(Total: {currentRerollState.TotalPointsScored})");
        }
    }
    
    /// <summary>
    /// Determines if rerolling should continue
    /// </summary>
    bool ShouldContinueRerolling(List<int> currentDice, AggressiveRerollResult result)
    {
        // Check if we have dice to work with
        if (currentDice == null || currentDice.Count == 0)
            return false;
        
        // Check iteration limit
        if (currentRerollState.TotalIterations >= currentRerollState.MaxIterationsAllowed)
            return false;
        
        // Check if we've already decided to stop in the last iteration
        if (rerollHistory.Count > 0)
        {
            var lastIteration = rerollHistory.Last();
            if (!lastIteration.ContinueDecision && lastIteration.RemainingDice > 0)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Generates new dice set for rerolling with aggressive mode advantage
    /// When dice count is low (< 4), gives 90% chance for one die to be 1 or 5
    /// </summary>
    List<int> GenerateNewDiceSet(int count)
    {
        var newDice = new List<int>();
        
        // Aggressive mode advantage: Help AI when in risky situation (< 4 dice)
        bool shouldApplyAdvantage = count < 4 && count > 0;
        bool advantageApplied = false;
        
        for (int i = 0; i < count; i++)
        {
            int value;
            
            // Apply advantage to ONE random die (90% chance to be 1 or 5)
            if (shouldApplyAdvantage && !advantageApplied && Random.Range(0f, 1f) < 0.9f)
            {
                // 90% chance: Make this die a 1 or 5
                value = Random.Range(0, 2) == 0 ? 1 : 5;
                advantageApplied = true;
                
                if (enableDebugLogs)
                    Debug.Log($"Aggressive advantage applied: Die {i} set to {value}");
            }
            else
            {
                // Normal random die
                value = Random.Range(1, 7);
            }
            
            newDice.Add(value);
        }
        
        if (enableDebugLogs)
        {
            string advantageNote = advantageApplied ? " (advantage applied)" : "";
            Debug.Log($"Generated {count} new dice: [{string.Join(",", newDice)}]{advantageNote}");
        }
        
        return newDice;
    }
    
    /// <summary>
    /// Generates selection reason for debugging
    /// </summary>
    string GenerateSelectionReason(CombinationResult combination, int totalDice)
    {
        int diceUsed = GetDiceUsedForCombination(combination.rule);
        float efficiency = diceUsed > 0 ? (float)combination.points / diceUsed : 0f;
        
        return $"Minimum dice strategy: {combination.rule} uses {diceUsed}/{totalDice} dice " +
               $"for {combination.points} points (efficiency: {efficiency:F1})";
    }
    
    /// <summary>
    /// Initializes reroll state for new execution
    /// </summary>
    void InitializeRerollState(List<int> initialDice, int currentTurnScore, int pointsPerTurnCap)
    {
        currentRerollState.Reset();
        currentRerollState.TotalPointsScored = currentTurnScore;
        currentRerollState.PointsPerTurnCap = pointsPerTurnCap;
        currentRerollState.RemainingDiceCount = initialDice.Count;
        currentRerollState.MaxIterationsAllowed = 5; // Aggressive mode allows up to 5 iterations
        
        rerollHistory.Clear();
    }
    
    /// <summary>
    /// Finalizes the aggressive reroll result
    /// </summary>
    void FinalizeAggressiveRerollResult(AggressiveRerollResult result)
    {
        result.EndTime = System.DateTime.Now;
        result.ExecutionTime = result.EndTime - result.StartTime;
        result.FinalDiceCount = currentRerollState.RemainingDiceCount;
        result.IterationLimitReached = currentRerollState.TotalIterations >= currentRerollState.MaxIterationsAllowed;
        result.CapEnforcementTriggered = currentRerollState.CapEnforcementActive;
        
        // Calculate efficiency metrics
        if (result.Iterations.Count > 0)
        {
            result.AveragePointsPerIteration = (float)result.TotalPointsScored / result.Iterations.Count;
            result.AverageDiceUsedPerIteration = (float)result.Iterations.Average(i => i.DiceUsed);
        }
        
        if (string.IsNullOrEmpty(result.FinalReason))
        {
            result.FinalReason = "Strategy completed successfully";
        }
    }
    
    /// <summary>
    /// Logs comprehensive summary of aggressive reroll execution
    /// </summary>
    void LogAggressiveRerollSummary(AggressiveRerollResult result)
    {
        Debug.Log($"=== AGGRESSIVE REROLL STRATEGY COMPLETE ===");
        Debug.Log($"Execution Time: {result.ExecutionTime.TotalMilliseconds:F1}ms");
        Debug.Log($"Total Selections: {result.Iterations.Count}");
        Debug.Log($"Total Hot Streaks: {result.HotStreakCount}");
        Debug.Log($"Total Points Scored: {result.TotalPointsScored}");
        Debug.Log($"Total Combinations: {result.TotalCombinations}");
        Debug.Log($"Final Reason: {result.FinalReason}");
        
        if (result.Iterations.Count > 0)
        {
            Debug.Log($"Average Points/Selection: {result.AveragePointsPerIteration:F1}");
            Debug.Log($"Average Dice Used/Selection: {result.AverageDiceUsedPerIteration:F1}");
        }
        
        Debug.Log("Selection Details:");
        foreach (var selection in result.Iterations)
        {
            string hotStreakMarker = selection.IsHotStreak ? " 🔥 HOT STREAK" : "";
            Debug.Log($"  {selection}{hotStreakMarker}");
        }
    }
    
    /// <summary>
    /// Validates required component references
    /// </summary>
    void ValidateComponents()
    {
        if (combinationStrategy == null)
        {
            combinationStrategy = GetComponent<AICombinationStrategy>();
            if (combinationStrategy == null)
            {
                Debug.LogError("AIAggressiveRerollStrategy: AICombinationStrategy component required!");
            }
        }
        
        if (riskCalculator == null)
        {
            riskCalculator = GetComponent<AIRiskCalculator>();
            if (riskCalculator == null)
            {
                Debug.LogError("AIAggressiveRerollStrategy: AIRiskCalculator component required!");
            }
        }
        
        if (gameStateAnalyzer == null)
        {
            gameStateAnalyzer = GetComponent<AIGameStateAnalyzer>();
            if (gameStateAnalyzer == null)
            {
                Debug.LogWarning("AIAggressiveRerollStrategy: AIGameStateAnalyzer not found - using defaults");
            }
        }
    }
    
    /// <summary>
    /// Initializes configuration
    /// </summary>
    void InitializeConfiguration()
    {
        if (config == null)
        {
            config = new AIConfiguration();
            if (enableDebugLogs)
                Debug.Log("AIAggressiveRerollStrategy: Using default configuration");
        }
    }
    
    /// <summary>
    /// Resets reroll state
    /// </summary>
    void ResetRerollState()
    {
        if (currentRerollState == null)
            currentRerollState = new AggressiveRerollState();
        
        currentRerollState.Reset();
        rerollHistory.Clear();
    }
    
    /// <summary>
    /// Updates configuration at runtime
    /// </summary>
    public void UpdateConfiguration(AIConfiguration newConfig)
    {
        config = newConfig;
        if (enableDebugLogs)
            Debug.Log("AIAggressiveRerollStrategy: Configuration updated");
    }
    
    /// <summary>
    /// Gets current reroll state for debugging
    /// </summary>
    public AggressiveRerollState GetCurrentState()
    {
        return currentRerollState;
    }
    
    /// <summary>
    /// Gets reroll history for analysis
    /// </summary>
    public List<RerollIteration> GetRerollHistory()
    {
        return new List<RerollIteration>(rerollHistory);
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

/// <summary>
/// Result of aggressive reroll strategy execution
/// </summary>
[System.Serializable]
public class AggressiveRerollResult
{
    [Header("Execution Info")]
    public List<int> InitialDice = new List<int>();
    public System.DateTime StartTime;
    public System.DateTime EndTime;
    public System.TimeSpan ExecutionTime;
    public string FinalReason = "";
    
    [Header("Results")]
    public List<AIAggressiveRerollStrategy.RerollIteration> Iterations = new List<AIAggressiveRerollStrategy.RerollIteration>();
    public int TotalPointsScored = 0;
    public int TotalCombinations = 0;
    public int HotStreakCount = 0;
    public int FinalDiceCount = 0;
    
    [Header("Strategy Metrics")]
    public bool IterationLimitReached = false;
    public bool CapEnforcementTriggered = false;
    public bool ZonkOccurred = false;
    public List<int> ZonkDice = new List<int>(); // The dice that caused the zonk
    public float AveragePointsPerIteration = 0f;
    public float AverageDiceUsedPerIteration = 0f;
}

/// <summary>
/// Decision result for continue/stop choice
/// </summary>
[System.Serializable]
public class ContinueDecision
{
    public bool ShouldContinue;
    public string Reason;
    public AIStopDecision StopDecision;
}