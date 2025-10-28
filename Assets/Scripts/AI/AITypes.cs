using UnityEngine;
using System.Collections.Generic;

namespace HybridEnemyAI
{
    /// <summary>
    /// Two-state behavior system for Hybrid AI
    /// </summary>
    public enum BehaviorMode
    {
        AGGRESSIVE,  // Behind by buffer amount - high risk, high reward
        PASSIVE      // Leading by buffer amount - conservative, efficient scoring
    }

    /// <summary>
    /// Current state of AI turn with all tracking data
    /// </summary>
    [System.Serializable]
    public class AITurnState
    {
        [Header("Turn Progress")]
        public int CurrentTurnScore = 0;
        public int PointsPerTurnCap = 300;
        public int IterationCount = 0;
        public int MaxIterations = 5;
        
        [Header("AI Behavior")]
        public BehaviorMode CurrentMode = BehaviorMode.AGGRESSIVE;
        public int SuccessfulCombinationsCount = 0; // For momentum calculation
        
        [Header("Dice State")]
        public List<int> CurrentDice = new List<int>();
        public List<CombinationResult> CompletedCombinations = new List<CombinationResult>();
        
        [Header("Probability Tracking")]
        public float ZonkProbability = 0f;
        public float CurrentMomentumStopChance = 0f;
        public float CurrentCapStopChance = 0f;
        public float CombinedStopChance = 0f;
        
        public void Reset()
        {
            CurrentTurnScore = 0;
            IterationCount = 0;
            SuccessfulCombinationsCount = 0;
            CurrentDice.Clear();
            CompletedCombinations.Clear();
            ZonkProbability = 0f;
            CurrentMomentumStopChance = 0f;
            CurrentCapStopChance = 0f;
            CombinedStopChance = 0f;
        }
        
        public void AddCombination(CombinationResult combination)
        {
            CompletedCombinations.Add(combination);
            CurrentTurnScore += combination.points;
            SuccessfulCombinationsCount++;
        }
    }

    /// <summary>
    /// Configuration parameters for AI behavior tuning
    /// </summary>
    [System.Serializable]
    public class AIConfiguration
    {
        [Header("State-based Settings")]
        public int PointsCapAggressive = 500;
        public int PointsCapPassive = 250;
        public int StateBufferThreshold = 100; // ±100 points for state switching
        
        [Header("Dynamic Buffer System")]
        public int InitialBufferCap = 200;
        public int BufferReductionPerRound = 20;
        public int RoundsPerReduction = 3;
        public int MinimumBufferCap = 50;
        
        [Header("Momentum System Parameters")]
        public float AggressiveBaseMultiplier = 0.10f;
        public float PassiveBaseMultiplier = 0.15f;
        public float MomentumReductionPerSuccess = 0.12f;
        public float MinimumMomentumMultiplier = 0.25f;
        public float DiceRiskExponent = 2f;
        public float DiceRiskMultiplier = 0.3f;
        public float IterationPressureIncrease = 0.2f;
        public float MaxMomentumStopChance = 0.90f;
        
        [Header("Cap Probability System")]
        public float BaseCapStopChance = 0.30f;
        public float AggressiveCapGrowthRate = 0.10f; // Slower growth
        public float PassiveCapGrowthRate = 0.20f;    // Faster growth
        public int CapGrowthInterval = 50;            // Points per growth step
        public float MaxCapStopChance = 0.80f;
        
        [Header("Combination Thresholds")]
        [Range(0f, 1f)]
        public float AggressiveInitialThreshold = 0.80f;
        [Range(0f, 0.5f)]
        public float AggressiveThresholdReduction = 0.20f;
        [Range(0f, 1f)]
        public float PassiveInitialThreshold = 0.40f;
        [Range(0f, 0.5f)]
        public float PassiveThresholdReduction = 0.10f;
        
        [Header("Conservative Behavior Settings")]
        public int ConservativeMaxIterations = 2;      // Reduced iteration limit for conservative play
        public float ConservativeHighRiskThreshold = 0.6f; // Stop at 60% Zonk risk when conservative
        public float ConservativeEarlySatisfactionThreshold = 0.8f; // Stop at 80% of cap when conservative
        public float ConservativeTwoDiceStopChance = 0.7f; // High stop chance with 2 dice in conservative mode
        public float ConservativeOneDiceStopChance = 0.9f; // Very high stop chance with 1 dice in conservative mode
        
        [Header("Debug")]
        public bool EnableDebugLogs = false;
        public bool ShowProbabilityCalculations = false;
    }

    /// <summary>
    /// Result of dual probability stop decision calculation
    /// </summary>
    [System.Serializable]
    public class AIStopDecision
    {
        public float MomentumStopChance;
        public float CapStopChance;
        public float CombinedStopChance;
        public bool MomentumRollResult;
        public bool CapRollResult;
        public bool ShouldStop;
        public string DecisionReason;
        
        public AIStopDecision()
        {
            MomentumStopChance = 0f;
            CapStopChance = 0f;
            CombinedStopChance = 0f;
            MomentumRollResult = false;
            CapRollResult = false;
            ShouldStop = false;
            DecisionReason = "";
        }
    }
}