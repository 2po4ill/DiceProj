# Combination Selection Flow Analysis

## The Problem
After 2nd hot streak, AI selects "Single One" from `[2, 5, 6, 1, 4, 4]` instead of better combinations.

## Current Flow

### 1. ExecuteFullDiceSetIteration (AITurnExecutor.cs, line ~260)
```
Loop: while (isTurnActive && GetRemainingDiceCount() > 0)
  → ExecuteSingleCombinationStep()
  
If GetRemainingDiceCount() == 0 (hot streak):
  → Generate fresh 6 dice
  → currentTurnState.CurrentDice = diceGenerator.GenerateRandomDice(6)
  → Loop continues (goes back to ExecuteSingleCombinationStep)
```

### 2. ExecuteSingleCombinationStep (AITurnExecutor.cs, line ~385)
```
Step 3: SelectBestCombination()
  → Calls combinationStrategy.SelectOptimalCombination(
       combinations,
       currentTurnState.CurrentMode,
       GetRemainingDiceCount(),  ← THIS IS THE KEY
       currentTurnState.IterationCount
     )
```

### 3. GetRemainingDiceCount (AITurnExecutor.cs, line ~642)
```csharp
int GetRemainingDiceCount()
{
    return currentTurnState.CurrentDice.Count;
}
```

### 4. SelectOptimalCombination (AICombinationStrategy.cs, line ~820)
```csharp
// Parameter: int remainingDice
if (mode == BehaviorMode.AGGRESSIVE && remainingDice <= 3)
{
    // MINIMAL RISK MODE
    bestStrategy = SelectMinimumDiceStrategy(strategies);
}
else
{
    // MAXIMIZE POINTS MODE
    bestStrategy = SelectBestCombination(strategies, mode);
}
```

## The Bug Hunt

### Hypothesis 1: CurrentDice.Count is wrong
After hot streak, fresh 6 dice are generated:
```csharp
currentTurnState.CurrentDice = diceGenerator.GenerateRandomDice(6);
```
So `GetRemainingDiceCount()` should return 6.

### Hypothesis 2: Mode is changing to PASSIVE
Check if `currentTurnState.CurrentMode` is being changed somewhere.

### Hypothesis 3: The condition is inverted
Current condition:
```csharp
if (mode == BehaviorMode.AGGRESSIVE && remainingDice <= 3)
```

This should ONLY trigger with 1-3 dice in AGGRESSIVE mode.
With 6 dice, it should go to the else branch (MAXIMIZE POINTS).

### Hypothesis 4: SelectBestCombination is broken
Even in MAXIMIZE POINTS mode, `SelectBestCombination()` might be selecting single dice.

## Investigation Steps

1. **Add temporary debug at SelectOptimalCombination entry:**
   ```csharp
   Debug.Log($"[COMBO] remainingDice={remainingDice}, mode={mode}, condition={mode == BehaviorMode.AGGRESSIVE && remainingDice <= 3}");
   ```

2. **Check what SelectBestCombination returns:**
   ```csharp
   bestStrategy = SelectBestCombination(strategies, mode);
   Debug.Log($"[COMBO] SelectBestCombination returned: {bestStrategy.combination.rule}, dice used: {bestStrategy.diceUsed}");
   ```

3. **Check if ApplyDiceCountWeighting is working:**
   ```csharp
   if (mode == BehaviorMode.AGGRESSIVE && remainingDice > 3)
   {
       Debug.Log($"[COMBO] Applying hot streak weighting");
       ApplyDiceCountWeighting(strategies, remainingDice, mode);
       // Log strategic values after weighting
       foreach (var s in strategies)
           Debug.Log($"  {s.combination.rule}: value={s.strategicValue}, diceUsed={s.diceUsed}");
   }
   ```

## Likely Root Cause

Looking at the log pattern, I suspect **SelectBestCombination** is the issue.

In SelectByStrategy (line ~410):
```csharp
case BehaviorMode.AGGRESSIVE:
    return combinations
        .OrderByDescending(c => c.strategicValue)
        .ThenBy(c => (int)c.tier)
        .ThenByDescending(c => c.combination.points)
        .First();
```

The problem: **Single dice have high strategic value** (100 points / 1 dice = 100 points per dice).

For `[2, 5, 6, 1, 4, 4]`:
- Single 1: 100 pts / 1 dice = **100 strategic value**
- Single 5: 50 pts / 1 dice = **50 strategic value**
- Pair of 4s: 80 pts / 2 dice = **40 strategic value**

So even WITHOUT hot streak weighting, single dice win!

## The Fix

The hot streak weighting (10x multiplier) should make combinations that use ALL dice win:
- Pair of 4s (uses 2/6): value = 40 * (1 + 0.1) = 44
- If there was a combo using all 6: value = X * 10 = huge

But we're not getting combinations that use all 6 dice from `[2, 5, 6, 1, 4, 4]`.

**The real issue:** There's no combination that uses all 6 dice in `[2, 5, 6, 1, 4, 4]`, so the weighting doesn't help. The AI should still pick the BEST available combination (Pair of 4s), not single dice.

## Solution

Change the priority in AGGRESSIVE mode to:
1. Hot streak potential (uses all dice) - 10x multiplier
2. **Highest points** (not strategic value)
3. Highest tier

```csharp
case BehaviorMode.AGGRESSIVE:
    return combinations
        .OrderByDescending(c => c.diceUsed == remainingDice ? 1000000 : 0) // Hot streak priority
        .ThenByDescending(c => c.combination.points) // Then highest points
        .ThenBy(c => (int)c.tier) // Then best tier
        .First();
```

This way:
- If a combo uses all dice → it wins (hot streak)
- Otherwise → highest points wins (Pair of 4s = 80 > Single 1 = 100... wait, that's wrong)

Actually, Pair of 4s is only 80 points, Single 1 is 100 points. So by points, Single 1 IS better!

The real fix: **Don't allow single dice selection when 4+ dice are available**, OR boost multi-dice combinations.
