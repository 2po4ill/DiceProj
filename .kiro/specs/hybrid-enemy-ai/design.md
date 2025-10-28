# Hybrid Enemy AI Design Document

## Overview

The Hybrid Enemy AI system implements an adaptive opponent that switches between aggressive and conservative strategies based on the current game state. The AI uses strategic combination selection, risk assessment, and iterative rerolling to maximize points per turn while managing Zonk risk.

## Architecture

### Core Components

```
HybridEnemyAI
├── AIGameStateAnalyzer (analyzes lead/behind state)
├── AICombinationStrategy (classifies and prioritizes combinations)
├── AIDecisionEngine (makes continue/stop decisions)
├── AIDiceGenerator (non-physics dice creation)
├── AITurnExecutor (manages turn flow)
└── AIRiskCalculator (probability assessments)
```

### Integration Points

- **GameTurnManager**: Turn switching and flow control
- **TurnScoreManager**: Shared scoring system
- **DiceCombinationDetector**: Shared combination validation
- **ScoreUIManager**: AI-specific UI updates

## Components and Interfaces

### AIGameStateAnalyzer

**Purpose**: Determines current game state and appropriate behavior mode

**Key Methods**:
- `AnalyzeGameState()`: Returns LEADING, BEHIND, or CLOSE
- `GetScoreDifference()`: Calculates point gap
- `DetermineBehaviorMode()`: Returns AGGRESSIVE or CONSERVATIVE
- `GetPointsPerTurnCap()`: Sets target based on game state

**Two-State Hybrid Logic**:
```
State Switching Logic:
AI Score - Player Score > +100: PASSIVE State
AI Score - Player Score < -100: AGGRESSIVE State
±100 buffer zone: Maintain current state

AGGRESSIVE State Characteristics:
- High combination value threshold (starts at 80% of max possible)
- Threshold reduces by 20% per dice count reduction
- Maximum risk tolerance with momentum system
- Points per turn cap: 400-600 points
- Aims for absolute maximum points possible

PASSIVE State Characteristics:  
- Low combination value threshold (starts at 40% of max possible)
- Threshold reduces by 10% per dice count reduction
- High stop probability with 2 dice (60-80% first iteration)
- Points per turn cap: 200-300 points
- Efficient scoring with risk management
```

### AICombinationStrategy

**Purpose**: Evaluates combinations hierarchically and determines optimal selection based on AI state

**Hierarchical Combination Evaluation**:
```
TIER 1 (Highest Priority - 100% value):
- Six of Kind (6 dice) - 500+ points
- Large Straight (5 dice) - 400+ points
- Full House (5 dice) - 300+ points

TIER 2 (High Priority - 80% value):
- Four of Kind (4 dice) - 200+ points
- Middle Straight (4 dice) - 150+ points

TIER 3 (Medium Priority - 60% value):
- Three of Kind (3 dice) - 100+ points
- Two Pair (4 dice) - 80+ points

TIER 4 (Low Priority - 40% value):
- Single Pair (2 dice) - 20+ points
- Low Straight (3 dice) - 30+ points

TIER 5 (Minimal Priority - 20% value):
- Single One/Five (1 dice) - 10+ points
```

**State-Based Selection Logic**:
```
AGGRESSIVE State:
1. Search from Tier 1 down to current threshold tier
2. If found above threshold → Take if dice usage efficient
3. If not found → Take minimum dice combination from any tier
4. Threshold starts at 80%, reduces 20% per dice count drop

PASSIVE State:
1. Search from Tier 1 down to current threshold tier  
2. If found above threshold → Take immediately
3. If not found → Take best available combination
4. Threshold starts at 40%, reduces 10% per dice count drop
```

### AIDecisionEngine

**Purpose**: Makes continue/stop decisions based on multiple factors

**Decision Matrix**:
```
Factors Considered:
├── Current turn score vs points cap
├── Remaining dice count
├── Zonk probability
├── Iteration count vs threshold
├── Behavior mode (aggressive/conservative)
└── Score pressure (behind/leading)
```

**Decision Algorithm**:
```
IF (turn_score >= points_cap) → STOP
ELSE IF (iterations >= max_iterations) → STOP  
ELSE IF (zonk_probability > risk_threshold) → STOP
ELSE IF (remaining_dice <= 1 AND conservative) → STOP
ELSE → CONTINUE
```

### AIDiceGenerator

**Purpose**: Generates dice results without physics simulation

**Generation Methods**:
- `GenerateRandomDice(count)`: Pure random generation
- `GenerateWithBias(count, target_combination)`: Slightly weighted for testing
- `ValidateGeneration(dice_values)`: Ensures realistic results

**Performance Requirements**:
- Instant generation (no animation delays)
- Same probability distribution as physical dice
- Integration with existing combination detection

### AIRiskCalculator

**Purpose**: Calculates dual probability system for decision making using momentum and cap-based calculations

