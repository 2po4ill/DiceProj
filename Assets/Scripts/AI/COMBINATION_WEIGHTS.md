# Combination Weights and Tiers

## Tier System

The AI uses a 5-tier system to classify combinations by strategic value:

| Tier | Value | Description |
|------|-------|-------------|
| Tier 1 | 100% | Highest value - uses all 6 dice |
| Tier 2 | 80% | High value - 4-5 dice |
| Tier 3 | 60% | Medium value - 3-4 dice |
| Tier 4 | 40% | Low value - 2-3 dice |
| Tier 5 | 20% | Lowest value - 1 dice |

## Complete Combination List

### Tier 1 (100% Value) - Uses All 6 Dice

| Combination | Dice Used | Points | Points/Dice | Rule |
|-------------|-----------|--------|-------------|------|
| **Six of a Kind (1s)** | 6 | 4000 | 666.7 | MaxStraight |
| **Six of a Kind (2s)** | 6 | 2000 | 333.3 | MaxStraight |
| **Six of a Kind (3s)** | 6 | 3000 | 500.0 | MaxStraight |
| **Six of a Kind (4s)** | 6 | 4000 | 666.7 | MaxStraight |
| **Six of a Kind (5s)** | 6 | 5000 | 833.3 | MaxStraight |
| **Six of a Kind (6s)** | 6 | 6000 | 1000.0 | MaxStraight |
| **Large Straight (1-6)** | 6 | 1500 | 250.0 | MaxStraight |
| **Three Pairs** | 6 | 1500 | 250.0 | ThreePairs |

**Strategy**: These clear the board → Hot Streak → New 6 dice

---

### Tier 2 (80% Value) - Uses 4-5 Dice

| Combination | Dice Used | Points | Points/Dice | Rule |
|-------------|-----------|--------|-------------|------|
| **Four of a Kind (1s)** | 4 | 2000 | 500.0 | FourOfKind |
| **Four of a Kind (2s)** | 4 | 400 | 100.0 | FourOfKind |
| **Four of a Kind (3s)** | 4 | 600 | 150.0 | FourOfKind |
| **Four of a Kind (4s)** | 4 | 800 | 200.0 | FourOfKind |
| **Four of a Kind (5s)** | 4 | 1000 | 250.0 | FourOfKind |
| **Four of a Kind (6s)** | 4 | 1200 | 300.0 | FourOfKind |
| **Middle Straight (5 consecutive)** | 5 | 1000 | 200.0 | Straight |
| **Small Straight (4 consecutive)** | 4 | 500 | 125.0 | MiddleStraight |

**Strategy**: High value, leaves 1-2 dice for reroll

---

### Tier 3 (60% Value) - Uses 3-4 Dice

| Combination | Dice Used | Points | Points/Dice | Rule |
|-------------|-----------|--------|-------------|------|
| **Three of a Kind (1s)** | 3 | 1000 | 333.3 | ThreeOfKind |
| **Three of a Kind (2s)** | 3 | 200 | 66.7 | ThreeOfKind |
| **Three of a Kind (3s)** | 3 | 300 | 100.0 | ThreeOfKind |
| **Three of a Kind (4s)** | 3 | 400 | 133.3 | ThreeOfKind |
| **Three of a Kind (5s)** | 3 | 500 | 166.7 | ThreeOfKind |
| **Three of a Kind (6s)** | 3 | 600 | 200.0 | ThreeOfKind |
| **Two Pairs** | 4 | 500 | 125.0 | TwoPair |

**Strategy**: Medium value, leaves 2-3 dice for reroll

---

### Tier 4 (40% Value) - Uses 2-3 Dice

| Combination | Dice Used | Points | Points/Dice | Rule |
|-------------|-----------|--------|-------------|------|
| **Pair of 1s** | 2 | 200 | 100.0 | Pair |
| **Pair of 2s** | 2 | 40 | 20.0 | Pair |
| **Pair of 3s** | 2 | 60 | 30.0 | Pair |
| **Pair of 4s** | 2 | 80 | 40.0 | Pair |
| **Pair of 5s** | 2 | 100 | 50.0 | Pair |
| **Pair of 6s** | 2 | 120 | 60.0 | Pair |
| **Low Straight (3 consecutive)** | 3 | 250 | 83.3 | LowStraight |

