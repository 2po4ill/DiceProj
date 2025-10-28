using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HybridEnemyAI;

/// <summary>
/// AI behavior debugging and analysis tools for real-time decision monitoring
/// Provides detailed insights into AI decision-making process and behavior patterns
/// </summary>
public class AIBehaviorDebugger : MonoBehaviour
{
    [Header("Debug Configuration")]
    public bool enableRealTimeDebugging = true;
    public bool logDecisionBreakdowns = true;
    public bool trackBehaviorPatterns = true;
    public int maxLogEntries = 100;
    
    [Header("AI Components")]
    public AIGameStateAnalyzer gameStateAnalyzer;
    public AIDecisionEngine decisionEngine;
    public AIRiskCalculator riskCalculator;
    public AITurnExecutor turnExecutor;
    
    [Header("Debug Display")]
    public bool showDebugUI = true;
    public KeyCode toggleDebugKey = KeyCode.F1;
    
    [Header("Current Debug State")]
    [SerializeField] private DebugSession currentSession;
    [SerializeField] private List<DecisionLogEntry> decisionLog = new List<DecisionLogEntry>();
    [SerializeField] private BehaviorPatterns behaviorPatterns;
    
    [System.Serializable]
    public class DebugSession
    {
        public int SessionID;
        public float StartTime;
        public int TotalDecisions;
        public int TotalTurns;
        public BehaviorMode CurrentMode;
        public AITurnState CurrentTurnState;
        public string LastDecisionReason;
        public float SessionDuration => Time.realtimeSinceStartup - StartTime;
    }
    
    [System.Serializable]
    public class DecisionLogEntry
    {
        public float Timestamp;
        public int TurnNumber;
        public int IterationNumber;
        public BehaviorMode Mode;
        public AIStopDecision StopDecision;
        public string DecisionReason;
        public int CurrentScore;
        public int DiceCount;
        public bool ContinuedTurn;
        public string DetailedBreakdown;
    }
    
    [System.Serializable]
    public class BehaviorPatterns
    {
        public Dictionary<BehaviorMode, ModePattern> ModePatterns = new Dictionary<BehaviorMode, ModePattern>();
        public List<string> IdentifiedPatterns = new List<string>();
        public float PatternConfidence;
    }
    
    [System.Serializable]
    public class ModePattern
    {
        public int TimesUsed;
        public float AverageDecisionTime;
        public float AverageStopChance;
        public float AverageIterations;
        public List<string> CommonReasons = new List<string>();
        public Dictionary<string, int> ReasonFrequency = new Dictionary<string, int>();
    }
    
    // Debug UI variables
    private bool showDebugWindow = false;
    private Vector2 scrollPosition = Vector2.zero;
    private int selectedLogIndex = -1;
    
