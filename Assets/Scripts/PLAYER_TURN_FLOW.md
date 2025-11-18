# Player Turn Flow - UI Button Wiring

## UI Buttons

### 1. Submit Combination Button
**File**: `GameTurnManager.cs`
**Setup**: Line 60-65
```csharp
if (submitCombinationButton != null)
{
    submitCombinationButton.onClick.AddListener(SubmitCombination);
    submitCombinationButton.gameObject.SetActive(false);
}
```

### 2. End Turn Button
**File**: `GameTurnManager.cs`
**Setup**: Line 67-72
```csharp
if (endTurnButton != null)
{
    endTurnButton.onClick.AddListener(EndTurn);
    endTurnButton.gameObject.SetActive(false);
}
```

### 3. Continue Turn Button
**File**: `GameTurnManager.cs`
**Setup**: Line 74-79
```csharp
if (continueTurnButton != null)
{
    continueTurnButton.onClick.AddListener(ContinueTurn);
    continueTurnButton.gameObject.SetActive(false);
}
```

## Complete Player Turn Flow

### Phase 1: Turn Start
```
StartNewTurn() [Line 90]
├─ scoreManager.StartNewTurn(currentTurn)
├─ HideAllButtons()
└─ diceController.SpawnNewDice()
   └─ Spawns 6 dice
   └─ Calls OnDiceAligned() when done
```

### Phase 2: Dice Aligned
```
OnDiceAligned() [Line 135]
├─ Check HasValidCombinations()
│  ├─ YES: Show Submit Button
│  │  └─ If score > 0: Show End Turn Button
│  └─ NO: HandleZonk()
└─ waitingForSubmission = true
```

### Phase 3A: Player Submits Combination
```
SubmitCombination() [Line 163]
├─ Get selectedDice from diceSelector
├─ Get selectedValues from diceController
├─ Check combination with combinationDetector
├─ scoreManager.AddCombination(combination)
├─ RemoveSelectedDice()
├─ Check remaining dice count:
│  ├─ 0 dice: Hot Streak!
│  │  └─ SpawnNewDice() (6 fresh dice)
│  └─ >0 dice: Show choice
│     ├─ Show End Turn Button
│     └─ Show Continue Turn Button
│     └─ waitingForTurnChoice = true
```

### Phase 3B: Player Continues Turn
```
ContinueTurn() [Line 405]
├─ waitingForTurnChoice = false
├─ HideAllButtons()
└─ diceController.RollDiceFromUI()
   └─ Rerolls remaining dice
   └─ Calls OnDiceAligned() when done
   └─ LOOP back to Phase 2
```

### Phase 3C: Player Ends Turn
```
EndTurn() [Line 339]
├─ scoreManager.CompleteTurn()
├─ totalScore = scoreManager.totalGameScore
├─ HideAllButtons()
├─ If AI Opponent mode:
│  ├─ SwitchTurn() (switch to AI)
│  └─ StartNewTurn() (AI's turn)
└─ If Single Player:
   ├─ currentTurn++
   └─ StartNewTurn() (next player turn)
```

### Phase 4: Zonk Handling
```
HandleZonk() [Line 287]
├─ scoreManager.ZonkTurn() (score = 0)
├─ Show "ZONK!" message
└─ ZonkDelayThenEndTurn()
   └─ Wait 2 seconds
   └─ Call EndTurn()
```

## Button Visibility States

### State 1: Turn Start
```
Submit: Hidden
End Turn: Hidden
Continue: Hidden
```

### State 2: Dice Aligned (Valid Combinations)
```
Submit: Visible ✓
End Turn: Visible if score > 0 ✓
Continue: Hidden
```

### State 3: Combination Submitted (Dice Remaining)
```
Submit: Hidden
End Turn: Visible ✓
Continue: Visible ✓
```

### State 4: Hot Streak (All Dice Used)
```
Submit: Hidden
End Turn: Hidden
Continue: Hidden
(Automatically spawns new dice)
```

