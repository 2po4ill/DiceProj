using UnityEngine;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// Implements dual probability points per turn cap system
/// Handles dynamic cap setting and separate probability growth curves for different AI states
/// Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 12.1, 12.2, 12.3, 12.4, 12.5
/// </summary>
public class AIDualProbabilityCapSystem : MonoBehaviour
{
    [Header("Configuration")]
    public AIConfiguration config;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool showCapCalculations = false;
    
    [Header("Cap System State")]
    [SerializeField] private CapSystemState currentState;
    [SerializeField] private List<CapDecisionRecord> decisionHistory = new List<CapDecisionRecord>();
    
    /// <summary>
    /// Current state of the cap system
    /// </summary>
    [System.Serializable]
    public class CapSystemState
    {
        [Header("Dynamic Cap Settings")]
        public BehaviorMode CurrentMode = BehaviorMode.AGGRESSIVE;
        public int AggressiveCapMin = 400;
        public int AggressiveCapMax = 600;
        public int PassiveCapMin = 200;
        public int PassiveCapMax = 300;
        public int CurrentPointsPerTurnCap = 500;
        
        [Header("Probability Curves")]
        public float AggressiveBaseCapChance = 0.30f;
        public float PassiveBaseCapChance = 0.30f;
        public float AggressiveGrowthRate = 0.10f; // Slower growth for aggressive
        public float PassiveGrowthRate = 0.20f;    // Faster growth for passive
        public int GrowthInterval = 50;            // Points per growth step
        public float MaxCapChance = 0.80f;
        
        [Header("Current Turn Tracking")]
        public int CurrentTurnScore = 0;
        public int PointsOverCap = 0;
        public float CurrentCapStopChance = 0f;
        public bool CapThresholdReached = false;
        
        public void Reset()
        {
            CurrentTurnScore = 0;
            PointsOverCap = 0;
            CurrentCapStopChance = 0f;
            CapThresholdReached = false;
        }
        
        public void UpdateForMode(BehaviorMode mode)
        {
            CurrentMode = mode;
            CurrentPointsPerTurnCap = mode == BehaviorMode.AGGRESSIVE ? 
                Random.Range(AggressiveCapMin, AggressiveCapMax + 1) :
                Random.Range(PassiveCapMin, PassiveCapMax + 1);
        }
    }
    
    /// <summary>
    /// Record of a cap decision for analysis
    /// </summary>
    [System.Serializable]
    public class CapDecisionRecord
    {
        public System.DateTime Timestamp;
        public BehaviorMode Mode;
        public int TurnScore;
        public int PointsCap;
        public int PointsOverCap;
        public float CapStopChance;
        public bool CapRollResult;
        public string DecisionReason;
        
        public override string ToString()
        {
            return $"{Mode}: {TurnScore}/{PointsCap} (+{PointsOverCap}) = {CapStopChance:P1} -> {(CapRollResult ? "STOP" : "CONTINUE")}";
        }
    }
    
    /// <summary>
    /// Result of dual probability cap calculation
    /// </summary>
    [System.Serializable]
    public class DualProbabilityCapResult
    {
        [Header("Cap Analysis")]
        public int CurrentTurnScore;
        public int PointsPerTurnCap;
        public int PointsOverCap;
        public bool CapThresholdReached;
        
        [Header("Probability Calculation")]
        public float BaseCapStopChance;
        public float GrowthRate;
        public int GrowthSteps;
        public float CalculatedCapStopChance;
        public float FinalCapStopChance;
        
        [Header("Decision")]
        public bool CapRollResult;
        public string CalculationDetails;
        public string DecisionReason;
        
        public DualProbabilityCapResult()
        {
            CurrentTurnScore = 0;
            PointsPerTurnCap = 0;
            PointsOverCap = 0;
            CapThresholdReached = false;
            BaseCapStopChance = 0f;
            GrowthRate = 0f;
            GrowthSteps = 0;
            CalculatedCapStopChance = 0f;
            FinalCapStopChance = 0f;
            CapRollResult = false;
            CalculationDetails = "";
            DecisionReason = "";
        }
    }
    
