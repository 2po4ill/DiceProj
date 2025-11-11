using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HybridEnemyAI;

/// <summary>
/// Strategic combination evaluation and selection for AI
/// Implements hierarchical combination classification and minimum dice usage algorithms
/// </summary>
public class AICombinationStrategy : MonoBehaviour
{
    [Header("Dependencies")]
    public DiceCombinationDetector combinationDetector;
    
    [Header("Strategy Configuration")]
    public AIConfiguration config;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Combination tier classification for strategic evaluation
    public enum CombinationTier
    {
        Tier1 = 1,  // 100% value: Six of Kind, Large Straight, Full House
        Tier2 = 2,  // 80% value: Four of Kind, Middle Straight  
        Tier3 = 3,  // 60% value: Three of Kind, Two Pair
        Tier4 = 4,  // 40% value: Single Pair, Low Straight
        Tier5 = 5   // 20% value: Single One/Five
    }
    
    // Risk levels for remaining dice analysis
    public enum RiskLevel
    {
        None,
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }
    
    /// <summary>
    /// Strategic combination evaluation result
    /// </summary>
    [System.Serializable]
    public class StrategyResult
    {
        public CombinationResult combination;
        public CombinationTier tier;
        public float strategicValue;  // Points per dice ratio
        public int diceUsed;
        public bool meetsThreshold;
        public string reasoning;
        
        public StrategyResult(CombinationResult combo, CombinationTier t, float value, int dice, bool threshold, string reason)
        {
            combination = combo;
            tier = t;
            strategicValue = value;
            diceUsed = dice;
            meetsThreshold = threshold;
            reasoning = reason;
        }
    }
    
    /// <summary>
    /// Analysis of remaining dice after taking a combination
    /// </summary>
    [System.Serializable]
    public class RemainingDiceAnalysis
    {
        public int RemainingDiceCount;
        public float RerollPotential;
        public RiskLevel Risk;
        public bool RecommendContinue;
        
        public override string ToString()
        {
            return $"Remaining: {RemainingDiceCount}, Potential: {RerollPotential:F1}, Risk: {Risk}, Continue: {RecommendContinue}";
        }
    }
    
    void Start()
    {
        if (combinationDetector == null)
        {
            combinationDetector = FindObjectOfType<DiceCombinationDetector>();
            if (combinationDetector == null)
            {
                Debug.LogError("AICombinationStrategy: No DiceCombinationDetector found!");
            }
        }
        
        if (config == null)
        {
            config = new AIConfiguration();
            if (enableDebugLogs)
                Debug.Log("AICombinationStrategy: Using default configuration");
        }
    }
    
    /// <summary>
    /// Evaluates all possible combinations and returns the best strategic choice
    /// </summary>
    public StrategyResult EvaluateBestStrategy(List<int> diceValues, BehaviorMode mode, int remainingDice)
    {
        if (diceValues == null || diceValues.Count == 0)
        {
            return null;
        }
        
        // Get combination threshold for current mode and dice count
        float threshold = GetCombinationThreshold(mode, remainingDice);
        
        // Find all possible combinations
        List<StrategyResult> allCombinations = FindAllCombinations(diceValues, threshold);
        
        if (allCombinations.Count == 0)
        {
            if (enableDebugLogs)
                Debug.Log("AICombinationStrategy: No combinations found (Zonk)");
            return null;
        }
        
        // Select best combination based on strategy
        StrategyResult bestStrategy = SelectBestCombination(allCombinations, mode);
        
        if (enableDebugLogs)
        {
            Debug.Log($"AICombinationStrategy: Selected {bestStrategy.combination.rule} " +
                     $"(Tier {bestStrategy.tier}, Value: {bestStrategy.strategicValue:F2}, " +
                     $"Dice: {bestStrategy.diceUsed}, Threshold: {bestStrategy.meetsThreshold})");
        }
        
        return bestStrategy;
    }
    
