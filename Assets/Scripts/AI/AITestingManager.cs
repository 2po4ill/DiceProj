using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HybridEnemyAI;

/// <summary>
/// Central testing manager that coordinates all AI testing frameworks
/// Provides unified interface for running comprehensive AI validation
/// </summary>
public class AITestingManager : MonoBehaviour
{
    [Header("Testing Frameworks")]
    public AIPerformanceTestFramework performanceFramework;
    public AIBalanceValidator balanceValidator;
    public AIBehaviorDebugger behaviorDebugger;
    public AITester legacyTester; // Existing comprehensive tester
    
    [Header("Test Configuration")]
    public bool runFullSuiteOnStart = false;
    public bool enableContinuousMonitoring = false;
    public float monitoringInterval = 30f; // seconds
    
    [Header("Test Results Summary")]
    [SerializeField] private TestingSummary overallResults;
    
    [System.Serializable]
    public class TestingSummary
    {
        public bool AllTestsCompleted;
        public float OverallSuccessRate;
        public int TotalTestsRun;
        public int TotalTestsPassed;
        public int TotalTestsFailed;
        public string OverallStatus;
        public List<FrameworkResult> FrameworkResults = new List<FrameworkResult>();
    }
    
    [System.Serializable]
    public class FrameworkResult
    {
        public string FrameworkName;
        public bool Completed;
        public float SuccessRate;
        public string Status;
        public float ExecutionTime;
    }
    
    private Coroutine monitoringCoroutine;
    
    void Start()
    {
        InitializeTestingManager();
        
        if (runFullSuiteOnStart)
        {
            StartCoroutine(RunComprehensiveTestSuite());
        }
        
        if (enableContinuousMonitoring)
        {
            StartContinuousMonitoring();
        }
    }
    
    void InitializeTestingManager()
    {
        overallResults = new TestingSummary();
        
        // Auto-find testing components if not assigned
        if (performanceFramework == null)
            performanceFramework = GetComponent<AIPerformanceTestFramework>();
        
        if (balanceValidator == null)
            balanceValidator = GetComponent<AIBalanceValidator>();
        
        if (behaviorDebugger == null)
            behaviorDebugger = GetComponent<AIBehaviorDebugger>();
        
        if (legacyTester == null)
            legacyTester = GetComponent<AITester>();
        
        ValidateTestingComponents();
    }
    
    void ValidateTestingComponents()
    {
        var components = new Dictionary<string, MonoBehaviour>
        {
            {"Performance Framework", performanceFramework},
            {"Balance Validator", balanceValidator},
            {"Behavior Debugger", behaviorDebugger},
            {"Legacy Tester", legacyTester}
        };
        
        foreach (var component in components)
        {
            if (component.Value == null)
            {
                Debug.LogWarning($"AITestingManager: {component.Key} not found!");
            }
        }
    }
    
    [ContextMenu("Run Comprehensive Test Suite")]
    public void RunComprehensiveTestSuiteMenu()
    {
        StartCoroutine(RunComprehensiveTestSuite());
    }
    
