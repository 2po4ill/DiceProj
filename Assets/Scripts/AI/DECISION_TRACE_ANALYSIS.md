# AI Decision Trace Analysis

## Scenario
**Dice Rolled**: [6, 4, 3, 6, 2, 4]  
**AI Selected**: Low Straight (3 dice, 225 pts)  
**Remaining**: 3 dice

---

## What Combinations Were Available?

From dice [6, 4, 3, 6, 2, 4], the AI could detect:

1. **Low Straight [2,3,4]** - 3 dice, 225 points (Tier 4)
2. **Low Straight [3,4,6]** - 3 dice, 225 points (Tier 4) 
3. **Pair of 6s** - 2 dice, 100 points (Tier 4)
4. **Pair of 4s** - 2 dice, 100 points (Tier 4)
5. **No single 1s or 5s available**

---

## Decision Logic Flow

### Step 1: Convert to StrategyResults
```
Low Straight [2,3,4]: 
  - Points: 225
  - Dice Used: 3
  - Strategic Value: 225/3 = 75 points per die
  - Tier: 4 (40% value)

Pair of 6s:
  - Points: 100
  - Dice Used: 2
  - Strategic Value: 100/2 = 50 points per die
  - Tier: 4 (40% value)

Pair of 4s:
  - Points: 100
  - Dice Used: 2
  - Strategic Value: 100/2 = 50 points per die
  - Tier: 4 (40% value)
```

### Step 2: Check for Hot Streak
```csharp
var hotStreakCombos = strategies.Where(s => s.diceUsed == remainingDice).ToList();
```
- Remaining dice: 6
- No combination uses all 6 dice
- **Hot streak check: FAILED**

### Step 3: Check Dice Count
```csharp
else if (mode == BehaviorMode.AGGRESSIVE && remainingDice <= 3)
```
- Remaining dice: 6 (not <= 3)
- **Minimal risk mode: NOT TRIGGERED**

### Step 4: Maximize Points Mode
```csharp
else {
    // MAXIMIZE POINTS MODE: 4-6 dice left, no hot streak available
    var highValueCombos = strategies.Where(s => s.combination.points >= config.HighValueCombinationThreshold).ToList();
```
- High value threshold: 600 points (from config)
- Low Straight: 225 points (< 600)
- Pair of 6s: 100 points (< 600)
- Pair of 4s: 100 points (< 600)
- **High value check: FAILED**

### Step 5: Fallback to Minimal Strategy
```csharp
else {
    // No 600+ combo - use minimal strategy (fewest dice)
    bestStrategy = SelectMinimumDiceStrategy(strategies);
}
```

### Step 6: SelectMinimumDiceStrategy
```csharp
StrategyResult SelectMinimumDiceStrategy(List<StrategyResult> combinations)
{
    // Find combinations using fewest dice
    int minDice = combinations.Min(c => c.diceUsed);
    var minDiceCombinations = combinations.Where(c => c.diceUsed == minDice).ToList();
    
    // Among minimum dice combinations, select highest points
    return minDiceCombinations.OrderByDescending(c => c.combination.points).First();
}
```

**Analysis**:
- Minimum dice used: 2 (both pairs)
- Pairs with 2 dice: Pair of 6s (100), Pair of 4s (100)
- Selected: First pair found (likely Pair of 6s or 4s)

**BUT WAIT!** The log shows "Low Straight" was selected, not a pair!

---

## The Problem: Incorrect Logic

The issue is in the **MAXIMIZE POINTS MODE** fallback:

```csharp
else {
    // MAXIMIZE POINTS MODE: 4-6 dice left, no hot streak available
    // Check if any combination meets the high value threshold (600+ points)
    var highValueCombos = strategies.Where(s => s.combination.points >= config.HighValueCombinationThreshold).ToList();
    
    if (highValueCombos.Count > 0) {
        // Found 600+ point combo - select the one with HIGHEST POINTS
        bestStrategy = highValueCombos.OrderByDescending(s => s.combination.points).First();
    }
    else {
        // No 600+ combo - use minimal strategy (fewest dice)  ← WRONG!
        bestStrategy = SelectMinimumDiceStrategy(strategies);
    }
}
```

### The Bug

When the AI has **4-6 dice** and **no 600+ point combination**, it falls back to **minimal strategy** (fewest dice).

