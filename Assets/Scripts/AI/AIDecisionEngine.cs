using UnityEngine;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// Multi-factor decision engine that integrates momentum system with success count tracking
/// Implements continue/stop decision logic with detailed explanation system for debugging
/// Requirements: 6.4, 7.2, 7.5, 11.1, 11.2
/// </summary>
public class AIDecisionEngine : MonoBehaviour
{
    [Header("Component References")]
    public AIRiskCalculator riskCalculator;
    public AIGameStateAnalyzer gameStateAnalyzer;
    
    [Header("Configuration")]
    public AIConfiguration config;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    public bool showDecisionBreakdown = false;
    
    [Header("Decision Tracking")]
    [SerializeField] private DecisionHistory lastDecision;
    [SerializeField] private int totalDecisionsMade = 0;
    [SerializeField] private int stopDecisionsMade = 0;
    [SerializeField] private int continueDecisionsMade = 0;
    
    void Start()
    {
        ValidateComponents();
        
        if (config == null)
        {
            config = new AIConfiguration();
            if (enableDebugLogs)
                Debug.Log("AIDecisionEngine: Using default configuration");
        }
    }
    
    /// <summary>
    /// Main decision entry point - determines whether AI should continue or stop turn
    /// Integrates momentum system with success count tracking
    /// </summary>
    public AIDecisionResult MakeDecision(AITurnState turnState, BehaviorMode currentMode, int aiScore, int playerScore)
    {
        if (turnState == null)
        {
            Debug.LogError("AIDecisionEngine: TurnState is null");
            return CreateErrorDecision("Null turn state");
        }
        
        var decisionResult = new AIDecisionResult();
        decisionResult.TurnState = turnState;
        decisionResult.BehaviorMode = currentMode;
        decisionResult.Timestamp = System.DateTime.Now;
        
        // Update turn state with current game analysis
        UpdateTurnStateFromGameState(turnState, currentMode, aiScore, playerScore);
        
        // Perform multi-factor decision analysis
        var factors = AnalyzeDecisionFactors(turnState, currentMode);
        
        // Add lead preservation analysis (Requirements 4.3, 4.5)
        if (gameStateAnalyzer != null)
        {
            factors.LeadAnalysis = gameStateAnalyzer.AnalyzeLeadState(aiScore, playerScore);
        }
        
        decisionResult.DecisionFactors = factors;
        
        // Make the final decision based on all factors
        bool shouldStop = EvaluateStopDecision(factors, turnState, currentMode);
        decisionResult.ShouldStop = shouldStop;
        
        // Generate detailed explanation
        decisionResult.DecisionExplanation = GenerateDecisionExplanation(factors, shouldStop, turnState, currentMode);
        
        // Update tracking statistics
        UpdateDecisionTracking(decisionResult);
        
        // Store for debugging
        lastDecision = new DecisionHistory
        {
            Result = decisionResult,
            GameState = $"AI:{aiScore} vs Player:{playerScore}",
            TurnProgress = $"Score:{turnState.CurrentTurnScore}, Iter:{turnState.IterationCount}, Dice:{turnState.CurrentDice.Count}"
        };
        
        if (enableDebugLogs)
        {
            LogDecisionDetails(decisionResult);
        }
        
        return decisionResult;
    }
    
    /// <summary>
    /// Updates turn state with current game state analysis
    /// </summary>
    void UpdateTurnStateFromGameState(AITurnState turnState, BehaviorMode mode, int aiScore, int playerScore)
    {
        turnState.CurrentMode = mode;
        
        // Use lead preservation adjusted cap for conservative mode (Requirements 4.3, 4.5)
        if (gameStateAnalyzer != null)
        {
            turnState.PointsPerTurnCap = gameStateAnalyzer.GetPointsPerTurnCapWithLeadPreservation(mode, aiScore, playerScore);
        }
        else
        {
            turnState.PointsPerTurnCap = mode == BehaviorMode.AGGRESSIVE ? 500 : 250; // Fallback values
        }
        
        // Set max iterations based on behavior mode (Requirements 4.1, 4.2)
        if (mode == BehaviorMode.PASSIVE)
        {
            turnState.MaxIterations = config.ConservativeMaxIterations;
        }
        else
        {
            turnState.MaxIterations = 5; // Default aggressive iterations
        }
        
        // Calculate Zonk probability for current dice state
        if (riskCalculator != null)
        {
            turnState.ZonkProbability = riskCalculator.CalculateZonkProbability(turnState.CurrentDice.Count);
        }
    }
    