    public IEnumerator RunComprehensiveTestSuite()
    {
        Debug.Log("=== STARTING COMPREHENSIVE AI TEST SUITE ===");
        var startTime = Time.realtimeSinceStartup;
        
        overallResults = new TestingSummary();
        overallResults.FrameworkResults.Clear();
        
        // 1. Run Performance Tests
        if (performanceFramework != null)
        {
            Debug.Log("--- Running Performance Tests ---");
            var perfStartTime = Time.realtimeSinceStartup;
            
            yield return StartCoroutine(performanceFramework.RunFullTestSuite());
            
            var perfResult = new FrameworkResult
            {
                FrameworkName = "Performance Framework",
                Completed = true,
                ExecutionTime = Time.realtimeSinceStartup - perfStartTime
            };
            
            var perfResults = performanceFramework.GetTestResults();
            if (perfResults != null)
            {
                perfResult.SuccessRate = perfResults.SuccessRate;
                perfResult.Status = perfResults.SuccessRate >= 90f ? "Excellent" : 
                                  perfResults.SuccessRate >= 75f ? "Good" : "Needs Attention";
                
                overallResults.TotalTestsRun += perfResults.TotalTests;
                overallResults.TotalTestsPassed += perfResults.PassedTests;
                overallResults.TotalTestsFailed += perfResults.FailedTests;
            }
            
            overallResults.FrameworkResults.Add(perfResult);
        }
        
        // 2. Run Balance Validation
        if (balanceValidator != null)
        {
            Debug.Log("--- Running Balance Validation ---");
            var balanceStartTime = Time.realtimeSinceStartup;
            
            yield return StartCoroutine(balanceValidator.PerformBalanceAnalysis());
            
            var balanceResult = new FrameworkResult
            {
                FrameworkName = "Balance Validator",
                Completed = true,
                ExecutionTime = Time.realtimeSinceStartup - balanceStartTime
            };
            
            var balanceAnalysis = balanceValidator.GetBalanceAnalysis();
            if (balanceAnalysis != null)
            {
                balanceResult.SuccessRate = balanceAnalysis.IsBalanced ? 100f : 
                                          Mathf.Max(0f, 100f - Mathf.Abs(balanceAnalysis.AIWinRate - 50f) * 2f);
                balanceResult.Status = balanceAnalysis.BalanceStatus;
            }
            
            overallResults.FrameworkResults.Add(balanceResult);
        }
        
        // 3. Run Legacy Comprehensive Tests
        if (legacyTester != null)
        {
            Debug.Log("--- Running Legacy Comprehensive Tests ---");
            var legacyStartTime = Time.realtimeSinceStartup;
            
            yield return StartCoroutine(legacyTester.RunAllTests());
            
            var legacyResult = new FrameworkResult
            {
                FrameworkName = "Legacy Tester",
                Completed = true,
                ExecutionTime = Time.realtimeSinceStartup - legacyStartTime,
                SuccessRate = legacyTester.totalTests > 0 ? 
                            (legacyTester.passedTests / (float)legacyTester.totalTests) * 100f : 0f
            };
            
            legacyResult.Status = legacyResult.SuccessRate >= 90f ? "Excellent" : 
                                legacyResult.SuccessRate >= 75f ? "Good" : "Needs Attention";
            
            overallResults.TotalTestsRun += legacyTester.totalTests;
            overallResults.TotalTestsPassed += legacyTester.passedTests;
            overallResults.TotalTestsFailed += legacyTester.failedTests;
            
            overallResults.FrameworkResults.Add(legacyResult);
        }
        
        // 4. Initialize Behavior Debugging (if not already running)
        if (behaviorDebugger != null && !behaviorDebugger.enableRealTimeDebugging)
        {
            Debug.Log("--- Initializing Behavior Debugger ---");
            behaviorDebugger.enableRealTimeDebugging = true;
            
            var debugResult = new FrameworkResult
            {
                FrameworkName = "Behavior Debugger",
                Completed = true,
                SuccessRate = 100f, // Always succeeds if initialized
                Status = "Monitoring",
                ExecutionTime = 0.1f
            };
            
            overallResults.FrameworkResults.Add(debugResult);
        }
        
        // Calculate overall results
        CalculateOverallResults();
        
        var totalTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"Comprehensive test suite completed in {totalTime:F2}s");
        
        LogOverallResults();
        
