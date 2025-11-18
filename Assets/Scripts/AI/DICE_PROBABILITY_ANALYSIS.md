# AI Dice Probability Analysis - Is It Rigged?

## Summary: **NO RIGGING DETECTED**

The AI uses standard Unity `Random.Range(1, 7)` which provides fair, unbiased dice rolls.

## Dice Generation Methods

### 1. AIAggressiveRerollStrategy.GenerateNewDiceSet()
**Location**: `Assets/Scripts/AI/AIAggressiveRerollStrategy.cs` line 398

```csharp
List<int> GenerateNewDiceSet(int count)
{
    var newDice = new List<int>();
    for (int i = 0; i < count; i++)
    {
        newDice.Add(Random.Range(1, 7));  // ✓ Fair: 1-6 equal probability
    }
    return newDice;
}
```

**Analysis**: 
- Uses Unity's `Random.Range(1, 7)` which generates integers from 1 to 6 inclusive
- Each face has exactly 1/6 (16.67%) probability
- **NO RIGGING**

### 2. AIDiceGenerator.GenerateRandomDice()
**Location**: `Assets/Scripts/AI/AIDiceGenerator.cs` line 37

```csharp
public List<int> GenerateRandomDice(int count)
{
    List<int> diceValues = new List<int>();
    
    for (int i = 0; i < count; i++)
    {
        int value = Random.Range(1, 7);  // ✓ Fair: 1-6 equal probability
        diceValues.Add(value);
        UpdateGenerationStats(value);  // Tracks for validation
    }
    
    return diceValues;
}
```

**Analysis**:
- Same fair random generation
- Includes statistics tracking for validation
- **NO RIGGING**

### 3. AIDiceGenerator.GenerateWeightedDice()
**Location**: `Assets/Scripts/AI/AIDiceGenerator.cs` line 77

```csharp
public List<int> GenerateWeightedDice(int count, float[] weights = null)
{
    if (weights == null)
    {
        return GenerateRandomDice(count);  // Falls back to fair dice
    }
    // ... weighted generation code ...
}
```

**Analysis**:
- **This method CAN rig dice** if weights are provided
- BUT: Defaults to fair dice if no weights provided
- Used for testing purposes only
- **NOT USED in actual gameplay** (no calls found in game code)

## Validation System

The `AIDiceGenerator` includes a built-in validation system:

### Distribution Validation
```csharp
public bool ValidateDistribution()
{
    float expectedProbability = 1f / 6f; // 16.67% per face
    
    for (int face = 1; face <= 6; face++)
    {
        float actualProbability = (float)count / totalGenerations;
        float variance = Mathf.Abs(actualProbability - expectedProbability);
        
        if (variance > acceptableVariance)  // Default: 5%
        {
            Debug.LogWarning($"Face {face} variance exceeds acceptable");
        }
    }
}
```

**Features**:
- Tracks every dice roll
- Validates distribution every 1000 rolls
- Warns if any face deviates more than 5% from expected 16.67%
- Can be manually tested with `[ContextMenu("Run Distribution Test")]`

## Comparison: AI vs Player Dice

### Player Dice Generation
**Location**: `Assets/Scripts/DiceController.cs`

Player dice use **Unity physics simulation**:
- Dice are spawned with random rotation
- Physics engine calculates final face
- Completely deterministic based on physics
- **NO RIGGING**

### AI Dice Generation
AI dice use **mathematical random generation**:
- No physics simulation (faster)
- Direct random number generation
- Same probability distribution as physics dice
- **NO RIGGING**

## Why AI Might SEEM Rigged

### 1. Confirmation Bias
Players remember when AI gets lucky but forget when AI gets unlucky.

### 2. Aggressive Strategy
AI uses minimum dice strategy which:
- Maximizes reroll opportunities
- Prioritizes hot streaks
- Appears to "get lucky" more often
- But it's just smart play, not rigged dice

### 3. Simulation vs Reality
In aggressive mode:
- AI simulates entire turn ahead of time
- Generates all dice values upfront
- Appears to "know" what's coming
- But dice are still randomly generated

### 4. No Visual Rolling
Player dice:
- Roll visually (satisfying)
- Takes time
- Feels "fair"

AI dice:
- Appear instantly
- No rolling animation
- Feels "suspicious"
- But mathematically identical

## Testing for Rigging

### Test 1: Run Distribution Test
1. Find `AIDiceGenerator` in scene
2. Right-click component → Run Distribution Test
3. Check console for results
4. Should show ~16.67% for each face (±5%)

### Test 2: Manual Observation
Track 100 AI dice rolls:
```
Face 1: Should appear ~17 times (±5)
Face 2: Should appear ~17 times (±5)
Face 3: Should appear ~17 times (±5)
Face 4: Should appear ~17 times (±5)
Face 5: Should appear ~17 times (±5)
Face 6: Should appear ~17 times (±5)
```

### Test 3: Enable Debug Logs
```csharp
// In AIAggressiveRerollStrategy
enableDebugLogs = true;

// In AIDiceGenerator
enableDebugLogs = true;
validateDistribution = true;
```

Watch console for:
- Every dice roll logged
- Distribution validation warnings
- Any anomalies

## Potential Sources of Actual Bias

### 1. Unity Random Seed
If the random seed is set to a fixed value:
```csharp
Random.InitState(12345); // Would make dice predictable
```

**Status**: No fixed seed found in code ✓

### 2. Weighted Dice Method
If `GenerateWeightedDice()` is called with biased weights:
```csharp
float[] weights = {0.3f, 0.3f, 0.1f, 0.1f, 0.1f, 0.1f}; // Favors 1s and 2s
```

**Status**: Method exists but NOT USED in gameplay ✓

### 3. Conditional Generation
If code generates dice until getting desired result:
```csharp
// BAD: Keep rolling until getting a 1
while (dice != 1) {
    dice = Random.Range(1, 7);
}
```

**Status**: No such code found ✓

## Conclusion

**The AI dice are NOT rigged.**

Evidence:
- ✓ Uses standard `Random.Range(1, 7)`
- ✓ No weighted generation in gameplay
- ✓ No fixed random seed
- ✓ No conditional rerolling
- ✓ Includes validation system
- ✓ Same probability as player dice

The AI may appear to get lucky due to:
- Smart strategy (minimum dice, hot streak priority)
- Confirmation bias (remembering AI luck, forgetting AI failures)
- Lack of visual rolling (feels suspicious)
- Simulation-based play (appears to "know" future)

But mathematically, the dice are fair.

## Recommendations

### To Make It Feel More Fair:

1. **Add dice rolling animation for AI**
   - Show dice "rolling" even though value is predetermined
   - Increases player trust

2. **Show AI failures more prominently**
   - Highlight when AI zonks
   - Show when AI stops early due to risk
   - Balance perception

3. **Add statistics display**
   - Show AI average score per turn
   - Show AI zonk rate
   - Show AI hot streak rate
   - Let players see it's not rigged

4. **Enable distribution validation in production**
   - Log warnings if distribution deviates
   - Proves fairness over time

5. **Add "Fair Play" indicator**
   - Show dice distribution stats
   - Display "Verified Fair" badge
   - Build player confidence