    /// <summary>
    /// Finds all possible combinations in the dice set
    /// </summary>
    List<StrategyResult> FindAllCombinations(List<int> diceValues, float threshold)
    {
        List<StrategyResult> combinations = new List<StrategyResult>();
        
        // Check each possible combination type
        combinations.AddRange(FindStraightCombinations(diceValues, threshold));
        combinations.AddRange(FindOfAKindCombinations(diceValues, threshold));
        combinations.AddRange(FindPairCombinations(diceValues, threshold));
        combinations.AddRange(FindSingleCombinations(diceValues, threshold));
        
        return combinations;
    }
    
    /// <summary>
    /// Finds straight combinations (consecutive numbers)
    /// </summary>
    List<StrategyResult> FindStraightCombinations(List<int> diceValues, float threshold)
    {
        List<StrategyResult> straights = new List<StrategyResult>();
        
        // Check for large straight (1,2,3,4,5,6) - Tier 1
        if (HasLargeStraight(diceValues))
        {
            var result = CreateStrategyResult(Rule.MaxStraight, 1500, CombinationTier.Tier1, 6, threshold, "Large Straight");
            result.combination.diceIndices = FindStraightIndices(diceValues, new List<int> { 1, 2, 3, 4, 5, 6 });
            straights.Add(result);
        }
        
        // Check for middle straight (any 5 consecutive) - Tier 2
        if (HasMiddleStraight(diceValues))
        {
            var result = CreateStrategyResult(Rule.Straight, 1000, CombinationTier.Tier2, 5, threshold, "Middle Straight");
            result.combination.diceIndices = FindAnyStraightIndices(diceValues, 5);
            straights.Add(result);
        }
        
        // Check for small straight (any 4 consecutive) - Tier 2
        if (HasSmallStraight(diceValues))
        {
            var result = CreateStrategyResult(Rule.MiddleStraight, 500, CombinationTier.Tier2, 4, threshold, "Small Straight");
            result.combination.diceIndices = FindAnyStraightIndices(diceValues, 4);
            straights.Add(result);
        }
        
        // Check for low straight (any 3 consecutive) - Tier 4
        if (HasLowStraight(diceValues))
        {
            var result = CreateStrategyResult(Rule.LowStraight, 250, CombinationTier.Tier4, 3, threshold, "Low Straight");
            result.combination.diceIndices = FindAnyStraightIndices(diceValues, 3);
            straights.Add(result);
        }
        
        return straights;
    }
    
    /// <summary>
    /// Finds of-a-kind combinations
    /// </summary>
    List<StrategyResult> FindOfAKindCombinations(List<int> diceValues, float threshold)
    {
        List<StrategyResult> ofAKind = new List<StrategyResult>();
        
        var counts = diceValues.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        
        foreach (var kvp in counts)
        {
            int value = kvp.Key;
            int count = kvp.Value;
            
            // Six of a kind - Tier 1
            if (count >= 6)
            {
                int points = value == 1 ? 4000 : value * 1000;
                var result = CreateStrategyResult(Rule.MaxStraight, points, CombinationTier.Tier1, 6, threshold, $"Six {value}s");
                result.combination.diceIndices = FindValueIndices(diceValues, value, 6);
                ofAKind.Add(result);
            }
            // Four of a kind - Tier 2
            else if (count >= 4)
            {
                int points = value == 1 ? 2000 : value * 200;
                var result = CreateStrategyResult(Rule.FourOfKind, points, CombinationTier.Tier2, 4, threshold, $"Four {value}s");
                result.combination.diceIndices = FindValueIndices(diceValues, value, 4);
                ofAKind.Add(result);
            }
            // Three of a kind - Tier 3
            else if (count >= 3)
            {
                int points = value == 1 ? 1000 : value * 100;
                var result = CreateStrategyResult(Rule.ThreeOfKind, points, CombinationTier.Tier3, 3, threshold, $"Three {value}s");
                result.combination.diceIndices = FindValueIndices(diceValues, value, 3);
                ofAKind.Add(result);
            }
        }
        
        return ofAKind;
    }
    
