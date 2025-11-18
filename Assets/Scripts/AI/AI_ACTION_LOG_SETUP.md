# AI Action Log Setup Guide

## What It Does

Displays AI actions in a scrollable text log so players can see what the AI is thinking and doing during its turn.

## Setup Steps

### 1. Create UI Elements

In your Unity scene:

1. **Create ScrollView**:
   - Right-click in Hierarchy → UI → Scroll View
   - Name it "AI Action Log"
   - Position it where you want (e.g., right side of screen)

2. **Configure ScrollView**:
   - Set Scroll Rect → Vertical: ✓ (checked)
   - Set Scroll Rect → Horizontal: ✗ (unchecked)
   - Adjust size (e.g., 300x400)

3. **Configure Content**:
   - Select "Content" child object
   - Add Component → Layout Element
   - Set Preferred Height: 1000 (or higher)
   - Add Component → Vertical Layout Group
   - Set Child Alignment: Upper Left
   - Set Padding: 10 on all sides

4. **Add Text Component**:
   - Select "Content" object
   - Add child: Right-click → UI → Text - TextMeshPro (or Text)
   - Name it "Log Text"
   - Set Anchor: Stretch/Stretch (fill parent)
   - Set Alignment: Top Left
   - Set Font Size: 14
   - Set Color: White
   - Enable Rich Text: ✓

### 2. Add AIActionLog Component

1. Create empty GameObject in scene
2. Name it "AI Action Log Manager"
3. Add Component → AI Action Log (script)
4. Assign references:
   - Log Text: Drag the "Log Text" TextMeshPro component
   - Scroll Rect: Drag the "AI Action Log" ScrollView component

### 3. Connect to AITurnExecutor

1. Find your AITurnExecutor GameObject
2. In Inspector, find "Action Log" field
3. Drag the "AI Action Log Manager" GameObject to this field

### 4. Optional: Customize Colors

In AI Action Log component:
- AI Color: Cyan (default) - color for AI messages
- Player Color: White (default) - color for player messages
- System Color: Yellow (default) - color for system messages
- Max Log Lines: 50 (default) - how many lines to keep
- Auto Scroll: ✓ (default) - automatically scroll to bottom

## What Gets Logged

### AI Turn Actions:
- "Rolled: [1, 2, 3, 4, 5, 6]"
- "Selected: Three of a Kind (3 dice, 300 pts)"
- "🔥 HOT STREAK! All dice used - continuing with 6 new dice"
- "Continues: 3 dice remaining"
- "Stops: Risk too high (65% stop chance)"
- "Turn complete: 450 points (2 iterations)"

### Player Turn Actions:
- "Submitted: Pair of 5s (100 pts)"
- "Turn complete: 250 points"

### System Messages:
- "=== AI Turn 3 ==="
- "=== Player Turn 4 ==="
- "---" (separator)

## Example Output

```
=== Player Turn 1 ===
[Player] Submitted: Single One (100 pts)
[Player] Submitted: Single Five (50 pts)
[Player] Turn complete: 150 points
---
=== AI Turn 1 ===
[AI] Rolled: [2, 2, 4, 4, 6, 6]
[AI] Selected: Three Pairs (6 dice, 1500 pts)
[AI] 🔥 HOT STREAK! All dice used - continuing with 6 new dice
[AI] Rolled: [1, 3, 4, 5, 6, 6]
[AI] Selected: Single One (1 dice, 100 pts)
[AI] Stops: Risk too high (45% stop chance)
[AI] Turn complete: 1600 points (2 iterations)
---
```

## Advanced: Add to GameTurnManager

To also log player actions, add to GameTurnManager:

```csharp
[Header("UI")]
public AIActionLog actionLog;

// In StartNewTurn():
if (actionLog != null)
{
    if (isAITurn)
        actionLog.LogAITurnStart(currentTurn);
    else
        actionLog.LogPlayerTurnStart(currentTurn);
}

// In ProcessCombination():
if (actionLog != null)
{
    actionLog.LogPlayerCombination(result.description, result.points);
}

// In EndTurn():
if (actionLog != null && !isAITurn)
{
    actionLog.LogPlayerTurnEnd(scoreManager.GetCurrentTurnScore());
}
```

## Styling Tips

### Make it look good:
1. Add background panel behind ScrollView (dark semi-transparent)
2. Add title text above: "Game Log" or "Action History"
3. Use monospace font for better alignment
4. Add subtle border around ScrollView
5. Consider adding clear button to reset log

### Color scheme suggestions:
- **AI Color**: #00FFFF (cyan) - tech/robotic feel
- **Player Color**: #FFFFFF (white) - neutral
- **System Color**: #FFFF00 (yellow) - important info
- **Hot Streak**: Use emoji 🔥 or color #FF6600 (orange)
- **Zonk**: Use emoji 💀 or color #FF0000 (red)

## Performance Notes

- Max log lines prevents memory issues (default 50)
- Old lines are automatically removed
- Auto-scroll can be disabled if player wants to read history
- Text updates are batched (one update per action, not per character)

## Troubleshooting

**Log not showing?**
- Check that Log Text and Scroll Rect are assigned
- Verify AIActionLog component is active
- Check that actionLog is assigned in AITurnExecutor

**Text overflowing?**
- Enable "Overflow: Overflow" on TextMeshPro
- Or increase Content height in ScrollView

**Not auto-scrolling?**
- Check Auto Scroll is enabled in AIActionLog
- Verify Scroll Rect is assigned
- Make sure Content is taller than Viewport

**Colors not showing?**
- Enable "Rich Text" on TextMeshPro component
- Check that colors are not too dark to see
