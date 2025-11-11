# Hot Streak Priority - Dynamic Weighting System

## Changes Made

Added dynamic weighting to the minimum dice algorithm so aggressive AI prioritizes clearing the board when possible.

## New Behavior

### 1. Hot Streak Detection (Priority #1)

**When**: A combination uses all remaining dice (6 dice initially, or all remaining after selections)

**Effect**: 10x multiplier to strategic value

**Example**:
```
Dice: [1,1,1,1,1,1]

Available:
- Single 1: 100 pts, 1 dice, value = 100
- Six of a Kind: 4000 pts, 6 dice, value = 666.7

After hot streak bonus:
- Single 1: value = 100
- Six of a Kind: value = 6667 (10x multiplier!)

Selection: Six of a Kind ✓ HOT STREAK!
```

### 2. Low Dice Bonus

**When**: 3 or fewer dice remaining

**Effect**: Up to 50% bonus based on dice usage ratio

**Formula**: `bonus = 1.0 + (diceUsed / totalDice × 0.5)`

**Example**:
```
Dice: [1,5,6] (3 dice remaining)

Available:
- Single 1: 100 pts, 1 dice, ratio = 1/3 = 0.33
  Bonus: 1.0 + (0.33 × 0.5) = 1.17 (17% boost)
  Value: 100 × 1.17 = 117

- Pair of... wait, no pairs possible
- Low Straight: 250 pts, 3 dice, ratio = 3/3 = 1.0
  Bonus: 1.0 + (1.0 × 0.5) = 1.5 (50% boost)
  Value: 83.3 × 1.5 = 125

If low straight exists: Selection = Low Straight (clears all 3 dice)
```

### 3. Normal Minimum Dice (Default)

**When**: More than 3 dice remaining and no hot streak possible

**Effect**: Standard minimum dice algorithm (use fewest dice)

**Example**:
```
Dice: [1,2,3,4,5,6] (6 dice, no Tier 1 combo)

Available:
- Single 1: 100 pts, 1 dice
- Single 5: 50 pts, 1 dice
- Low Straight: 250 pts, 3 dice

Selection: Single 1 (minimum dice, highest points)
```

## Decision Priority

```
1. HOT STREAK (uses all dice)
   └─ 10x multiplier
   └─ Always selected if available
   
2. LOW DICE BONUS (≤3 dice remaining)
   └─ Up to 50% bonus
   └─ Favors using more dice
   
3. MINIMUM DICE (default)
   └─ Standard algorithm
   └─ Uses fewest dice possible
```

## Impact on Gameplay

### Before Changes:
```
Dice: [2,2,4,4,6,6]
Available: Three Pairs (1500 pts, 6 dice)
Selection: Pair of 6s (120 pts, 2 dice) ← Minimum dice
Result: No hot streak, 4 dice remaining
```

### After Changes:
```
Dice: [2,2,4,4,6,6]
Available: Three Pairs (1500 pts, 6 dice)
Hot Streak Bonus: 250 × 10 = 2500 value
Selection: Three Pairs (1500 pts, 6 dice) ← HOT STREAK!
Result: All dice cleared, new 6 dice, iteration 2!
```

## Hot Streak Scenarios

### Scenario 1: Six of a Kind
```
Dice: [3,3,3,3,3,3]
Combination: Six 3s (3000 pts, 6 dice)
Bonus: 10x multiplier
Result: HOT STREAK → Iteration 2
```

### Scenario 2: Large Straight
```
Dice: [1,2,3,4,5,6]
Combination: Large Straight (1500 pts, 6 dice)
Bonus: 10x multiplier
Result: HOT STREAK → Iteration 2
```

### Scenario 3: Three Pairs
```
Dice: [1,1,5,5,6,6]
Combination: Three Pairs (1500 pts, 6 dice)
Bonus: 10x multiplier
Result: HOT STREAK → Iteration 2
```

### Scenario 4: Partial Clear (3 dice remaining)
```
Dice: [1,2,3] (after previous selections)
Combination: Low Straight (250 pts, 3 dice)
Bonus: 10x multiplier (uses all remaining!)
Result: HOT STREAK → Iteration 2
```

## Why 10x Multiplier?

The 10x multiplier ensures hot streaks are ALWAYS selected when available:

```
Worst case comparison:
- Single 1: 100 pts/dice = 100 value
- Three Pairs: 250 pts/dice = 250 value
- With 10x: 250 × 10 = 2500 value

Even the worst Tier 1 combo (250 pts/dice) becomes 2500 value,
which beats any Tier 5 combo (100 pts/dice max).
```

## Configuration

The multipliers can be adjusted:

```csharp
// In ApplyDiceCountWeighting()

// Hot streak multiplier (currently 10x)
combo.strategicValue = baseValue * 10.0f;

// Low dice bonus (currently up to 50%)
float bonus = 1.0f + (diceUsageRatio * 0.5f);
```

## Expected Behavior Changes

### Iteration 1 (6 dice):
- **Before**: Almost never clears board (uses 1-2 dice)
- **After**: Clears board when Tier 1 combo available (~5% of rolls)

### Iteration 2+ (after hot streak):
- **Before**: Rarely reached
- **After**: Reached ~5% of the time on iteration 1, then continues

### Low Dice Situations (1-3 dice):
- **Before**: Always uses minimum (1 dice)
- **After**: Favors using all remaining dice (50% bonus)

## Testing

To verify hot streak detection works:

```csharp
// Test 1: Six of a Kind
var dice = new List<int> { 4, 4, 4, 4, 4, 4 };
var result = FindMinimumDiceCombination(dice, BehaviorMode.AGGRESSIVE);
// Expected: Six 4s (4000 pts, 6 dice)

// Test 2: Large Straight
var dice = new List<int> { 1, 2, 3, 4, 5, 6 };
var result = FindMinimumDiceCombination(dice, BehaviorMode.AGGRESSIVE);
// Expected: Large Straight (1500 pts, 6 dice)

// Test 3: Three Pairs
var dice = new List<int> { 2, 2, 5, 5, 6, 6 };
var result = FindMinimumDiceCombination(dice, BehaviorMode.AGGRESSIVE);
// Expected: Three Pairs (1500 pts, 6 dice)

// Test 4: No Hot Streak Available
var dice = new List<int> { 1, 2, 3, 4, 5, 6 }; // Wait, this IS a straight!
var dice = new List<int> { 1, 2, 2, 3, 4, 6 }; // No Tier 1 combo
var result = FindMinimumDiceCombination(dice, BehaviorMode.AGGRESSIVE);
// Expected: Single 1 (100 pts, 1 dice) - minimum dice
```

## Summary

✓ Hot streaks now prioritized with 10x multiplier
✓ Low dice situations favor using more dice (up to 50% bonus)
✓ Normal situations still use minimum dice strategy
✓ Aggressive AI will now reach iteration 2+ more often
✓ Makes aggressive mode more dynamic and exciting
