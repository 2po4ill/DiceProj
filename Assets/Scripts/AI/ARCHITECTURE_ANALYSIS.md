# AI System Architecture Analysis

## Major Architectural Flaws

### 1. **CRITICAL: Dice Array Synchronization Issue**

**Problem**: There are TWO separate dice arrays that must stay synchronized:
- `currentTurnState.CurrentDice` (AI's internal logic array)
- `DiceController.aiDice` (Visual GameObjects array)

**Current Flow**:
```
AITurnExecutor generates: [2,2,3,1,5,6]
    ↓
DiceController spawns visual dice in same order
    ↓
AI selects combination using internal array
    ↓
AI finds indices to remove
    ↓
PROBLEM: Indices might not match if arrays diverged!
```

**Root Cause**: The arrays can become desynchronized when:
- Dice are regenerated mid-turn
- Dice are removed and new ones added
- Any sorting or reordering happens

**Solution**: Use a SINGLE source of truth with GameObject-to-value mapping

---

### 2. **Duplicate Dice Management Systems**

**Problem**: Multiple components manage dice state independently:

| Component | Dice Storage | Purpose |
|-----------|--------------|---------|
| `AITurnExecutor` | `currentTurnState.CurrentDice` | AI logic |
| `DiceController` | `aiDice` (GameObjects) | Visual display |
| `AIDiceDisplaySystem` | `currentDiceValues` | UI display |
| `AIDiceManager` | Separate dice tracking | Alternative system |

**Issues**:
- Each system can get out of sync
- No single source of truth
- Difficult to debug which array is wrong

---

### 3. **Combination Selection vs Dice Identification Mismatch**

**Problem**: `CombinationResult` doesn't store which specific dice were used

```csharp
public class CombinationResult
{
    public Rule rule;        // e.g., "One"
    public int points;       // e.g., 100
    public string description;
    public float multiplier;
    // ❌ MISSING: List<int> usedDiceIndices or usedDiceValues
}
```

**Impact**:
- For `Rule.One`: Could be a 1 (100pts) or 5 (50pts)
- AI must **guess** which dice were used based on points
- Fragile logic that breaks if point values change

**Solution**: Store the actual dice values/indices used in the combination

---

### 4. **Aggressive Reroll Strategy Creates New Arrays**

**File**: `AIAggressiveRerollStrategy.cs`

**Problem**:
```csharp
List<int> currentDice = new List<int>(initialDice);  // Creates copy

while (ShouldContinueRerolling(currentDice, result))
{
    currentDice = GenerateNewDiceSet(6);  // Replaces array!
}
```

This creates **new arrays** that don't match the visual dice order!

**Impact**:
- Visual dice show one order
- Internal array has different order
- Indices don't match

---

### 5. **Multiple Turn Execution Paths**

**Problem**: AI has multiple execution flows:

```
AITurnExecutor.ExecuteTurnFlow()
    ├─→ ExecuteAggressiveTurnFlow()  (uses AIAggressiveRerollStrategy)
    └─→ ExecuteStandardTurnFlow()    (uses different logic)
```

**Issues**:
- Different paths handle dice differently
- Aggressive path creates new arrays
- Standard path modifies existing arrays
- Inconsistent behavior

---

### 6. **Dice Removal Logic Duplication**

**Three different removal methods**:

```csharp
// Method 1: Remove by values
RemoveUsedDice(List<int> usedDice)

// Method 2: Remove by count
RemoveUsedDice(int diceCount)

// Method 3: Remove by indices
RemoveUsedDiceByIndices(List<int> indices)
```

**Problem**: Different parts of code use different methods, causing confusion

---

## Recommended Fixes

### Priority 1: Single Source of Truth

Create a unified dice management system:

```csharp
public class AIDiceState
{
    public List<GameObject> visualDice;      // GameObjects
    public Dictionary<GameObject, int> diceValues;  // GameObject → value mapping
    
    public int GetValue(int index) => diceValues[visualDice[index]];
    public List<int> GetAllValues() => visualDice.Select(d => diceValues[d]).ToList();
    public void RemoveAt(int index) { /* Remove both visual and value */ }
}
```

### Priority 2: Store Used Dice in CombinationResult

```csharp
public class CombinationResult
{
    public Rule rule;
    public int points;
    public string description;
    public float multiplier;
    public List<int> usedDiceIndices;  // ← ADD THIS
    public List<int> usedDiceValues;   // ← ADD THIS
}
```

### Priority 3: Unify Turn Execution

Remove the aggressive/standard split and use ONE consistent flow

### Priority 4: Single Removal Method

Keep only `RemoveUsedDiceByIndices()` and remove the others

---

## Current Bug Root Cause

**The dice removal bug is caused by**:

The aggressive reroll strategy generates NEW dice arrays that don't match the visual order:

```csharp
// Visual dice spawned: [2,2,3,1,5,6]
// But aggressive strategy creates: [1,2,2,3,5,6] (sorted or reordered)
// So when AI looks for value 1, it finds it at index 0 instead of 3
```

**Immediate Fix**: Ensure `currentTurnState.CurrentDice` always matches the visual dice order exactly, never sort or reorder it.