    /// <summary>
    /// Analyzes all decision factors using momentum system and success tracking
    /// </summary>
    DecisionFactors AnalyzeDecisionFactors(AITurnState turnState, BehaviorMode mode)
    {
        var factors = new DecisionFactors();
        
        // Factor 1: Dual Probability System (Momentum + Cap)
        if (riskCalculator != null)
        {
            factors.StopDecision = riskCalculator.CalculateStopDecision(turnState, mode);
        }
        else
        {
            factors.StopDecision = new AIStopDecision { ShouldStop = false, DecisionReason = "No risk calculator available" };
        }
        
        // Factor 2: Iteration Threshold Check
        factors.IterationLimitReached = turnState.IterationCount >= turnState.MaxIterations;
        factors.IterationPressure = CalculateIterationPressure(turnState.IterationCount, turnState.MaxIterations);
        
        // Factor 3: Success Momentum Analysis
        factors.SuccessStreak = turnState.SuccessfulCombinationsCount;
        factors.MomentumBonus = CalculateMomentumBonus(turnState.SuccessfulCombinationsCount);
        factors.IsHotStreak = turnState.SuccessfulCombinationsCount >= 3;
        
        // Factor 4: Dice Risk Assessment
        factors.DiceCount = turnState.CurrentDice.Count;
        factors.HighRiskDiceState = turnState.CurrentDice.Count <= 2;
        factors.ZonkProbability = turnState.ZonkProbability;
        
        // Factor 5: Score Progress Analysis
        factors.CurrentTurnScore = turnState.CurrentTurnScore;
        factors.PointsPerTurnCap = turnState.PointsPerTurnCap;
        factors.CapProgress = (float)turnState.CurrentTurnScore / turnState.PointsPerTurnCap;
        factors.OverCap = turnState.CurrentTurnScore > turnState.PointsPerTurnCap;
        
        // Factor 6: Behavior Mode Influence
        factors.BehaviorMode = mode;
        factors.AggressiveMode = mode == BehaviorMode.AGGRESSIVE;
        factors.ConservativeMode = mode == BehaviorMode.PASSIVE;
        
        // Factor 7: Lead Preservation Analysis (Requirements 4.3, 4.5)
        if (gameStateAnalyzer != null)
        {
            // Note: We need aiScore and playerScore parameters for this
            // For now, we'll set this in the calling method
            factors.LeadAnalysis = null; // Will be set by caller
        }
        
        return factors;
    }
    