    /// <summary>
    /// Finds pair combinations
    /// </summary>
    List<StrategyResult> FindPairCombinations(List<int> diceValues, float threshold)
    {
        List<StrategyResult> pairs = new List<StrategyResult>();
        
        var counts = diceValues.GroupBy(x => x).Where(g => g.Count() >= 2).ToList();
        
        // Three pairs - Tier 1
        if (counts.Count >= 3)
        {
            var result = CreateStrategyResult(Rule.ThreePairs, 1500, CombinationTier.Tier1, 6, threshold, "Three Pairs");
            // Find indices for all three pairs
            List<int> indices = new List<int>();
            foreach (var group in counts.Take(3))
            {
                indices.AddRange(FindValueIndices(diceValues, group.Key, 2));
            }
            result.combination.diceIndices = indices;
            pairs.Add(result);
        }
        // Two pairs - Tier 3
        else if (counts.Count >= 2)
        {
            var result = CreateStrategyResult(Rule.TwoPair, 500, CombinationTier.Tier3, 4, threshold, "Two Pairs");
            // Find indices for both pairs
            List<int> indices = new List<int>();
            foreach (var group in counts.Take(2))
            {
                indices.AddRange(FindValueIndices(diceValues, group.Key, 2));
            }
            result.combination.diceIndices = indices;
            pairs.Add(result);
        }
        // Single pair - Tier 4
        else if (counts.Count >= 1)
        {
            int value = counts.First().Key;
            int points = value == 1 ? 200 : value * 20;
            var result = CreateStrategyResult(Rule.Pair, points, CombinationTier.Tier4, 2, threshold, $"Pair of {value}s");
            result.combination.diceIndices = FindValueIndices(diceValues, value, 2);
            pairs.Add(result);
        }
        
        return pairs;
    }
    
    /// <summary>
    /// Finds single dice combinations (1s and 5s)
    /// </summary>
    List<StrategyResult> FindSingleCombinations(List<int> diceValues, float threshold)
    {
        List<StrategyResult> singles = new List<StrategyResult>();
        
        // Count 1s and 5s
        int ones = diceValues.Count(x => x == 1);
        int fives = diceValues.Count(x => x == 5);
        
        // For minimum dice strategy, prefer single 1 over single 5
        // Single 1 - Tier 5 (100 points)
        if (ones > 0)
        {
            var result = CreateStrategyResult(Rule.One, 100, CombinationTier.Tier5, 1, threshold, "Single One");
            result.combination.diceIndices = FindValueIndices(diceValues, 1, 1);
            singles.Add(result);
        }
        
        // Single 5 - Tier 5 (50 points)
        if (fives > 0)
        {
            var result = CreateStrategyResult(Rule.One, 50, CombinationTier.Tier5, 1, threshold, "Single Five");
            result.combination.diceIndices = FindValueIndices(diceValues, 5, 1);
            singles.Add(result);
        }
        
        return singles;
    }
    
    /// <summary>
    /// Creates a strategy result with threshold evaluation
    /// </summary>
    StrategyResult CreateStrategyResult(Rule rule, int points, CombinationTier tier, int diceUsed, float threshold, string description)
    {
        float strategicValue = diceUsed > 0 ? (float)points / diceUsed : 0f;
        float tierValue = GetTierValue(tier);
        bool meetsThreshold = tierValue >= threshold;
        
        var combination = new CombinationResult(rule, points, description);
        return new StrategyResult(combination, tier, strategicValue, diceUsed, meetsThreshold, description);
    }
    
    /// <summary>
    /// Selects the best combination based on AI behavior mode
    /// </summary>
    StrategyResult SelectBestCombination(List<StrategyResult> combinations, BehaviorMode mode)
    {
        if (combinations.Count == 0) return null;
        
        // Filter combinations that meet threshold first
        var thresholdCombinations = combinations.Where(c => c.meetsThreshold).ToList();
        
        if (thresholdCombinations.Count > 0)
        {
            // Select from threshold-meeting combinations
            return SelectByStrategy(thresholdCombinations, mode);
        }
        else
        {
            // No combinations meet threshold - use minimum dice strategy
            return SelectMinimumDiceStrategy(combinations);
        }
    }
    
