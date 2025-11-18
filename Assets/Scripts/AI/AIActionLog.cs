using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays AI actions in a scrollable text log for player visibility
/// </summary>
public class AIActionLog : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI logText; // Or use Text if not using TextMeshPro
    public ScrollRect scrollRect;
    
    [Header("Settings")]
    public int maxLogLines = 50;
    public bool autoScroll = true;
    public Color aiColor = Color.cyan;
    public Color playerColor = Color.white;
    public Color systemColor = Color.yellow;
    
    private Queue<string> logLines = new Queue<string>();
    
    void Start()
    {
        if (logText == null)
        {
            Debug.LogWarning("AIActionLog: No text component assigned!");
        }
        
        ClearLog();
    }
    
    /// <summary>
    /// Adds an AI action to the log
    /// </summary>
    public void LogAIAction(string message)
    {
        AddLogLine($"<color=#{ColorUtility.ToHtmlStringRGB(aiColor)}>[AI] {message}</color>");
    }
    
    /// <summary>
    /// Adds a player action to the log
    /// </summary>
    public void LogPlayerAction(string message)
    {
        AddLogLine($"<color=#{ColorUtility.ToHtmlStringRGB(playerColor)}>[Player] {message}</color>");
    }
    
    /// <summary>
    /// Adds a system message to the log
    /// </summary>
    public void LogSystem(string message)
    {
        AddLogLine($"<color=#{ColorUtility.ToHtmlStringRGB(systemColor)}>[System] {message}</color>");
    }
    
    /// <summary>
    /// Adds a raw line to the log
    /// </summary>
    void AddLogLine(string line)
    {
        logLines.Enqueue(line);
        
        // Remove old lines if exceeding max
        while (logLines.Count > maxLogLines)
        {
            logLines.Dequeue();
        }
        
        UpdateLogDisplay();
    }
    
    /// <summary>
    /// Updates the text display with all log lines
    /// </summary>
    void UpdateLogDisplay()
    {
        if (logText == null) return;
        
        logText.text = string.Join("\n", logLines);
        
        // Auto-scroll to bottom
        if (autoScroll && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    /// <summary>
    /// Clears all log lines
    /// </summary>
    public void ClearLog()
    {
        logLines.Clear();
        if (logText != null)
            logText.text = "";
    }
    
    /// <summary>
    /// Logs AI turn start
    /// </summary>
    public void LogAITurnStart(int turnNumber)
    {
        LogSystem($"=== AI Turn {turnNumber} ===");
    }
    
    /// <summary>
    /// Logs AI dice roll
    /// </summary>
    public void LogAIDiceRoll(List<int> dice)
    {
        LogAIAction($"Rolled: [{string.Join(", ", dice)}]");
    }
    
    /// <summary>
    /// Logs AI combination selection
    /// </summary>
    public void LogAICombination(string combinationName, int points, int diceUsed)
    {
        LogAIAction($"Selected: {combinationName} ({diceUsed} dice, {points} pts)");
    }
    
    /// <summary>
    /// Logs AI decision (continue or stop)
    /// </summary>
    public void LogAIDecision(bool continues, string reason)
    {
        if (continues)
            LogAIAction($"Continues: {reason}");
        else
            LogAIAction($"Stops: {reason}");
    }
    
    /// <summary>
    /// Logs AI hot streak
    /// </summary>
    public void LogAIHotStreak()
    {
        LogAIAction("🔥 HOT STREAK! All dice used - continuing with 6 new dice");
    }
    
    /// <summary>
    /// Logs AI zonk
    /// </summary>
    public void LogAIZonk()
    {
        LogAIAction("💀 ZONK! No valid combinations - turn ends with 0 points");
    }
    
    /// <summary>
    /// Logs AI turn end
    /// </summary>
    public void LogAITurnEnd(int totalPoints, int iterations)
    {
        LogAIAction($"Turn complete: {totalPoints} points ({iterations} iterations)");
        LogSystem("---");
    }
    
    /// <summary>
    /// Logs player turn start
    /// </summary>
    public void LogPlayerTurnStart(int turnNumber)
    {
        LogSystem($"=== Player Turn {turnNumber} ===");
    }
    
    /// <summary>
    /// Logs player combination
    /// </summary>
    public void LogPlayerCombination(string combinationName, int points)
    {
        LogPlayerAction($"Submitted: {combinationName} ({points} pts)");
    }
    
    /// <summary>
    /// Logs player turn end
    /// </summary>
    public void LogPlayerTurnEnd(int totalPoints)
    {
        LogPlayerAction($"Turn complete: {totalPoints} points");
        LogSystem("---");
    }
}
