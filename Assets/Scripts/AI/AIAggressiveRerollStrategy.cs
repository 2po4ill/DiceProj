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
        public int TotalIterations;
        public int MaxIterationsAllowed;
        public int TotalPointsScored;
        public int PointsPerTurnCap;
        public int RemainingDiceCount;
        public bool CapEnforcementActive;
        public List<CombinationResult> SelectedCombinations = new List<CombinationResult>();
        
        public void Reset()
        {
            TotalIterations = 0;
            MaxIterationsAllowed = 5;
            TotalPointsScored = 0;
            PointsPerTurnCap = 500;
            RemainingDiceCount = 6;
            CapEnforcementActive = false;
            SelectedCombinations.Clear();
        }
    }
    
    /// <summary>
    /// Record of a single reroll iteration
    /// </summary>
    [System.Serializable]
    public class RerollIteration
    {
        public int IterationNumber;
        public List<int> InitialDice;
        public CombinationResult SelectedCombination;
        public int DiceUsed;
        public int RemainingDice;
        public int PointsGained;
        public string SelectionReason;
        public bool ContinueDecision;
        public string DecisionReason;
        
        public override string ToString()
        {
            return $"Iter {IterationNumber}: {SelectedCombination?.rule} ({DiceUsed} dice, {PointsGained} pts) -> {RemainingDice} remaining";
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
        
        while (ShouldContinueRerolling(currentDice, result))
        {
            var iteration = ExecuteRerollIteration(currentDice, result);
            
            if (iteration == null || iteration.SelectedCombination == null)
            {
                // No valid combination found (Zonk)
                result.ZonkOccurred = true;
                result.FinalReason = "Zonk - no valid combinations found";
                break;
            }
            
            // Process the iteration
            ProcessRerollIteration(iteration, result);
            
            // Check for hot streak (all dice used)
            if (iteration.RemainingDice == 0)
            {
                if (enableDebugLogs)
                    Debug.Log("Hot streak! All dice used - generating new full set");
                
                currentDice = GenerateNewDiceSet(6);
                result.HotStreakCount++;
            }
            else
            {
                // Generate new dice for remaining positions
                currentDice = GenerateNewDiceSet(iteration.RemainingDice);
            }
            
            // Check iteration limit
            if (currentRerollState.TotalIterations >= currentRerollState.MaxIterationsAllowed)
            {
                result.FinalReason = $"Iteration limit reached ({currentRerollState.MaxIterationsAllowed})";
                break;
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
    /// Executes a single reroll iteration with minimum dice selection
    /// </summary>
    RerollIteration ExecuteRerollIteration(List<int> currentDice, AggressiveRerollResult result)
    {
        currentRerollState.TotalIterations++;
        
        var iteration = new RerollIteration
        {
            IterationNumber = currentRerollState.TotalIterations,
            InitialDice = new List<int>(currentDice)
        };
        
        if (enableDebugLogs)
        {
            Debug.Log($"--- Aggressive Reroll Iteration {iteration.IterationNumber} ---");
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
        
        // Fill iteration details
        iteration.SelectedCombination = selectedCombination;
        iteration.DiceUsed = GetDiceUsedForCombination(selectedCombination.rule);
        iteration.RemainingDice = currentDice.Count - iteration.DiceUsed;
        iteration.PointsGained = selectedCombination.points;
        iteration.SelectionReason = GenerateSelectionReason(selectedCombination, currentDice.Count);
        
        // Make continue/stop decision
        var continueDecision = MakeContinueDecision(iteration, result);
        iteration.ContinueDecision = continueDecision.ShouldContinue;
        iteration.DecisionReason = continueDecision.Reason;
        
        if (showRerollDetails)
        {
            Debug.Log($"Selected: {selectedCombination.rule} ({iteration.DiceUsed} dice, {iteration.PointsGained} pts)");
            Debug.Log($"Remaining: {iteration.RemainingDice} dice");
            Debug.Log($"Decision: {(iteration.ContinueDecision ? "CONTINUE" : "STOP")} - {iteration.DecisionReason}");
        }
        
        return iteration;
    }
    
    /// <summary>
    /// Selects combination using minimum dice algorithm for aggressive strategy
    /// </summary>
    CombinationResult SelectMinimumDiceCombination(List<int> diceValues)
    {
        if (diceValues == null || diceValues.Count == 0)
            return null;
        
        // Use combination strategy to find minimum dice combination
        var strategyResult = combinationStrategy.FindMinimumDiceCombination(diceValues, BehaviorMode.AGGRESSIVE);
        
        if (strategyResult == null)
            return null;
        
        // Apply aggressive-specific selection criteria
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
            // Select combination with best points-per-dice ratio
            return allCombinations.OrderByDescending(c => 
                GetDiceUsedForCombination(c.rule) > 0 ? (float)c.points / GetDiceUsedForCombination(c.rule) : 0f).First();
        }
        
        return null;
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
    /// Makes continue/stop decision for current iteration
    /// </summary>
    ContinueDecision MakeContinueDecision(RerollIteration iteration, AggressiveRerollResult result)
    {
        var decision = new ContinueDecision();
        
        // Update turn state for decision making
        int projectedTurnScore = currentRerollState.TotalPointsScored + iteration.PointsGained;
        
        // Check hard stops first
        if (currentRerollState.TotalIterations >= currentRerollState.MaxIterationsAllowed)
        {
            decision.ShouldContinue = false;
            decision.Reason = $"Iteration limit reached ({currentRerollState.MaxIterationsAllowed})";
            return decision;
        }
        
        if (iteration.RemainingDice <= 0)
        {
            decision.ShouldContinue = true; // Hot streak - always continue
            decision.Reason = "Hot streak - all dice used, continuing with new set";
            return decision;
        }
        
        // Use dual probability system for decision
        var stopDecision = riskCalculator.CalculateStopDecision(
            currentRerollState.TotalIterations,
            iteration.RemainingDice,
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
    /// Generates new dice set for rerolling
    /// </summary>
    List<int> GenerateNewDiceSet(int count)
    {
        var newDice = new List<int>();
        for (int i = 0; i < count; i++)
        {
            newDice.Add(Random.Range(1, 7));
        }
        
        if (enableDebugLogs)
            Debug.Log($"Generated {count} new dice: [{string.Join(",", newDice)}]");
        
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
        Debug.Log($"Total Iterations: {result.Iterations.Count}");
        Debug.Log($"Total Points Scored: {result.TotalPointsScored}");
        Debug.Log($"Total Combinations: {result.TotalCombinations}");
        Debug.Log($"Hot Streaks: {result.HotStreakCount}");
        Debug.Log($"Final Reason: {result.FinalReason}");
        
        if (result.Iterations.Count > 0)
        {
            Debug.Log($"Average Points/Iteration: {result.AveragePointsPerIteration:F1}");
            Debug.Log($"Average Dice Used/Iteration: {result.AverageDiceUsedPerIteration:F1}");
        }
        
        Debug.Log("Iteration Details:");
        foreach (var iteration in result.Iterations)
        {
            Debug.Log($"  {iteration}");
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