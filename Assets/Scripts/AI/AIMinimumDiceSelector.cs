using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HybridEnemyAI;

/// <summary>
/// Specialized component for minimum dice selection algorithm
/// Implements advanced logic to find combinations using fewest dice with strategic validation
/// </summary>
public class AIMinimumDiceSelector : MonoBehaviour
{
    [Header("Dependencies")]
    public AICombinationStrategy combinationStrategy;
    
    [Header("Algorithm Configuration")]
    [Range(1, 6)]
    public int maxDiceUsageThreshold = 4; // Don't use more than 4 dice unless exceptional value
    
    [Range(10f, 100f)]
    public float minEfficiencyThreshold = 30f; // Minimum points per dice
    
    [Range(100, 1000)]
    public int minPointsThreshold = 150; // Minimum points to consider viable
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool showValidationDetails = false;
    
    /// <summary>
    /// Result of minimum dice selection with detailed analysis
    /// </summary>
    [System.Serializable]
    public class MinimumDiceResult
    {
        public AICombinationStrategy.StrategyResult selectedCombination;
        public List<AICombinationStrategy.StrategyResult> alternativeCombinations;
        public AICombinationStrategy.RemainingDiceAnalysis remainingAnalysis;
        public SelectionReason reason;
        public float confidenceScore; // 0-1 confidence in the selection
        
        public MinimumDiceResult()
        {
            alternativeCombinations = new List<AICombinationStrategy.StrategyResult>();
        }
    }
    
    public enum SelectionReason
    {
        OptimalEfficiency,      // Best points per dice ratio
        MinimumDiceUsage,      // Uses fewest dice
        HighValueException,    // High value justifies more dice
        SafetyFirst,           // Conservative choice
        OnlyViableOption,      // No other valid combinations
        FallbackSelection      // Emergency fallback
    }
    
    void Start()
    {
        if (combinationStrategy == null)
        {
            combinationStrategy = GetComponent<AICombinationStrategy>();
            if (combinationStrategy == null)
            {
                combinationStrategy = FindObjectOfType<AICombinationStrategy>();
            }
        }
        
        if (combinationStrategy == null)
        {
            Debug.LogError("AIMinimumDiceSelector: No AICombinationStrategy found!");
        }
    }
    