    /// <summary>
    /// Selects combination based on behavior mode strategy
    /// </summary>
    StrategyResult SelectByStrategy(List<StrategyResult> combinations, BehaviorMode mode)
    {
        switch (mode)
        {
            case BehaviorMode.AGGRESSIVE:
                // Prioritize highest tier, then highest points
                return combinations
                    .OrderBy(c => (int)c.tier)
                    .ThenByDescending(c => c.combination.points)
                    .First();
                
            case BehaviorMode.PASSIVE:
                // Prioritize strategic value (points per dice)
                return combinations
                    .OrderByDescending(c => c.strategicValue)
                    .ThenBy(c => c.diceUsed)
                    .First();
                
            default:
                return combinations.OrderByDescending(c => c.combination.points).First();
        }
    }
    
    /// <summary>
    /// Selects combination using minimum dice when no combinations meet threshold
    /// </summary>
    StrategyResult SelectMinimumDiceStrategy(List<StrategyResult> combinations)
    {
        // Find combinations using fewest dice
        int minDice = combinations.Min(c => c.diceUsed);
        var minDiceCombinations = combinations.Where(c => c.diceUsed == minDice).ToList();
        
        // Among minimum dice combinations, select highest points
        return minDiceCombinations.OrderByDescending(c => c.combination.points).First();
    }
    
    /// <summary>
    /// Advanced minimum dice selection algorithm with strategic comparison and hot streak detection
    /// </summary>
    public StrategyResult FindMinimumDiceCombination(List<int> diceValues, BehaviorMode mode)
    {
        if (diceValues == null || diceValues.Count == 0)
        {
            return null;
        }
        
        int totalDiceCount = diceValues.Count;
        
        // Find all possible combinations
        List<StrategyResult> allCombinations = FindAllCombinations(diceValues, 0.0f); // No threshold for minimum dice
        
        if (allCombinations.Count == 0)
        {
            if (enableDebugLogs)
                Debug.Log("AICombinationStrategy: No combinations found for minimum dice selection");
            return null;
        }
        
        // Apply dynamic weighting based on dice count
        ApplyDiceCountWeighting(allCombinations, totalDiceCount, mode);
        
        // Apply minimum dice selection algorithm (includes hot streak detection)
        StrategyResult bestMinimum = ApplyMinimumDiceAlgorithm(allCombinations, totalDiceCount, mode);
        
        if (enableDebugLogs)
        {
            Debug.Log($"AICombinationStrategy: Minimum dice selection - {bestMinimum.combination.rule} " +
                     $"using {bestMinimum.diceUsed}/{totalDiceCount} dice for {bestMinimum.combination.points} points " +
                     $"(Efficiency: {bestMinimum.strategicValue:F2})");
        }
        
        return bestMinimum;
    }
    
    /// <summary>
    /// Applies dynamic weighting based on remaining dice count
    /// Aggressive mode always prioritizes clearing all dice for hot streaks
    /// </summary>
    void ApplyDiceCountWeighting(List<StrategyResult> combinations, int totalDiceCount, BehaviorMode mode)
    {
        if (mode != BehaviorMode.AGGRESSIVE)
            return;
        
        foreach (var combo in combinations)
        {
            float baseValue = combo.strategicValue;
            
            // Hot streak bonus: If combination uses all dice, massive bonus
            // This applies to ANY dice count (1-6) to maximize hot streak opportunities
            if (combo.diceUsed == totalDiceCount)
            {
                combo.strategicValue = baseValue * 10.0f; // 10x multiplier for hot streak
                if (enableDebugLogs)
                    Debug.Log($"  Hot Streak Bonus: {combo.combination.rule} ({combo.diceUsed}/{totalDiceCount} dice) " +
                             $"value boosted from {baseValue:F1} to {combo.strategicValue:F1}");
            }
            // Progressive bonus: Favor combinations that use more dice
            // This helps AI work toward clearing the board
            else
            {
                float diceUsageRatio = (float)combo.diceUsed / totalDiceCount;
                float bonus = 1.0f + (diceUsageRatio * 0.3f); // Up to 30% bonus for partial usage
                combo.strategicValue = baseValue * bonus;
                
                if (enableDebugLogs && bonus > 1.1f)
                    Debug.Log($"  Dice Usage Bonus: {combo.combination.rule} ({combo.diceUsed}/{totalDiceCount} dice) " +
                             $"value boosted from {baseValue:F1} to {combo.strategicValue:F1}");
            }
        }
    }
    