**Dual Probability System**:
```csharp
public class AIStopDecision
{
    public float MomentumStopChance;
    public float CapStopChance;
    public float CombinedStopChance;
    public bool ShouldStop;
}

AIStopDecision CalculateStopDecision(int iteration, int diceCount, int successCount, 
                                   int currentTurnScore, int pointsPerTurnCap, bool isAggressive)
{
    var decision = new AIStopDecision();
    
    // Calculate momentum-based stop chance
    decision.MomentumStopChance = CalculateMomentumStopChance(iteration, diceCount, successCount, isAggressive);
    
    // Calculate cap-based stop chance
    decision.CapStopChance = CalculateCapStopChance(currentTurnScore, pointsPerTurnCap, isAggressive);
    
    // Combined probability: 1 - (1-p1) * (1-p2)
    decision.CombinedStopChance = 1f - (1f - decision.MomentumStopChance) * (1f - decision.CapStopChance);
    
    // Perform two independent rolls
    bool momentumRoll = Random.Range(0f, 1f) < decision.MomentumStopChance;
    bool capRoll = Random.Range(0f, 1f) < decision.CapStopChance;
    
    decision.ShouldStop = momentumRoll || capRoll;
    
    return decision;
}

float CalculateCapStopChance(int currentScore, int pointsCap, bool isAggressive)
{
    if (currentScore < pointsCap) return 0f;
    
    int pointsOverCap = currentScore - pointsCap;
    float baseCapChance = 0.30f; // 30% at exactly cap
    
    // Different growth rates for states
    float growthRate = isAggressive ? 0.10f : 0.20f; // Aggressive grows slower
    float capChance = baseCapChance + (pointsOverCap / 50f) * growthRate;
    
    return Mathf.Min(capChance, 0.80f); // Cap at 80%
}
```

**Example Progressions**:
```
Aggressive AI Hot Streak (3 successes):
Iteration 2, 4 dice: 10% × 0.64 × 1.0 × 1.0 = 6.4%
Iteration 3, 2 dice: 20% × 0.64 × 1.3 × 1.2 = 19.9%
Iteration 4, 1 dice: 30% × 0.64 × 1.9 × 1.4 = 50.7%

Conservative AI Cold Streak (0 successes):
Iteration 2, 3 dice: 15% × 1.0 × 1.0 × 1.0 = 15%
Iteration 3, 2 dice: 30% × 1.0 × 1.3 × 1.2 = 46.8%
Iteration 4, 2 dice: 45% × 1.0 × 1.3 × 1.4 = 81.9%
```

## Data Models

### AITurnState
```csharp
public class AITurnState
{
    public int CurrentTurnScore;
    public int PointsPerTurnCap;
    public int IterationCount;
    public int MaxIterations;
    public BehaviorMode CurrentMode;
    public List<int> CurrentDice;
    public List<CombinationResult> CompletedCombinations;
    public int SuccessfulCombinationsCount; // For momentum calculation
    public float ZonkProbability;
    public float CurrentStopChance; // For debugging/display
}
```

### AIConfiguration
```csharp
public class AIConfiguration
{
    // State-based settings
    public int PointsCapAggressive = 500;
    public int PointsCapPassive = 250;
    
    // Dynamic Buffer System
    public int InitialBufferCap = 200;
    public int BufferReductionPerRound = 20;
    public int RoundsPerReduction = 3;
    public int MinimumBufferCap = 50;
    
    // Momentum System Parameters
    public float AggressiveBaseMultiplier = 0.10f;
    public float PassiveBaseMultiplier = 0.15f;
    public float MomentumReductionPerSuccess = 0.12f;
    public float MinimumMomentumMultiplier = 0.25f;
    public float DiceRiskExponent = 2f;
    public float DiceRiskMultiplier = 0.3f;
    public float IterationPressureIncrease = 0.2f;
    public float MaxMomentumStopChance = 0.90f;
    
    // Cap Probability System
    public float BaseCapStopChance = 0.30f;
    public float AggressiveCapGrowthRate = 0.10f; // Slower growth
    public float PassiveCapGrowthRate = 0.20f;    // Faster growth
    public int CapGrowthInterval = 50;            // Points per growth step
    public float MaxCapStopChance = 0.80f;
}
```

## Error Handling

### Zonk Scenarios
- AI follows same Zonk rules as player
- All turn progress lost, multipliers reset
- Automatic turn end with appropriate UI feedback

### Invalid Combinations
- Validation using existing DiceCombinationDetector
- Fallback to safe combination if generation fails
- Logging for debugging invalid states

### Performance Safeguards
- Maximum turn time limit (10 seconds)
- Forced turn end if AI gets stuck in loops
- Error recovery to prevent game freezing

## Testing Strategy

### Unit Tests
- AIGameStateAnalyzer behavior mode selection
- AICombinationStrategy classification accuracy
- AIRiskCalculator probability calculations
- AIDiceGenerator distribution validation

### Integration Tests
- Full AI turn execution flow
- Integration with existing game systems
- UI updates during AI turns
- Score management consistency

### Behavioral Tests
- AI performance in different game states
- Risk/reward decision validation
- Turn completion time measurements
- Player experience and challenge level

### Balance Testing
- Win rate analysis (target: 45-55% AI wins)
- Average game length measurement
- Player engagement metrics
- Difficulty curve validation

## Performance Considerations

### Optimization Targets
- AI turn completion: < 5 seconds average
- Decision calculation: < 100ms per decision
- Memory usage: Minimal additional overhead
- UI responsiveness: No blocking operations

### Scalability
- Configurable difficulty parameters
- Modular design for future AI personalities
- Easy tuning of risk thresholds and caps
- Support for multiple AI opponents (future)

## Future Enhancements

### Planned Extensions
- Multiple AI personalities (Cautious, Balanced, Reckless)
- Learning AI that adapts to player behavior
- Tournament mode with bracket-style AI opponents
- Difficulty scaling based on player performance

### Configuration Options
- Player-selectable AI difficulty
- Custom risk threshold settings
- Adjustable AI speed (instant vs animated)
- Debug mode for AI decision visibility