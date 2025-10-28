using UnityEngine;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// Advanced risk calculation system implementing dual probability mechanics
/// Handles momentum-based Fibonacci calculations and cap-based probability systems
/// </summary>
public class AIRiskCalculator : MonoBehaviour
{
    [Header("Configuration")]
    public AIConfiguration config;
    
    [Header("Dependencies")]
    public AIDualProbabilityCapSystem dualProbabilityCapSystem;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool showProbabilityBreakdown = false;
    
    // Fibonacci sequence for momentum calculations
    private readonly int[] fibonacciSequence = { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
    
    void Start()
    {
        if (config == null)
        {
            config = new AIConfiguration();
            if (enableDebugLogs)
                Debug.Log("AIRiskCalculator: Using default configuration");
        }
        
        // Get dual probability cap system if not assigned
        if (dualProbabilityCapSystem == null)
        {
            dualProbabilityCapSystem = GetComponent<AIDualProbabilityCapSystem>();
            if (dualProbabilityCapSystem == null && enableDebugLogs)
            {
                Debug.Log("AIRiskCalculator: No AIDualProbabilityCapSystem found - using fallback calculations");
            }
        }
    }
    
    /// <summary>
    /// Main entry point for dual probability stop decision calculation
    /// </summary>
    public AIStopDecision CalculateStopDecision(AITurnState turnState, BehaviorMode mode)
    {
        return CalculateStopDecision(
            turnState.IterationCount,
            turnState.CurrentDice.Count,
            turnState.SuccessfulCombinationsCount,
            turnState.CurrentTurnScore,
            turnState.PointsPerTurnCap,
            mode == BehaviorMode.AGGRESSIVE
        );
    }
    
    /// <summary>
    /// Overloaded method for direct parameter access
    /// </summary>
    public AIStopDecision CalculateStopDecision(int iteration, int diceCount, int successCount, 
                                               int currentTurnScore, int pointsPerTurnCap, bool isAggressive)
    {
        var decision = new AIStopDecision();
        BehaviorMode mode = isAggressive ? BehaviorMode.AGGRESSIVE : BehaviorMode.PASSIVE;
        
        // Calculate momentum-based stop chance using Fibonacci system
        decision.MomentumStopChance = CalculateMomentumStopChance(
            iteration,
            diceCount,
            successCount,
            mode
        );
        
        // Calculate cap-based stop chance using dual probability cap system if available
        if (dualProbabilityCapSystem != null)
        {
            var capResult = dualProbabilityCapSystem.CalculateCapProbability(currentTurnScore, mode);
            decision.CapStopChance = capResult.FinalCapStopChance;
        }
        else
        {
            // Fallback to local calculation
            decision.CapStopChance = CalculateCapStopChance(
                currentTurnScore,
                pointsPerTurnCap,
                mode
            );
        }
        
        // Calculate combined probability: 1 - (1-p1) × (1-p2)
        decision.CombinedStopChance = 1f - (1f - decision.MomentumStopChance) * (1f - decision.CapStopChance);
        
        // Perform two independent probability rolls
        decision.MomentumRollResult = Random.Range(0f, 1f) < decision.MomentumStopChance;
        decision.CapRollResult = Random.Range(0f, 1f) < decision.CapStopChance;
        
        // Stop if either roll succeeds (OR logic)
        decision.ShouldStop = decision.MomentumRollResult || decision.CapRollResult;
        
        // Generate decision reasoning
        var tempTurnState = new AITurnState
        {
            IterationCount = iteration,
            CurrentTurnScore = currentTurnScore,
            PointsPerTurnCap = pointsPerTurnCap,
            SuccessfulCombinationsCount = successCount,
            CurrentDice = new List<int>(new int[diceCount]) // Dummy dice for count
        };
        decision.DecisionReason = GenerateDecisionReason(decision, tempTurnState, mode);
        
        if (enableDebugLogs)
        {
            LogDecisionDetails(decision, tempTurnState, mode);
        }
        
        return decision;
    }
    
    /// <summary>
    /// Calculates momentum-based stop chance using Fibonacci sequence and multiple factors
    /// </summary>
    float CalculateMomentumStopChance(int iteration, int diceCount, int successCount, BehaviorMode mode)
    {
        // Base multiplier based on behavior mode
        float baseMultiplier = mode == BehaviorMode.AGGRESSIVE ? 
            config.AggressiveBaseMultiplier : config.PassiveBaseMultiplier;
        
        // Fibonacci multiplier based on iteration
        float fibonacciMultiplier = GetFibonacciMultiplier(iteration);
        
        // Success momentum multiplier (reduces stop chance for successful streaks)
        float successMomentumMultiplier = CalculateSuccessMomentumMultiplier(successCount);
        
        // Dice risk multiplier (exponential scaling for fewer dice)
        float diceRiskMultiplier = CalculateDiceRiskMultiplier(diceCount);
        
        // Iteration pressure multiplier (increases over time)
        float iterationPressureMultiplier = CalculateIterationPressureMultiplier(iteration);
        
        // Combined momentum stop chance
        float momentumStopChance = baseMultiplier * fibonacciMultiplier * 
                                  successMomentumMultiplier * diceRiskMultiplier * 
                                  iterationPressureMultiplier;
        
        // Cap at maximum to prevent infinite loops
        momentumStopChance = Mathf.Min(momentumStopChance, config.MaxMomentumStopChance);
        
        if (showProbabilityBreakdown)
        {
            Debug.Log($"Momentum Breakdown - Base: {baseMultiplier:F3}, Fib: {fibonacciMultiplier:F3}, " +
                     $"Success: {successMomentumMultiplier:F3}, Dice: {diceRiskMultiplier:F3}, " +
                     $"Pressure: {iterationPressureMultiplier:F3}, Final: {momentumStopChance:F3}");
        }
        
        return momentumStopChance;
    }
    
    /// <summary>
    /// Gets Fibonacci multiplier based on iteration count
    /// </summary>
    float GetFibonacciMultiplier(int iteration)
    {
        if (iteration <= 0) return 1f;
        
        int fibIndex = Mathf.Min(iteration - 1, fibonacciSequence.Length - 1);
        return fibonacciSequence[fibIndex];
    }
    
    /// <summary>
    /// Calculates success momentum multiplier (reduces stop chance for hot streaks)
    /// </summary>
    float CalculateSuccessMomentumMultiplier(int successCount)
    {
        if (successCount <= 0) return 1f;
        
        // Each success reduces stop chance by the configured percentage
        float reduction = successCount * config.MomentumReductionPerSuccess;
        float multiplier = 1f - reduction;
        
        // Ensure minimum multiplier to prevent infinite loops
        return Mathf.Max(multiplier, config.MinimumMomentumMultiplier);
    }
    
    /// <summary>
    /// Calculates dice risk multiplier with exponential scaling for fewer dice
    /// </summary>
    float CalculateDiceRiskMultiplier(int diceCount)
    {
        if (diceCount > 2) return 1f; // No risk multiplier for 3+ dice
        
        // Exponential scaling for 2 or fewer dice
        float riskFactor = Mathf.Pow(3f - diceCount, config.DiceRiskExponent);
        return 1f + (riskFactor * config.DiceRiskMultiplier);
    }
    
    /// <summary>
    /// Calculates iteration pressure multiplier that increases over time
    /// </summary>
    float CalculateIterationPressureMultiplier(int iteration)
    {
        if (iteration <= 2) return 1f; // No pressure for first 2 iterations
        
        // Increase by configured percentage for each iteration after the second
        int pressureIterations = iteration - 2;
        return 1f + (pressureIterations * config.IterationPressureIncrease);
    }
    
    /// <summary>
    /// Calculates cap-based stop chance with different curves for aggressive vs passive
    /// </summary>
    float CalculateCapStopChance(int currentScore, int pointsCap, BehaviorMode mode)
    {
        if (currentScore < pointsCap) return 0f;
        
        int pointsOverCap = currentScore - pointsCap;
        
        // Base cap stop chance at exactly the cap
        float baseCapChance = config.BaseCapStopChance;
        
        // Different growth rates based on behavior mode
        float growthRate = mode == BehaviorMode.AGGRESSIVE ? 
            config.AggressiveCapGrowthRate : config.PassiveCapGrowthRate;
        
        // Calculate growth steps (every X points increases chance)
        int growthSteps = pointsOverCap / config.CapGrowthInterval;
        float capChance = baseCapChance + (growthSteps * growthRate);
        
        // Cap at maximum to maintain some continuation chance
        return Mathf.Min(capChance, config.MaxCapStopChance);
    }
    
    /// <summary>
    /// Calculates Zonk probability based on remaining dice count
    /// </summary>
    public float CalculateZonkProbability(int diceCount)
    {
        if (diceCount <= 0) return 1f;
        if (diceCount >= 6) return 0.02f; // Very low chance with 6 dice
        
        // Statistical probability of getting no 1s or 5s
        // Each die has 4/6 chance of not being 1 or 5
        float noScoringDiceProbability = Mathf.Pow(4f / 6f, diceCount);
        
        // Adjust for combination possibilities (pairs, straights, etc.)
        float combinationAdjustment = GetCombinationAdjustment(diceCount);
        
        return noScoringDiceProbability * combinationAdjustment;
    }
    
    /// <summary>
    /// Gets adjustment factor for combination possibilities
    /// </summary>
    float GetCombinationAdjustment(int diceCount)
    {
        switch (diceCount)
        {
            case 1: return 1.0f;   // No combinations possible
            case 2: return 0.95f;  // Slight chance of pair
            case 3: return 0.85f;  // Three of a kind, straights possible
            case 4: return 0.70f;  // More combination opportunities
            case 5: return 0.50f;  // Many combination possibilities
            default: return 0.30f; // High combination potential
        }
    }
    
    /// <summary>
    /// Generates human-readable decision reasoning
    /// </summary>
    string GenerateDecisionReason(AIStopDecision decision, AITurnState turnState, BehaviorMode mode)
    {
        if (decision.ShouldStop)
        {
            if (decision.MomentumRollResult && decision.CapRollResult)
            {
                return $"Both momentum ({decision.MomentumStopChance:P1}) and cap ({decision.CapStopChance:P1}) rolls succeeded";
            }
            else if (decision.MomentumRollResult)
            {
                return $"Momentum roll succeeded ({decision.MomentumStopChance:P1}) - " +
                       $"Iteration {turnState.IterationCount}, {turnState.CurrentDice.Count} dice, {turnState.SuccessfulCombinationsCount} successes";
            }
            else if (decision.CapRollResult)
            {
                return $"Cap roll succeeded ({decision.CapStopChance:P1}) - " +
                       $"{turnState.CurrentTurnScore} points (cap: {turnState.PointsPerTurnCap})";
            }
        }
        
        return $"Continue - Combined chance {decision.CombinedStopChance:P1} failed " +
               $"(Momentum: {decision.MomentumStopChance:P1}, Cap: {decision.CapStopChance:P1})";
    }
    
    /// <summary>
    /// Logs detailed decision information for debugging
    /// </summary>
    void LogDecisionDetails(AIStopDecision decision, AITurnState turnState, BehaviorMode mode)
    {
        Debug.Log($"AIRiskCalculator Decision ({mode}):");
        Debug.Log($"  Turn Score: {turnState.CurrentTurnScore}/{turnState.PointsPerTurnCap}");
        Debug.Log($"  Iteration: {turnState.IterationCount}, Dice: {turnState.CurrentDice.Count}, Successes: {turnState.SuccessfulCombinationsCount}");
        Debug.Log($"  Momentum Chance: {decision.MomentumStopChance:P2} (Roll: {decision.MomentumRollResult})");
        Debug.Log($"  Cap Chance: {decision.CapStopChance:P2} (Roll: {decision.CapRollResult})");
        Debug.Log($"  Combined Chance: {decision.CombinedStopChance:P2}");
        Debug.Log($"  Decision: {(decision.ShouldStop ? "STOP" : "CONTINUE")} - {decision.DecisionReason}");
    }
    
    /// <summary>
    /// Validates risk calculation parameters for debugging
    /// </summary>
    [ContextMenu("Validate Risk Parameters")]
    public void ValidateRiskParameters()
    {
        Debug.Log("=== RISK CALCULATOR VALIDATION ===");
        
        // Test Fibonacci sequence
        Debug.Log("Fibonacci Sequence:");
        for (int i = 0; i < fibonacciSequence.Length; i++)
        {
            Debug.Log($"  F({i+1}) = {fibonacciSequence[i]}");
        }
        
        // Test momentum calculations
        Debug.Log("\nMomentum Calculations (Aggressive, 3 dice, 2 successes):");
        for (int iteration = 1; iteration <= 5; iteration++)
        {
            float momentum = CalculateMomentumStopChance(iteration, 3, 2, BehaviorMode.AGGRESSIVE);
            Debug.Log($"  Iteration {iteration}: {momentum:P2}");
        }
        
        // Test cap calculations
        Debug.Log("\nCap Calculations (400 cap, Aggressive):");
        for (int score = 400; score <= 600; score += 50)
        {
            float cap = CalculateCapStopChance(score, 400, BehaviorMode.AGGRESSIVE);
            Debug.Log($"  Score {score}: {cap:P2}");
        }
        
        // Test Zonk probabilities
        Debug.Log("\nZonk Probabilities:");
        for (int dice = 1; dice <= 6; dice++)
        {
            float zonk = CalculateZonkProbability(dice);
            Debug.Log($"  {dice} dice: {zonk:P2}");
        }
    }
    
    /// <summary>
    /// Runs performance test on risk calculations
    /// </summary>
    [ContextMenu("Performance Test")]
    public void RunPerformanceTest()
    {
        Debug.Log("=== RISK CALCULATOR PERFORMANCE TEST ===");
        
        var testTurnState = new AITurnState
        {
            CurrentTurnScore = 300,
            PointsPerTurnCap = 400,
            IterationCount = 3,
            SuccessfulCombinationsCount = 2,
            CurrentDice = new List<int> { 1, 2, 3 }
        };
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Run 1000 risk calculations
        for (int i = 0; i < 1000; i++)
        {
            CalculateStopDecision(testTurnState, BehaviorMode.AGGRESSIVE);
        }
        
        stopwatch.Stop();
        Debug.Log($"1000 risk calculations completed in {stopwatch.ElapsedMilliseconds}ms");
        Debug.Log($"Average: {stopwatch.ElapsedMilliseconds / 1000f:F3}ms per calculation");
    }
    
    /// <summary>
    /// Updates configuration at runtime
    /// </summary>
    public void UpdateConfiguration(AIConfiguration newConfig)
    {
        config = newConfig;
        if (enableDebugLogs)
            Debug.Log("AIRiskCalculator: Configuration updated");
    }
}