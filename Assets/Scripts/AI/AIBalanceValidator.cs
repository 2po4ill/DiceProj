using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HybridEnemyAI;

/// <summary>
/// Balance validation and tuning tools for AI behavior analysis
/// Provides win rate analysis, difficulty adjustment, and player experience metrics
/// </summary>
public class AIBalanceValidator : MonoBehaviour
{
    [Header("Balance Configuration")]
    public int simulationGames = 100;
    public bool enableRealTimeAnalysis = true;
    public float targetWinRate = 50f; // Target 50% win rate for balanced gameplay
    public float acceptableVariance = 10f; // ±10% variance acceptable
    
    [Header("AI Components")]
    public AIGameStateAnalyzer gameStateAnalyzer;
    public AITurnExecutor turnExecutor;
    public AIDecisionEngine decisionEngine;
    
    [Header("Balance Results")]
    [SerializeField] private BalanceAnalysis currentAnalysis;
    [SerializeField] private List<GameSimulationResult> simulationResults = new List<GameSimulationResult>();
    [SerializeField] private DifficultyRecommendations recommendations;
    
    [System.Serializable]
    public class BalanceAnalysis
    {
        public float AIWinRate;
        public float PlayerWinRate;
        public float AverageGameLength;
        public float AverageAIScore;
        public float AveragePlayerScore;
        public float ScoreVariance;
        public bool IsBalanced;
        public string BalanceStatus;
        public Dictionary<BehaviorMode, ModeAnalysis> ModePerformance = new Dictionary<BehaviorMode, ModeAnalysis>();
    }
    
    [System.Serializable]
    public class ModeAnalysis
    {
        public float WinRate;
        public float AverageScore;
        public float AverageTurnLength;
        public float RiskRewardRatio;
        public int TimesUsed;
    }
    
    [System.Serializable]
    public class GameSimulationResult
    {
        public int GameNumber;
        public int AIFinalScore;
        public int PlayerFinalScore;
        public bool AIWon;
        public int TotalTurns;
        public float GameDuration;
        public List<TurnResult> AITurns = new List<TurnResult>();
        public Dictionary<BehaviorMode, int> ModeUsage = new Dictionary<BehaviorMode, int>();
    }
    
    [System.Serializable]
    public class TurnResult
    {
        public BehaviorMode Mode;
        public int PointsScored;
        public int Iterations;
        public bool Zonked;
        public float RiskTaken;
    }
    
    [System.Serializable]
    public class DifficultyRecommendations
    {
        public bool NeedsAdjustment;
        public string RecommendationType; // "Easier", "Harder", "Balanced"
        public List<string> SpecificAdjustments = new List<string>();
        public AIConfiguration RecommendedConfig;
    }
    
    void Start()
    {
        InitializeBalanceValidator();
    }
    
    void InitializeBalanceValidator()
    {
        currentAnalysis = new BalanceAnalysis();
        recommendations = new DifficultyRecommendations();
        
        // Initialize mode performance tracking
        currentAnalysis.ModePerformance[BehaviorMode.AGGRESSIVE] = new ModeAnalysis();
        currentAnalysis.ModePerformance[BehaviorMode.PASSIVE] = new ModeAnalysis();
    }
    
    [ContextMenu("Run Balance Analysis")]
    public void RunBalanceAnalysis()
    {
        StartCoroutine(PerformBalanceAnalysis());
    }
    
    public IEnumerator PerformBalanceAnalysis()
    {
        Debug.Log("=== STARTING AI BALANCE ANALYSIS ===");
        
        simulationResults.Clear();
        var startTime = Time.realtimeSinceStartup;
        
        // Run game simulations
        for (int i = 0; i < simulationGames; i++)
        {
            var gameResult = SimulateGame(i + 1);
            simulationResults.Add(gameResult);
            
            // Update real-time analysis
            if (enableRealTimeAnalysis && i % 10 == 0)
            {
                UpdateAnalysis();
                yield return null; // Prevent frame drops
            }
        }
        
        // Final analysis
        UpdateAnalysis();
        GenerateRecommendations();
        
        var totalTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"Balance analysis completed in {totalTime:F2}s");
        