    /// <summary>
    /// Evaluates final stop decision based on all analyzed factors
    /// </summary>
    bool EvaluateStopDecision(DecisionFactors factors, AITurnState turnState, BehaviorMode mode)
    {
        // Hard stops (override all other factors)
        if (factors.IterationLimitReached)
        {
            return true; // Must stop at iteration limit
        }
        
        // Dual probability system decision (primary decision mechanism)
        if (factors.StopDecision.ShouldStop)
        {
            return true; // Either momentum or cap probability triggered
        }
        
        // Conservative mode special cases (Requirements 4.1, 4.2, 4.4)
        if (mode == BehaviorMode.PASSIVE)
        {
            // Conservative high risk threshold - stop at lower risk than aggressive
            if (factors.ZonkProbability > config.ConservativeHighRiskThreshold)
            {
                return true; // Conservative mode avoids moderate risk
            }
            
            // Conservative early satisfaction - stop when reaching good progress
            if (factors.CapProgress >= config.ConservativeEarlySatisfactionThreshold)
            {
                return true; // Conservative mode satisfied with 80% of cap
            }
            
            // Conservative dice-based stopping - high stop chance with few dice
            if (factors.DiceCount == 2 && Random.Range(0f, 1f) < config.ConservativeTwoDiceStopChance)
            {
                return true; // 70% chance to stop with 2 dice in conservative mode
            }
            
            if (factors.DiceCount == 1 && Random.Range(0f, 1f) < config.ConservativeOneDiceStopChance)
            {
                return true; // 90% chance to stop with 1 dice in conservative mode
            }
            
            // Conservative iteration limit enforcement
            if (turnState.IterationCount >= config.ConservativeMaxIterations)
            {
                return true; // Conservative mode has lower iteration limit
            }
            
            // Lead preservation logic (Requirements 4.3, 4.5)
            if (factors.LeadAnalysis != null && factors.LeadAnalysis.LeadState == LeadState.LEADING)
            {
                // Early turn ending for lead protection
                if (factors.LeadAnalysis.RecommendEarlyTurnEnd)
                {
                    return true; // Preserve lead by ending turn early
                }
                
                // Risk avoidance when maintaining leads
                float leadRiskThreshold = config.ConservativeHighRiskThreshold * (1f - factors.LeadAnalysis.RiskAvoidanceFactor);
                if (factors.ZonkProbability > leadRiskThreshold)
                {
                    return true; // Lower risk tolerance when leading
                }
                
                // Early satisfaction adjusted for lead preservation
                float leadSatisfactionThreshold = config.ConservativeEarlySatisfactionThreshold * (1f - factors.LeadAnalysis.RiskAvoidanceFactor * 0.3f);
                if (factors.CapProgress >= leadSatisfactionThreshold)
                {
                    return true; // Earlier satisfaction when preserving leads
                }
            }
        }
        
        // Extreme risk scenarios (any mode)
        if (factors.ZonkProbability > 0.8f)
        {
            return true; // Extremely high Zonk risk
        }
        
        // Default to continue if no stop conditions met
        return false;
    }
    
    /// <summary>
    /// Calculates iteration pressure based on current vs maximum iterations
    /// </summary>
    float CalculateIterationPressure(int currentIteration, int maxIterations)
    {
        if (maxIterations <= 0) return 1f;
        return (float)currentIteration / maxIterations;
    }
    
    /// <summary>
    /// Calculates momentum bonus based on successful combinations count
    /// </summary>
    float CalculateMomentumBonus(int successCount)
    {
        if (successCount <= 0) return 0f;
        
        // Diminishing returns on momentum bonus
        return 1f - Mathf.Pow(0.8f, successCount);
    }
    
