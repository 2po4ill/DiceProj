# Risk Calculator - What It Actually Calculates

## Overview

The Risk Calculator implements a **Dual Probability System** that calculates two independent stop chances and combines them to decide whether the AI should stop or continue.

## Two Independent Probabilities

### 1. Momentum Stop Chance (Game State Risk)
Calculates risk based on the current game state momentum.

### 2. Cap Stop Chance (Score Proximity Risk)
Calculates risk based on how close the score is to the points cap.

## Momentum Stop Chance Calculation

**Formula**: 
```
MomentumStopChance = BaseMultiplier × FibonacciMultiplier × 
                     SuccessMomentumMultiplier × DiceRiskMultiplier × 
                     IterationPressureMultiplier
```

### Components:

#### 1. Base Multiplier
- **Aggressive**: Lower base (takes more risk)
- **Passive**: Higher base (more cautious)
- **Purpose**: Sets the baseline risk tolerance

#### 2. Fibonacci Multiplier
```
Iteration 1: 1
Iteration 2: 1
Iteration 3: 2
Iteration 4: 3
Iteration 5: 5
Iteration 6: 8
...
```
- **Purpose**: Exponentially increases risk with each iteration (hot streak)
- **Effect**: Makes AI more likely to stop after multiple hot streaks

#### 3. Success Momentum Multiplier
```
successCount = 0: multiplier = 1.0
successCount = 1: multiplier = 0.9 (reduces stop chance by 10%)
successCount = 2: multiplier = 0.8 (reduces stop chance by 20%)
```
- **Purpose**: Rewards successful combinations
- **Effect**: AI is less likely to stop when on a winning streak

#### 4. Dice Risk Multiplier
```
6 dice: multiplier = 1.0 (no risk)
5 dice: multiplier = 1.0 (no risk)
4 dice: multiplier = 1.0 (no risk)
3 dice: multiplier = 1.0 (no risk)
2 dice: multiplier = 2.0+ (high risk)
1 dice: multiplier = 4.0+ (very high risk)
```
- **Purpose**: Exponentially increases risk with fewer dice
- **Effect**: AI much more likely to stop with 1-2 dice remaining

#### 5. Iteration Pressure Multiplier
```
Iteration 1-2: multiplier = 1.0 (no pressure)
Iteration 3: multiplier = 1.1 (10% increase)
Iteration 4: multiplier = 1.2 (20% increase)
Iteration 5: multiplier = 1.3 (30% increase)
```
- **Purpose**: Increases pressure to stop over time
- **Effect**: AI more likely to stop in later iterations

### Example Calculation:

```
Iteration 3, 2 dice remaining, 1 success, Aggressive mode:

BaseMultiplier = 0.05 (aggressive)
FibonacciMultiplier = 2 (iteration 3)
SuccessMomentumMultiplier = 0.9 (1 success)
DiceRiskMultiplier = 2.5 (2 dice)
IterationPressureMultiplier = 1.1 (iteration 3)

MomentumStopChance = 0.05 × 2 × 0.9 × 2.5 × 1.1
                   = 0.2475 (24.75%)
```

## Cap Stop Chance Calculation

**Formula**:
```
if (currentScore < pointsCap):
    CapStopChance = 0%
else:
    pointsOverCap = currentScore - pointsCap
    growthSteps = pointsOverCap / CapGrowthInterval
    CapStopChance = BaseCapChance + (growthSteps × GrowthRate)
```

### Components:

#### 1. Base Cap Chance
- Chance to stop when exactly at the cap
- Example: 30% at exactly 500 points

#### 2. Growth Rate
- **Aggressive**: Slower growth (willing to go over cap)
- **Passive**: Faster growth (stops quickly when over cap)

#### 3. Growth Interval
- How many points before chance increases
- Example: Every 50 points over cap increases chance

### Example Calculation:

```
Current Score: 650
Points Cap: 500
Mode: Aggressive

pointsOverCap = 650 - 500 = 150
growthSteps = 150 / 50 = 3
CapStopChance = 0.3 + (3 × 0.05)
              = 0.45 (45%)
```

## Combined Probability

**Formula**: `CombinedStopChance = 1 - (1 - p1) × (1 - p2)`

This is the probability that **at least one** of the two independent events occurs.

### Example:
```
MomentumStopChance = 25%
CapStopChance = 45%

CombinedStopChance = 1 - (1 - 0.25) × (1 - 0.45)
                   = 1 - (0.75 × 0.55)
                   = 1 - 0.4125
                   = 0.5875 (58.75%)
```

## Decision Logic

The calculator performs **two independent random rolls**:

```csharp
// Roll 1: Momentum check
momentumRoll = Random(0, 1)
momentumStop = momentumRoll < MomentumStopChance

// Roll 2: Cap check
capRoll = Random(0, 1)
capStop = capRoll < CapStopChance

// Final decision: Stop if EITHER roll succeeds
ShouldStop = momentumStop OR capStop
```

### Why Two Independent Rolls?

This creates more dynamic behavior:
- AI can stop due to game state risk (momentum) even if score is low
- AI can stop due to score risk (cap) even if game state is favorable
- Both risks are evaluated independently

## Zonk Probability (Separate Calculation)

**Formula**: `ZonkProbability = (4/6)^diceCount × CombinationAdjustment`

### Logic:
- Each die has 4/6 chance of NOT being 1 or 5
- Multiply probabilities for all dice
- Adjust for combination possibilities (pairs, straights, etc.)

### Example:
```
3 dice remaining:
Base probability = (4/6)^3 = 0.296 (29.6%)
Combination adjustment = 0.85 (15% reduction for possible combinations)
Final Zonk probability = 0.296 × 0.85 = 0.252 (25.2%)
```

## What the Risk Calculator Does NOT Calculate

❌ Which dice to select (that's AICombinationStrategy)
❌ Optimal stopping point (it's probabilistic, not deterministic)
❌ Expected value of continuing (it's risk-based, not reward-based)
❌ Opponent's score or game state (it's turn-focused)

## What the Risk Calculator DOES Calculate

✓ Probability of stopping based on momentum (game state)
✓ Probability of stopping based on cap proximity (score)
✓ Combined probability using dual independent rolls
✓ Zonk probability for remaining dice
✓ Dynamic risk that changes with each selection

## Key Insight

The risk calculator doesn't make the decision directly. It calculates probabilities and then **rolls the dice** (literally) to decide. This creates:

- **Variability**: Same situation can have different outcomes
- **Realism**: Mimics human decision-making uncertainty
- **Balance**: Aggressive mode takes more risks but isn't suicidal
- **Dynamics**: Each turn feels different even with similar states

## Current Issue with Aggressive Mode

**Problem**: The aggressive strategy ignores the `ShouldStop` decision and continues anyway.

**Location**: AIAggressiveRerollStrategy.cs line ~141

**Impact**: Risk calculator runs but its decision is ignored, making the AI always run to iteration limit (5 hot streaks) instead of stopping when risk is high.
