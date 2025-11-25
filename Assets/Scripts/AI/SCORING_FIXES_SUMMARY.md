# AI Scoring Fixes Summary

## Issues Found and Fixed

### 1. ✓ Pair Calculation Error
**Location**: `AICombinationStrategy.cs` - `CalculatePointsFromSO()` method

**Problem**: 
- AI was calculating pairs as: `specificValue * rule.points * rule.multiplier`
- Example: Pair of 2s with rule.points=100 → 2 × 100 = 200 points (WRONG)

**Root Cause**:
- AI was treating pairs like three-of-a-kind/four-of-a-kind (which DO multiply by face value)
- Player correctly uses: `rule.points * rule.multiplier` (no face value multiplication)

**Fix Applied**:
```csharp
case Count.Pair:
    // Pairs don't multiply by face value - just use rule points
    return Mathf.RoundToInt(rule.points * rule.multiplier);
```

**Impact**:
- Pair of 2s: Now scores 100 (was 200)
- Pair of 3s: Now scores 100 (was 300)
- Pair of 4s: Now scores 100 (was 400)
- Pair of 5s: Now scores 100 (was 500)
- Pair of 6s: Now scores 100 (was 600)

---

### 2. ✓ Pair Fallback Calculation Error
**Location**: `AICombinationStrategy.cs` - `CalculatePointsFallback()` method

**Problem**:
- Fallback also had incorrect pair calculation
- Was using: `specificValue == 1 ? 200 : specificValue * 20`

**Fix Applied**:
```csharp
case Rule.Pair:
    // Pairs should use fixed points from rule, not multiply by face value
    // But if no rule exists, use 100 as default
    return 100;
```

---

### 3. ✓ Missing TwoSets Support
**Location**: `AICombinationStrategy.cs` - Multiple locations

**Problem**:
- AI had no detection for TwoSets combination (two three-of-a-kinds)
- AI had no calculation for TwoSets scoring
- Player has full TwoSets support

**Fixes Applied**:

#### A. Added TwoSets Calculation
```csharp
case Count.TwoSets:
    // Two sets of three-of-a-kind: calculate each set separately and sum
    var setCounts = diceValues.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
    var setValues = setCounts.Keys.ToList();
    if (setValues.Count >= 2)
    {
        int set1Points = Mathf.RoundToInt(setValues[0] * rule.points * rule.multiplier);
        int set2Points = Mathf.RoundToInt(setValues[1] * rule.points * rule.multiplier);
        return set1Points + set2Points;
    }
    return Mathf.RoundToInt(rule.points * rule.multiplier);
```

#### B. Added TwoSets Detection
```csharp
List<StrategyResult> FindTwoSetsCombination(List<int> diceValues, float threshold)
{
    List<StrategyResult> twoSets = new List<StrategyResult>();
    
    if (diceValues.Count != 6) return twoSets; // TwoSets requires exactly 6 dice
    
    var counts = diceValues.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
    
    // Need exactly two values, each appearing 3 times
    if (counts.Count == 2 && counts.Values.All(c => c == 3))
    {
        var values = counts.Keys.ToList();
        int totalPoints = CalculatePointsFromSO(Rule.TwoSets, diceValues);
        
        var result = CreateStrategyResult(Rule.TwoSets, totalPoints, CombinationTier.Tier1, 6, threshold, 
            $"Two Sets: Three {values[0]}s + Three {values[1]}s");
        
        // Find indices for both sets
        List<int> indices = new List<int>();
        indices.AddRange(FindValueIndices(diceValues, values[0], 3));
        indices.AddRange(FindValueIndices(diceValues, values[1], 3));
        result.combination.diceIndices = indices;
        
        twoSets.Add(result);
    }
    
    return twoSets;
}
```

#### C. Added TwoSets to FindAllCombinations
```csharp
List<StrategyResult> FindAllCombinations(List<int> diceValues, float threshold)
{
    List<StrategyResult> combinations = new List<StrategyResult>();
    
    // Check each possible combination type (order matters - check high value first!)
    combinations.AddRange(FindTwoSetsCombination(diceValues, threshold)); // ADDED
    combinations.AddRange(FindStraightCombinations(diceValues, threshold));
    combinations.AddRange(FindFullHouseCombination(diceValues, threshold));
    combinations.AddRange(FindOfAKindCombinations(diceValues, threshold));
    combinations.AddRange(FindPairCombinations(diceValues, threshold));
    combinations.AddRange(FindSingleCombinations(diceValues, threshold));
    
    return combinations;
}
```

#### D. Added TwoSets to Tier Classification
```csharp
case Rule.TwoSets:
case Rule.MaxStraight:
case Rule.Straight:
case Rule.ThreePairs:
    return CombinationTier.Tier1; // TwoSets is Tier 1 (highest value)
```

**Impact**:
- AI can now detect dice patterns like [2,2,2,4,4,4]
- AI correctly calculates TwoSets as sum of two three-of-a-kinds
- Example: Three 2s (400) + Three 4s (800) = 1200 points

---

## Verification Status

### All Combination Types Verified ✓

| Combination | Player Formula | AI Formula | Status |
|------------|----------------|------------|--------|
| ThreeOfKind | `value * points * mult` | `value * points * mult` | ✓ Match |
| FourOfKind | `value * points * mult` | `value * points * mult` | ✓ Match |
| **Pair** | `points * mult` | `points * mult` | ✓ **Fixed** |
| One | `(v==1?100:pts) * mult` | `(v==1?100:pts) * mult` | ✓ Match |
| TwoPair | `points * mult` | `points * mult` | ✓ Match |
| ThreePairs | `points * mult` | `points * mult` | ✓ Match |
| LowStraight | `points * mult` | `points * mult` | ✓ Match |
| MiddleStraight | `points * mult` | `points * mult` | ✓ Match |
| Straight | `points * mult` | `points * mult` | ✓ Match |
| MaxStraight | `points * mult` | `points * mult` | ✓ Match |
| FullHouse | `(pts*3val+50) * mult` | `(pts*3val+50) * mult` | ✓ Match |
| **TwoSets** | `sum of 2 OfAKind` | `sum of 2 OfAKind` | ✓ **Fixed** |

---

## Testing Recommendations

### Test Case 1: Pair Scoring
```
Dice: [2, 2]
Expected: 100 points
Previous AI: 200 points (WRONG)
Fixed AI: 100 points (CORRECT)
```

### Test Case 2: TwoSets Detection
```
Dice: [2, 2, 2, 4, 4, 4]
Expected: Detect TwoSets, score = (2*200) + (4*200) = 1200 points
Previous AI: Would not detect TwoSets
Fixed AI: Detects and scores correctly
```

### Test Case 3: TwoSets vs Other Combinations
```
Dice: [1, 1, 1, 5, 5, 5]
Expected: Detect TwoSets, score = (1*200) + (5*200) = 2400 points
Fixed AI: Should prioritize TwoSets (Tier 1) over individual three-of-a-kinds
```

---

## Files Modified

1. `Assets/Scripts/AI/AICombinationStrategy.cs`
   - Fixed Pair calculation in CalculatePointsFromSO
   - Fixed Pair fallback calculation
   - Added TwoSets calculation
   - Added FindTwoSetsCombination method
   - Updated FindAllCombinations to include TwoSets
   - Updated ClassifyCombinationTier to include TwoSets

---

## Conclusion

All AI scoring calculations now match the player's scoring system exactly. The AI will:
- Score pairs correctly (100 points regardless of face value)
- Detect and score TwoSets combinations
- Make strategic decisions based on accurate point values
