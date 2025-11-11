# Aggressive AI Stop Analysis - Single Dice Selection

## Scenario: AI Takes Single Dice (e.g., one "1" for 100 points)

### Initial State
```
Dice: [2,2,3,1,5,6]
AI selects: Index 3 (value 1)
Points gained: 100
Remaining dice: 5
Iteration: 1 (first hot streak cycle)
Success count: 1
Current turn score: 100
Points cap: 500
```

## Decision Flow

### Step 1: MakeContinueDecision Called

**Location**: `AIAggressiveRerollStrategy.cs` line ~295

```csharp
ContinueDecision MakeContinueDecision(RerollIteration selection, AggressiveRerollResult result)
{
    int projectedTurnScore = currentRerollState.TotalPointsScored + selection.PointsGained;
    // projectedTurnScore = 0 + 100 = 100
    
    // Check: Hot streak?
    if (selection.RemainingDice <= 0)
    {
        return true; // Always continue on hot streak
    }
    // RemainingDice = 5, so NO hot streak
    
    // Call risk calculator
    var stopDecision = riskCalculator.CalculateStopDecision(
        currentRerollState.TotalIterations, // = 1
        selection.RemainingDice,            // = 5
        currentRerollState.SelectedCombinations.Count, // = 1
        projectedTurnScore,                 // = 100
        currentRerollState.PointsPerTurnCap, // = 500
        true // isAggressive
    );
}
```

### Step 2: Risk Calculator - Momentum Stop Chance

**Location**: `AIRiskCalculator.cs` line ~125

```csharp
float CalculateMomentumStopChance(int iteration, int diceCount, int successCount, BehaviorMode mode)
{
    // iteration = 1
    // diceCount = 5
    // successCount = 1
    // mode = AGGRESSIVE
    
    // 1. Base Multiplier (Aggressive)
    baseMultiplier = config.AggressiveBaseMultiplier
    // Typical value: 0.05 (5%)
    
    // 2. Fibonacci Multiplier (Iteration 1)
    fibonacciMultiplier = fibonacciSequence[0] = 1
    
    // 3. Success Momentum Multiplier (1 success)
    successMomentumMultiplier = 1.0 - (1 × config.MomentumReductionPerSuccess)
    // Typical: 1.0 - (1 × 0.1) = 0.9
    
    // 4. Dice Risk Multiplier (5 dice)
    if (diceCount > 2) return 1.0
    diceRiskMultiplier = 1.0 // No risk with 5 dice
    
    // 5. Iteration Pressure Multiplier (Iteration 1)
    if (iteration <= 2) return 1.0
    iterationPressureMultiplier = 1.0 // No pressure in iteration 1
    
    // FINAL CALCULATION:
    momentumStopChance = 0.05 × 1 × 0.9 × 1.0 × 1.0
                       = 0.045 (4.5%)
}
```

### Step 3: Risk Calculator - Cap Stop Chance

**Location**: `AIRiskCalculator.cs` line ~220

```csharp
float CalculateCapStopChance(int currentScore, int pointsCap, BehaviorMode mode)
{
    // currentScore = 100
    // pointsCap = 500
    
    if (currentScore < pointsCap) return 0f;
    // 100 < 500, so return 0%
    
    capStopChance = 0.0 (0%)
}
```

### Step 4: Combined Stop Chance

**Location**: `AIRiskCalculator.cs` line ~95

```csharp
// Calculate combined probability
decision.CombinedStopChance = 1f - (1f - 0.045) × (1f - 0.0)
                            = 1f - (0.955 × 1.0)
                            = 1f - 0.955
                            = 0.045 (4.5%)
```

### Step 5: Probability Rolls

**Location**: `AIRiskCalculator.cs` line ~98

```csharp
// Roll 1: Momentum
decision.MomentumRollResult = Random.Range(0f, 1f) < 0.045
// 4.5% chance to succeed

// Roll 2: Cap
decision.CapRollResult = Random.Range(0f, 1f) < 0.0
// 0% chance to succeed (always false)

// Final decision
decision.ShouldStop = MomentumRollResult || CapRollResult
// ShouldStop = (4.5% chance) || false
// ShouldStop = 4.5% chance of being true
```

## What Would Make AI Stop?

### Current Scenario (5 dice remaining, 100 points, iteration 1):

**Momentum Stop Chance**: 4.5%
**Cap Stop Chance**: 0%
**Combined**: 4.5%

**Result**: AI has only **4.5% chance** to stop after taking single dice.

### Factors That Would INCREASE Stop Chance:

#### 1. Fewer Remaining Dice
```
5 dice: 4.5% stop chance
4 dice: 4.5% stop chance
3 dice: 4.5% stop chance
2 dice: ~11% stop chance (2.5× dice risk multiplier)
1 dice: ~18% stop chance (4× dice risk multiplier)
```

#### 2. Higher Iteration Count
```
Iteration 1: 4.5% (Fibonacci = 1)
Iteration 2: 4.5% (Fibonacci = 1)
Iteration 3: 9% (Fibonacci = 2)
Iteration 4: 13.5% (Fibonacci = 3)
Iteration 5: 22.5% (Fibonacci = 5)
```

#### 3. Score Over Cap
```
Score 100/500: 0% cap chance
Score 500/500: 30% cap chance
Score 600/500: 45% cap chance
Score 700/500: 60% cap chance
```

#### 4. Fewer Successes (Less Momentum)
```
0 successes: 5% stop chance (no reduction)
1 success: 4.5% stop chance (10% reduction)
2 successes: 4% stop chance (20% reduction)
```

## Realistic Scenarios Where AI Would Stop

### Scenario A: Low Dice + High Iteration
```
Iteration 5, 2 dice remaining, 1 success, 200 points

Momentum = 0.05 × 5 (Fib) × 0.9 × 2.5 (dice) × 1.3 (pressure)
         = 0.73 (73%)
Cap = 0%
Combined = 73%

Result: 73% chance to stop
```

### Scenario B: Over Cap
```
Iteration 2, 4 dice remaining, 2 successes, 550 points

Momentum = 0.05 × 1 × 0.8 × 1.0 × 1.0
         = 0.04 (4%)
Cap = 0.3 + (1 × 0.05) = 0.35 (35%)
Combined = 1 - (0.96 × 0.65) = 0.376 (37.6%)

Result: 37.6% chance to stop
```

### Scenario C: Extreme Risk (1 dice, high iteration)
```
Iteration 4, 1 dice remaining, 1 success, 300 points

Momentum = 0.05 × 3 (Fib) × 0.9 × 4.0 (dice) × 1.2 (pressure)
         = 0.648 (64.8%)
Cap = 0%
Combined = 64.8%

Result: 64.8% chance to stop
```

## Why AI Rarely Stops After Single Dice Selection

### In Early Game (Iteration 1, 5 dice, low score):

1. **Low Fibonacci multiplier** (1×) - early in turn
2. **No dice risk** (1×) - plenty of dice remaining
3. **No iteration pressure** (1×) - first iteration
4. **No cap pressure** (0%) - score well below cap
5. **Success momentum reduction** (0.9×) - had a success

**Result**: Only 4.5% chance to stop

### This is BY DESIGN for Aggressive Mode

Aggressive mode is supposed to:
- Take risks early in the turn
- Continue with many dice remaining
- Push for high scores
- Only stop when risk becomes significant

## Current Bug Impact

**The bug**: AI ignores the stop decision and continues anyway.

**Impact on this scenario**: 
- Even if the 4.5% roll succeeds, AI continues
- Even if risk calculator says stop, AI continues
- Makes the 4.5% meaningless

**But**: Even without the bug, AI would continue 95.5% of the time in this scenario, which is correct behavior for aggressive mode.

## Configuration Values That Control This

**File**: `AIConfiguration` (need to check actual values)

```csharp
// Base risk
AggressiveBaseMultiplier = 0.05 // Lower = more aggressive

// Momentum reduction
MomentumReductionPerSuccess = 0.1 // Higher = more willing to continue on streaks

// Dice risk
DiceRiskExponent = 2.0 // Higher = more scared of low dice
DiceRiskMultiplier = 0.5 // Higher = more scared of low dice

// Iteration pressure
IterationPressureIncrease = 0.1 // Higher = stops sooner in later iterations

// Cap pressure
BaseCapStopChance = 0.3 // Higher = stops sooner at cap
AggressiveCapGrowthRate = 0.05 // Higher = stops sooner over cap
```

## Summary

**Q: What makes aggressive AI stop after taking single dice?**

**A**: Almost nothing in early game with many dice remaining.

With 5 dice, iteration 1, score under cap:
- **4.5% chance** to stop (by design)
- **95.5% chance** to continue (correct for aggressive)

AI will only stop early if:
1. Very unlucky (4.5% roll succeeds)
2. Already over points cap
3. Very few dice remaining (1-2)
4. Later in turn (iteration 3+)

**This is working as intended** - aggressive mode should be aggressive!
