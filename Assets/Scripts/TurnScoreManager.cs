using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TurnScoreData
{
    public int turnNumber;
    public int baseScore;
    public float turnMultiplier;
    public int finalScore;
    public List<CombinationResult> combinations;
    
    public TurnScoreData(int turn)
    {
        turnNumber = turn;
        baseScore = 0;
        turnMultiplier = 1f;
        finalScore = 0;
        combinations = new List<CombinationResult>();
    }
}

public class TurnScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    public float baseTurnMultiplier = 1f;
    public float consecutiveTurnBonus = 0f; // Disabled - no bonus for consecutive turns
    public int minimumScoreThreshold = 50; // Minimum score to get bonuses
    
    [Header("Current Turn")]
    public TurnScoreData currentTurn;
    
    [Header("Game Stats")]
    public int totalGameScore = 0;
    public int consecutiveSuccessfulTurns = 0;
    public List<TurnScoreData> turnHistory = new List<TurnScoreData>();
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    public void StartNewTurn(int turnNumber)
    {
        currentTurn = new TurnScoreData(turnNumber);
        Debug.Log($"✅ TurnScoreManager.StartNewTurn({turnNumber}) - currentTurn created");
        
        // Calculate turn multiplier based on consecutive successes
        currentTurn.turnMultiplier = baseTurnMultiplier + (consecutiveSuccessfulTurns * consecutiveTurnBonus);
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== TURN {turnNumber} SCORING STARTED ===");
            Debug.Log($"Turn Multiplier: {currentTurn.turnMultiplier:F2}x (Base: {baseTurnMultiplier}, Bonus: {consecutiveSuccessfulTurns * consecutiveTurnBonus:F2})");
        }
    }
    
    public void AddCombination(CombinationResult combination)
    {
        Debug.Log($"🔍 AddCombination called. currentTurn null? {currentTurn == null}");
        
        if (currentTurn == null) 
        {
            Debug.LogError("❌ TurnScoreManager.AddCombination: currentTurn is NULL! StartNewTurn() was not called.");
            return;
        }
        
        Debug.Log($"🔍 currentTurn exists. combinations null? {currentTurn.combinations == null}");
        
        if (currentTurn.combinations == null)
        {
            Debug.LogError("❌ TurnScoreManager.AddCombination: currentTurn.combinations is NULL! Constructor failed?");
            currentTurn.combinations = new List<CombinationResult>();
        }
        
        currentTurn.combinations.Add(combination);
        currentTurn.baseScore += combination.points;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Added combination: {combination.rule} (+{combination.points} points)");
            Debug.Log($"Turn base score: {currentTurn.baseScore}");
        }
    }
    
    public int CalculateFinalTurnScore()
    {
        if (currentTurn == null) return 0;
        
        // Apply turn multiplier to base score
        currentTurn.finalScore = Mathf.RoundToInt(currentTurn.baseScore * currentTurn.turnMultiplier);
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== TURN {currentTurn.turnNumber} SCORING COMPLETE ===");
            Debug.Log($"Base Score: {currentTurn.baseScore}");
            Debug.Log($"Turn Multiplier: {currentTurn.turnMultiplier:F2}x");
            Debug.Log($"Final Turn Score: {currentTurn.finalScore}");
        }
        
        return currentTurn.finalScore;
    }
    
    public void CompleteTurn()
    {
        if (currentTurn == null) return;
        
        int finalScore = CalculateFinalTurnScore();
        totalGameScore += finalScore;
        
        // Update consecutive turn bonus
        if (finalScore >= minimumScoreThreshold)
        {
            consecutiveSuccessfulTurns++;
        }
        else
        {
            consecutiveSuccessfulTurns = 0; // Reset streak
        }
        
        // Store turn in history
        turnHistory.Add(currentTurn);
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== TURN COMPLETED ===");
            Debug.Log($"Total Game Score: {totalGameScore}");
            Debug.Log($"Consecutive Successful Turns: {consecutiveSuccessfulTurns}");
            Debug.Log($"Next Turn Multiplier: {baseTurnMultiplier + (consecutiveSuccessfulTurns * consecutiveTurnBonus):F2}x");
        }
        
        currentTurn = null;
    }
    
    public void ZonkTurn()
    {
        if (currentTurn == null) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== ZONK! TURN {currentTurn.turnNumber} ===");
            Debug.Log($"All progress lost! Turn score: 0, Multipliers reset!");
            Debug.Log($"Previous turn score: {currentTurn.baseScore} (lost)");
        }
        
        // Zonk: lose all turn progress, reset multipliers
        currentTurn.baseScore = 0;
        currentTurn.finalScore = 0;
        currentTurn.turnMultiplier = 0f;
        
        // Reset consecutive streak (harsh penalty)
        consecutiveSuccessfulTurns = 0;
        
        // Add Zonk to combination history for record
        var zonkResult = new CombinationResult(Rule.Zonk, 0, "ZONK - All progress lost!", 0f);
        currentTurn.combinations.Add(zonkResult);
        
        // Store turn in history (as a failed turn)
        turnHistory.Add(currentTurn);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Consecutive streak reset to 0");
            Debug.Log($"Next Turn Multiplier: {baseTurnMultiplier:F2}x (back to base)");
        }
        
        currentTurn = null;
    }
    
    public float GetCurrentTurnMultiplier()
    {
        return currentTurn?.turnMultiplier ?? baseTurnMultiplier;
    }
    
    public int GetCurrentTurnScore()
    {
        return currentTurn?.baseScore ?? 0;
    }
    
    public int GetProjectedFinalScore()
    {
        if (currentTurn == null) return 0;
        return Mathf.RoundToInt(currentTurn.baseScore * currentTurn.turnMultiplier);
    }
    
    // Future expansion methods
    public void ApplySpecialMultiplier(float multiplier, string reason)
    {
        if (currentTurn == null) return;
        
        currentTurn.turnMultiplier *= multiplier;
        
        if (enableDebugLogs)
            Debug.Log($"Applied special multiplier: {multiplier:F2}x ({reason}). New turn multiplier: {currentTurn.turnMultiplier:F2}x");
    }
    
    public void AddBonusPoints(int bonusPoints, string reason)
    {
        if (currentTurn == null) return;
        
        currentTurn.baseScore += bonusPoints;
        
        if (enableDebugLogs)
            Debug.Log($"Added bonus points: +{bonusPoints} ({reason}). Turn base score: {currentTurn.baseScore}");
    }
    
    // Analytics methods
    public float GetAverageScorePerTurn()
    {
        if (turnHistory.Count == 0) return 0f;
        return (float)totalGameScore / turnHistory.Count;
    }
    
    public int GetHighestTurnScore()
    {
        int highest = 0;
        foreach (var turn in turnHistory)
        {
            if (turn.finalScore > highest)
                highest = turn.finalScore;
        }
        return highest;
    }
    
    /// <summary>
    /// Resets the score manager for a new game (used in AI vs Player mode)
    /// </summary>
    public void ResetForNewGame()
    {
        currentTurn = null;
        totalGameScore = 0;
        consecutiveSuccessfulTurns = 0;
        turnHistory.Clear();
        
        if (enableDebugLogs)
        {
            Debug.Log("TurnScoreManager: Reset for new game");
        }
    }
}