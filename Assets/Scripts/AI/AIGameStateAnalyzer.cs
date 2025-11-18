using UnityEngine;
using HybridEnemyAI;

/// <summary>
/// Analyzes current game state and determines appropriate AI behavior mode
/// </summary>
public class AIGameStateAnalyzer : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("AI Configuration - Edit PointsCapAggressive and PointsCapPassive here!")]
    public AIConfiguration config = new AIConfiguration();
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private int currentRound = 1;
    private int currentBufferCap;
    
    void Start()
    {
        if (config == null)
        {
            config = new AIConfiguration();
            if (enableDebugLogs)
                Debug.Log("AIGameStateAnalyzer: Using default configuration");
        }
        
        currentBufferCap = config.InitialBufferCap;
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIGameStateAnalyzer initialized with caps: Aggressive={config.PointsCapAggressive}, Passive={config.PointsCapPassive}");
        }
    }
    
    /// <summary>
    /// Analyzes current game state and returns appropriate behavior mode
    /// </summary>
    public BehaviorMode AnalyzeGameState(int aiScore, int playerScore)
    {
        int scoreDifference = aiScore - playerScore;
        
        // Update buffer cap based on current round
        UpdateBufferCap();
        
        BehaviorMode newMode;
        
        if (scoreDifference > currentBufferCap)
        {
            newMode = BehaviorMode.PASSIVE;
        }
        else if (scoreDifference < -currentBufferCap)
        {
            newMode = BehaviorMode.AGGRESSIVE;
        }
        else
        {
            // Within buffer zone - maintain current state or default to aggressive
            newMode = BehaviorMode.AGGRESSIVE; // Default to aggressive for close games
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIGameStateAnalyzer: AI={aiScore}, Player={playerScore}, Diff={scoreDifference}");
            Debug.Log($"Buffer Cap: {currentBufferCap}, Mode: {newMode}");
        }
        
        return newMode;
    }
    
    /// <summary>
    /// Gets the score difference between AI and player
    /// </summary>
    public int GetScoreDifference(int aiScore, int playerScore)
    {
        return aiScore - playerScore;
    }
    
    /// <summary>
    /// Determines points per turn cap based on behavior mode
    /// </summary>
    public int GetPointsPerTurnCap(BehaviorMode mode)
    {
        switch (mode)
        {
            case BehaviorMode.AGGRESSIVE:
                return config.PointsCapAggressive;
            case BehaviorMode.PASSIVE:
                return config.PointsCapPassive;
            default:
                return config.PointsCapAggressive;
        }
    }
    
    /// <summary>
    /// Gets points per turn cap with lead preservation adjustments
    /// </summary>
    public int GetPointsPerTurnCapWithLeadPreservation(BehaviorMode mode, int aiScore, int playerScore)
    {
        int baseCap = GetPointsPerTurnCap(mode);
        
        if (mode == BehaviorMode.PASSIVE)
        {
            var leadAnalysis = AnalyzeLeadState(aiScore, playerScore);
            int adjustment = GetLeadPreservationCapAdjustment(leadAnalysis, mode);
            baseCap += adjustment; // adjustment is negative for reductions
            
            // Ensure minimum cap
            baseCap = Mathf.Max(baseCap, 100);
            
            if (enableDebugLogs && adjustment != 0)
            {
                Debug.Log($"Lead preservation cap adjustment: {adjustment}, Final cap: {baseCap}");
            }
        }
        
        return baseCap;
    }
    
    /// <summary>
    /// Gets combination value threshold based on mode and dice count
    /// </summary>
    public float GetCombinationThreshold(BehaviorMode mode, int diceCount)
    {
        float initialThreshold;
        float reductionRate;
        
        switch (mode)
        {
            case BehaviorMode.AGGRESSIVE:
                initialThreshold = config.AggressiveInitialThreshold;
                reductionRate = config.AggressiveThresholdReduction;
                break;
            case BehaviorMode.PASSIVE:
                initialThreshold = config.PassiveInitialThreshold;
                reductionRate = config.PassiveThresholdReduction;
                break;
            default:
                initialThreshold = config.AggressiveInitialThreshold;
                reductionRate = config.AggressiveThresholdReduction;
                break;
        }
        
        // Reduce threshold based on dice count (fewer dice = lower threshold)
        int diceReductions = 6 - diceCount;
        float threshold = initialThreshold - (diceReductions * reductionRate);
        
        // Ensure threshold doesn't go below 0.1 (10%)
        threshold = Mathf.Max(threshold, 0.1f);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Combination Threshold: {threshold:F2} (Mode: {mode}, Dice: {diceCount})");
        }
        
        return threshold;
    }
    
    /// <summary>
    /// Updates buffer cap based on current round (tightens over time)
    /// </summary>
    void UpdateBufferCap()
    {
        int reductionCycles = (currentRound - 1) / config.RoundsPerReduction;
        int totalReduction = reductionCycles * config.BufferReductionPerRound;
        
        currentBufferCap = Mathf.Max(
            config.InitialBufferCap - totalReduction,
            config.MinimumBufferCap
        );
        
        if (enableDebugLogs)
        {
            Debug.Log($"Round {currentRound}: Buffer Cap = {currentBufferCap}");
        }
    }
    
    /// <summary>
    /// Advances to next round (call this when both players complete a turn)
    /// </summary>
    public void AdvanceRound()
    {
        currentRound++;
        UpdateBufferCap();
        
        if (enableDebugLogs)
        {
            Debug.Log($"Advanced to Round {currentRound}, New Buffer Cap: {currentBufferCap}");
        }
    }
    
    /// <summary>
    /// Gets current buffer cap for debugging/display
    /// </summary>
    public int GetCurrentBufferCap()
    {
        return currentBufferCap;
    }
    
    /// <summary>
    /// Gets current round number
    /// </summary>
    public int GetCurrentRound()
    {
        return currentRound;
    }
    
    /// <summary>
    /// Resets analyzer state (for new games)
    /// </summary>
    public void ResetState()
    {
        currentRound = 1;
        currentBufferCap = config.InitialBufferCap;
        
        if (enableDebugLogs)
        {
            Debug.Log("AIGameStateAnalyzer: State reset to Round 1");
        }
    }
    
    /// <summary>
    /// Analyzes score gap for lead preservation strategies (Requirements 4.3, 4.5)
    /// </summary>
    public LeadAnalysis AnalyzeLeadState(int aiScore, int playerScore)
    {
        var analysis = new LeadAnalysis();
        analysis.ScoreDifference = aiScore - playerScore;
        analysis.AbsoluteScoreGap = Mathf.Abs(analysis.ScoreDifference);
        analysis.CurrentBufferCap = currentBufferCap;
        analysis.CurrentRound = currentRound;
        
        // Determine lead state using dynamic buffer thresholds
        if (analysis.ScoreDifference > currentBufferCap)
        {
            analysis.LeadState = LeadState.LEADING;
            analysis.LeadStrength = CalculateLeadStrength(analysis.ScoreDifference, currentBufferCap);
        }
        else if (analysis.ScoreDifference < -currentBufferCap)
        {
            analysis.LeadState = LeadState.BEHIND;
            analysis.LeadStrength = CalculateLeadStrength(-analysis.ScoreDifference, currentBufferCap);
        }
        else
        {
            analysis.LeadState = LeadState.CLOSE;
            analysis.LeadStrength = 0f; // No significant lead
        }
        
        // Calculate risk avoidance factor for lead preservation
        analysis.RiskAvoidanceFactor = CalculateRiskAvoidanceFactor(analysis);
        
        // Determine if early turn ending is recommended
        analysis.RecommendEarlyTurnEnd = ShouldRecommendEarlyTurnEnd(analysis);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Lead Analysis: {analysis.LeadState}, Strength: {analysis.LeadStrength:F2}, " +
                     $"Risk Avoidance: {analysis.RiskAvoidanceFactor:F2}, Early End: {analysis.RecommendEarlyTurnEnd}");
        }
        
        return analysis;
    }
    
    /// <summary>
    /// Calculates lead strength as a normalized value (0-1)
    /// </summary>
    float CalculateLeadStrength(int leadAmount, int bufferCap)
    {
        if (leadAmount <= bufferCap) return 0f;
        
        // Normalize lead strength: stronger leads = higher values
        // Cap at 1.0 for very large leads (3x buffer cap)
        float strength = (float)(leadAmount - bufferCap) / (bufferCap * 2f);
        return Mathf.Min(strength, 1f);
    }
    
    /// <summary>
    /// Calculates risk avoidance factor based on lead state and round progression
    /// </summary>
    float CalculateRiskAvoidanceFactor(LeadAnalysis analysis)
    {
        if (analysis.LeadState != LeadState.LEADING) return 0f;
        
        // Base risk avoidance increases with lead strength
        float baseAvoidance = analysis.LeadStrength * 0.5f;
        
        // Round progression factor - more conservative as game progresses
        float roundFactor = Mathf.Min((float)currentRound / 10f, 0.3f);
        
        // Buffer tightening factor - more conservative as buffer tightens
        float bufferFactor = (float)(config.InitialBufferCap - currentBufferCap) / config.InitialBufferCap * 0.2f;
        
        return Mathf.Min(baseAvoidance + roundFactor + bufferFactor, 0.8f);
    }
    
    /// <summary>
    /// Determines if early turn ending should be recommended for lead preservation
    /// </summary>
    bool ShouldRecommendEarlyTurnEnd(LeadAnalysis analysis)
    {
        // Only recommend early turn end when leading
        if (analysis.LeadState != LeadState.LEADING) return false;
        
        // Strong leads with tight buffer caps should end early
        if (analysis.LeadStrength > 0.6f && analysis.CurrentBufferCap <= 100) return true;
        
        // Moderate leads in late game should be more conservative
        if (analysis.LeadStrength > 0.3f && analysis.CurrentRound >= 8) return true;
        
        // Very tight buffer caps require extreme caution
        if (analysis.CurrentBufferCap <= config.MinimumBufferCap && analysis.LeadStrength > 0.1f) return true;
        
        return false;
    }
    
    /// <summary>
    /// Gets recommended points per turn cap adjustment for lead preservation
    /// </summary>
    public int GetLeadPreservationCapAdjustment(LeadAnalysis leadAnalysis, BehaviorMode mode)
    {
        if (leadAnalysis.LeadState != LeadState.LEADING || mode != BehaviorMode.PASSIVE) return 0;
        
        // Reduce cap based on lead strength and risk avoidance
        float reductionFactor = leadAnalysis.RiskAvoidanceFactor;
        int baseReduction = (int)(config.PointsCapPassive * reductionFactor * 0.3f); // Max 30% reduction
        
        // Additional reduction for very tight buffer caps
        if (leadAnalysis.CurrentBufferCap <= config.MinimumBufferCap + 20)
        {
            baseReduction += 50; // Extra conservative with tight buffers
        }
        
        return -baseReduction; // Negative value to reduce the cap
    }
}

/// <summary>
/// Analysis result for lead preservation strategies
/// </summary>
[System.Serializable]
public class LeadAnalysis
{
    public int ScoreDifference;
    public int AbsoluteScoreGap;
    public int CurrentBufferCap;
    public int CurrentRound;
    public LeadState LeadState;
    public float LeadStrength;        // 0-1, how strong the lead is
    public float RiskAvoidanceFactor; // 0-1, how much to avoid risk
    public bool RecommendEarlyTurnEnd;
}

/// <summary>
/// Lead state enumeration for clearer analysis
/// </summary>
public enum LeadState
{
    BEHIND,   // AI is behind by buffer amount
    CLOSE,    // Within buffer zone
    LEADING   // AI is leading by buffer amount
}