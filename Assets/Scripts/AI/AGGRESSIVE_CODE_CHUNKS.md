# Aggressive AI - Code Chunks & Decision Making Flow

## Entry Point: AITurnExecutor.ExecuteAggressiveTurnFlow()

**File**: `Assets/Scripts/AI/AITurnExecutor.cs` (lines ~190-230)

```csharp
IEnumerator ExecuteAggressiveTurnFlow()
{
    // 1. Initial setup and delays
    // 2. Call aggressive strategy simulation
    var aggressiveResult = aggressiveRerollStrategy.ExecuteAggressiveReroll(
        currentTurnState.CurrentDice,
        currentTurnState.CurrentMode,
        currentTurnState.CurrentTurnScore,
        currentTurnState.PointsPerTurnCap
    );
    
    // 3. Playback the simulated iterations
    yield return StartCoroutine(VisualizeAggressiveIterations(aggressiveResult));
    
    // 4. Process final results
    ProcessAggressiveRerollResult(aggressiveResult);
}
```

**Purpose**: Orchestrates the aggressive turn flow
**Key Decision**: None - just coordinates

---

## Chunk 1: Main Simulation Loop

**File**: `Assets/Scripts/AI/AIAggressiveRerollStrategy.cs` (lines 86-160)

```csharp
public AggressiveRerollResult ExecuteAggressiveReroll(
    List<int> initialDice, BehaviorMode mode, 
    int currentTurnScore, int pointsPerTurnCap)
{
    // Initialize state
    InitializeRerollState(initialDice, currentTurnScore, pointsPerTurnCap);
    
    List<int> currentDice = new List<int>(initialDice);
    
    // MAIN LOOP - continues until stop condition
    while (ShouldContinueRerolling(currentDice, result))
    {
        // Execute one iteration
        var iteration = ExecuteRerollIteration(currentDice, result);
        
        if (iteration == null || iteration.SelectedCombination == null)
        {
            // ZONK - no valid combinations
            result.ZonkOccurred = true;
            break;
        }
        
        // Store iteration results
        ProcessRerollIteration(iteration, result);
        
        // Generate next dice set
        if (iteration.RemainingDice == 0)
        {
            // HOT STREAK - all dice used
            currentDice = GenerateNewDiceSet(6);
            result.HotStreakCount++;
        }
        else
        {
            // Normal reroll
            currentDice = GenerateNewDiceSet(iteration.RemainingDice);
        }
        
        // Check iteration limit
        if (currentRerollState.TotalIterations >= currentRerollState.MaxIterationsAllowed)
        {
            result.FinalReason = "Iteration limit reached";
            break;
        }
    }
    
    return result;
}
```

**Purpose**: Simulates entire turn, generates all iterations
**Key Decisions**: 
- When to stop looping (ShouldContinueRerolling)
- Hot streak detection
- Iteration limit enforcement

---

## Chunk 2: Single Iteration Execution

**File**: `Assets/Scripts/AI/AIAggressiveRerollStrategy.cs` (lines 165-215)

```csharp
RerollIteration ExecuteRerollIteration(List<int> currentDice, AggressiveRerollResult result)
{
    currentRerollState.TotalIterations++;
    
    var iteration = new RerollIteration
    {
        IterationNumber = currentRerollState.TotalIterations,
        InitialDice = new List<int>(currentDice)
    };
    
    // DECISION: Find minimum dice combination
    var selectedCombination = SelectMinimumDiceCombination(currentDice);
    
    if (selectedCombination == null)
    {
        // No combination found - Zonk
        return null;
    }
    
    // Fill iteration details
    iteration.SelectedCombination = selectedCombination;
    iteration.DiceIndicesUsed = selectedCombination.diceIndices;
    iteration.DiceUsed = GetDiceUsedForCombination(selectedCombination.rule);
    iteration.RemainingDice = currentDice.Count - iteration.DiceUsed;
    iteration.PointsGained = selectedCombination.points;
    
    // DECISION: Should continue or stop after this?
    var continueDecision = MakeContinueDecision(iteration, result);
    iteration.ContinueDecision = continueDecision.ShouldContinue;
    iteration.DecisionReason = continueDecision.Reason;
    
    return iteration;
}
```