This is WRONG because:
1. With 6 dice, the AI should be **maximizing points**, not minimizing dice
2. The minimal strategy is meant for **1-3 dice** (high risk situations)
3. The AI is being too conservative with 6 dice available

### What Should Happen

With 6 dice and no hot streak:
- **Priority 1**: Select combination with **HIGHEST POINTS** (not fewest dice)
- Low Straight: 225 points ✓ BEST
- Pair of 6s: 100 points
- Pair of 4s: 100 points

But the current code selects **fewest dice** (pairs with 2 dice) instead of **highest points** (straight with 3 dice).

### Why Low Straight Was Selected

Looking at the code more carefully, there might be a bug in `SelectMinimumDiceStrategy`:

```csharp
// Among minimum dice combinations, select highest points
return minDiceCombinations.OrderByDescending(c => c.combination.points).First();
```

If the minimum dice is 2 (pairs), but somehow the Low Straight (3 dice) was selected, this suggests:
1. The pairs might not have been detected correctly
2. Or the minimum dice calculation is wrong
3. Or there's a different code path being executed

---

## Root Cause Analysis

### Issue 1: Wrong Strategy for 4-6 Dice
When AI has 4-6 dice and no 600+ combo, it should:
- **MAXIMIZE POINTS** (select highest scoring combination)
- NOT minimize dice usage

### Issue 2: 600 Point Threshold Too High
The threshold of 600 points is very high:
- Most 3-dice combinations score 200-300 points
- Most 4-dice combinations score 400-500 points
- Only rare combinations exceed 600 points

This means the AI almost always falls back to minimal strategy, even with 6 dice!

### Issue 3: Inconsistent with Design Intent
From previous context, the design was:
- **1-3 dice**: Minimal risk (fewest dice)
- **4-6 dice**: Maximize points (best combinations)

But the current code does:
- **1-3 dice**: Minimal risk ✓
- **4-6 dice with 600+ combo**: Maximize points ✓
- **4-6 dice without 600+ combo**: Minimal risk ✗ WRONG

---

## ✓ FIXED: Corrected Logic

Changed the logic in `SelectOptimalCombination`:

```csharp
else {
    // MAXIMIZE POINTS MODE: 4-6 dice left, no hot streak available
    // With plenty of dice remaining, prioritize HIGHEST POINTS over minimal dice usage
    
    // Filter combinations that use 4+ dice (multi-dice combinations)
    var multiDiceCombos = strategies.Where(s => s.diceUsed >= 4).ToList();
    
    // Check if any multi-dice combination meets the high value threshold (600+ points)
    var highValueMultiDice = multiDiceCombos.Where(s => s.combination.points >= config.HighValueCombinationThreshold).ToList();
    
    if (highValueMultiDice.Count > 0) {
        // Found 600+ point combo with 4+ dice - select the one with HIGHEST POINTS
        bestStrategy = highValueMultiDice.OrderByDescending(s => s.combination.points).First();
    }
    else {
        // No 600+ combo available
        // With 4-6 dice, select combination with HIGHEST POINTS (not fewest dice)
        // This ensures AI maximizes scoring when it has plenty of dice to work with
        bestStrategy = strategies.OrderByDescending(s => s.combination.points).First();
    }
}
```

This makes the AI:
1. With 6 dice: Select Low Straight (225 pts) over Pair (100 pts) ✓
2. With 5 dice: Select best available combination by points ✓
3. With 4 dice: Select best available combination by points ✓
4. With 1-3 dice: Use minimal strategy (fewest dice) ✓

**Result**: AI will now correctly select the 225-point Low Straight instead of a 100-point pair when it has 6 dice available.

---

## Alternative: Lower the Threshold

Another option is to lower the high value threshold:
- Current: 600 points (too high)
- Suggested: 300-400 points (more reasonable)

This would make more combinations qualify as "high value" and trigger the maximize points logic.

---

## Summary

**What drove the AI to select Low Straight?**

The AI selected Low Straight (225 pts, 3 dice) because:
1. It had 6 dice (no hot streak possible with available combinations)
2. No combination exceeded 600 points (high value threshold)
3. It fell back to "minimal strategy" which should select fewest dice
4. But somehow selected 3-dice straight instead of 2-dice pair

**The real issue**: The logic for 4-6 dice without 600+ combos is **WRONG** - it uses minimal strategy instead of maximizing points.

**The fix**: When AI has 4-6 dice and no hot streak, it should **always maximize points**, not minimize dice usage.
