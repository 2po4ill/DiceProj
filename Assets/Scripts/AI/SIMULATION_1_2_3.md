# Aggressive AI Simulation: Dice [1,2,3]

## Initial State
```
Dice: [1, 2, 3]
Total Dice Count: 3
Iteration: 1 (assuming first iteration)
Mode: AGGRESSIVE
```

## Step 1: Find All Combinations

### Available Combinations:

#### Option A: Single 1
- **Rule**: One
- **Dice Used**: 1
- **Points**: 100
- **Base Strategic Value**: 100 / 1 = 100.0

#### Option B: Low Straight (1,2,3)
- **Rule**: LowStraight
- **Dice Used**: 3
- **Points**: 250
- **Base Strategic Value**: 250 / 3 = 83.3

## Step 2: Apply Dynamic Weighting

### Option A: Single 1
```
Base Value: 100.0
Dice Used: 1 / 3 = 0.33 (33% of dice)

Progressive Bonus:
bonus = 1.0 + (0.33 × 0.3) = 1.0 + 0.1 = 1.1 (10% bonus)

Weighted Value: 100.0 × 1.1 = 110.0
```

### Option B: Low Straight
```
Base Value: 83.3
Dice Used: 3 / 3 = 1.0 (100% of dice) ← CLEARS ALL DICE!

Hot Streak Bonus:
multiplier = 10.0 (uses all dice!)

Weighted Value: 83.3 × 10.0 = 833.0
```

## Step 3: Apply Minimum Dice Algorithm

### Hot Streak Detection
```csharp
// Check for combinations that use all dice
var hotStreakCombos = combinations.Where(c => c.diceUsed == 3).ToList();
// Found: Low Straight (3 dice)

if (hotStreakCombos.Count > 0)
{
    // Select best hot streak combination
    var bestHotStreak = hotStreakCombos.OrderByDescending(c => c.combination.points).First();
    // Returns: Low Straight (250 points)
    
    Debug.Log("HOT STREAK DETECTED: LowStraight clears all 3 dice for 250 points!");
    return bestHotStreak;
}
```

## Step 4: Comparison

| Combination | Base Value | Weighted Value | Selected? |
|-------------|------------|----------------|-----------|
| Single 1 | 100.0 | 110.0 | ❌ |
| Low Straight | 83.3 | **833.0** | ✅ |

**Winner**: Low Straight (10x multiplier for hot streak)

## Step 5: AI Decision

```
Selected Combination: Low Straight
Dice Indices Used: [0, 1, 2] (all three dice)
Points Gained: 250
Remaining Dice: 0

Result: HOT STREAK! 🔥
```

## Step 6: What Happens Next

```csharp
// In AIAggressiveRerollStrategy.cs

if (selection.RemainingDice == 0)
{
    selection.IsHotStreak = true;
    currentRerollState.TotalIterations++; // Increment from 1 to 2
    
    Debug.Log("Hot streak! All dice used - starting iteration 2");
    
    // Check iteration limit
    if (currentRerollState.TotalIterations > 5)
    {
        // Stop at 5 hot streaks
        break;
    }
    
    // Generate new 6 dice for iteration 2
    currentDice = GenerateNewDiceSet(6);
    result.HotStreakCount++;
}
```

## Final Result

```
✅ AI selects: Low Straight (1,2,3)
✅ Points scored: 250
✅ Dice cleared: 3/3 (100%)
✅ Hot streak achieved!
✅ New dice: 6 fresh dice for iteration 2
✅ Turn continues with iteration 2
```

## Why This Is Correct

1. **Hot Streak Bonus**: 10x multiplier (833.0) beats any other option (110.0)
2. **Competitive Play**: AI maximizes opportunities by clearing board
3. **Iteration Advancement**: Gets to iteration 2 with fresh 6 dice
4. **Point Efficiency**: 250 points + continuation > 100 points + risky 2 dice

## Alternative Scenario (Without Hot Streak Bonus)

If we didn't have the hot streak bonus:

```
Single 1: 110.0 value
Low Straight: 83.3 value (no bonus)

Selection: Single 1 ❌
Remaining: 2 dice [2,3]
Result: No hot streak, risky position
```

This would be suboptimal because:
- Only 100 points gained
- 2 dice remaining (high zonk risk)
- No iteration advancement
- Likely to stop or zonk

## Conclusion

With dice [1,2,3], the aggressive AI will:
- **Detect** the Low Straight uses all 3 dice
- **Apply** 10x hot streak multiplier (833.0 value)
- **Select** Low Straight over Single 1
- **Clear** all dice for hot streak
- **Continue** to iteration 2 with 6 new dice
- **Compete** effectively by maximizing opportunities

This is exactly the behavior we want for aggressive mode!