**Purpose**: Executes one iteration - select combination and decide continue/stop
**Key Decisions**:
- Which combination to select (SelectMinimumDiceCombination)
- Whether to continue after this iteration (MakeContinueDecision)

---

## Chunk 3: Combination Selection (Minimum Dice Strategy)

**File**: `Assets/Scripts/AI/AIAggressiveRerollStrategy.cs` (lines 220-250)

```csharp
CombinationResult SelectMinimumDiceCombination(List<int> diceValues)
{
    if (diceValues == null || diceValues.Count == 0)
        return null;
    
    // Use combination strategy to find minimum dice combination
    var strategyResult = combinationStrategy.FindMinimumDiceCombination(
        diceValues, BehaviorMode.AGGRESSIVE);
    
    if (strategyResult == null)
        return null;
    
    var combination = strategyResult.combination;
    
    // Validate combination meets aggressive criteria
    if (ValidateAggressiveCombination(combination, diceValues.Count))
    {
        return combination;
    }
    
    // Fallback: find any valid combination
    var allCombinations = combinationStrategy.FindAllValidCombinations(diceValues);
    if (allCombinations.Count > 0)
    {
        // Select combination with best points-per-dice ratio
        return allCombinations.OrderByDescending(c => 
            GetDiceUsedForCombination(c.rule) > 0 ? 
            (float)c.points / GetDiceUsedForCombination(c.rule) : 0f).First();
    }
    
    return null;
}
```

**Purpose**: Selects which combination to use (minimum dice for aggressive)
**Key Decision**: Which dice to select based on efficiency

---

## Chunk 4: Continue/Stop Decision

**File**: `Assets/Scripts/AI/AIAggressiveRerollStrategy.cs` (lines 290-330)

```csharp
ContinueDecision MakeContinueDecision(RerollIteration iteration, AggressiveRerollResult result)
{
    var decision = new ContinueDecision();
    
    int projectedTurnScore = currentRerollState.TotalPointsScored + iteration.PointsGained;
    
    // HARD STOP 1: Iteration limit
    if (currentRerollState.TotalIterations >= currentRerollState.MaxIterationsAllowed)
    {
        decision.ShouldContinue = false;
        decision.Reason = "Iteration limit reached";
        return decision;
    }
    
    // ALWAYS CONTINUE: Hot streak
    if (iteration.RemainingDice <= 0)
    {
        decision.ShouldContinue = true;
        decision.Reason = "Hot streak - all dice used";
        return decision;
    }
    
    // USE RISK CALCULATOR: Dual probability system
    var stopDecision = riskCalculator.CalculateStopDecision(
        currentRerollState.TotalIterations,
        iteration.RemainingDice,
        currentRerollState.SelectedCombinations.Count,
        projectedTurnScore,
        currentRerollState.PointsPerTurnCap,
        true // isAggressive
    );
    
    decision.ShouldContinue = !stopDecision.ShouldStop;
    decision.Reason = stopDecision.DecisionReason;
    
    return decision;
}
```

**Purpose**: Decides whether to continue rerolling or stop
**Key Decision**: Continue vs Stop based on risk analysis

---

## Chunk 5: Risk Calculation (Dual Probability System)

**File**: `Assets/Scripts/AI/AIRiskCalculator.cs` (need to check this)

