using UnityEngine;

/// <summary>
/// Simple script to run AI tests via code or Inspector
/// </summary>
public class AITestRunner : MonoBehaviour
{
    [Header("Test Components")]
    public AITester tester;
    
    [Header("Quick Actions")]
    [SerializeField] private bool runTestsOnStart = false;
    
    void Start()
    {
        if (runTestsOnStart && tester != null)
        {
            RunAllTests();
        }
    }
    
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        if (tester == null)
        {
            Debug.LogError("AITester not assigned!");
            return;
        }
        
        StartCoroutine(tester.RunAllTests());
    }
    
    [ContextMenu("Validate Completed Tasks Only")]
    public void ValidateCompletedTasks()
    {
        if (tester == null)
        {
            Debug.LogError("AITester not assigned!");
            return;
        }
        
        tester.ValidateCompletedTasks();
    }
    
    [ContextMenu("Run Performance Test")]
    public void RunPerformanceTest()
    {
        if (tester == null)
        {
            Debug.LogError("AITester not assigned!");
            return;
        }
        
        tester.RunPerformanceTest();
    }
    
    // Method you can call from other scripts
    public void RunTestsViaScript()
    {
        if (tester != null)
        {
            StartCoroutine(tester.RunAllTests());
        }
        else
        {
            Debug.LogError("AITester component not found or assigned!");
        }
    }
    
    // Get test results programmatically
    public (int total, int passed, int failed) GetLastTestResults()
    {
        if (tester != null)
        {
            return (tester.totalTests, tester.passedTests, tester.failedTests);
        }
        return (0, 0, 0);
    }
}