        Debug.Log("=== COMPREHENSIVE AI TEST SUITE COMPLETED ===");
    }
    
    void CalculateOverallResults()
    {
        overallResults.AllTestsCompleted = overallResults.FrameworkResults.Count > 0 &&
                                         overallResults.FrameworkResults.TrueForAll(r => r.Completed);
        
        if (overallResults.TotalTestsRun > 0)
        {
            overallResults.OverallSuccessRate = (overallResults.TotalTestsPassed / (float)overallResults.TotalTestsRun) * 100f;
        }
        
        // Determine overall status
        if (overallResults.OverallSuccessRate >= 95f)
        {
            overallResults.OverallStatus = "Excellent - AI system performing at high quality";
        }
        else if (overallResults.OverallSuccessRate >= 85f)
        {
            overallResults.OverallStatus = "Good - AI system performing well with minor issues";
        }
        else if (overallResults.OverallSuccessRate >= 70f)
        {
            overallResults.OverallStatus = "Acceptable - AI system functional but needs improvement";
        }
        else
        {
            overallResults.OverallStatus = "Needs Attention - AI system has significant issues";
        }
    }
    
    void LogOverallResults()
    {
        Debug.Log("=== OVERALL TEST RESULTS SUMMARY ===");
        Debug.Log($"All Tests Completed: {overallResults.AllTestsCompleted}");
        Debug.Log($"Overall Success Rate: {overallResults.OverallSuccessRate:F1}%");
        Debug.Log($"Total Tests: {overallResults.TotalTestsRun}");
        Debug.Log($"Passed: {overallResults.TotalTestsPassed}");
        Debug.Log($"Failed: {overallResults.TotalTestsFailed}");
        Debug.Log($"Status: {overallResults.OverallStatus}");
        
        Debug.Log("--- Framework Breakdown ---");
        foreach (var framework in overallResults.FrameworkResults)
        {
            Debug.Log($"{framework.FrameworkName}: {framework.SuccessRate:F1}% - {framework.Status} ({framework.ExecutionTime:F2}s)");
        }
        
        // Provide recommendations
        if (overallResults.OverallSuccessRate < 85f)
        {
            Debug.LogWarning("⚠️ RECOMMENDATION: AI system needs attention before production use.");
            
            foreach (var framework in overallResults.FrameworkResults)
            {
                if (framework.SuccessRate < 80f)
                {
                    Debug.LogWarning($"  - Focus on {framework.FrameworkName} issues");
                }
            }
        }
        else
        {
            Debug.Log("✅ AI system is ready for production use!");
        }
    }
    
    void StartContinuousMonitoring()
    {
        if (monitoringCoroutine != null)
        {
            StopCoroutine(monitoringCoroutine);
        }
        
        monitoringCoroutine = StartCoroutine(ContinuousMonitoringLoop());
        Debug.Log($"Started continuous AI monitoring (interval: {monitoringInterval}s)");
    }
    
    IEnumerator ContinuousMonitoringLoop()
    {
        while (enableContinuousMonitoring)
        {
            yield return new WaitForSeconds(monitoringInterval);
            
            // Run lightweight monitoring tests
            yield return StartCoroutine(RunMonitoringTests());
        }
    }
    
    IEnumerator RunMonitoringTests()
    {
        Debug.Log("--- Running Monitoring Tests ---");
        
        // Quick performance check
        if (performanceFramework != null)
        {
            // Run a subset of performance tests
            var testResults = performanceFramework.GetTestResults();
            if (testResults != null && testResults.SuccessRate < 80f)
            {
                Debug.LogWarning("Performance degradation detected during monitoring!");
            }
        }
        
        // Check balance if validator is available
        if (balanceValidator != null)
        {
            var balanceAnalysis = balanceValidator.GetBalanceAnalysis();
            if (balanceAnalysis != null && !balanceAnalysis.IsBalanced)
            {
                Debug.LogWarning("Balance issues detected during monitoring!");
            }
        }
        
        // Check behavior patterns
        if (behaviorDebugger != null)
        {
            var patterns = behaviorDebugger.GetBehaviorPatterns();
            if (patterns != null && patterns.PatternConfidence > 0.8f)
            {
                Debug.Log($"Behavior patterns stable (confidence: {patterns.PatternConfidence:P0})");
            }
        }
        
        yield return null;
    }
    
    [ContextMenu("Stop Continuous Monitoring")]
    public void StopContinuousMonitoring()
    {
        enableContinuousMonitoring = false;
        
        if (monitoringCoroutine != null)
        {
            StopCoroutine(monitoringCoroutine);
            monitoringCoroutine = null;
        }
        
        Debug.Log("Stopped continuous AI monitoring");
    }
    
    [ContextMenu("Quick Health Check")]
    public void RunQuickHealthCheck()
    {
        Debug.Log("=== AI SYSTEM QUICK HEALTH CHECK ===");
        
        // Check component availability
        int availableComponents = 0;
        int totalComponents = 4;
        
        if (performanceFramework != null) availableComponents++;
        if (balanceValidator != null) availableComponents++;
        if (behaviorDebugger != null) availableComponents++;
        if (legacyTester != null) availableComponents++;
        
        Debug.Log($"Testing Components Available: {availableComponents}/{totalComponents}");
        
        // Quick validation of key AI components
        var aiComponents = FindObjectsOfType<MonoBehaviour>()
            .Where(mb => mb.GetType().Namespace == "HybridEnemyAI" || 
                        mb.GetType().Name.StartsWith("AI"))
            .ToList();
        
        Debug.Log($"AI Components Found: {aiComponents.Count}");
        
        foreach (var component in aiComponents.Take(10)) // Show first 10
        {
            Debug.Log($"  - {component.GetType().Name}");
        }
        
        // Overall health status
        float healthScore = (availableComponents / (float)totalComponents) * 100f;
        
        if (healthScore >= 75f)
        {
            Debug.Log($"✅ AI System Health: {healthScore:F0}% - Good");
        }
        else if (healthScore >= 50f)
        {
            Debug.Log($"⚠️ AI System Health: {healthScore:F0}% - Needs Attention");
        }
        else
        {
            Debug.Log($"❌ AI System Health: {healthScore:F0}% - Critical Issues");
        }
    }
    
    /// <summary>
    /// Get overall testing summary for external use
    /// </summary>
    public TestingSummary GetOverallResults()
    {
        return overallResults;
    }
    
    /// <summary>
    /// Export all testing data for external analysis
    /// </summary>
    [ContextMenu("Export All Testing Data")]
    public void ExportAllTestingData()
    {
        var exportData = new
        {
            OverallResults = overallResults,
            PerformanceResults = performanceFramework?.GetTestResults(),
            BalanceAnalysis = balanceValidator?.GetBalanceAnalysis(),
            BehaviorPatterns = behaviorDebugger?.GetBehaviorPatterns(),
            Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        var json = JsonUtility.ToJson(exportData, true);
        Debug.Log("=== COMPREHENSIVE AI TESTING DATA EXPORT ===");
        Debug.Log(json);
    }
    
    void OnDestroy()
    {
        StopContinuousMonitoring();
    }
}