```csharp
public AIStopDecision CalculateStopDecision(
    int iterationCount,
    int remainingDice,
    int combinationsFound,
    int projectedScore,
    int pointsCap,
    bool isAggressive)
{
    // Calculate zonk probability
    float zonkProbability = CalculateZonkProbability(remainingDice);
    
    // Calculate cap probability
    float capProbability = CalculateCapProbability(projectedScore, pointsCap);
    
    // Combine probabilities
    float combinedRisk = CombineDualProbabilities(zonkProbability, capProbability);
    
    // Apply aggressive threshold
    float stopThreshold = isAggressive ? 0.7f : 0.5f; // Aggressive takes more risk
    
    bool shouldStop = combinedRisk > stopThreshold;
    
    return new AIStopDecision
    {
        ShouldStop = shouldStop,
        ZonkProbability = zonkProbability,
        CapProbability = capProbability,
        CombinedRisk = combinedRisk,
        DecisionReason = GenerateReason(...)
    };
}
```

**Purpose**: Calculates risk and determines stop threshold
**Key Decision**: Risk assessment based on dual probability system

---

## Chunk 6: Should Continue Rerolling Check

**File**: `Assets/Scripts/AI/AIAggressiveRerollStrategy.cs` (lines 350-370)

```csharp
bool ShouldContinueRerolling(List<int> currentDice, AggressiveRerollResult result)
{
    // Check if we have dice to work with
    if (currentDice == null || currentDice.Count == 0)
        return false;
    
    // Check iteration limit
    if (currentRerollState.TotalIterations >= currentRerollState.MaxIterationsAllowed)
        return false;
    
    // Check if we've already decided to stop in the last iteration
    if (rerollHistory.Count > 0)
    {
        var lastIteration = rerollHistory.Last();
        if (!lastIteration.ContinueDecision && lastIteration.RemainingDice > 0)
            return false;
    }
    
    return true;
}
```

**Purpose**: Gate check before each iteration
**Key Decision**: Whether to enter the next iteration loop

---

## Chunk 7: Minimum Dice Algorithm (AICombinationStrategy)

**File**: `Assets/Scripts/AI/AICombinationStrategy.cs` (lines 369-420)

```csharp
public StrategyResult FindMinimumDiceCombination(List<int> diceValues, BehaviorMode mode)
{
    // Find all possible combinations
    List<StrategyResult> allCombinations = FindAllCombinations(diceValues, 0.0f);
    
    if (allCombinations.Count == 0)
        return null;
    
    // Apply minimum dice selection algorithm
    StrategyResult bestMinimum = ApplyMinimumDiceAlgorithm(allCombinations, mode);
    
    return bestMinimum;
}

StrategyResult ApplyMinimumDiceAlgorithm(List<StrategyResult> combinations, BehaviorMode mode)
{
    // Step 1: Group combinations by dice count
    var groupedByDice = combinations.GroupBy(c => c.diceUsed).OrderBy(g => g.Key);
    
    // Step 2: Find the minimum dice count that has viable combinations
    foreach (var diceGroup in groupedByDice)
    {
        var viableCombinations = ValidateMinimumViableCombinations(diceGroup.ToList(), mode);
        
        if (viableCombinations.Count > 0)
        {
            // Step 3: Apply strategic comparison within minimum dice group
            return CompareMinimumDiceCombinations(viableCombinations, mode);
        }
    }
    
    // Fallback
    return combinations.OrderByDescending(c => c.strategicValue).First();
}
```

**Purpose**: Finds the combination that uses minimum dice
**Key Decision**: Which combination uses fewest dice while meeting thresholds

---

## Chunk 8: Playback Visualization

**File**: `Assets/Scripts/AI/AITurnExecutor.cs` (lines 1054-1140)

```csharp
IEnumerator VisualizeAggressiveIterations(AggressiveRerollResult result)
{
    for (int i = 0; i < result.Iterations.Count; i++)
    {
        var iteration = result.Iterations[i];
        
        // Spawn dice for this iteration
        diceController.SpawnAIDice(iteration.InitialDice);
        yield return new WaitForSeconds(1.5f);
        
        // Remove selected dice
        if (iteration.DiceIndicesUsed != null && iteration.DiceIndicesUsed.Count > 0)
        {
            diceController.RemoveAIDice(iteration.DiceIndicesUsed);
        }
        yield return new WaitForSeconds(1.0f);
        
        // Check for hot streak
        if (iteration.RemainingDice == 0 && i < result.Iterations.Count - 1)
        {
            // Hot streak message
            yield return new WaitForSeconds(1.5f);
        }
    }
}
```

