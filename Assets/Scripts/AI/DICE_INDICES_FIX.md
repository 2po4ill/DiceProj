# Aggressive Strategy Dice Indices Fix

## Problem
The aggressive AI strategy was removing the wrong dice visually because it didn't track which specific dice indices were selected for combinations.

### Root Cause
1. `CombinationResult` class only contained rule type and points, but **no dice indices**
2. `AIAggressiveRerollStrategy` selected combinations but didn't know which dice to remove
3. `AITurnExecutor.VisualizeAggressiveIterations()` had to guess which dice to remove, always removing from the start (indices 0, 1, 2...)

### Example Bug
- Visual dice: `[2,2,3,1,5,6]`
- AI selects "One" (value 1 at index 3) for 100 points
- But visualization removed index 0 (value 2) instead!
- This caused immediate mismatch between AI logic and visual display

## Solution

### 1. Added Dice Indices to CombinationResult
**File**: `Assets/Scripts/DiceCombinationTypes.cs`
```csharp
public class CombinationResult
{
    public Rule rule;
    public int points;
    public string description;
    public float multiplier;
    public List<int> diceIndices; // NEW: Tracks which dice are used
}
```

### 2. Updated RerollIteration to Store Indices
**File**: `Assets/Scripts/AI/AIAggressiveRerollStrategy.cs`
```csharp
public class RerollIteration
{
    public List<int> DiceIndicesUsed; // NEW: Actual indices selected
    // ... other fields
}
```

### 3. Added Index Tracking Helper Methods
**File**: `Assets/Scripts/AI/AICombinationStrategy.cs`

New helper methods:
- `FindStraightIndices()` - Finds indices for straight patterns
- `FindAnyStraightIndices()` - Finds any consecutive sequence
- `FindValueIndices()` - Finds indices of specific dice values
- `FindSingleOneOrFiveIndex()` - Finds single 1 or 5 for minimum dice

### 4. Updated Combination Detection
All combination finding methods now populate `diceIndices`:
- `FindStraightCombinations()` - Tracks straight indices
- `FindOfAKindCombinations()` - Tracks of-a-kind indices
- `FindPairCombinations()` - Tracks pair indices
- `FindSingleCombinations()` - Tracks single 1/5 indices

### 5. Fixed Visualization
**File**: `Assets/Scripts/AI/AITurnExecutor.cs`
```csharp
// OLD: Always removed from start
for (int j = 0; j < diceToRemove; j++)
{
    indicesToRemove.Add(j); // WRONG!
}

// NEW: Uses actual indices from combination
if (iteration.DiceIndicesUsed != null && iteration.DiceIndicesUsed.Count > 0)
{
    diceController.RemoveAIDice(iteration.DiceIndicesUsed); // CORRECT!
}
```

## Testing
Use `AIAggressiveDiceIndicesFix.cs` to verify the fix:
1. Attach to GameObject with AI components
2. Set `runTest = true` in inspector
3. Check console for verification that indices are tracked correctly

## Impact
- ✓ AI now removes the correct dice visually
- ✓ No more mismatch between AI logic and display
- ✓ Works from the very first iteration
- ✓ All combination types properly tracked
- ✓ Minimum dice strategy now accurate
