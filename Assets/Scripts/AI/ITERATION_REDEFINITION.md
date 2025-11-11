# Iteration Redefinition - Hot Streak Based

## Changes Made

### Old Definition
- **Iteration** = Each dice selection/reroll decision
- Iteration limit (5) = Maximum 5 selections per turn
- Risk calculator used selection count

### New Definition
- **Iteration** = Full clear of all 6 dice (hot streak)
- **Selection** = Each dice selection/reroll decision
- Iteration limit (5) = Maximum 5 hot streaks per turn
- Risk calculator uses hot streak count, not selection count

## Code Changes

### 1. AggressiveRerollState Structure
```csharp
// Added:
public int TotalSelections; // Total number of dice selections made

// Clarified:
public int TotalIterations; // Number of hot streaks (full 6-dice clears)
public int MaxIterationsAllowed; // Max hot streaks allowed (5)
```

### 2. RerollIteration Structure (now represents a selection)
```csharp
// Added:
public int SelectionNumber; // Which selection within this cycle
public bool IsHotStreak; // True if this selection cleared all dice

// Clarified:
public int IterationNumber; // Which hot streak cycle (1-5)
```

### 3. Main Loop Logic
```csharp
// OLD:
while (ShouldContinueRerolling(currentDice, result))
{
    var iteration = ExecuteRerollIteration(currentDice, result);
    currentRerollState.TotalIterations++; // Incremented every selection
    
    if (iteration.RemainingDice == 0)
    {
        currentDice = GenerateNewDiceSet(6);
        result.HotStreakCount++;
    }
}

// NEW:
currentRerollState.TotalIterations = 1; // Start at iteration 1

while (ShouldContinueRerolling(currentDice, result))
{
    var selection = ExecuteRerollIteration(currentDice, result);
    // TotalSelections incremented in ExecuteRerollIteration
    
    if (selection.RemainingDice == 0)
    {
        selection.IsHotStreak = true;
        currentRerollState.TotalIterations++; // Only increment on hot streak
        
        // Check limit BEFORE generating new dice
        if (currentRerollState.TotalIterations > currentRerollState.MaxIterationsAllowed)
        {
            break; // Stop at 5 hot streaks
        }
        
        currentDice = GenerateNewDiceSet(6);
        result.HotStreakCount++;
    }
}
```

### 4. Risk Calculator Input
```csharp
// OLD:
var stopDecision = riskCalculator.CalculateStopDecision(
    currentRerollState.TotalIterations, // Selection count
    ...
);

// NEW:
var stopDecision = riskCalculator.CalculateStopDecision(
    currentRerollState.TotalIterations, // Hot streak count
    ...
);
```

## Behavior Changes

### Example Turn Flow

**Scenario**: AI gets multiple hot streaks

```
ITERATION 1 (Initial 6 dice):
├─ Selection 1.1: Remove 2 dice → 4 remaining
├─ Selection 1.2: Remove 1 dice → 3 remaining
├─ Selection 1.3: Remove 3 dice → 0 remaining (HOT STREAK!)
└─ TotalIterations = 2

ITERATION 2 (New 6 dice):
├─ Selection 2.1: Remove 3 dice → 3 remaining
├─ Selection 2.2: Remove 2 dice → 1 remaining
├─ Selection 2.3: Remove 1 dice → 0 remaining (HOT STREAK!)
└─ TotalIterations = 3

ITERATION 3 (New 6 dice):
├─ Selection 3.1: Remove 2 dice → 4 remaining
├─ Selection 3.2: AI decides to STOP
└─ Turn ends with 4 dice remaining
```

### Iteration Limit Enforcement

**OLD Behavior**:
- Limit reached after 5 selections (regardless of hot streaks)
- Could stop mid-cycle

**NEW Behavior**:
- Limit reached after 5 hot streaks
- Each hot streak is a complete cycle
- Can make unlimited selections within a cycle (until stop decision)

## Risk Calculation Impact

The risk calculator now receives the hot streak count instead of selection count:

```
Selection 1.1: iteration=1, remainingDice=4 → Low risk
Selection 1.2: iteration=1, remainingDice=3 → Medium risk
Selection 1.3: iteration=1, remainingDice=0 → HOT STREAK

Selection 2.1: iteration=2, remainingDice=3 → Medium risk (iteration increased)
Selection 2.2: iteration=2, remainingDice=1 → High risk
```

This means:
- Risk increases with each hot streak, not each selection
- More selections allowed within a single iteration
- Aggressive mode can be more aggressive within each cycle

## Logging Changes

```
OLD:
Total Iterations: 8
Hot Streaks: 2

NEW:
Total Selections: 8
Total Hot Streaks: 2
```

## Summary

✓ Iteration now means "hot streak" (full 6-dice clear)
✓ Selection means "dice selection/reroll decision"
✓ Iteration limit (5) applies to hot streaks, not selections
✓ Risk calculator uses hot streak count for momentum
✓ AI can make unlimited selections within one iteration cycle
✓ More accurate representation of game rules