    /// <summary>
    /// Core minimum dice algorithm with multi-criteria evaluation and hot streak detection
    /// </summary>
    StrategyResult ApplyMinimumDiceAlgorithm(List<StrategyResult> combinations, int totalDiceCount, BehaviorMode mode)
    {
        // PRIORITY CHECK: Hot Streak Detection (clears all dice)
        // If aggressive mode and a combination uses all dice, strongly prefer it
        if (mode == BehaviorMode.AGGRESSIVE)
        {
            var hotStreakCombos = combinations.Where(c => c.diceUsed == totalDiceCount).ToList();
            if (hotStreakCombos.Count > 0)
            {
                // Select best hot streak combination (highest points)
                var bestHotStreak = hotStreakCombos.OrderByDescending(c => c.combination.points).First();
                
                if (enableDebugLogs)
                    Debug.Log($"HOT STREAK DETECTED: {bestHotStreak.combination.rule} clears all {totalDiceCount} dice for {bestHotStreak.combination.points} points!");
                
                return bestHotStreak;
            }
        }
        
        // Step 1: Group combinations by dice count
        var groupedByDice = combinations.GroupBy(c => c.diceUsed).OrderBy(g => g.Key);
        
        // Step 2: Find the minimum dice count that has viable combinations
        foreach (var diceGroup in groupedByDice)
        {
            var viableCombinations = ValidateMinimumViableCombinations(diceGroup.ToList(), mode);
            
            if (viableCombinations.Count > 0)
            {
                // Step 3: Apply strategic comparison within minimum dice group
                return CompareMinimumDiceCombinations(viableCombinations, mode);
            }
        }
        
        // Fallback: return any combination if no viable ones found
        return combinations.OrderByDescending(c => c.strategicValue).First();
    }
    
    /// <summary>
    /// Validates that combinations are viable for minimum dice strategy
    /// </summary>
    List<StrategyResult> ValidateMinimumViableCombinations(List<StrategyResult> combinations, BehaviorMode mode)
    {
        List<StrategyResult> viable = new List<StrategyResult>();
        
        foreach (var combo in combinations)
        {
            bool isViable = true;
            string validationReason = "";
            
            // Validation Rule 1: Must have positive points
            if (combo.combination.points <= 0)
            {
                isViable = false;
                validationReason = "Zero or negative points";
            }
            
            // Validation Rule 2: Strategic value threshold based on mode
            float minStrategicValue = mode == BehaviorMode.AGGRESSIVE ? 25f : 40f; // Points per dice
            if (combo.strategicValue < minStrategicValue)
            {
                isViable = false;
                validationReason = $"Strategic value {combo.strategicValue:F1} below threshold {minStrategicValue}";
            }
            
            // Validation Rule 3: Avoid extremely inefficient combinations in passive mode
            if (mode == BehaviorMode.PASSIVE && combo.diceUsed > 3 && combo.combination.points < 200)
            {
                isViable = false;
                validationReason = "Inefficient for passive mode";
            }
            
            // Validation Rule 4: Prefer combinations that leave dice for rerolling
            if (combo.diceUsed == 6 && combo.combination.points < 500)
            {
                isViable = false;
                validationReason = "Uses all dice for low points";
            }
            
            if (isViable)
            {
                viable.Add(combo);
            }
            else if (enableDebugLogs)
            {
                Debug.Log($"AICombinationStrategy: Rejected {combo.combination.rule} - {validationReason}");
            }
        }
        
        return viable;
    }
    
