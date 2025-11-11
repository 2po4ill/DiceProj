# Aggressive AI Logic Review (Developer Perspective)

## Architecture Overview

The aggressive AI uses a **simulation-then-playback** architecture:

1. **Simulation Phase**: `AIAggressiveRerollStrategy.ExecuteAggressiveReroll()` simulates the entire turn
2. **Playback Phase**: `AITurnExecutor.VisualizeAggressiveIterations()` replays the decisions visually

## Current Flow

### Phase 1: Simulation (AIAggressiveRerollStrategy)
```
ExecuteAggressiveReroll(initialDice=[2,2,3,1,5,6])
├─ while (ShouldContinueRerolling)
│  ├─ ExecuteRerollIteration(currentDice)
│  │  ├─ SelectMinimumDiceCombination(currentDice)
│  │  │  └─ Returns: CombinationResult + diceIndices
│  │  ├─ MakeContinueDecision()
│  │  └─ Returns: RerollIteration with DiceIndicesUsed
│  ├─ ProcessRerollIteration()
│  └─ currentDice = GenerateNewDiceSet(remainingCount)  ← Simulates reroll
└─ Returns: AggressiveRerollResult with all iterations
```

### Phase 2: Playback (AITurnExecutor)
```
VisualizeAggressiveIterations(result)
├─ for each iteration in result.Iterations
│  ├─ SpawnAIDice(iteration.InitialDice)  ← Show dice
│  ├─ Wait for player to see
│  ├─ RemoveAIDice(iteration.DiceIndicesUsed)  ← Remove selected
│  └─ Wait before next iteration
└─ Complete
```

## CRITICAL ISSUES IDENTIFIED

### Issue #1: Dice State Mismatch Between Iterations ⚠️

**Location**: `VisualizeAggressiveIterations()` line 1073

**Problem**: Each iteration spawns NEW dice from `iteration.InitialDice`, but these are the simulated dice values, not the actual visual dice that remain after removal.

**Example**:
```
Iteration 1:
- InitialDice: [2,2,3,1,5,6] (from game)
- Removes index 3 (value 1)
- Visual remaining: [2,2,3,5,6]

Iteration 2:
- InitialDice: [4,1,2,6,3] (from simulation's GenerateNewDiceSet)
- SpawnAIDice([4,1,2,6,3])  ← DESTROYS visual [2,2,3,5,6]!
- Now showing completely different dice!
```

**Root Cause**: 
- Simulation generates random dice for next iteration
- Playback spawns those simulated dice
- Visual dice don't match what player would see if they rerolled

### Issue #2: No Actual Reroll Happening 🎲

**Location**: `VisualizeAggressiveIterations()` - missing reroll step

**Problem**: After removing dice, the visualization doesn't reroll the remaining dice. It just waits, then spawns the next iteration's simulated dice.

**What Should Happen**:
```
1. Show initial dice: [2,2,3,1,5,6]
2. Remove selected: [2,2,3,5,6] (removed index 3)
3. REROLL remaining 5 dice → [4,1,2,6,3]
4. Continue with new dice
```

**What Actually Happens**:
```
1. Show initial dice: [2,2,3,1,5,6]
2. Remove selected: [2,2,3,5,6]
3. Wait...
4. DESTROY all dice and spawn new: [4,1,2,6,3]
```

### Issue #3: Indices Become Invalid After First Iteration 📍

**Location**: `iteration.DiceIndicesUsed` in iteration 2+

**Problem**: The indices stored in iteration 2+ refer to positions in the SIMULATED dice array, not the VISUAL dice array.

**Example**:
```
Iteration 1:
- Visual: [2,2,3,1,5,6] (6 dice, indices 0-5)
- Removes index 3
- Visual now: [2,2,3,5,6] (5 dice, indices 0-4)

Iteration 2:
- Simulation thinks: [4,1,2,6,3] (5 dice, indices 0-4)
- Finds "One" at index 1 (value 1)
- Stores DiceIndicesUsed = [1]
- Visual has: [2,2,3,5,6]
- Tries to remove index 1 → removes value 2 instead of 1!
```

## PROPOSED FIXES

### Fix #1: Don't Respawn Dice Between Iterations
```csharp
// In VisualizeAggressiveIterations
for (int i = 0; i < result.Iterations.Count; i++)
{
    var iteration = result.Iterations[i];
    
    // ONLY spawn dice on first iteration
    if (i == 0 && diceController != null)
    {
        diceController.SpawnAIDice(iteration.InitialDice);
    }
    
    // ... rest of logic
}
```

### Fix #2: Add Actual Reroll Step
```csharp
// After removing dice
diceController.RemoveAIDice(iteration.DiceIndicesUsed);
yield return new WaitForSeconds(1.0f);

// If continuing, reroll remaining dice
if (iteration.ContinueDecision && iteration.RemainingDice > 0)
{
    // Reroll the remaining dice in place
    diceController.RerollRemainingAIDice();
    yield return new WaitForSeconds(1.5f);
    
    // NOW get the actual dice values for next iteration
    var actualDiceValues = diceController.GetCurrentAIDiceValues();
    // Store these for next iteration validation
}
```

### Fix #3: Recalculate Indices Based on Actual Visual Dice
```csharp
// Before each iteration (except first)
if (i > 0)
{
    // Get actual visual dice values
    var actualDice = diceController.GetCurrentAIDiceValues();
    
    // Recalculate combination and indices based on ACTUAL dice
    var actualCombination = combinationStrategy.FindMinimumDiceCombination(
        actualDice, BehaviorMode.AGGRESSIVE);
    
    // Use actual indices instead of simulated ones
    iteration.DiceIndicesUsed = actualCombination.combination.diceIndices;
}
```

## ALTERNATIVE APPROACH: Real-Time Decision Making

Instead of simulating the entire turn, make decisions iteration-by-iteration:

```csharp
IEnumerator ExecuteAggressiveTurnFlowRealTime()
{
    // Show initial dice
    diceController.SpawnAIDice(initialDice);
    yield return new WaitForSeconds(2.0f);
    
    while (shouldContinue)
    {
        // Get CURRENT visual dice
        var currentDice = diceController.GetCurrentAIDiceValues();
        
        // Make decision based on CURRENT state
        var combination = FindMinimumDiceCombination(currentDice);
        
        // Remove selected dice
        diceController.RemoveAIDice(combination.diceIndices);
        yield return new WaitForSeconds(1.0f);
        
        // Decide: continue or stop?
        shouldContinue = MakeContinueDecision(...);
        
        if (shouldContinue)
        {
            // Reroll remaining dice
            diceController.RerollRemainingAIDice();
            yield return new WaitForSeconds(1.5f);
        }
    }
}
```

## IMMEDIATE ACTION NEEDED

**Priority 1**: Add `DiceController.GetCurrentAIDiceValues()` method to get actual visual dice
**Priority 2**: Add `DiceController.RerollRemainingAIDice()` method to reroll in place
**Priority 3**: Either fix the playback to match simulation OR switch to real-time decisions

## Questions to Answer

1. Should aggressive mode simulate ahead (current) or decide in real-time?
2. If simulating, how do we ensure simulated dice match visual dice?
3. Should we store actual dice values in each iteration instead of just indices?
4. Do we need to validate that simulation matches reality before each playback step?
