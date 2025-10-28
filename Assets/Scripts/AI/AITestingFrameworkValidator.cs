using UnityEngine;
using System.Collections;
using HybridEnemyAI;

/// <summary>
/// Validator for the AI testing framework itself - ensures all testing components work correctly
/// </summary>
public class AITestingFrameworkValidator : MonoBehaviour
{
    [Header("Framework Components")]
    public AITestingManager testingManager;
    public AIPerformanceTestFramework performanceFramework;
    public AIBalanceValidator balanceValidator;
    public AIBehaviorDebugger behaviorDebugger;
    
    [Header("Validation Results")]
    [SerializeField] private bool allComponentsValid = false;
    [SerializeField] private int validationsPassed = 0;
    [SerializeField] private int validationsFailed = 0;
    
    void Start()
    {
        StartCoroutine(ValidateTestingFramework());
    }
    
    IEnumerator ValidateTestingFramework()
    {
        Debug.Log("=== VALIDATING AI TESTING FRAMEWORK ===");
        
        validationsPassed = 0;
        validationsFailed = 0;
        
        // Test 1: Component availability
        yield return StartCoroutine(ValidateComponentAvailability());
        
        // Test 2: Basic functionality
        yield return StartCoroutine(ValidateBasicFunctionality());
        
        // Test 3: Integration
        yield return StartCoroutine(ValidateIntegration());
        
        // Final results
        allComponentsValid = validationsFailed == 0;
        
        Debug.Log($"=== FRAMEWORK VALIDATION COMPLETE ===");
        Debug.Log($"Validations Passed: {validationsPassed}");
        Debug.Log($"Validations Failed: {validationsFailed}");
        Debug.Log($"Framework Status: {(allComponentsValid ? "VALID" : "INVALID")}");
        
        if (allComponentsValid)
        {
            Debug.Log("🎉 AI Testing Framework is ready for use!");
        }
        else
        {
            Debug.LogWarning("⚠️ AI Testing Framework has issues that need addressing.");
        }
    }
    
    IEnumerator ValidateComponentAvailability()
    {
        Debug.Log("--- Validating Component Availability ---");
        
        // Check AITestingManager
        if (testingManager != null)
        {
            validationsPassed++;
            Debug.Log("✓ AITestingManager available");
        }
        else
        {
            validationsFailed++;
            Debug.LogError("✗ AITestingManager not found");
        }
        
        // Check AIPerformanceTestFramework
        if (performanceFramework != null)
        {
            validationsPassed++;
            Debug.Log("✓ AIPerformanceTestFramework available");
        }
        else
        {
            validationsFailed++;
            Debug.LogError("✗ AIPerformanceTestFramework not found");
        }
        
        // Check AIBalanceValidator
        if (balanceValidator != null)
        {
            validationsPassed++;
            Debug.Log("✓ AIBalanceValidator available");
        }
        else
        {
            validationsFailed++;
            Debug.LogError("✗ AIBalanceValidator not found");
        }
        
        // Check AIBehaviorDebugger
        if (behaviorDebugger != null)
        {
            validationsPassed++;
            Debug.Log("✓ AIBehaviorDebugger available");
        }
        else
        {
            validationsFailed++;
            Debug.LogError("✗ AIBehaviorDebugger not found");
        }
        
        yield return null;
    }
    
    IEnumerator ValidateBasicFunctionality()
    {
        Debug.Log("--- Validating Basic Functionality ---");
        
        // Test Performance Framework initialization
        if (performanceFramework != null)
        {
            try
            {
                var testResults = performanceFramework.GetTestResults();
                validationsPassed++;
                Debug.Log("✓ Performance Framework initialization works");
            }
            catch (System.Exception e)
            {
                validationsFailed++;
                Debug.LogError($"✗ Performance Framework initialization failed: {e.Message}");
            }
        }
        
        // Test Balance Validator initialization
        if (balanceValidator != null)
        {
            try
            {
                var balanceAnalysis = balanceValidator.GetBalanceAnalysis();
                validationsPassed++;
                Debug.Log("✓ Balance Validator initialization works");
            }
            catch (System.Exception e)
            {
                validationsFailed++;
                Debug.LogError($"✗ Balance Validator initialization failed: {e.Message}");
            }
        }
        
        // Test Behavior Debugger initialization
        if (behaviorDebugger != null)
        {
            try
            {
                var debugSession = behaviorDebugger.GetCurrentSession();
                validationsPassed++;
                Debug.Log("✓ Behavior Debugger initialization works");
            }
            catch (System.Exception e)
            {
                validationsFailed++;
                Debug.LogError($"✗ Behavior Debugger initialization failed: {e.Message}");
            }
        }
        
        yield return null;
    }
    
    IEnumerator ValidateIntegration()
    {
        Debug.Log("--- Validating Integration ---");
        
        // Test Testing Manager integration
        if (testingManager != null)
        {
            try
            {
                var overallResults = testingManager.GetOverallResults();
                validationsPassed++;
                Debug.Log("✓ Testing Manager integration works");
            }
            catch (System.Exception e)
            {
                validationsFailed++;
                Debug.LogError($"✗ Testing Manager integration failed: {e.Message}");
            }
        }
        
        // Test data export functionality
        try
        {
            if (testingManager != null)
            {
                testingManager.ExportAllTestingData();
                validationsPassed++;
                Debug.Log("✓ Data export functionality works");
            }
        }
        catch (System.Exception e)
        {
            validationsFailed++;
            Debug.LogError($"✗ Data export functionality failed: {e.Message}");
        }
        
        yield return null;
    }
    
    [ContextMenu("Run Quick Validation")]
    public void RunQuickValidation()
    {
        StartCoroutine(ValidateTestingFramework());
    }
    
    [ContextMenu("Test Framework Components")]
    public void TestFrameworkComponents()
    {
        Debug.Log("=== TESTING FRAMEWORK COMPONENTS ===");
        
        // Auto-find components if not assigned
        if (testingManager == null)
            testingManager = FindObjectOfType<AITestingManager>();
        
        if (performanceFramework == null)
            performanceFramework = FindObjectOfType<AIPerformanceTestFramework>();
        
        if (balanceValidator == null)
            balanceValidator = FindObjectOfType<AIBalanceValidator>();
        
        if (behaviorDebugger == null)
            behaviorDebugger = FindObjectOfType<AIBehaviorDebugger>();
        
        // Report findings
        Debug.Log($"AITestingManager: {(testingManager != null ? "Found" : "Not Found")}");
        Debug.Log($"AIPerformanceTestFramework: {(performanceFramework != null ? "Found" : "Not Found")}");
        Debug.Log($"AIBalanceValidator: {(balanceValidator != null ? "Found" : "Not Found")}");
        Debug.Log($"AIBehaviorDebugger: {(behaviorDebugger != null ? "Found" : "Not Found")}");
        
        if (testingManager != null)
        {
            testingManager.RunQuickHealthCheck();
        }
    }
}