        LogBalanceResults();
    }
    
    GameSimulationResult SimulateGame(int gameNumber)
    {
        var result = new GameSimulationResult
        {
            GameNumber = gameNumber,
            ModeUsage = new Dictionary<BehaviorMode, int>
            {
                { BehaviorMode.AGGRESSIVE, 0 },
                { BehaviorMode.PASSIVE, 0 }
            }
        };
        
        int aiScore = 0;
        int playerScore = 0;
        int turns = 0;
        var gameStartTime = Time.realtimeSinceStartup;
        
        // Simulate game until someone reaches winning score (typically 10,000)
        while (aiScore < 10000 && playerScore < 10000 && turns < 100) // Max 100 turns to prevent infinite games
        {
            turns++;
            
            // AI Turn
            var aiTurnResult = SimulateAITurn(aiScore, playerScore);
            aiScore += aiTurnResult.PointsScored;
            result.AITurns.Add(aiTurnResult);
            result.ModeUsage[aiTurnResult.Mode]++;
            
            // Player Turn (simplified simulation)
            if (aiScore < 10000)
            {
                var playerTurnScore = SimulatePlayerTurn(playerScore, aiScore);
                playerScore += playerTurnScore;
            }
        }
        
        result.AIFinalScore = aiScore;
        result.PlayerFinalScore = playerScore;
        result.AIWon = aiScore >= playerScore;
        result.TotalTurns = turns;
        result.GameDuration = Time.realtimeSinceStartup - gameStartTime;
        
        return result;
    }
    
    TurnResult SimulateAITurn(int currentAIScore, int currentPlayerScore)
    {
        var turnResult = new TurnResult();
        
        // Determine behavior mode
        if (gameStateAnalyzer != null)
        {
            turnResult.Mode = gameStateAnalyzer.AnalyzeGameState(currentAIScore, currentPlayerScore);
        }
        else
        {
            // Fallback logic
            turnResult.Mode = currentAIScore < currentPlayerScore ? BehaviorMode.AGGRESSIVE : BehaviorMode.PASSIVE;
        }
        
        // Simulate turn based on mode
        if (turnResult.Mode == BehaviorMode.AGGRESSIVE)
        {
            turnResult.PointsScored = SimulateAggressiveTurn();
            turnResult.RiskTaken = 0.7f; // High risk
        }
        else
        {
            turnResult.PointsScored = SimulatePassiveTurn();
            turnResult.RiskTaken = 0.3f; // Low risk
        }
        
        // Simulate iterations and zonk probability
        turnResult.Iterations = Random.Range(1, turnResult.Mode == BehaviorMode.AGGRESSIVE ? 5 : 3);
        turnResult.Zonked = Random.Range(0f, 1f) < (turnResult.RiskTaken * 0.2f); // Risk affects zonk chance
        
        if (turnResult.Zonked)
        {
            turnResult.PointsScored = 0;
        }
        
        return turnResult;
    }
    
    int SimulateAggressiveTurn()
    {
        // Aggressive mode: Higher potential points, higher variance
        var basePoints = Random.Range(200, 600);
        var bonusChance = Random.Range(0f, 1f);
        
        if (bonusChance < 0.3f) // 30% chance for bonus points
        {
            basePoints += Random.Range(100, 300);
        }
        
        return basePoints;
    }
    
    int SimulatePassiveTurn()
    {
        // Passive mode: Lower but more consistent points
        var basePoints = Random.Range(150, 350);
        var consistencyBonus = Random.Range(0f, 1f);
        
        if (consistencyBonus < 0.5f) // 50% chance for small bonus
        {
            basePoints += Random.Range(50, 100);
        }
        
        return basePoints;
    }
    
    int SimulatePlayerTurn(int currentPlayerScore, int currentAIScore)
    {
        // Simplified player simulation - assumes average human performance
        var baseScore = Random.Range(150, 400);
        
        // Player adapts to AI score (simple rubber-band AI)
        if (currentPlayerScore < currentAIScore - 500)
        {
            baseScore += Random.Range(50, 150); // Player takes more risks when behind
        }
        else if (currentPlayerScore > currentAIScore + 500)
        {
            baseScore = Mathf.Max(100, baseScore - Random.Range(50, 100)); // Player plays safer when ahead
        }
        
        return baseScore;
    }
    
    void UpdateAnalysis()
    {
        if (simulationResults.Count == 0) return;
        
        // Calculate win rates
        int aiWins = simulationResults.Count(r => r.AIWon);
        currentAnalysis.AIWinRate = (aiWins / (float)simulationResults.Count) * 100f;
        currentAnalysis.PlayerWinRate = 100f - currentAnalysis.AIWinRate;
        
        // Calculate averages
        currentAnalysis.AverageGameLength = (float)simulationResults.Average(r => r.TotalTurns);
        currentAnalysis.AverageAIScore = (float)simulationResults.Average(r => r.AIFinalScore);
        currentAnalysis.AveragePlayerScore = (float)simulationResults.Average(r => r.PlayerFinalScore);
        
        // Calculate score variance
        var aiScores = simulationResults.Select(r => (float)r.AIFinalScore).ToList();
        var mean = (float)aiScores.Average();
        currentAnalysis.ScoreVariance = (float)aiScores.Sum(score => Mathf.Pow(score - mean, 2)) / aiScores.Count;
        
        // Analyze mode performance
        AnalyzeModePerformance();
        
        // Determine if balanced
        currentAnalysis.IsBalanced = Mathf.Abs(currentAnalysis.AIWinRate - targetWinRate) <= acceptableVariance;
        currentAnalysis.BalanceStatus = GetBalanceStatus();
    }
    
    void AnalyzeModePerformance()
    {
        foreach (var mode in System.Enum.GetValues(typeof(BehaviorMode)).Cast<BehaviorMode>())
        {
            var modeAnalysis = currentAnalysis.ModePerformance[mode];
            
            // Collect all turns for this mode
            var modeTurns = simulationResults
                .SelectMany(r => r.AITurns)
                .Where(t => t.Mode == mode)
                .ToList();
            
            if (modeTurns.Count > 0)
            {
                modeAnalysis.AverageScore = (float)modeTurns.Average(t => t.PointsScored);
                modeAnalysis.AverageTurnLength = (float)modeTurns.Average(t => t.Iterations);
                modeAnalysis.RiskRewardRatio = (float)modeTurns.Average(t => t.PointsScored / Mathf.Max(t.RiskTaken, 0.1f));
                modeAnalysis.TimesUsed = modeTurns.Count;
                
                // Calculate win rate for games where this mode was used
                var gamesWithMode = simulationResults.Where(r => r.ModeUsage[mode] > 0);
                if (gamesWithMode.Any())
                {
                    modeAnalysis.WinRate = (gamesWithMode.Count(r => r.AIWon) / (float)gamesWithMode.Count()) * 100f;
                }
            }
        }
    }
    
    string GetBalanceStatus()
    {
        if (currentAnalysis.AIWinRate > targetWinRate + acceptableVariance)
        {
            return "AI Too Strong";
        }
        else if (currentAnalysis.AIWinRate < targetWinRate - acceptableVariance)
        {
            return "AI Too Weak";
        }
        else
        {
            return "Balanced";
        }
    }
    
    void GenerateRecommendations()
    {
        recommendations = new DifficultyRecommendations();
        recommendations.SpecificAdjustments.Clear();
        
        if (currentAnalysis.IsBalanced)
        {
            recommendations.NeedsAdjustment = false;
            recommendations.RecommendationType = "Balanced";
            recommendations.SpecificAdjustments.Add("AI is well-balanced. No adjustments needed.");
        }
        else if (currentAnalysis.AIWinRate > targetWinRate + acceptableVariance)
        {
            // AI is too strong
            recommendations.NeedsAdjustment = true;
            recommendations.RecommendationType = "Easier";
            
            if (currentAnalysis.ModePerformance[BehaviorMode.AGGRESSIVE].WinRate > 60f)
            {
                recommendations.SpecificAdjustments.Add("Reduce aggressive mode effectiveness");
                recommendations.SpecificAdjustments.Add("Increase aggressive mode risk thresholds");
            }
            
            if (currentAnalysis.AverageAIScore > currentAnalysis.AveragePlayerScore * 1.2f)
            {
                recommendations.SpecificAdjustments.Add("Reduce points per turn caps");
                recommendations.SpecificAdjustments.Add("Increase stop probabilities");
            }
            
            recommendations.SpecificAdjustments.Add("Consider increasing zonk probability");
        }
        else
        {
            // AI is too weak
            recommendations.NeedsAdjustment = true;
            recommendations.RecommendationType = "Harder";
            
            if (currentAnalysis.ModePerformance[BehaviorMode.PASSIVE].WinRate < 40f)
            {
                recommendations.SpecificAdjustments.Add("Improve passive mode efficiency");
                recommendations.SpecificAdjustments.Add("Reduce passive mode risk aversion");
            }
            
            if (currentAnalysis.AverageAIScore < currentAnalysis.AveragePlayerScore * 0.8f)
            {
                recommendations.SpecificAdjustments.Add("Increase points per turn caps");
                recommendations.SpecificAdjustments.Add("Reduce stop probabilities");
            }
            
            recommendations.SpecificAdjustments.Add("Consider improving combination selection");
        }
        
        // Generate recommended configuration
        GenerateRecommendedConfig();
    }
    
    void GenerateRecommendedConfig()
    {
        recommendations.RecommendedConfig = new AIConfiguration();
        
        if (recommendations.RecommendationType == "Easier")
        {
            // Make AI easier
            recommendations.RecommendedConfig.PointsCapAggressive = 400; // Reduced from default
            recommendations.RecommendedConfig.PointsCapPassive = 200;   // Reduced from default
            recommendations.RecommendedConfig.AggressiveBaseMultiplier = 0.15f; // Increased stop chance
            recommendations.RecommendedConfig.PassiveBaseMultiplier = 0.20f;    // Increased stop chance
        }
        else if (recommendations.RecommendationType == "Harder")
        {
            // Make AI harder
            recommendations.RecommendedConfig.PointsCapAggressive = 600; // Increased from default
            recommendations.RecommendedConfig.PointsCapPassive = 300;   // Increased from default
            recommendations.RecommendedConfig.AggressiveBaseMultiplier = 0.08f; // Reduced stop chance
            recommendations.RecommendedConfig.PassiveBaseMultiplier = 0.12f;    // Reduced stop chance
        }
        // Balanced case uses default configuration
    }
    
    void LogBalanceResults()
    {
        Debug.Log("=== AI BALANCE ANALYSIS RESULTS ===");
        Debug.Log($"Games Simulated: {simulationResults.Count}");
        Debug.Log($"AI Win Rate: {currentAnalysis.AIWinRate:F1}% (Target: {targetWinRate:F1}%)");
        Debug.Log($"Player Win Rate: {currentAnalysis.PlayerWinRate:F1}%");
        Debug.Log($"Average Game Length: {currentAnalysis.AverageGameLength:F1} turns");
        Debug.Log($"Average AI Score: {currentAnalysis.AverageAIScore:F0}");
        Debug.Log($"Average Player Score: {currentAnalysis.AveragePlayerScore:F0}");
        Debug.Log($"Balance Status: {currentAnalysis.BalanceStatus}");
        
        // Mode performance
        Debug.Log("--- Mode Performance ---");
        foreach (var mode in currentAnalysis.ModePerformance)
        {
            var analysis = mode.Value;
            Debug.Log($"{mode.Key}: Win Rate {analysis.WinRate:F1}%, Avg Score {analysis.AverageScore:F0}, " +
                     $"Risk/Reward {analysis.RiskRewardRatio:F1}, Used {analysis.TimesUsed} times");
        }
        
        // Recommendations
        Debug.Log("--- Recommendations ---");
        Debug.Log($"Needs Adjustment: {recommendations.NeedsAdjustment}");
        Debug.Log($"Recommendation Type: {recommendations.RecommendationType}");
        
        foreach (var adjustment in recommendations.SpecificAdjustments)
        {
            Debug.Log($"  - {adjustment}");
        }
        
        if (currentAnalysis.IsBalanced)
        {
            Debug.Log("🎯 BALANCED: AI provides appropriate challenge level!");
        }
        else if (recommendations.RecommendationType == "Easier")
        {
            Debug.Log("⚠️ TOO HARD: AI needs to be made easier for better player experience.");
        }
        else
        {
            Debug.Log("⚠️ TOO EASY: AI needs to be made more challenging.");
        }
    }
    
    /// <summary>
    /// Apply recommended configuration to AI components
    /// </summary>
    [ContextMenu("Apply Recommendations")]
    public void ApplyRecommendations()
    {
        if (recommendations.RecommendedConfig == null)
        {
            Debug.LogWarning("No recommendations available. Run balance analysis first.");
            return;
        }
        
        // Apply to game state analyzer
        if (gameStateAnalyzer != null && gameStateAnalyzer.config != null)
        {
            gameStateAnalyzer.config = recommendations.RecommendedConfig;
            Debug.Log("Applied recommended configuration to AIGameStateAnalyzer");
        }
        
        // Apply to decision engine
        if (decisionEngine != null && decisionEngine.config != null)
        {
            decisionEngine.config = recommendations.RecommendedConfig;
            Debug.Log("Applied recommended configuration to AIDecisionEngine");
        }
        
        Debug.Log($"Recommendations applied: {recommendations.RecommendationType}");
    }
    
    /// <summary>
    /// Get current balance analysis for external use
    /// </summary>
    public BalanceAnalysis GetBalanceAnalysis()
    {
        return currentAnalysis;
    }
    
    /// <summary>
    /// Get difficulty recommendations for external use
    /// </summary>
    public DifficultyRecommendations GetRecommendations()
    {
        return recommendations;
    }
    
    /// <summary>
    /// Export balance data for external analysis
    /// </summary>
    [ContextMenu("Export Balance Data")]
    public void ExportBalanceData()
    {
        var exportData = new
        {
            Analysis = currentAnalysis,
            Recommendations = recommendations,
            SimulationResults = simulationResults.Take(10).ToList() // Export first 10 games as sample
        };
        
        var json = JsonUtility.ToJson(exportData, true);
        Debug.Log("=== BALANCE DATA EXPORT ===");
        Debug.Log(json);
    }
}