    /// <summary>
    /// Compares combinations within the same minimum dice count using strategic criteria
    /// </summary>
    StrategyResult CompareMinimumDiceCombinations(List<StrategyResult> combinations, BehaviorMode mode)
    {
        if (combinations.Count == 1)
        {
            return combinations[0];
        }
        
        // Multi-criteria comparison for minimum dice selection
        switch (mode)
        {
            case BehaviorMode.AGGRESSIVE:
                return CompareAggressiveMinimum(combinations);
                
            case BehaviorMode.PASSIVE:
                return ComparePassiveMinimum(combinations);
                
            default:
                return combinations.OrderByDescending(c => c.combination.points).First();
        }
    }
    
    /// <summary>
    /// Aggressive mode minimum dice comparison - prioritizes maximum points
    /// </summary>
    StrategyResult CompareAggressiveMinimum(List<StrategyResult> combinations)
    {
        // Aggressive priority: Highest points first, then highest tier, then efficiency
        return combinations
            .OrderByDescending(c => c.combination.points)
            .ThenBy(c => (int)c.tier)
            .ThenByDescending(c => c.strategicValue)
            .First();
    }
    
    /// <summary>
    /// Passive mode minimum dice comparison - prioritizes efficiency and safety
    /// </summary>
    StrategyResult ComparePassiveMinimum(List<StrategyResult> combinations)
    {
        // Passive priority: Highest efficiency first, then lowest dice usage, then points
        return combinations
            .OrderByDescending(c => c.strategicValue)
            .ThenBy(c => c.diceUsed)
            .ThenByDescending(c => c.combination.points)
            .First();
    }
    
    /// <summary>
    /// Analyzes remaining dice after taking a combination
    /// </summary>
    public RemainingDiceAnalysis AnalyzeRemainingDice(List<int> originalDice, StrategyResult selectedCombination)
    {
        int remainingCount = originalDice.Count - selectedCombination.diceUsed;
        
        // Estimate reroll potential
        float rerollPotential = EstimateRerollPotential(remainingCount);
        
        // Calculate risk level
        RiskLevel risk = CalculateRemainingDiceRisk(remainingCount);
        
        return new RemainingDiceAnalysis
        {
            RemainingDiceCount = remainingCount,
            RerollPotential = rerollPotential,
            Risk = risk,
            RecommendContinue = ShouldContinueWithRemainingDice(remainingCount, rerollPotential, risk)
        };
    }
    
    /// <summary>
    /// Estimates the potential points from rerolling remaining dice
    /// </summary>
    float EstimateRerollPotential(int diceCount)
    {
        if (diceCount <= 0) return 0f;
        
        // Statistical expectation based on dice count
        // Each die has ~33% chance of being 1 or 5 (basic scoring)
        float basicExpectation = diceCount * 0.33f * 75f; // Average of 100 (for 1) and 50 (for 5)
        
        // Bonus potential for combinations
        float combinationBonus = diceCount >= 3 ? diceCount * 20f : 0f;
        
        return basicExpectation + combinationBonus;
    }
    
    /// <summary>
    /// Calculates risk level for remaining dice count
    /// </summary>
    RiskLevel CalculateRemainingDiceRisk(int diceCount)
    {
        if (diceCount <= 0) return RiskLevel.None;
        if (diceCount == 1) return RiskLevel.VeryHigh;
        if (diceCount == 2) return RiskLevel.High;
        if (diceCount == 3) return RiskLevel.Medium;
        if (diceCount <= 4) return RiskLevel.Low;
        return RiskLevel.VeryLow;
    }
    
    /// <summary>
    /// Determines if AI should continue with remaining dice
    /// </summary>
    bool ShouldContinueWithRemainingDice(int diceCount, float potential, RiskLevel risk)
    {
        if (diceCount <= 0) return false;
        
        // Risk vs reward analysis
        float riskFactor = GetRiskFactor(risk);
        return potential > (100f * riskFactor); // Continue if potential exceeds risk-adjusted threshold
    }
    