    void Start()
    {
        InitializeCapSystem();
    }
    
    /// <summary>
    /// Sets dynamic cap based on AI state (AGGRESSIVE vs PASSIVE)
    /// </summary>
    public void SetDynamicCap(BehaviorMode mode)
    {
        currentState.UpdateForMode(mode);
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIDualProbabilityCapSystem: Dynamic cap set for {mode} mode");
            Debug.Log($"  Points Per Turn Cap: {currentState.CurrentPointsPerTurnCap}");
            Debug.Log($"  Cap Range: {(mode == BehaviorMode.AGGRESSIVE ? $"{currentState.AggressiveCapMin}-{currentState.AggressiveCapMax}" : $"{currentState.PassiveCapMin}-{currentState.PassiveCapMax}")}");
        }
    }
    
    /// <summary>
    /// Calculates cap probability based on points over threshold with separate curves for AI states
    /// </summary>
    public DualProbabilityCapResult CalculateCapProbability(int currentTurnScore, BehaviorMode mode)
    {
        var result = new DualProbabilityCapResult();
        
        // Update current state
        currentState.CurrentTurnScore = currentTurnScore;
        currentState.PointsOverCap = Mathf.Max(0, currentTurnScore - currentState.CurrentPointsPerTurnCap);
        currentState.CapThresholdReached = currentTurnScore >= currentState.CurrentPointsPerTurnCap;
        
        // Fill result basic info
        result.CurrentTurnScore = currentTurnScore;
        result.PointsPerTurnCap = currentState.CurrentPointsPerTurnCap;
        result.PointsOverCap = currentState.PointsOverCap;
        result.CapThresholdReached = currentState.CapThresholdReached;
        
        // Calculate cap stop probability
        if (!currentState.CapThresholdReached)
        {
            // Below cap - no cap stop chance
            result.FinalCapStopChance = 0f;
            result.DecisionReason = "Below points per turn cap - no cap stop chance";
        }
        else
        {
            // Above cap - calculate probability based on mode-specific curve
            result.BaseCapStopChance = mode == BehaviorMode.AGGRESSIVE ? 
                currentState.AggressiveBaseCapChance : currentState.PassiveBaseCapChance;
            
            result.GrowthRate = mode == BehaviorMode.AGGRESSIVE ? 
                currentState.AggressiveGrowthRate : currentState.PassiveGrowthRate;
            
            result.GrowthSteps = currentState.PointsOverCap / currentState.GrowthInterval;
            
            result.CalculatedCapStopChance = result.BaseCapStopChance + (result.GrowthSteps * result.GrowthRate);
            result.FinalCapStopChance = Mathf.Min(result.CalculatedCapStopChance, currentState.MaxCapChance);
            
            result.DecisionReason = $"Over cap by {currentState.PointsOverCap} points - {mode} growth curve applied";
        }
        
        // Perform cap probability roll
        result.CapRollResult = Random.Range(0f, 1f) < result.FinalCapStopChance;
        
        // Update current state
        currentState.CurrentCapStopChance = result.FinalCapStopChance;
        
        // Generate calculation details
        result.CalculationDetails = GenerateCalculationDetails(result, mode);
        
        // Record decision for history
        RecordCapDecision(result, mode);
        
        if (showCapCalculations)
        {
            LogCapCalculationDetails(result, mode);
        }
        
        return result;
    }
    
    /// <summary>
    /// Implements dual probability roll system (cap roll + momentum roll)
    /// </summary>
    public DualProbabilityDecision MakeDualProbabilityDecision(int currentTurnScore, BehaviorMode mode, 
                                                              float momentumStopChance, bool momentumRollResult)
    {
        var decision = new DualProbabilityDecision();
        
        // Calculate cap probability
        var capResult = CalculateCapProbability(currentTurnScore, mode);
        
        // Fill decision with both probabilities
        decision.MomentumStopChance = momentumStopChance;
        decision.CapStopChance = capResult.FinalCapStopChance;
        decision.MomentumRollResult = momentumRollResult;
        decision.CapRollResult = capResult.CapRollResult;
        
        // Calculate combined probability: 1 - (1-p1) × (1-p2)
        decision.CombinedStopChance = 1f - (1f - decision.MomentumStopChance) * (1f - decision.CapStopChance);
        
        // Final decision: stop if either roll succeeds (OR logic)
        decision.ShouldStop = decision.MomentumRollResult || decision.CapRollResult;
        
        // Generate decision reasoning
        decision.DecisionReason = GenerateDualProbabilityReason(decision);
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIDualProbabilityCapSystem: Dual probability decision");
            Debug.Log($"  Momentum: {decision.MomentumStopChance:P1} (Roll: {decision.MomentumRollResult})");
            Debug.Log($"  Cap: {decision.CapStopChance:P1} (Roll: {decision.CapRollResult})");
            Debug.Log($"  Combined: {decision.CombinedStopChance:P1}");
            Debug.Log($"  Decision: {(decision.ShouldStop ? "STOP" : "CONTINUE")} - {decision.DecisionReason}");
        }
        
        return decision;
    }
    
    /// <summary>
    /// Creates separate probability growth curves for different AI states
    /// </summary>
    public ProbabilityGrowthCurve GetProbabilityGrowthCurve(BehaviorMode mode)
    {
        var curve = new ProbabilityGrowthCurve();
        curve.Mode = mode;
        curve.BaseStopChance = mode == BehaviorMode.AGGRESSIVE ? 
            currentState.AggressiveBaseCapChance : currentState.PassiveBaseCapChance;
        curve.GrowthRate = mode == BehaviorMode.AGGRESSIVE ? 
            currentState.AggressiveGrowthRate : currentState.PassiveGrowthRate;
        curve.GrowthInterval = currentState.GrowthInterval;
        curve.MaxStopChance = currentState.MaxCapChance;
        
        // Generate curve points for visualization/analysis
        curve.CurvePoints = new List<ProbabilityPoint>();
        for (int pointsOver = 0; pointsOver <= 300; pointsOver += curve.GrowthInterval)
        {
            int growthSteps = pointsOver / curve.GrowthInterval;
            float probability = curve.BaseStopChance + (growthSteps * curve.GrowthRate);
            probability = Mathf.Min(probability, curve.MaxStopChance);
            
            curve.CurvePoints.Add(new ProbabilityPoint
            {
                PointsOverCap = pointsOver,
                StopProbability = probability
            });
        }
        
        return curve;
    }
    
    /// <summary>
    /// Writes combined probability calculation and decision logic
    /// </summary>
    public float CalculateCombinedProbability(float momentumChance, float capChance)
    {
        // Combined probability formula: 1 - (1-p1) × (1-p2)
        // This represents the probability that at least one of the two independent events occurs
        float combinedChance = 1f - (1f - momentumChance) * (1f - capChance);
        
        if (showCapCalculations)
        {
            Debug.Log($"Combined Probability Calculation:");
            Debug.Log($"  Momentum Chance: {momentumChance:P2}");
            Debug.Log($"  Cap Chance: {capChance:P2}");
            Debug.Log($"  Formula: 1 - (1-{momentumChance:F3}) × (1-{capChance:F3})");
            Debug.Log($"  Result: 1 - {(1f - momentumChance):F3} × {(1f - capChance):F3} = {combinedChance:P2}");
        }
        
        return combinedChance;
    }
    
    /// <summary>
    /// Generates detailed calculation explanation
    /// </summary>
    string GenerateCalculationDetails(DualProbabilityCapResult result, BehaviorMode mode)
    {
        if (!result.CapThresholdReached)
        {
            return $"Score {result.CurrentTurnScore} below cap {result.PointsPerTurnCap} - no cap probability";
        }
        
        var details = new System.Text.StringBuilder();
        details.AppendLine($"=== CAP PROBABILITY CALCULATION ({mode}) ===");
        details.AppendLine($"Turn Score: {result.CurrentTurnScore}");
        details.AppendLine($"Points Cap: {result.PointsPerTurnCap}");
        details.AppendLine($"Points Over Cap: {result.PointsOverCap}");
        details.AppendLine($"Base Cap Chance: {result.BaseCapStopChance:P1}");
        details.AppendLine($"Growth Rate: {result.GrowthRate:P1} per {currentState.GrowthInterval} points");
        details.AppendLine($"Growth Steps: {result.GrowthSteps}");
        details.AppendLine($"Calculated: {result.BaseCapStopChance:F3} + ({result.GrowthSteps} × {result.GrowthRate:F3}) = {result.CalculatedCapStopChance:F3}");
        details.AppendLine($"Final (capped): {result.FinalCapStopChance:P2}");
        details.AppendLine($"Roll Result: {(result.CapRollResult ? "STOP" : "CONTINUE")}");
        
        return details.ToString();
    }
    
    /// <summary>
    /// Generates dual probability decision reasoning
    /// </summary>
    string GenerateDualProbabilityReason(DualProbabilityDecision decision)
    {
        if (decision.ShouldStop)
        {
            if (decision.MomentumRollResult && decision.CapRollResult)
            {
                return $"Both momentum ({decision.MomentumStopChance:P1}) and cap ({decision.CapStopChance:P1}) rolls succeeded";
            }
            else if (decision.MomentumRollResult)
            {
                return $"Momentum roll succeeded ({decision.MomentumStopChance:P1}) - cap roll failed ({decision.CapStopChance:P1})";
            }
            else if (decision.CapRollResult)
            {
                return $"Cap roll succeeded ({decision.CapStopChance:P1}) - momentum roll failed ({decision.MomentumStopChance:P1})";
            }
        }
        
        return $"Both rolls failed - Continue (Combined: {decision.CombinedStopChance:P1}, " +
               $"Momentum: {decision.MomentumStopChance:P1}, Cap: {decision.CapStopChance:P1})";
    }
    
    /// <summary>
    /// Records cap decision for analysis
    /// </summary>
    void RecordCapDecision(DualProbabilityCapResult result, BehaviorMode mode)
    {
        var record = new CapDecisionRecord
        {
            Timestamp = System.DateTime.Now,
            Mode = mode,
            TurnScore = result.CurrentTurnScore,
            PointsCap = result.PointsPerTurnCap,
            PointsOverCap = result.PointsOverCap,
            CapStopChance = result.FinalCapStopChance,
            CapRollResult = result.CapRollResult,
            DecisionReason = result.DecisionReason
        };
        
        decisionHistory.Add(record);
        
        // Keep history manageable
        if (decisionHistory.Count > 100)
        {
            decisionHistory.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// Logs detailed cap calculation information
    /// </summary>
    void LogCapCalculationDetails(DualProbabilityCapResult result, BehaviorMode mode)
    {
        Debug.Log(result.CalculationDetails);
    }
    
    /// <summary>
    /// Initializes cap system with default configuration
    /// </summary>
    void InitializeCapSystem()
    {
        if (config == null)
        {
            config = new AIConfiguration();
            if (enableDebugLogs)
                Debug.Log("AIDualProbabilityCapSystem: Using default configuration");
        }
        
        if (currentState == null)
        {
            currentState = new CapSystemState();
        }
        
        // Initialize state from configuration
        currentState.AggressiveBaseCapChance = config.BaseCapStopChance;
        currentState.PassiveBaseCapChance = config.BaseCapStopChance;
        currentState.AggressiveGrowthRate = config.AggressiveCapGrowthRate;
        currentState.PassiveGrowthRate = config.PassiveCapGrowthRate;
        currentState.GrowthInterval = config.CapGrowthInterval;
        currentState.MaxCapChance = config.MaxCapStopChance;
        currentState.AggressiveCapMin = config.PointsCapAggressive - 100;
        currentState.AggressiveCapMax = config.PointsCapAggressive + 100;
        currentState.PassiveCapMin = config.PointsCapPassive - 50;
        currentState.PassiveCapMax = config.PointsCapPassive + 50;
        
        if (enableDebugLogs)
        {
            Debug.Log("AIDualProbabilityCapSystem: Initialized with configuration");
            Debug.Log($"  Aggressive Cap Range: {currentState.AggressiveCapMin}-{currentState.AggressiveCapMax}");
            Debug.Log($"  Passive Cap Range: {currentState.PassiveCapMin}-{currentState.PassiveCapMax}");
            Debug.Log($"  Growth Rates: Aggressive {currentState.AggressiveGrowthRate:P1}, Passive {currentState.PassiveGrowthRate:P1}");
        }
    }
    
    /// <summary>
    /// Gets current cap system state for debugging
    /// </summary>
    public CapSystemState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// Gets decision history for analysis
    /// </summary>
    public List<CapDecisionRecord> GetDecisionHistory()
    {
        return new List<CapDecisionRecord>(decisionHistory);
    }
    
    /// <summary>
    /// Resets cap system state
    /// </summary>
    public void ResetState()
    {
        currentState.Reset();
        decisionHistory.Clear();
        
        if (enableDebugLogs)
        {
            Debug.Log("AIDualProbabilityCapSystem: State reset");
        }
    }
    
    /// <summary>
    /// Updates configuration at runtime
    /// </summary>
    public void UpdateConfiguration(AIConfiguration newConfig)
    {
        config = newConfig;
        InitializeCapSystem(); // Reinitialize with new config
        
        if (enableDebugLogs)
        {
            Debug.Log("AIDualProbabilityCapSystem: Configuration updated");
        }
    }
    
    /// <summary>
    /// Context menu for testing cap calculations
    /// </summary>
    [ContextMenu("Test Cap Calculations")]
    public void TestCapCalculations()
    {
        Debug.Log("=== CAP SYSTEM TEST ===");
        
        // Test aggressive mode
        SetDynamicCap(BehaviorMode.AGGRESSIVE);
        Debug.Log($"Aggressive Mode (Cap: {currentState.CurrentPointsPerTurnCap}):");
        
        for (int score = currentState.CurrentPointsPerTurnCap; score <= currentState.CurrentPointsPerTurnCap + 200; score += 50)
        {
            var result = CalculateCapProbability(score, BehaviorMode.AGGRESSIVE);
            Debug.Log($"  Score {score}: {result.FinalCapStopChance:P1} (Roll: {result.CapRollResult})");
        }
        
        // Test passive mode
        SetDynamicCap(BehaviorMode.PASSIVE);
        Debug.Log($"Passive Mode (Cap: {currentState.CurrentPointsPerTurnCap}):");
        
        for (int score = currentState.CurrentPointsPerTurnCap; score <= currentState.CurrentPointsPerTurnCap + 200; score += 50)
        {
            var result = CalculateCapProbability(score, BehaviorMode.PASSIVE);
            Debug.Log($"  Score {score}: {result.FinalCapStopChance:P1} (Roll: {result.CapRollResult})");
        }
    }
}

/// <summary>
/// Result of dual probability decision combining momentum and cap systems
/// </summary>
[System.Serializable]
public class DualProbabilityDecision
{
    [Header("Individual Probabilities")]
    public float MomentumStopChance;
    public float CapStopChance;
    public float CombinedStopChance;
    
    [Header("Roll Results")]
    public bool MomentumRollResult;
    public bool CapRollResult;
    
    [Header("Final Decision")]
    public bool ShouldStop;
    public string DecisionReason;
}

/// <summary>
/// Probability growth curve for a specific AI mode
/// </summary>
[System.Serializable]
public class ProbabilityGrowthCurve
{
    public BehaviorMode Mode;
    public float BaseStopChance;
    public float GrowthRate;
    public int GrowthInterval;
    public float MaxStopChance;
    public List<ProbabilityPoint> CurvePoints = new List<ProbabilityPoint>();
}

/// <summary>
/// Point on probability growth curve
/// </summary>
[System.Serializable]
public class ProbabilityPoint
{
    public int PointsOverCap;
    public float StopProbability;
    
    public override string ToString()
    {
        return $"+{PointsOverCap}: {StopProbability:P1}";
    }
}