    void Start()
    {
        InitializeDebugger();
        StartNewSession();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleDebugKey))
        {
            showDebugWindow = !showDebugWindow;
        }
        
        if (enableRealTimeDebugging)
        {
            MonitorAIBehavior();
        }
    }
    
    void InitializeDebugger()
    {
        behaviorPatterns = new BehaviorPatterns();
        behaviorPatterns.ModePatterns[BehaviorMode.AGGRESSIVE] = new ModePattern();
        behaviorPatterns.ModePatterns[BehaviorMode.PASSIVE] = new ModePattern();
        
        // Subscribe to AI events if components are available
        SubscribeToAIEvents();
    }
    
    void SubscribeToAIEvents()
    {
        if (turnExecutor != null)
        {
            turnExecutor.OnTurnStarted += OnAITurnStarted;
            turnExecutor.OnDecisionMade += OnAIDecisionMade;
            turnExecutor.OnTurnCompleted += OnAITurnCompleted;
        }
    }
    
    void StartNewSession()
    {
        currentSession = new DebugSession
        {
            SessionID = Random.Range(1000, 9999),
            StartTime = Time.realtimeSinceStartup,
            TotalDecisions = 0,
            TotalTurns = 0
        };
        
        decisionLog.Clear();
        
        Debug.Log($"AI Debug Session {currentSession.SessionID} started");
    }
    
    void MonitorAIBehavior()
    {
        if (gameStateAnalyzer != null && currentSession != null)
        {
            // Update current session state
            // Current mode is already tracked in currentSession.CurrentMode
            
            if (turnExecutor != null && turnExecutor.currentTurnState != null)
            {
                currentSession.CurrentTurnState = turnExecutor.currentTurnState;
            }
        }
    }
    
    void OnAITurnStarted(AITurnState turnState)
    {
        currentSession.TotalTurns++;
        
        if (logDecisionBreakdowns)
        {
            Debug.Log($"[AI Debug] Turn {currentSession.TotalTurns} started - Mode: {currentSession.CurrentMode}");
        }
    }
    
    void OnAIDecisionMade(AIStopDecision stopDecision)
    {
        currentSession.TotalDecisions++;
        
        var logEntry = new DecisionLogEntry
        {
            Timestamp = Time.realtimeSinceStartup,
            TurnNumber = currentSession.TotalTurns,
            IterationNumber = currentSession.CurrentTurnState?.IterationCount ?? 0,
            Mode = currentSession.CurrentMode,
            StopDecision = stopDecision,
            DecisionReason = stopDecision.DecisionReason ?? "Unknown",
            CurrentScore = currentSession.CurrentTurnState?.CurrentTurnScore ?? 0,
            DiceCount = currentSession.CurrentTurnState?.CurrentDice?.Count ?? 0,
            ContinuedTurn = !stopDecision.ShouldStop,
            DetailedBreakdown = GenerateDetailedBreakdown(stopDecision)
        };
        
        AddDecisionLogEntry(logEntry);
        UpdateBehaviorPatterns(logEntry);
        
        currentSession.LastDecisionReason = logEntry.DecisionReason;
        
        if (logDecisionBreakdowns)
        {
            Debug.Log($"[AI Debug] Decision: {(logEntry.ContinuedTurn ? "CONTINUE" : "STOP")} - {logEntry.DecisionReason}");
            Debug.Log($"  Momentum: {stopDecision.MomentumStopChance:P1}, Cap: {stopDecision.CapStopChance:P1}, Combined: {stopDecision.CombinedStopChance:P1}");
        }
    }
    
    void OnAITurnCompleted(AITurnState turnState)
    {
        if (logDecisionBreakdowns)
        {
            Debug.Log($"[AI Debug] Turn {currentSession.TotalTurns} completed - Score: {turnState.CurrentTurnScore}, Iterations: {turnState.IterationCount}");
        }
    }
    
    string GenerateDetailedBreakdown(AIStopDecision stopDecision)
    {
        var breakdown = $"Momentum: {stopDecision.MomentumStopChance:P1} | ";
        breakdown += $"Cap: {stopDecision.CapStopChance:P1} | ";
        breakdown += $"Combined: {stopDecision.CombinedStopChance:P1} | ";
        breakdown += $"Result: {(stopDecision.ShouldStop ? "STOP" : "CONTINUE")}";
        
        if (!string.IsNullOrEmpty(stopDecision.DecisionReason))
        {
            breakdown += $" | Reason: {stopDecision.DecisionReason}";
        }
        
        return breakdown;
    }
    
    void AddDecisionLogEntry(DecisionLogEntry entry)
    {
        decisionLog.Add(entry);
        
        // Maintain log size limit
        if (decisionLog.Count > maxLogEntries)
        {
            decisionLog.RemoveAt(0);
        }
    }
    
    void UpdateBehaviorPatterns(DecisionLogEntry entry)
    {
        if (!trackBehaviorPatterns) return;
        
        var modePattern = behaviorPatterns.ModePatterns[entry.Mode];
        modePattern.TimesUsed++;
        
        // Update averages
        modePattern.AverageStopChance = UpdateRunningAverage(
            modePattern.AverageStopChance, 
            entry.StopDecision.CombinedStopChance, 
            modePattern.TimesUsed);
        
        modePattern.AverageIterations = UpdateRunningAverage(
            modePattern.AverageIterations, 
            entry.IterationNumber, 
            modePattern.TimesUsed);
        
        // Track reason frequency
        if (!modePattern.ReasonFrequency.ContainsKey(entry.DecisionReason))
        {
            modePattern.ReasonFrequency[entry.DecisionReason] = 0;
        }
        modePattern.ReasonFrequency[entry.DecisionReason]++;
        
        // Update common reasons
        UpdateCommonReasons(modePattern);
        
        // Analyze patterns
        AnalyzeBehaviorPatterns();
    }
    
    float UpdateRunningAverage(float currentAverage, float newValue, int count)
    {
        return ((currentAverage * (count - 1)) + newValue) / count;
    }
    
    void UpdateCommonReasons(ModePattern pattern)
    {
        pattern.CommonReasons = pattern.ReasonFrequency
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => $"{kvp.Key} ({kvp.Value})")
            .ToList();
    }
    
    void AnalyzeBehaviorPatterns()
    {
        behaviorPatterns.IdentifiedPatterns.Clear();
        
        // Analyze aggressive vs passive usage
        var aggressiveUsage = behaviorPatterns.ModePatterns[BehaviorMode.AGGRESSIVE].TimesUsed;
        var passiveUsage = behaviorPatterns.ModePatterns[BehaviorMode.PASSIVE].TimesUsed;
        var totalUsage = aggressiveUsage + passiveUsage;
        
        if (totalUsage > 10) // Need sufficient data
        {
            var aggressiveRatio = aggressiveUsage / (float)totalUsage;
            
            if (aggressiveRatio > 0.7f)
            {
                behaviorPatterns.IdentifiedPatterns.Add("Predominantly Aggressive - AI frequently behind or taking risks");
            }
            else if (aggressiveRatio < 0.3f)
            {
                behaviorPatterns.IdentifiedPatterns.Add("Predominantly Passive - AI frequently ahead or playing safe");
            }
            else
            {
                behaviorPatterns.IdentifiedPatterns.Add("Balanced Mode Usage - AI adapting well to game state");
            }
        }
        
        // Analyze stop chance patterns
        var avgAggressiveStop = behaviorPatterns.ModePatterns[BehaviorMode.AGGRESSIVE].AverageStopChance;
        var avgPassiveStop = behaviorPatterns.ModePatterns[BehaviorMode.PASSIVE].AverageStopChance;
        
        if (avgPassiveStop > avgAggressiveStop * 1.5f)
        {
            behaviorPatterns.IdentifiedPatterns.Add("Conservative Passive Mode - Stopping much more frequently when ahead");
        }
        
        if (avgAggressiveStop < 0.3f)
        {
            behaviorPatterns.IdentifiedPatterns.Add("High Risk Aggressive Mode - Very low stop probability when behind");
        }
        
        // Calculate pattern confidence
        behaviorPatterns.PatternConfidence = Mathf.Min(totalUsage / 50f, 1f); // Max confidence at 50+ decisions
    }
    
    void OnGUI()
    {
        if (!showDebugUI || !showDebugWindow) return;
        
        // Debug window
        var windowRect = new Rect(10, 10, 600, 400);
        GUI.Window(0, windowRect, DrawDebugWindow, $"AI Behavior Debugger - Session {currentSession?.SessionID}");
    }
    
    void DrawDebugWindow(int windowID)
    {
        GUILayout.BeginVertical();
        
        // Session info
        if (currentSession != null)
        {
            GUILayout.Label($"Session Duration: {currentSession.SessionDuration:F1}s");
            GUILayout.Label($"Total Decisions: {currentSession.TotalDecisions}");
            GUILayout.Label($"Total Turns: {currentSession.TotalTurns}");
            GUILayout.Label($"Current Mode: {currentSession.CurrentMode}");
            GUILayout.Label($"Last Decision: {currentSession.LastDecisionReason}");
        }
        
        GUILayout.Space(10);
        
        // Tabs
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Decision Log")) selectedLogIndex = -1;
        if (GUILayout.Button("Behavior Patterns")) selectedLogIndex = -2;
        if (GUILayout.Button("Real-time Stats")) selectedLogIndex = -3;
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Content based on selected tab
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));
        
        if (selectedLogIndex == -1)
        {
            DrawDecisionLog();
        }
        else if (selectedLogIndex == -2)
        {
            DrawBehaviorPatterns();
        }
        else if (selectedLogIndex == -3)
        {
            DrawRealTimeStats();
        }
        
        GUILayout.EndScrollView();
        
        // Controls
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Log"))
        {
            decisionLog.Clear();
        }
        if (GUILayout.Button("Export Data"))
        {
            ExportDebugData();
        }
        if (GUILayout.Button("New Session"))
        {
            StartNewSession();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
        
        GUI.DragWindow();
    }
    
    void DrawDecisionLog()
    {
        GUILayout.Label($"Recent Decisions ({decisionLog.Count}/{maxLogEntries}):");
        
        foreach (var entry in decisionLog.TakeLast(20).Reverse())
        {
            var color = entry.ContinuedTurn ? Color.green : Color.red;
            var originalColor = GUI.color;
            GUI.color = color;
            
            GUILayout.Label($"T{entry.TurnNumber}.{entry.IterationNumber} [{entry.Mode}] {(entry.ContinuedTurn ? "CONTINUE" : "STOP")} - {entry.DecisionReason}");
            GUILayout.Label($"  Score: {entry.CurrentScore}, Dice: {entry.DiceCount}, Combined: {entry.StopDecision.CombinedStopChance:P1}");
            
            GUI.color = originalColor;
            GUILayout.Space(2);
        }
    }
    
    void DrawBehaviorPatterns()
    {
        GUILayout.Label($"Pattern Analysis (Confidence: {behaviorPatterns.PatternConfidence:P0}):");
        
        foreach (var pattern in behaviorPatterns.IdentifiedPatterns)
        {
            GUILayout.Label($"• {pattern}");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Mode Statistics:");
        
        foreach (var mode in behaviorPatterns.ModePatterns)
        {
            GUILayout.Label($"{mode.Key}:");
            GUILayout.Label($"  Used: {mode.Value.TimesUsed} times");
            GUILayout.Label($"  Avg Stop Chance: {mode.Value.AverageStopChance:P1}");
            GUILayout.Label($"  Avg Iterations: {mode.Value.AverageIterations:F1}");
            GUILayout.Label($"  Common Reasons: {string.Join(", ", mode.Value.CommonReasons)}");
            GUILayout.Space(5);
        }
    }
    
    void DrawRealTimeStats()
    {
        if (currentSession?.CurrentTurnState != null)
        {
            var turnState = currentSession.CurrentTurnState;
            GUILayout.Label("Current Turn State:");
            GUILayout.Label($"  Score: {turnState.CurrentTurnScore}");
            GUILayout.Label($"  Iteration: {turnState.IterationCount}");
            GUILayout.Label($"  Successes: {turnState.SuccessfulCombinationsCount}");
            GUILayout.Label($"  Dice Count: {turnState.CurrentDice?.Count ?? 0}");
            GUILayout.Label($"  Points Cap: {turnState.PointsPerTurnCap}");
        }
        
        if (gameStateAnalyzer != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Game State:");
            GUILayout.Label($"  Current Mode: {currentSession.CurrentMode}");
            GUILayout.Label($"  Buffer Cap: {gameStateAnalyzer.GetCurrentBufferCap()}");
            GUILayout.Label($"  Round: {gameStateAnalyzer.GetCurrentRound()}");
        }
    }
    
    [ContextMenu("Export Debug Data")]
    public void ExportDebugData()
    {
        var exportData = new
        {
            Session = currentSession,
            DecisionLog = decisionLog.TakeLast(50).ToList(),
            BehaviorPatterns = behaviorPatterns
        };
        
        var json = JsonUtility.ToJson(exportData, true);
        Debug.Log("=== AI DEBUG DATA EXPORT ===");
        Debug.Log(json);
    }
    
    /// <summary>
    /// Get current debug session for external analysis
    /// </summary>
    public DebugSession GetCurrentSession()
    {
        return currentSession;
    }
    
    /// <summary>
    /// Get decision log for external analysis
    /// </summary>
    public List<DecisionLogEntry> GetDecisionLog()
    {
        return new List<DecisionLogEntry>(decisionLog);
    }
    
    /// <summary>
    /// Get behavior patterns for external analysis
    /// </summary>
    public BehaviorPatterns GetBehaviorPatterns()
    {
        return behaviorPatterns;
    }
    
    /// <summary>
    /// Force analyze current behavior patterns
    /// </summary>
    [ContextMenu("Analyze Patterns")]
    public void ForceAnalyzePatterns()
    {
        AnalyzeBehaviorPatterns();
        Debug.Log($"Pattern analysis complete. Found {behaviorPatterns.IdentifiedPatterns.Count} patterns.");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (turnExecutor != null)
        {
            turnExecutor.OnTurnStarted -= OnAITurnStarted;
            turnExecutor.OnDecisionMade -= OnAIDecisionMade;
            turnExecutor.OnTurnCompleted -= OnAITurnCompleted;
        }
    }
}