    float GetRiskFactor(RiskLevel risk)
    {
        switch (risk)
        {
            case RiskLevel.VeryLow: return 0.5f;
            case RiskLevel.Low: return 0.7f;
            case RiskLevel.Medium: return 1.0f;
            case RiskLevel.High: return 1.5f;
            case RiskLevel.VeryHigh: return 2.5f;
            default: return 1.0f;
        }
    }
    
    /// <summary>
    /// Gets combination threshold based on behavior mode and dice count
    /// </summary>
    float GetCombinationThreshold(BehaviorMode mode, int diceCount)
    {
        float initialThreshold;
        float reductionRate;
        
        switch (mode)
        {
            case BehaviorMode.AGGRESSIVE:
                initialThreshold = config.AggressiveInitialThreshold;
                reductionRate = config.AggressiveThresholdReduction;
                break;
            case BehaviorMode.PASSIVE:
                initialThreshold = config.PassiveInitialThreshold;
                reductionRate = config.PassiveThresholdReduction;
                break;
            default:
                initialThreshold = config.AggressiveInitialThreshold;
                reductionRate = config.AggressiveThresholdReduction;
                break;
        }
        
        // Reduce threshold based on dice count (fewer dice = lower threshold)
        int diceReductions = 6 - diceCount;
        float threshold = initialThreshold - (diceReductions * reductionRate);
        
        // Ensure threshold doesn't go below 0.1 (10%)
        return Mathf.Max(threshold, 0.1f);
    }
    
    /// <summary>
    /// Gets tier value for threshold comparison
    /// </summary>
    float GetTierValue(CombinationTier tier)
    {
        switch (tier)
        {
            case CombinationTier.Tier1: return 1.0f;  // 100%
            case CombinationTier.Tier2: return 0.8f;  // 80%
            case CombinationTier.Tier3: return 0.6f;  // 60%
            case CombinationTier.Tier4: return 0.4f;  // 40%
            case CombinationTier.Tier5: return 0.2f;  // 20%
            default: return 0.0f;
        }
    }
    
    // Helper methods for combination detection
    bool HasLargeStraight(List<int> dice) => dice.Distinct().Count() == 6 && dice.Contains(1) && dice.Contains(6);
    bool HasMiddleStraight(List<int> dice) => HasConsecutive(dice, 5);
    bool HasSmallStraight(List<int> dice) => HasConsecutive(dice, 4);
    bool HasLowStraight(List<int> dice) => HasConsecutive(dice, 3);
    
    bool HasConsecutive(List<int> dice, int length)
    {
        var sorted = dice.Distinct().OrderBy(x => x).ToList();
        for (int i = 0; i <= sorted.Count - length; i++)
        {
            bool consecutive = true;
            for (int j = 1; j < length; j++)
            {
                if (sorted[i + j] != sorted[i] + j)
                {
                    consecutive = false;
                    break;
                }
            }
            if (consecutive) return true;
        }
        return false;
    }
    
    /// <summary>
    /// Finds all valid combinations from dice values
    /// </summary>
    public List<CombinationResult> FindAllValidCombinations(List<int> diceValues)
    {
        if (diceValues == null || diceValues.Count == 0)
            return new List<CombinationResult>();
        
        List<CombinationResult> combinations = new List<CombinationResult>();
        
        // Use existing combination detection logic
        var allCombinations = FindAllCombinations(diceValues, 0.0f); // No threshold
        
        // Convert StrategyResult to CombinationResult
        foreach (var strategy in allCombinations)
        {
            combinations.Add(strategy.combination);
        }
        
        return combinations;
    }
    
    /// <summary>
    /// Selects optimal combination based on AI behavior mode and current state
    /// </summary>
    public CombinationResult SelectOptimalCombination(List<CombinationResult> combinations, 
                                                     BehaviorMode mode, int remainingDice, int iteration)
    {
        if (combinations == null || combinations.Count == 0)
            return null;
        
        // Convert to StrategyResults for evaluation
        List<StrategyResult> strategies = new List<StrategyResult>();
        float threshold = GetCombinationThreshold(mode, remainingDice);
        
        foreach (var combo in combinations)
        {
            var tier = ClassifyCombinationTier(combo);
            int diceUsed = GetDiceUsedForCombination(combo.rule);
            float strategicValue = diceUsed > 0 ? (float)combo.points / diceUsed : 0f;
            bool meetsThreshold = GetTierValue(tier) >= threshold;
            
            var strategy = new StrategyResult(combo, tier, strategicValue, 
                                            diceUsed, meetsThreshold, combo.description);
            strategies.Add(strategy);
        }
        
        // Select best strategy
        var bestStrategy = SelectBestCombination(strategies, mode);
        return bestStrategy?.combination;
    }
    