**Purpose**: Plays back the simulated iterations visually
**Key Decision**: None - just visualization

---

## Decision Flow Summary

```
1. ExecuteAggressiveReroll (MAIN LOOP)
   ├─ while (ShouldContinueRerolling) ← GATE CHECK
   │  ├─ ExecuteRerollIteration
   │  │  ├─ SelectMinimumDiceCombination ← DECISION: Which dice?
   │  │  │  └─ FindMinimumDiceCombination ← ALGORITHM
   │  │  └─ MakeContinueDecision ← DECISION: Continue or stop?
   │  │     └─ CalculateStopDecision ← RISK ANALYSIS
   │  └─ GenerateNewDiceSet ← SIMULATION
   └─ Return all iterations

2. VisualizeAggressiveIterations (PLAYBACK)
   └─ for each iteration
      ├─ SpawnAIDice
      └─ RemoveAIDice
```

## Key Issues to Address

### Issue 1: Iteration Definition
**Current**: Iteration = full clear of cubes (SpawnAIDice clears all)
**Expected**: Iteration = one selection + reroll decision

**Problem**: Each iteration spawns completely new dice, making it look like 5 separate turns instead of 5 selections within one turn.

### Issue 2: Hot Streak Not Properly Handled
**Current**: Hot streak increments counter but treats it like normal iteration
**Expected**: Hot streak should reset to 6 dice and continue same turn

### Issue 3: Stop Decision Ignored
**Current**: Loop continues even if `iteration.ContinueDecision = false`
**Expected**: Loop should break when AI decides to stop

**Location**: Line 141 in AIAggressiveRerollStrategy - generates new dice even if decision was to stop


---

## CRITICAL BUG IDENTIFIED

### Location: AIAggressiveRerollStrategy.cs, lines 115-145

```csharp
while (ShouldContinueRerolling(currentDice, result))
{
    var iteration = ExecuteRerollIteration(currentDice, result);
    
    // ... process iteration ...
    
    // BUG: Generates new dice REGARDLESS of continue decision
    if (iteration.RemainingDice == 0)
    {
        currentDice = GenerateNewDiceSet(6);
    }
    else
    {
        currentDice = GenerateNewDiceSet(iteration.RemainingDice);  // ← ALWAYS GENERATES
    }
}
```

### The Problem

**The loop generates new dice even when `iteration.ContinueDecision = false`**

This means:
1. AI decides to STOP (ContinueDecision = false)
2. Loop generates new dice anyway
3. Next iteration starts with new dice
4. Loop continues until iteration limit (5) is reached

### Expected Behavior

```csharp
while (ShouldContinueRerolling(currentDice, result))
{
    var iteration = ExecuteRerollIteration(currentDice, result);
    
    // ... process iteration ...
    
    // CHECK: Should we continue?
    if (!iteration.ContinueDecision)
    {
        // AI decided to stop - break the loop
        result.FinalReason = iteration.DecisionReason;
        break;
    }
    
    // Only generate new dice if continuing
    if (iteration.RemainingDice == 0)
    {
        currentDice = GenerateNewDiceSet(6);
    }
    else
    {
        currentDice = GenerateNewDiceSet(iteration.RemainingDice);
    }
}
```

### Impact

- AI always does 5 iterations (max limit) instead of stopping when risk is too high
- Ignores the risk calculator's stop decision
- Makes aggressive mode too aggressive (never stops early)
- Defeats the purpose of the dual probability system

### Fix Required

Add a check for `iteration.ContinueDecision` before generating new dice and continuing the loop.