    /// <summary>
    /// Generates detailed decision explanation for debugging and analysis
    /// </summary>
    string GenerateDecisionExplanation(DecisionFactors factors, bool shouldStop, AITurnState turnState, BehaviorMode mode)
    {
        var explanation = new System.Text.StringBuilder();
        
        explanation.AppendLine($"=== AI DECISION ANALYSIS ({mode}) ===");
        explanation.AppendLine($"Final Decision: {(shouldStop ? "STOP" : "CONTINUE")}");
        explanation.AppendLine();
        
        // Primary decision factors
        explanation.AppendLine("PRIMARY FACTORS:");
        explanation.AppendLine($"• Dual Probability: {factors.StopDecision.CombinedStopChance:P1} " +
                              $"(Momentum: {factors.StopDecision.MomentumStopChance:P1}, Cap: {factors.StopDecision.CapStopChance:P1})");
        explanation.AppendLine($"• Probability Rolls: Momentum={factors.StopDecision.MomentumRollResult}, Cap={factors.StopDecision.CapRollResult}");
        explanation.AppendLine($"• Iteration: {turnState.IterationCount}/{turnState.MaxIterations} (Pressure: {factors.IterationPressure:P1})");
        explanation.AppendLine();
        
        // Success momentum analysis
        explanation.AppendLine("MOMENTUM ANALYSIS:");
        explanation.AppendLine($"• Success Streak: {factors.SuccessStreak} combinations");
        explanation.AppendLine($"• Momentum Bonus: {factors.MomentumBonus:P1}");
        explanation.AppendLine($"• Hot Streak: {(factors.IsHotStreak ? "YES" : "NO")} (3+ successes)");
        explanation.AppendLine();
        
        // Risk assessment
        explanation.AppendLine("RISK ASSESSMENT:");
        explanation.AppendLine($"• Dice Count: {factors.DiceCount} (High Risk: {(factors.HighRiskDiceState ? "YES" : "NO")})");
        explanation.AppendLine($"• Zonk Probability: {factors.ZonkProbability:P1}");
        explanation.AppendLine();
        
        // Score progress
        explanation.AppendLine("SCORE PROGRESS:");
        explanation.AppendLine($"• Turn Score: {factors.CurrentTurnScore}/{factors.PointsPerTurnCap} ({factors.CapProgress:P1})");
        explanation.AppendLine($"• Over Cap: {(factors.OverCap ? "YES" : "NO")}");
        explanation.AppendLine();
        
        // Lead preservation analysis
        if (factors.LeadAnalysis != null)
        {
            explanation.AppendLine("LEAD PRESERVATION:");
            explanation.AppendLine($"• Lead State: {factors.LeadAnalysis.LeadState}");
            explanation.AppendLine($"• Score Difference: {factors.LeadAnalysis.ScoreDifference}");
            explanation.AppendLine($"• Lead Strength: {factors.LeadAnalysis.LeadStrength:F2}");
            explanation.AppendLine($"• Risk Avoidance Factor: {factors.LeadAnalysis.RiskAvoidanceFactor:F2}");
            explanation.AppendLine($"• Recommend Early End: {(factors.LeadAnalysis.RecommendEarlyTurnEnd ? "YES" : "NO")}");
            explanation.AppendLine($"• Buffer Cap: {factors.LeadAnalysis.CurrentBufferCap} (Round {factors.LeadAnalysis.CurrentRound})");
            explanation.AppendLine();
        }
        
        // Decision reasoning
        explanation.AppendLine("DECISION REASONING:");
        if (shouldStop)
        {
            if (factors.IterationLimitReached)
                explanation.AppendLine($"• STOP: Iteration limit reached ({turnState.IterationCount}/{turnState.MaxIterations})");
            else if (factors.StopDecision.ShouldStop)
                explanation.AppendLine($"• STOP: {factors.StopDecision.DecisionReason}");
            else if (factors.ConservativeMode && factors.ZonkProbability > config.ConservativeHighRiskThreshold)
                explanation.AppendLine($"• STOP: Conservative mode avoiding moderate risk ({factors.ZonkProbability:P1} > {config.ConservativeHighRiskThreshold:P1})");
            else if (factors.ConservativeMode && factors.CapProgress >= config.ConservativeEarlySatisfactionThreshold)
                explanation.AppendLine($"• STOP: Conservative mode satisfied with progress ({factors.CapProgress:P1} >= {config.ConservativeEarlySatisfactionThreshold:P1})");
            else if (factors.ConservativeMode && factors.DiceCount <= 2)
                explanation.AppendLine($"• STOP: Conservative mode with {factors.DiceCount} dice (high stop probability)");
            else if (factors.LeadAnalysis != null && factors.LeadAnalysis.RecommendEarlyTurnEnd)
                explanation.AppendLine($"• STOP: Lead preservation - early turn end recommended (Lead: {factors.LeadAnalysis.LeadStrength:F2})");
            else if (factors.LeadAnalysis != null && factors.LeadAnalysis.LeadState == LeadState.LEADING)
            {
                float leadRiskThreshold = config.ConservativeHighRiskThreshold * (1f - factors.LeadAnalysis.RiskAvoidanceFactor);
                if (factors.ZonkProbability > leadRiskThreshold)
                    explanation.AppendLine($"• STOP: Lead preservation - risk avoidance ({factors.ZonkProbability:P1} > {leadRiskThreshold:P1})");
                else
                {
                    float leadSatisfactionThreshold = config.ConservativeEarlySatisfactionThreshold * (1f - factors.LeadAnalysis.RiskAvoidanceFactor * 0.3f);
                    if (factors.CapProgress >= leadSatisfactionThreshold)
                        explanation.AppendLine($"• STOP: Lead preservation - early satisfaction ({factors.CapProgress:P1} >= {leadSatisfactionThreshold:P1})");
                }
            }
            else if (factors.ZonkProbability > 0.8f)
                explanation.AppendLine("• STOP: Extreme Zonk risk");
        }
        else
        {
            explanation.AppendLine("• CONTINUE: No stop conditions met");
            explanation.AppendLine($"• Risk tolerance acceptable for {mode} mode");
            if (factors.ConservativeMode)
            {
                explanation.AppendLine($"• Conservative checks: Risk {factors.ZonkProbability:P1} < {config.ConservativeHighRiskThreshold:P1}, " +
                                     $"Progress {factors.CapProgress:P1} < {config.ConservativeEarlySatisfactionThreshold:P1}");
                
                if (factors.LeadAnalysis != null && factors.LeadAnalysis.LeadState == LeadState.LEADING)
                {
                    float leadRiskThreshold = config.ConservativeHighRiskThreshold * (1f - factors.LeadAnalysis.RiskAvoidanceFactor);
                    float leadSatisfactionThreshold = config.ConservativeEarlySatisfactionThreshold * (1f - factors.LeadAnalysis.RiskAvoidanceFactor * 0.3f);
                    explanation.AppendLine($"• Lead preservation checks: Risk {factors.ZonkProbability:P1} < {leadRiskThreshold:P1}, " +
                                         $"Progress {factors.CapProgress:P1} < {leadSatisfactionThreshold:P1}");
                }
            }
        }
        
        return explanation.ToString();
    }
    