    /// <summary>
    /// Classifies combination into appropriate tier
    /// </summary>
    CombinationTier ClassifyCombinationTier(CombinationResult combination)
    {
        switch (combination.rule)
        {
            case Rule.MaxStraight:
            case Rule.Straight:
            case Rule.ThreePairs:
                return CombinationTier.Tier1;
                
            case Rule.FourOfKind:
            case Rule.MiddleStraight:
                return CombinationTier.Tier2;
                
            case Rule.ThreeOfKind:
            case Rule.TwoPair:
                return CombinationTier.Tier3;
                
            case Rule.Pair:
            case Rule.LowStraight:
                return CombinationTier.Tier4;
                
            case Rule.One:
                return CombinationTier.Tier5;
                
            default:
                return CombinationTier.Tier5;
        }
    }
    
    /// <summary>
    /// Updates configuration at runtime
    /// </summary>
    public void UpdateConfiguration(AIConfiguration newConfig)
    {
        config = newConfig;
        if (enableDebugLogs)
            Debug.Log("AICombinationStrategy: Configuration updated");
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
                return 4; // Four of the same value
            case Rule.ThreePairs:
                return 6;
            case Rule.MaxStraight:
                return 6;
            case Rule.TwoSets:
                return 6;
            case Rule.Zonk:
                return 0;
            default:
                return 1; // Default fallback
        }
    }
    
    /// <summary>
    /// Finds indices of dice that form a specific straight pattern
    /// </summary>
    List<int> FindStraightIndices(List<int> diceValues, List<int> targetValues)
    {
        List<int> indices = new List<int>();
        List<int> remainingTargets = new List<int>(targetValues);
        
        for (int i = 0; i < diceValues.Count && remainingTargets.Count > 0; i++)
        {
            if (remainingTargets.Contains(diceValues[i]))
            {
                indices.Add(i);
                remainingTargets.Remove(diceValues[i]);
            }
        }
        
        return indices;
    }
    
    /// <summary>
    /// Finds indices for any straight of given length
    /// </summary>
    List<int> FindAnyStraightIndices(List<int> diceValues, int length)
    {
        var sorted = diceValues.Select((value, index) => new { value, index })
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
                consecutiveCount = 1;
                indices.Clear();
                indices.Add(sorted[i].index);
                lastValue = sorted[i].value;
            }
        }
        
        return indices.Take(length).ToList();
    }
    
    /// <summary>
    /// Finds indices of dice with a specific value (for of-a-kind combinations)
    /// </summary>
    List<int> FindValueIndices(List<int> diceValues, int targetValue, int count)
    {
        List<int> indices = new List<int>();
        
        for (int i = 0; i < diceValues.Count && indices.Count < count; i++)
        {
            if (diceValues[i] == targetValue)
            {
                indices.Add(i);
            }
        }
        
        return indices;
    }
    
    /// <summary>
    /// Finds indices for single 1 or 5 (minimum dice strategy)
    /// </summary>
    List<int> FindSingleOneOrFiveIndex(List<int> diceValues)
    {
        // Prefer 1 over 5 for minimum dice (1 gives 100 points, 5 gives 50)
        for (int i = 0; i < diceValues.Count; i++)
        {
            if (diceValues[i] == 1)
            {
                return new List<int> { i };
            }
        }
        
        for (int i = 0; i < diceValues.Count; i++)
        {
            if (diceValues[i] == 5)
            {
                return new List<int> { i };
            }
        }
        
        return new List<int>();
    }
}
