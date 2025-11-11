# What Actually Stops Aggressive AI NOW (Current Code)

## Stop Conditions in Current Implementation

### 1. ZONK (No Valid Combination)
**Location**: `AIAggressiveRerollStrategy.cs` line 125
```csharp
if (selection == null || selection.SelectedCombination == null)
{
    result.ZonkOccurred = true;
    result.FinalReason = "Zonk - no valid combinations found";
    break; // ✓ STOPS
}
```
**Trigger**: Cannot find any valid combination in current dice
**Example**: Dice = [2,3,4,6,6,6] but combination detector fails

---

### 2. HOT STREAK ITERATION LIMIT
**Location**: `AIAggressiveRerollStrategy.cs` line 141
```csharp
if (selection.RemainingDice == 0)
{
    currentRerollState.TotalIterations++;
    
    if (currentRerollState.TotalIterations > currentRerollState.MaxIterationsAllowed)
    {
        result.FinalReason = "Iteration limit reached";
        break; // ✓ STOPS
    }
}
```
**Trigger**: More than 5 hot streaks (full 6-dice clears)
**Example**: AI clears all dice 5 times, on 6th hot streak it stops

---

### 3. NO DICE REMAINING (Gate Check)
**Location**: `AIAggressiveRerollStrategy.cs` line 365 (ShouldContinueRerolling)
```csharp
if (currentDice == null || currentDice.Count == 0)
    return false; // ✓ STOPS
```
**Trigger**: No dice to work with
**Example**: Edge case where dice list becomes empty

---

### 4. ITERATION LIMIT (Gate Check)
**Location**: `AIAggressiveRerollStrategy.cs` line 369 (ShouldContinueRerolling)
```csharp
if (currentRerollState.TotalIterations >= currentRerollState.MaxIterationsAllowed)
    return false; // ✓ STOPS
```
**Trigger**: Reached max iterations (5 hot streaks)
**Example**: Same as #2 but checked before loop continues

---

### 5. LAST SELECTION DECIDED TO STOP
**Location**: `AIAggressiveRerollStrategy.cs` line 373 (ShouldContinueRerolling)
```csharp
if (rerollHistory.Count > 0)
{
    var lastIteration = rerollHistory.Last();
    if (!lastIteration.ContinueDecision && lastIteration.RemainingDice > 0)
        return false; // ✓ STOPS
}
```
**Trigger**: Previous selection's risk calculator said to stop AND there were dice remaining
**Example**: 
- Selection 1: Take 1 dice, 5 remaining, risk calculator says STOP
- Loop checks: lastIteration.ContinueDecision = false, RemainingDice = 5 > 0
- Result: STOPS before next selection

**THIS IS THE RISK CALCULATOR STOP!**

---

## Summary of What Stops AI NOW

| Condition | Trigger | Frequency |
|-----------|---------|-----------|
| **Zonk** | No valid combination | Rare (depends on dice) |
| **Hot Streak Limit** | 5+ hot streaks | Rare (need to clear all dice 5 times) |
| **No Dice** | Empty dice list | Very rare (edge case) |
| **Iteration Limit (gate)** | 5+ hot streaks | Same as hot streak limit |
| **Risk Calculator Stop** | Previous selection said stop | **DEPENDS ON RISK CALC** |

## The Risk Calculator Stop (Condition #5)

This is the ONLY place where the risk calculator's decision matters!

### How It Works:

```
Selection N:
├─ ExecuteRerollIteration
│  └─ MakeContinueDecision
│     └─ riskCalculator.CalculateStopDecision
│        └─ Returns: ShouldStop = true/false
├─ selection.ContinueDecision = !ShouldStop
└─ Store in rerollHistory

Next Loop Iteration:
├─ ShouldContinueRerolling checks:
│  └─ if (!lastIteration.ContinueDecision && lastIteration.RemainingDice > 0)
│     └─ return false ✓ STOPS
```

### Example Scenario:

```
Turn Start: [2,2,3,1,5,6]

Selection 1:
- Takes dice at index 3 (value 1)
- Remaining: 5 dice
- Risk calculator: 4.5% chance to stop
- Random roll: 0.03 (3%) < 0.045 (4.5%) → STOP!
- selection.ContinueDecision = false
- Stored in rerollHistory

Loop Check (ShouldContinueRerolling):
- lastIteration.ContinueDecision = false ✓
- lastIteration.RemainingDice = 5 > 0 ✓
- return false → LOOP STOPS

Result: AI stops after taking single dice!
```

## Why It Seems Like AI Never Stops

### Problem: Low Stop Probability Early Game

With typical early game conditions:
- Iteration 1
- 5 dice remaining  
- Low score

**Stop chance = 4.5%**

This means:
- **95.5% of the time**: AI continues
- **4.5% of the time**: AI stops

### You Need to See ~20 Turns to See One Early Stop

If you only test a few turns, you'll almost never see an early stop because it's so rare.

## What WOULD Trigger Stop More Often

### Scenario A: Few Dice Remaining
```
Selection after taking 4 dice:
- Remaining: 2 dice
- Dice risk multiplier: 2.5×
- Stop chance: ~11%
- 1 in 9 turns stops here
```

### Scenario B: High Iteration
```
Iteration 5 (5th hot streak):
- Fibonacci multiplier: 5×
- Stop chance: ~22%
- 1 in 5 turns stops here
```

### Scenario C: Over Cap
```
Score 600/500:
- Cap stop chance: 45%
- Combined with momentum: ~50%
- 1 in 2 turns stops here
```

## Testing the Stop Mechanism

To verify the risk calculator stop works:

### Test 1: Force High Stop Chance
```csharp
// Temporarily modify config
config.AggressiveBaseMultiplier = 0.5f; // Was 0.05
// Now stop chance = 45% instead of 4.5%
// Should see stops in ~half of turns
```

### Test 2: Check Logs
```csharp
// Enable debug logs
enableDebugLogs = true;
showRerollDetails = true;

// Look for:
"Decision: STOP - Momentum roll succeeded"
// Then next log should be:
"ShouldContinueRerolling: Last selection decided to stop"
```

### Test 3: Run Many Turns
```
Run 100 aggressive turns
Count how many stop early (not at iteration limit)
Expected: ~4-5 stops (4.5% chance)
```

## Conclusion

**Q: What can trigger stop NOW?**

**A: The risk calculator CAN trigger stop, but:**
1. It's checked in `ShouldContinueRerolling` (condition #5)
2. It works correctly
3. But the probability is very low early game (4.5%)
4. So you rarely see it happen

**The mechanism works, the probability is just low by design for aggressive mode.**

To see stops more often, you need:
- Fewer dice remaining (1-2)
- Higher iteration count (3-5)
- Score over cap
- Or modify config to increase base stop chance