    /// <summary>
    /// Main entry point for minimum dice selection
    /// </summary>
    public MinimumDiceResult SelectMinimumDiceCombination(List<int> diceValues, BehaviorMode mode)
    {
        if (diceValues == null || diceValues.Count == 0 || combinationStrategy == null)
        {
            return null;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIMinimumDiceSelector: Analyzing {diceValues.Count} dice: [{string.Join(",", diceValues)}] in {mode} mode");
        }
        
        // Step 1: Get all possible combinations
        var allCombinations = GetAllPossibleCombinations(diceValues);
        
        if (allCombinations.Count == 0)
        {
            if (enableDebugLogs)
                Debug.Log("AIMinimumDiceSelector: No combinations found (Zonk)");
            return null;
        }
        
        // Step 2: Apply minimum dice algorithm
        var result = ApplyMinimumDiceAlgorithm(allCombinations, diceValues, mode);
        
        if (enableDebugLogs)
        {
            LogSelectionResult(result);
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets all possible combinations using the strategy component
    /// </summary>
    List<AICombinationStrategy.StrategyResult> GetAllPossibleCombinations(List<int> diceValues)
    {
        // Use the combination strategy to find all combinations
        var strategyResult = combinationStrategy.EvaluateBestStrategy(diceValues, BehaviorMode.AGGRESSIVE, diceValues.Count);
        
        // For minimum dice selection, we need to manually find all combinations
        // This is a simplified approach - in a full implementation, we'd extract all combinations
        List<AICombinationStrategy.StrategyResult> combinations = new List<AICombinationStrategy.StrategyResult>();
        
        // Find single dice combinations (1s and 5s) - always minimum dice
        combinations.AddRange(FindSingleDiceCombinations(diceValues));
        
        // Find pair combinations
        combinations.AddRange(FindPairCombinations(diceValues));
        
        // Find of-a-kind combinations
        combinations.AddRange(FindOfAKindCombinations(diceValues));
        
        // Find straight combinations
        combinations.AddRange(FindStraightCombinations(diceValues));
        
        return combinations;
    }
    
    /// <summary>
    /// Core minimum dice selection algorithm
    /// </summary>
    MinimumDiceResult ApplyMinimumDiceAlgorithm(List<AICombinationStrategy.StrategyResult> combinations, List<int> originalDice, BehaviorMode mode)
    {
        var result = new MinimumDiceResult();
        
        // Step 1: Filter viable combinations
        var viableCombinations = FilterViableCombinations(combinations, mode);
        
        if (viableCombinations.Count == 0)
        {
            // Fallback to any combination
            result.selectedCombination = combinations.OrderBy(c => c.diceUsed).ThenByDescending(c => c.combination.points).FirstOrDefault();
            result.reason = SelectionReason.FallbackSelection;
            result.confidenceScore = 0.1f;
            return result;
        }
        
        // Step 2: Group by dice usage
        var groupedByDice = viableCombinations.GroupBy(c => c.diceUsed).OrderBy(g => g.Key);
        
        // Step 3: Find optimal group
        var optimalGroup = FindOptimalDiceGroup(groupedByDice, mode);
        
        // Step 4: Select best combination within optimal group
        result.selectedCombination = SelectBestInGroup(optimalGroup.ToList(), mode);
        result.alternativeCombinations = optimalGroup.Where(c => c != result.selectedCombination).ToList();
        
        // Step 5: Analyze remaining dice
        result.remainingAnalysis = combinationStrategy.AnalyzeRemainingDice(originalDice, result.selectedCombination);
        
        // Step 6: Determine selection reason and confidence
        DetermineSelectionReasonAndConfidence(result, optimalGroup.ToList(), mode);
        
        return result;
    }
    
    /// <summary>
    /// Filters combinations based on viability criteria
    /// </summary>
    List<AICombinationStrategy.StrategyResult> FilterViableCombinations(List<AICombinationStrategy.StrategyResult> combinations, BehaviorMode mode)
    {
        var viable = new List<AICombinationStrategy.StrategyResult>();
        
        foreach (var combo in combinations)
        {
            bool isViable = true;
            string rejectionReason = "";
            
            // Criterion 1: Minimum points threshold
            if (combo.combination.points < minPointsThreshold)
            {
                isViable = false;
                rejectionReason = $"Points {combo.combination.points} below threshold {minPointsThreshold}";
            }
            
            // Criterion 2: Efficiency threshold
            else if (combo.strategicValue < minEfficiencyThreshold)
            {
                isViable = false;
                rejectionReason = $"Efficiency {combo.strategicValue:F1} below threshold {minEfficiencyThreshold}";
            }
            
            // Criterion 3: Dice usage threshold (with exceptions for high value)
            else if (combo.diceUsed > maxDiceUsageThreshold && combo.combination.points < 500)
            {
                isViable = false;
                rejectionReason = $"Uses {combo.diceUsed} dice (>{maxDiceUsageThreshold}) for only {combo.combination.points} points";
            }
            
            // Criterion 4: Mode-specific filters
            else if (mode == BehaviorMode.PASSIVE && combo.diceUsed > 3 && combo.strategicValue < 50f)
            {
                isViable = false;
                rejectionReason = "Inefficient for passive mode";
            }
            
            if (isViable)
            {
                viable.Add(combo);
            }
            else if (showValidationDetails)
            {
                Debug.Log($"AIMinimumDiceSelector: Rejected {combo.combination.rule} - {rejectionReason}");
            }
        }
        
        return viable;
    }
    
    /// <summary>
    /// Finds the optimal dice usage group
    /// </summary>
    IGrouping<int, AICombinationStrategy.StrategyResult> FindOptimalDiceGroup(IOrderedEnumerable<IGrouping<int, AICombinationStrategy.StrategyResult>> groupedByDice, BehaviorMode mode)
    {
        // Strategy: Find the group with minimum dice that has good combinations
        foreach (var group in groupedByDice)
        {
            var bestInGroup = group.OrderByDescending(c => c.strategicValue).First();
            
            // Accept this group if it has a strong combination
            if (bestInGroup.strategicValue >= minEfficiencyThreshold * 1.2f || // 20% above threshold
                bestInGroup.combination.points >= minPointsThreshold * 2)      // Double points threshold
            {
                return group;
            }
        }
        
        // Fallback: return the minimum dice group
        return groupedByDice.First();
    }
    
    /// <summary>
    /// Selects the best combination within a dice usage group
    /// </summary>
    AICombinationStrategy.StrategyResult SelectBestInGroup(List<AICombinationStrategy.StrategyResult> group, BehaviorMode mode)
    {
        switch (mode)
        {
            case BehaviorMode.AGGRESSIVE:
                // Aggressive: Highest points first, then efficiency
                return group.OrderByDescending(c => c.combination.points)
                           .ThenByDescending(c => c.strategicValue)
                           .First();
                
            case BehaviorMode.PASSIVE:
                // Passive: Highest efficiency first, then points
                return group.OrderByDescending(c => c.strategicValue)
                           .ThenByDescending(c => c.combination.points)
                           .First();
                
            default:
                return group.OrderByDescending(c => c.strategicValue).First();
        }
    }
    
    /// <summary>
    /// Determines the reason for selection and confidence score
    /// </summary>
    void DetermineSelectionReasonAndConfidence(MinimumDiceResult result, List<AICombinationStrategy.StrategyResult> groupOptions, BehaviorMode mode)
    {
        var selected = result.selectedCombination;
        
        // Determine reason
        if (selected.strategicValue >= minEfficiencyThreshold * 1.5f)
        {
            result.reason = SelectionReason.OptimalEfficiency;
            result.confidenceScore = 0.9f;
        }
        else if (selected.diceUsed <= 2)
        {
            result.reason = SelectionReason.MinimumDiceUsage;
            result.confidenceScore = 0.8f;
        }
        else if (selected.combination.points >= 500)
        {
            result.reason = SelectionReason.HighValueException;
            result.confidenceScore = 0.7f;
        }
        else if (mode == BehaviorMode.PASSIVE && selected.strategicValue >= minEfficiencyThreshold)
        {
            result.reason = SelectionReason.SafetyFirst;
            result.confidenceScore = 0.6f;
        }
        else if (groupOptions.Count == 1)
        {
            result.reason = SelectionReason.OnlyViableOption;
            result.confidenceScore = 0.5f;
        }
        else
        {
            result.reason = SelectionReason.FallbackSelection;
            result.confidenceScore = 0.3f;
        }
    }
    
    /// <summary>
    /// Logs detailed selection result for debugging
    /// </summary>
    void LogSelectionResult(MinimumDiceResult result)
    {
        if (result?.selectedCombination == null) return;
        
        var selected = result.selectedCombination;
        Debug.Log($"AIMinimumDiceSelector: Selected {selected.combination.rule} " +
                 $"({selected.diceUsed} dice, {selected.combination.points} pts, " +
                 $"efficiency: {selected.strategicValue:F1}) " +
                 $"Reason: {result.reason}, Confidence: {result.confidenceScore:F1}");
        
        if (result.remainingAnalysis != null)
        {
            Debug.Log($"AIMinimumDiceSelector: {result.remainingAnalysis}");
        }
        
        if (result.alternativeCombinations.Count > 0)
        {
            Debug.Log($"AIMinimumDiceSelector: {result.alternativeCombinations.Count} alternatives available");
        }
    }
    
    // Simplified combination finding methods (these would ideally use the full strategy component)
    List<AICombinationStrategy.StrategyResult> FindSingleDiceCombinations(List<int> dice)
    {
        var results = new List<AICombinationStrategy.StrategyResult>();
        
        int ones = dice.Count(d => d == 1);
        int fives = dice.Count(d => d == 5);
        
        if (ones > 0)
        {
            var combo = new CombinationResult(Rule.One, ones * 100, $"{ones} One(s)");
            results.Add(new AICombinationStrategy.StrategyResult(combo, AICombinationStrategy.CombinationTier.Tier5, 100f, ones, true, "Single ones"));
        }
        
        if (fives > 0)
        {
            var combo = new CombinationResult(Rule.One, fives * 50, $"{fives} Five(s)");
            results.Add(new AICombinationStrategy.StrategyResult(combo, AICombinationStrategy.CombinationTier.Tier5, 50f, fives, true, "Single fives"));
        }
        
        return results;
    }
    
    List<AICombinationStrategy.StrategyResult> FindPairCombinations(List<int> dice)
    {
        var results = new List<AICombinationStrategy.StrategyResult>();
        var counts = dice.GroupBy(x => x).Where(g => g.Count() >= 2);
        
        foreach (var group in counts)
        {
            int value = group.Key;
            int points = value == 1 ? 200 : value * 20;
            var combo = new CombinationResult(Rule.Pair, points, $"Pair of {value}s");
            results.Add(new AICombinationStrategy.StrategyResult(combo, AICombinationStrategy.CombinationTier.Tier4, points / 2f, 2, true, "Pair"));
        }
        
        return results;
    }
    
    List<AICombinationStrategy.StrategyResult> FindOfAKindCombinations(List<int> dice)
    {
        var results = new List<AICombinationStrategy.StrategyResult>();
        var counts = dice.GroupBy(x => x).Where(g => g.Count() >= 3);
        
        foreach (var group in counts)
        {
            int value = group.Key;
            int count = group.Count();
            
            if (count >= 3)
            {
                int points = value == 1 ? 1000 : value * 100;
                var combo = new CombinationResult(Rule.ThreeOfKind, points, $"Three {value}s");
                results.Add(new AICombinationStrategy.StrategyResult(combo, AICombinationStrategy.CombinationTier.Tier3, points / 3f, 3, true, "Three of a kind"));
            }
        }
        
        return results;
    }
    
    List<AICombinationStrategy.StrategyResult> FindStraightCombinations(List<int> dice)
    {
        var results = new List<AICombinationStrategy.StrategyResult>();
        
        // Simple straight detection (this would be more sophisticated in full implementation)
        var unique = dice.Distinct().OrderBy(x => x).ToList();
        
        if (unique.Count >= 6 && unique.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }))
        {
            var combo = new CombinationResult(Rule.MaxStraight, 1500, "Large Straight");
            results.Add(new AICombinationStrategy.StrategyResult(combo, AICombinationStrategy.CombinationTier.Tier1, 250f, 6, true, "Large straight"));
        }
        
        return results;
    }
}