    /// <summary>
    /// Updates decision tracking statistics
    /// </summary>
    void UpdateDecisionTracking(AIDecisionResult result)
    {
        totalDecisionsMade++;
        
        if (result.ShouldStop)
            stopDecisionsMade++;
        else
            continueDecisionsMade++;
    }
    
    /// <summary>
    /// Logs detailed decision information for debugging
    /// </summary>
    void LogDecisionDetails(AIDecisionResult result)
    {
        if (showDecisionBreakdown)
        {
            Debug.Log(result.DecisionExplanation);
        }
        else
        {
            Debug.Log($"AIDecisionEngine: {(result.ShouldStop ? "STOP" : "CONTINUE")} - " +
                     $"Combined: {result.DecisionFactors.StopDecision.CombinedStopChance:P1}, " +
                     $"Iter: {result.TurnState.IterationCount}, " +
                     $"Successes: {result.DecisionFactors.SuccessStreak}");
        }
    }
    
    /// <summary>
    /// Creates error decision result
    /// </summary>
    AIDecisionResult CreateErrorDecision(string errorMessage)
    {
        return new AIDecisionResult
        {
            ShouldStop = true,
            DecisionExplanation = $"ERROR: {errorMessage}",
            DecisionFactors = new DecisionFactors(),
            TurnState = new AITurnState(),
            BehaviorMode = BehaviorMode.AGGRESSIVE,
            Timestamp = System.DateTime.Now
        };
    }
    
    /// <summary>
    /// Validates required component references
    /// </summary>
    void ValidateComponents()
    {
        if (riskCalculator == null)
        {
            riskCalculator = GetComponent<AIRiskCalculator>();
            if (riskCalculator == null)
            {
                Debug.LogWarning("AIDecisionEngine: No AIRiskCalculator found. Risk calculations will be limited.");
            }
        }
        
        if (gameStateAnalyzer == null)
        {
            gameStateAnalyzer = GetComponent<AIGameStateAnalyzer>();
            if (gameStateAnalyzer == null)
            {
                Debug.LogWarning("AIDecisionEngine: No AIGameStateAnalyzer found. Game state analysis will be limited.");
            }
        }
    }
    
