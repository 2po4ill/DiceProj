using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DiceCombinationDetector : MonoBehaviour
{
    [Header("Rules")]
    public DiceCombinationRules combinationRules;
    
    public CombinationResult CheckForBestCombination(List<int> diceValues)
    {
        if (combinationRules == null)
        {
            Debug.LogError("No combination rules assigned!");
            return null;
        }
        
        // Check combinations in order of value (best first)
        foreach (var rule in combinationRules.combinations)
        {
            if (CheckRule(diceValues, rule.rule))
            {
                int score = CalculateScore(diceValues, rule);
                return new CombinationResult(rule.rule, score, rule.description, rule.multiplier);
            }
        }
        
        return null; // No combination found
    }
    
    // New method to check if ANY combination exists in the dice set
    public bool HasAnyCombination(List<int> diceValues)
    {
        if (combinationRules == null) return false;
        
        // Check basic combinations first (most common)
        // If any basic combination exists, higher tiers might also exist
        
        // Check for basic patterns that enable multiple combinations
        if (CanFormOne(diceValues)) return true;           // Most basic
        if (CanFormPair(diceValues)) return true;          // Enables many higher combinations
        if (CanFormLowStraight(diceValues)) return true;   // Enables higher straights
        
        // If no combinations found, it's a Zonk
        return false;
    }
    
    // Check if the current dice state is a Zonk (no combinations possible)
    public bool IsZonk(List<int> diceValues)
    {
        return !HasAnyCombination(diceValues);
    }
    
    // Get Zonk result for scoring
    public CombinationResult GetZonkResult()
    {
        // Find Zonk rule in combination rules
        var zonkRule = System.Array.Find(combinationRules.combinations, r => r.rule == Rule.Zonk);
        if (zonkRule != null)
        {
            return new CombinationResult(Rule.Zonk, 0, zonkRule.description, 0f);
        }
        
        // Default Zonk result if no rule defined
        return new CombinationResult(Rule.Zonk, 0, "Zonk! No combinations available - turn ends with 0 points", 0f);
    }
    
    // Check if the dice set contains the potential to form a specific combination
    bool CanFormCombination(List<int> diceValues, Rule rule)
    {
        var counts = GetValueCounts(diceValues);
        
        switch (rule)
        {
            // Straight hierarchy: MaxStraight > Straight > MiddleStraight > LowStraight
            case Rule.MaxStraight:
                return CanFormMaxStraight(diceValues);
            case Rule.Straight:
                return CanFormMaxStraight(diceValues) || CanFormStraight(diceValues);
            case Rule.MiddleStraight:
                return CanFormMaxStraight(diceValues) || CanFormStraight(diceValues) || CanFormMiddleStraight(diceValues);
            case Rule.LowStraight:
                return CanFormMaxStraight(diceValues) || CanFormStraight(diceValues) || CanFormMiddleStraight(diceValues) || CanFormLowStraight(diceValues);
            
            // Count hierarchy: Higher counts can form lower count combinations
            case Rule.TwoSets:
                return CanFormTwoSets(diceValues);
            case Rule.ThreePairs:
                return CanFormTwoSets(diceValues) || CanFormThreePairs(diceValues);
            case Rule.FourOfKind:
                return CanFormFourOfKind(diceValues);
            case Rule.FullHouse:
                return CanFormFourOfKind(diceValues) || CanFormFullHouse(diceValues);
            case Rule.ThreeOfKind:
                return CanFormFourOfKind(diceValues) || CanFormFullHouse(diceValues) || CanFormThreeOfKind(diceValues);
            case Rule.TwoPair:
                return CanFormFourOfKind(diceValues) || CanFormFullHouse(diceValues) || CanFormThreePairs(diceValues) || CanFormTwoPair(diceValues);
            case Rule.Pair:
                return CanFormFourOfKind(diceValues) || CanFormFullHouse(diceValues) || CanFormThreeOfKind(diceValues) || CanFormThreePairs(diceValues) || CanFormTwoPair(diceValues) || CanFormPair(diceValues);
            
            // Special cases
            case Rule.One:
                return CanFormOne(diceValues);
            
            default:
                return false;
        }
    }
    
    // Potential combination checkers (look for patterns in 6 dice)
    bool CanFormTwoSets(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        var threeOrMoreCounts = counts.Values.Where(count => count >= 3).Count();
        return threeOrMoreCounts >= 2;
    }
    
    bool CanFormMaxStraight(List<int> diceValues)
    {
        var unique = diceValues.Distinct().ToList();
        return unique.Count == 6; // All different values
    }
    
    bool CanFormStraight(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        
        int[][] straights = {
            new int[] {1, 2, 3, 4, 5},
            new int[] {2, 3, 4, 5, 6}
        };
        
        foreach (var straight in straights)
        {
            bool hasAllValues = straight.All(value => counts.ContainsKey(value));
            if (hasAllValues) return true;
        }
        return false;
    }
    
    bool CanFormThreePairs(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        var pairCount = counts.Values.Where(count => count >= 2).Count();
        return pairCount >= 3;
    }
    
    bool CanFormFourOfKind(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        return counts.Values.Any(count => count >= 4);
    }
    
    bool CanFormFullHouse(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        bool hasThree = counts.Values.Any(count => count >= 3);
        bool hasPair = counts.Values.Any(count => count >= 2);
        return hasThree && hasPair && counts.Keys.Count >= 2;
    }
    
    bool CanFormThreeOfKind(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        return counts.Values.Any(count => count >= 3);
    }
    
    bool CanFormMiddleStraight(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        
        int[][] straights = {
            new int[] {1, 2, 3, 4},
            new int[] {2, 3, 4, 5},
            new int[] {3, 4, 5, 6}
        };
        
        foreach (var straight in straights)
        {
            bool hasAllValues = straight.All(value => counts.ContainsKey(value));
            if (hasAllValues) return true;
        }
        return false;
    }
    
    bool CanFormLowStraight(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        
        int[][] straights = {
            new int[] {1, 2, 3},
            new int[] {2, 3, 4},
            new int[] {3, 4, 5},
            new int[] {4, 5, 6}
        };
        
        foreach (var straight in straights)
        {
            bool hasAllValues = straight.All(value => counts.ContainsKey(value));
            if (hasAllValues) return true;
        }
        return false;
    }
    
    bool CanFormTwoPair(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        var pairCount = counts.Values.Where(count => count >= 2).Count();
        return pairCount >= 2;
    }
    
    bool CanFormPair(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        return counts.Values.Any(count => count >= 2);
    }
    
    bool CanFormOne(List<int> diceValues)
    {
        return diceValues.Contains(1) || diceValues.Contains(5);
    }
    
    bool CheckRule(List<int> diceValues, Rule rule)
    {
        switch (rule)
        {
            case Rule.TwoSets:
                return CheckTwoSets(diceValues);
            case Rule.MaxStraight:
                return CheckMaxStraight(diceValues);
            case Rule.Straight:
                return CheckStraight(diceValues);
            case Rule.ThreePairs:
                return CheckThreePairs(diceValues);
            case Rule.FourOfKind:
                return CheckFourOfKind(diceValues);
            case Rule.FullHouse:
                return CheckFullHouse(diceValues);
            case Rule.ThreeOfKind:
                return CheckThreeOfKind(diceValues);
            case Rule.MiddleStraight:
                return CheckMiddleStraight(diceValues);
            case Rule.LowStraight:
                return CheckLowStraight(diceValues);
            case Rule.TwoPair:
                return CheckTwoPair(diceValues);
            case Rule.Pair:
                return CheckPair(diceValues);
            case Rule.One:
                return CheckOne(diceValues);        
            default:
                return false;
        }
    }
    
    // Detection methods
    bool CheckTwoSets(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        var uniqueValues = counts.Keys.ToList();
        return uniqueValues.Count == 2 && diceValues.Count == 6;
    }

    bool CheckMaxStraight(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        var uniqueValues = counts.Keys.ToList();
        return uniqueValues.Count == 6;
    }

    bool CheckStraight(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        
        // Define the two possible straights
        int[][] straights = {
            new int[] {1, 2, 3, 4, 5},  // Low straight
            new int[] {2, 3, 4, 5, 6}   // High straight
        };
        
        // Check if any straight pattern exists
        foreach (var straight in straights)
        {
            bool hasAllValues = straight.All(value => counts.ContainsKey(value));
            if (hasAllValues) return diceValues.Count == 5;
        }
        
        return false;
    }

    bool CheckThreePairs(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        return counts.Values.Count(count => count == 2) == 3;
    }

    bool CheckFourOfKind(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        return counts.Values.Any(count => count >= 4);
    }
    bool CheckFullHouse(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        return counts.Values.Contains(3) && counts.Values.Contains(2) && diceValues.Count == 5;
    }

    bool CheckThreeOfKind(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        var uniqueValues = counts.Keys.ToList();
        return diceValues.Count == 3 && uniqueValues.Count == 1;
    }

    bool CheckMiddleStraight(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        
        int[][] straights = {
            new int[] {1, 2, 3, 4},  
            new int[] {2, 3, 4, 5},
            new int[] {3, 4, 5, 6}   
        };
        
        foreach (var straight in straights)
        {
            bool hasAllValues = straight.All(value => counts.ContainsKey(value));
            if (hasAllValues) return diceValues.Count == 4;
        }
        
        return false;
    }
    bool CheckLowStraight(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        
        int[][] straights = {
            new int[] {1, 2, 3},  
            new int[] {2, 3, 4},
            new int[] {3, 4, 5},
            new int[] {4, 5, 6}   
        };
        
        foreach (var straight in straights)
        {
            bool hasAllValues = straight.All(value => counts.ContainsKey(value));
            if (hasAllValues) return diceValues.Count == 3;
        }
        
        return false;
    }

    bool CheckTwoPair(List<int> diceValues)
    {
        var counts = GetValueCounts(diceValues);
        return counts.Values.Count(count => count == 2) == 2 && diceValues.Count == 4;
    }

    bool CheckPair(List<int> diceValues)
    {
        return diceValues.Count == 2 && diceValues[0] == diceValues[1];
    }

    bool CheckOne(List<int> diceValues)
    {
        return diceValues.Count == 1 && (diceValues[0] == 1 || diceValues[0] == 5);
    }
    
    Dictionary<int, int> GetValueCounts(List<int> diceValues)
    {
        var counts = new Dictionary<int, int>();
        foreach (int value in diceValues)
        {
            if (counts.ContainsKey(value))
                counts[value]++;
            else
                counts[value] = 1;
        }
        return counts;
    }

    // Calculation Methods

    int CalculateOfAKind(int diceValue, DiceCombinationRules.CombinationRule rule)
    {
        return Mathf.RoundToInt(diceValue * rule.points * rule.multiplier);
    }

    int CalculateOne(int diceValue, DiceCombinationRules.CombinationRule rule)
    {
        return Mathf.RoundToInt((diceValue == 1 ? 100 : rule.points) * rule.multiplier);
    }

    int CalculateRule(DiceCombinationRules.CombinationRule rule)
    {
        return Mathf.RoundToInt(rule.points * rule.multiplier);
    }

    int CalculateFullHouse(List<int> diceValues, DiceCombinationRules.CombinationRule rule)
    {
        var counts = GetValueCounts(diceValues);
        int threeOfKindValue = counts.First(kvp => kvp.Value == 3).Key;
        return Mathf.RoundToInt((rule.points * threeOfKindValue + 50) * rule.multiplier);
    }
    
    int CalculateScore(List<int> diceValues, DiceCombinationRules.CombinationRule rule)
    {
        switch (rule.count)
        {
            case Count.TwoSets:
                var counts = GetValueCounts(diceValues);
                var uniqueValues = counts.Keys.ToList();
                return CalculateOfAKind(uniqueValues[0], rule) + CalculateOfAKind(uniqueValues[1], rule);
            case Count.MaxStraight:
                return CalculateRule(rule);
            case Count.Straight:
                return CalculateRule(rule);
            case Count.ThreePairs:
                return CalculateRule(rule);
            case Count.FourOfKind:
                return CalculateOfAKind(diceValues[0], rule);
            case Count.FullHouse:
                return CalculateFullHouse(diceValues, rule);
            case Count.ThreeOfKind:
                return CalculateOfAKind(diceValues[0], rule);
            case Count.MiddleStraight:
                return CalculateRule(rule);
            case Count.LowStraight:
                return CalculateRule(rule);
            case Count.TwoPair:
                return CalculateRule(rule);
            case Count.Pair:
                return CalculateRule(rule);
            case Count.One:
                return CalculateOne(diceValues[0], rule);        
            default:
                return 0;
        }
    }
    

    


}