**Strategy**: Low value, leaves 3-4 dice for reroll

---

### Tier 5 (20% Value) - Uses 1 Dice

| Combination | Dice Used | Points | Points/Dice | Rule |
|-------------|-----------|--------|-------------|------|
| **Single 1** | 1 | 100 | 100.0 | One |
| **Single 5** | 1 | 50 | 50.0 | One |

**Strategy**: Minimum dice usage, leaves 5 dice for reroll

---

## Minimum Dice Strategy (Aggressive Mode)

The aggressive AI uses **minimum dice strategy**, which means:

1. **Find all possible combinations**
2. **Group by dice count** (1, 2, 3, 4, 5, 6)
3. **Select from the group with FEWEST dice**
4. **Within that group, select highest points/dice ratio**

### Example Decision Tree:

```
Dice: [1, 1, 2, 3, 4, 5]

Available combinations:
- Single 1 (1 dice, 100 pts) ← Tier 5
- Single 1 (1 dice, 100 pts) ← Tier 5
- Single 5 (1 dice, 50 pts) ← Tier 5
- Pair of 1s (2 dice, 200 pts) ← Tier 4
- Low Straight (3 dice, 250 pts) ← Tier 4

Minimum dice group: 1 dice
Options in group:
- Single 1: 100 pts/dice
- Single 1: 100 pts/dice
- Single 5: 50 pts/dice

Selection: Single 1 (100 pts, 1 dice)
Remaining: 5 dice
```

## Why Aggressive Mode Rarely Clears Board on Iteration 1

### Problem: Minimum Dice Strategy Conflicts with Hot Streak Goal

**Minimum dice strategy** = Use fewest dice possible
**Hot streak goal** = Use all 6 dice

These are **opposite strategies**!

### Example Scenario:

```
Dice: [1, 1, 1, 5, 5, 6]

Option A (Minimum Dice):
- Take single 1 (1 dice, 100 pts)
- Remaining: 5 dice
- No hot streak

Option B (Hot Streak):
- Take three 1s (3 dice, 1000 pts)
- Take two 5s (2 dice, 100 pts)
- Take... nothing (6 is worthless)
- Remaining: 1 dice
- No hot streak

Option C (Force Hot Streak):
- Take three 1s (3 dice, 1000 pts)
- Take pair of 5s (2 dice, 100 pts)
- Take single 6... WAIT, 6 has no value!
- CANNOT clear board
```

### When Hot Streaks Happen:

Hot streaks only occur when dice allow Tier 1 combinations:

1. **Six of a Kind** - All 6 dice same value
2. **Large Straight** - 1,2,3,4,5,6
3. **Three Pairs** - Three different pairs

**Probability**: Very low (~2-5% per roll)

## Solution: Aggressive Mode Needs Hot Streak Bias

To make aggressive AI try to clear the board, we need to:

### Option 1: Detect Hot Streak Potential
```csharp
// If we can clear all dice, prefer that over minimum dice
if (CanClearAllDice(diceValues))
{
    return SelectCombinationsThatClearBoard(diceValues);
}
else
{
    return SelectMinimumDiceCombination(diceValues);
}
```

### Option 2: Weighted Selection
```csharp
// Give bonus weight to combinations that leave fewer dice
float hotStreakBonus = (6 - remainingDice) * 0.2f;
strategicValue = (points / diceUsed) * (1 + hotStreakBonus);
```

### Option 3: Multi-Selection Strategy
```csharp
// Instead of selecting one combination, select multiple to clear board
List<CombinationResult> selections = SelectCombinationsToMaximizeDiceUsage(diceValues);
// Example: Take three 1s + pair of 5s + single 6 (if possible)
```

## Current Behavior

**Iteration 1**: AI uses minimum dice strategy
- Takes 1-2 dice per selection
- Rarely clears all 6 dice
- Almost never gets hot streak on iteration 1
- Usually stops after 3-5 selections within iteration 1

**Result**: AI rarely reaches iteration 2+ because it doesn't clear the board.

## Recommendation

Add a **hot streak bias** to aggressive mode that:
1. Detects when all dice can be cleared
2. Selects combinations that maximize dice usage
3. Prioritizes Tier 1 combinations (6 dice)
4. Falls back to minimum dice if hot streak impossible