    /// <summary>
    /// Gets decision statistics for debugging and analysis
    /// </summary>
    public DecisionStatistics GetDecisionStatistics()
    {
        return new DecisionStatistics
        {
            TotalDecisions = totalDecisionsMade,
            StopDecisions = stopDecisionsMade,
            ContinueDecisions = continueDecisionsMade,
            StopRate = totalDecisionsMade > 0 ? (float)stopDecisionsMade / totalDecisionsMade : 0f,
            LastDecision = lastDecision
        };
    }
    
    /// <summary>
    /// Resets decision tracking statistics
    /// </summary>
    public void ResetStatistics()
    {
        totalDecisionsMade = 0;
        stopDecisionsMade = 0;
        continueDecisionsMade = 0;
        lastDecision = null;
        
        if (enableDebugLogs)
        {
            Debug.Log("AIDecisionEngine: Statistics reset");
        }
    }
    
    /// <summary>
    /// Context menu for testing decision logic
    /// </summary>
    [ContextMenu("Test Decision Logic")]
    public void TestDecisionLogic()
    {
        Debug.Log("=== DECISION ENGINE TEST ===");
        
        var testTurnState = new AITurnState
        {
            CurrentTurnScore = 300,
            PointsPerTurnCap = 400,
            IterationCount = 2,
            MaxIterations = 5,
            SuccessfulCombinationsCount = 2,
            CurrentDice = new List<int> { 1, 2, 3 }
        };
        
        var result = MakeDecision(testTurnState, BehaviorMode.AGGRESSIVE, 800, 900);
        Debug.Log($"Test Result: {(result.ShouldStop ? "STOP" : "CONTINUE")}");
        Debug.Log(result.DecisionExplanation);
    }
    
    /// <summary>
    /// Updates configuration at runtime
    /// </summary>
    public void UpdateConfiguration(AIConfiguration newConfig)
    {
        config = newConfig;
        if (enableDebugLogs)
            Debug.Log("AIDecisionEngine: Configuration updated");
    }
}

/// <summary>
/// Comprehensive result of AI decision analysis
/// </summary>
[System.Serializable]
public class AIDecisionResult
{
    public bool ShouldStop;
    public string DecisionExplanation;
    public DecisionFactors DecisionFactors;
    public AITurnState TurnState;
    public BehaviorMode BehaviorMode;
    public System.DateTime Timestamp;
}

/// <summary>
/// All factors considered in decision making
/// </summary>
[System.Serializable]
public class DecisionFactors
{
    [Header("Probability System")]
    public AIStopDecision StopDecision;
    
    [Header("Iteration Analysis")]
    public bool IterationLimitReached;
    public float IterationPressure;
    
    [Header("Success Momentum")]
    public int SuccessStreak;
    public float MomentumBonus;
    public bool IsHotStreak;
    
    [Header("Risk Assessment")]
    public int DiceCount;
    public bool HighRiskDiceState;
    public float ZonkProbability;
    
    [Header("Score Progress")]
    public int CurrentTurnScore;
    public int PointsPerTurnCap;
    public float CapProgress;
    public bool OverCap;
    
    [Header("Behavior Mode")]
    public BehaviorMode BehaviorMode;
    public bool AggressiveMode;
    public bool ConservativeMode;
    
    [Header("Lead Preservation")]
    public LeadAnalysis LeadAnalysis;
}

/// <summary>
/// Decision tracking statistics
/// </summary>
[System.Serializable]
public class DecisionStatistics
{
    public int TotalDecisions;
    public int StopDecisions;
    public int ContinueDecisions;
    public float StopRate;
    public DecisionHistory LastDecision;
}

/// <summary>
/// Historical decision record for debugging
/// </summary>
[System.Serializable]
public class DecisionHistory
{
    public AIDecisionResult Result;
    public string GameState;
    public string TurnProgress;
}