### State 5: Zonk
```
Submit: Hidden
End Turn: Hidden
Continue: Hidden
(Automatically ends turn after delay)
```

## Key Methods

### SubmitCombination() - Line 163
**Purpose**: Process player's selected dice combination
**Flow**:
1. Validate selection
2. Check combination validity
3. Add points to turn score
4. Remove selected dice
5. Check for hot streak or show choice buttons

### EndTurn() - Line 339
**Purpose**: End player's turn and switch to next turn
**Flow**:
1. Complete turn (finalize score)
2. Update total score
3. Hide all buttons
4. Switch turn (if AI mode)
5. Start new turn

### ContinueTurn() - Line 405
**Purpose**: Reroll remaining dice to continue turn
**Flow**:
1. Hide choice buttons
2. Reroll remaining dice
3. Wait for OnDiceAligned()

### OnDiceAligned() - Line 135
**Purpose**: Check for valid combinations after dice settle
**Flow**:
1. Check if any combinations exist
2. Show submit button if valid
3. Show end turn button if score > 0
4. Handle zonk if no combinations

## Issues to Fix

### Issue 1: End Turn Button Visibility
**Problem**: End turn button only shows if `score > 0`
**Location**: Line 152-153
```csharp
if (scoreManager.GetCurrentTurnScore() > 0 && endTurnButton != null)
    endTurnButton.gameObject.SetActive(true);
```
**Impact**: Player cannot end turn on first roll if they don't want to take any combination

**Fix**: Always show end turn button during player's turn
```csharp
if (!isAIOpponent || !isAITurn)
{
    if (endTurnButton != null)
        endTurnButton.gameObject.SetActive(true);
}
```

### Issue 2: Hot Streak Not Clearly Indicated
**Problem**: When all dice are used, new dice spawn automatically without clear indication
**Location**: Line 250-260
**Impact**: Player might not realize they got a hot streak

**Fix**: Add visual feedback before spawning new dice
```csharp
if (remainingDiceCount == 0)
{
    Debug.Log("🔥 HOT STREAK! All dice used - spawning fresh set!");
    // Show hot streak UI message
    yield return new WaitForSeconds(1.5f);
    diceController.SpawnNewDice();
}
```

### Issue 3: No Iteration Limit for Player
**Problem**: Player can continue indefinitely, unlike AI which has 5-iteration limit
**Location**: No limit implemented
**Impact**: Inconsistent rules between player and AI

**Fix**: Add iteration tracking for player
```csharp
private int playerIterationCount = 0;
private const int MAX_ITERATIONS = 5;

// In hot streak handling:
if (remainingDiceCount == 0)
{
    playerIterationCount++;
    if (playerIterationCount >= MAX_ITERATIONS)
    {
        Debug.Log("Maximum iterations reached! Turn ends.");
        EndTurn();
        return;
    }
    diceController.SpawnNewDice();
}
```

### Issue 4: Continue Button Shows After Hot Streak
**Problem**: After hot streak, choice buttons might show briefly before new dice spawn
**Location**: Line 270-275
**Impact**: Confusing UI state

**Fix**: Don't show choice buttons on hot streak
```csharp
if (remainingDiceCount == 0)
{
    // Hot streak - spawn new dice immediately
    diceController.SpawnNewDice();
}
else
{
    // Show choice buttons
    waitingForTurnChoice = true;
    ShowTurnChoiceButtons();
}
```

## Summary

The player turn flow is:
1. **Start** → Spawn 6 dice
2. **Aligned** → Show submit button
3. **Submit** → Remove dice, check remaining
4. **Choice** → End turn OR Continue (reroll)
5. **Hot Streak** → Auto-spawn 6 new dice (loop to step 2)
6. **Zonk** → Auto-end turn
7. **End** → Finalize score, switch turn

The main issues are:
- End turn button visibility logic
- Hot streak feedback
- No iteration limit for player
- Button state management during hot streaks
