using UnityEngine;
using System.Collections;
using HybridEnemyAI;

/// <summary>
/// Validates AI configuration system and ensures all settings work correctly
/// </summary>
public class AISettingsValidator : MonoBehaviour
{
    [Header("Validation Settings")]
    public bool runValidationOnStart = false;
    public bool enableDetailedLogs = true;
    
    [Header("Components to Test")]
    public AIConfigurationManager configManager;
    public AIDifficultyManager difficultyManager;
    public AITurnExecutor aiTurnExecutor;
    public AIGameStateAnalyzer gameStateAnalyzer;
    public AIRiskCalculator riskCalculator;
    
    [Header("Validation Results")]
    public bool configurationSystemWorking = false;
    public bool difficultyPresetsWorking = false;
    public bool runtimeAdjustmentWorking = false;
    public bool componentIntegrationWorking = false;
    
    private bool validationInProgress = false;
    
    void Start()
    {
        if (runValidationOnStart)
        {
            StartCoroutine(RunSettingsValidation());
        }
    }
    
    /// <summary>
    /// Runs complete validation of AI configuration system
    /// </summary>
    [ContextMenu("Validate AI Settings")]
    public void ValidateSettings()
    {
        if (validationInProgress)
        {
            Debug.LogWarning("Settings validation already in progress!");
            return;
        }
        
        StartCoroutine(RunSettingsValidation());
    }
    
    IEnumerator RunSettingsValidation()
    {
        validationInProgress = true;
        
        Debug.Log("=== STARTING AI SETTINGS VALIDATION ===");
        
        // Initialize components
        InitializeComponents();
        
        // Test 1: Configuration Manager
        yield return StartCoroutine(TestConfigurationManager());
        
        // Test 2: Difficulty Presets
        yield return StartCoroutine(TestDifficultyPresets());
        
        // Test 3: Runtime Adjustments
        yield return StartCoroutine(TestRuntimeAdjustments());
        
        // Test 4: Component Integration
        yield return StartCoroutine(TestComponentIntegration());
        
        // Generate validation report
        GenerateValidationReport();
        
        validationInProgress = false;
        Debug.Log("=== AI SETTINGS VALIDATION COMPLETE ===");
    }
    
    void InitializeComponents()
    {
        if (configManager == null) configManager = FindObjectOfType<AIConfigurationManager>();
        if (difficultyManager == null) difficultyManager = FindObjectOfType<AIDifficultyManager>();
        if (aiTurnExecutor == null) aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        if (gameStateAnalyzer == null) gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
        if (riskCalculator == null) riskCalculator = FindObjectOfType<AIRiskCalculator>();
    }
    
    IEnumerator TestConfigurationManager()
    {
        Debug.Log("--- Testing Configuration Manager ---");
        
        if (configManager == null)
        {
            Debug.LogError("AIConfigurationManager not found!");
            configurationSystemWorking = false;
            yield break;
        }
        
        // Test getting current configuration
        AIConfiguration currentConfig = configManager.GetCurrentConfiguration();
        bool configRetrievalWorks = currentConfig != null;
        
        // Test applying configuration
        if (configRetrievalWorks)
        {
            configManager.ApplyCurrentConfiguration();
            yield return new WaitForSeconds(0.1f);
        }
        
        // Test reset to default
        configManager.ResetToDefault();
        yield return new WaitForSeconds(0.1f);
        
        configurationSystemWorking = configRetrievalWorks;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Configuration Manager: {(configurationSystemWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestDifficultyPresets()
    {
        Debug.Log("--- Testing Difficulty Presets ---");
        
        if (difficultyManager == null)
        {
            Debug.LogWarning("AIDifficultyManager not found - skipping difficulty preset tests");
            difficultyPresetsWorking = false;
            yield break;
        }
        
        bool allPresetsWork = true;
        
        // Test Easy difficulty
        difficultyManager.SetDifficulty(AIDifficultyManager.AIDifficultyLevel.Easy);
        yield return new WaitForSeconds(0.1f);
        
        AIConfiguration easyConfig = difficultyManager.GetCurrentConfiguration();
        bool easyWorks = easyConfig != null && easyConfig.PointsCapAggressive <= 400;
        
        if (!easyWorks)
        {
            Debug.LogError("Easy difficulty preset failed!");
            allPresetsWork = false;
        }
        
        // Test Medium difficulty
        difficultyManager.SetDifficulty(AIDifficultyManager.AIDifficultyLevel.Medium);
        yield return new WaitForSeconds(0.1f);
        
        AIConfiguration mediumConfig = difficultyManager.GetCurrentConfiguration();
        bool mediumWorks = mediumConfig != null && 
                          mediumConfig.PointsCapAggressive > easyConfig.PointsCapAggressive &&
                          mediumConfig.PointsCapAggressive < 600;
        
        if (!mediumWorks)
        {
            Debug.LogError("Medium difficulty preset failed!");
            allPresetsWork = false;
        }
        
        // Test Hard difficulty
        difficultyManager.SetDifficulty(AIDifficultyManager.AIDifficultyLevel.Hard);
        yield return new WaitForSeconds(0.1f);
        
        AIConfiguration hardConfig = difficultyManager.GetCurrentConfiguration();
        bool hardWorks = hardConfig != null && 
                        hardConfig.PointsCapAggressive > mediumConfig.PointsCapAggressive;
        
        if (!hardWorks)
        {
            Debug.LogError("Hard difficulty preset failed!");
            allPresetsWork = false;
        }
        
        difficultyPresetsWorking = allPresetsWork;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Easy Config - Aggressive Cap: {easyConfig?.PointsCapAggressive}");
            Debug.Log($"Medium Config - Aggressive Cap: {mediumConfig?.PointsCapAggressive}");
            Debug.Log($"Hard Config - Aggressive Cap: {hardConfig?.PointsCapAggressive}");
            Debug.Log($"Difficulty Presets: {(difficultyPresetsWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestRuntimeAdjustments()
    {
        Debug.Log("--- Testing Runtime Adjustments ---");
        
        if (configManager == null)
        {
            Debug.LogError("Cannot test runtime adjustments - AIConfigurationManager missing");
            runtimeAdjustmentWorking = false;
            yield break;
        }
        
        // Get initial configuration
        AIConfiguration initialConfig = configManager.GetCurrentConfiguration();
        
        // Create modified configuration
        AIConfiguration modifiedConfig = new AIConfiguration
        {
            PointsCapAggressive = initialConfig.PointsCapAggressive + 100,
            PointsCapPassive = initialConfig.PointsCapPassive + 50,
            InitialBufferCap = initialConfig.InitialBufferCap + 25,
            MomentumReductionPerSuccess = initialConfig.MomentumReductionPerSuccess + 0.02f
        };
        
        // Apply modified configuration
        configManager.SetConfiguration(modifiedConfig);
        yield return new WaitForSeconds(0.1f);
        
        // Verify changes were applied
        AIConfiguration appliedConfig = configManager.GetCurrentConfiguration();
        
        bool adjustmentsWork = appliedConfig != null &&
                              appliedConfig.PointsCapAggressive == modifiedConfig.PointsCapAggressive &&
                              appliedConfig.PointsCapPassive == modifiedConfig.PointsCapPassive;
        
        // Reset to original configuration
        configManager.SetConfiguration(initialConfig);
        yield return new WaitForSeconds(0.1f);
        
        runtimeAdjustmentWorking = adjustmentsWork;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Initial Aggressive Cap: {initialConfig.PointsCapAggressive}");
            Debug.Log($"Modified Aggressive Cap: {appliedConfig?.PointsCapAggressive}");
            Debug.Log($"Runtime Adjustments: {(runtimeAdjustmentWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestComponentIntegration()
    {
        Debug.Log("--- Testing Component Integration ---");
        
        bool integrationWorks = true;
        
        // Test AITurnExecutor integration
        if (aiTurnExecutor != null && configManager != null)
        {
            AIConfiguration testConfig = configManager.GetCurrentConfiguration();
            aiTurnExecutor.UpdateConfiguration(testConfig);
            yield return new WaitForSeconds(0.1f);
            
            if (enableDetailedLogs)
            {
                Debug.Log("AITurnExecutor configuration updated successfully");
            }
        }
        else
        {
            Debug.LogWarning("AITurnExecutor or ConfigurationManager missing - skipping integration test");
            integrationWorks = false;
        }
        
        // Test AIGameStateAnalyzer integration
        if (gameStateAnalyzer != null && configManager != null)
        {
            AIConfiguration testConfig = configManager.GetCurrentConfiguration();
            gameStateAnalyzer.config = testConfig;
            yield return new WaitForSeconds(0.1f);
            
            if (enableDetailedLogs)
            {
                Debug.Log("AIGameStateAnalyzer configuration updated successfully");
            }
        }
        else
        {
            Debug.LogWarning("AIGameStateAnalyzer or ConfigurationManager missing - skipping integration test");
        }
        
        // Test AIRiskCalculator integration
        if (riskCalculator != null && configManager != null)
        {
            AIConfiguration testConfig = configManager.GetCurrentConfiguration();
            riskCalculator.UpdateConfiguration(testConfig);
            yield return new WaitForSeconds(0.1f);
            
            if (enableDetailedLogs)
            {
                Debug.Log("AIRiskCalculator configuration updated successfully");
            }
        }
        else
        {
            Debug.LogWarning("AIRiskCalculator or ConfigurationManager missing - skipping integration test");
        }
        
        componentIntegrationWorking = integrationWorks;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Component Integration: {(componentIntegrationWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    void GenerateValidationReport()
    {
        Debug.Log("=== AI SETTINGS VALIDATION REPORT ===");
        
        // Component availability
        Debug.Log("--- Component Availability ---");
        Debug.Log($"AIConfigurationManager: {(configManager != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AIDifficultyManager: {(difficultyManager != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AITurnExecutor: {(aiTurnExecutor != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AIGameStateAnalyzer: {(gameStateAnalyzer != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AIRiskCalculator: {(riskCalculator != null ? "FOUND" : "MISSING")}");
        
        // Test results
        Debug.Log("--- Test Results ---");
        Debug.Log($"Configuration System: {(configurationSystemWorking ? "PASSED" : "FAILED")}");
        Debug.Log($"Difficulty Presets: {(difficultyPresetsWorking ? "PASSED" : "FAILED")}");
        Debug.Log($"Runtime Adjustments: {(runtimeAdjustmentWorking ? "PASSED" : "FAILED")}");
        Debug.Log($"Component Integration: {(componentIntegrationWorking ? "PASSED" : "FAILED")}");
        
        // Overall status
        bool overallSuccess = configurationSystemWorking && difficultyPresetsWorking && 
                             runtimeAdjustmentWorking && componentIntegrationWorking;
        
        Debug.Log($"--- OVERALL STATUS: {(overallSuccess ? "PASSED" : "FAILED")} ---");
        
        if (overallSuccess)
        {
            Debug.Log("All AI configuration tests passed! The system is ready for use.");
        }
        else
        {
            Debug.LogWarning("Some AI configuration tests failed. Check individual test results above.");
        }
        
        // Recommendations
        if (!configurationSystemWorking)
        {
            Debug.LogError("CRITICAL: Configuration system not working - AI behavior cannot be adjusted");
        }
        
        if (!difficultyPresetsWorking)
        {
            Debug.LogWarning("Difficulty presets not working - players cannot easily adjust AI difficulty");
        }
        
        if (!componentIntegrationWorking)
        {
            Debug.LogWarning("Component integration issues - configuration changes may not take effect");
        }
    }
    
    /// <summary>
    /// Quick test for manual validation
    /// </summary>
    [ContextMenu("Quick Settings Test")]
    public void QuickTest()
    {
        ValidateSettings();
    }
    
    /// <summary>
    /// Test specific difficulty preset
    /// </summary>
    [ContextMenu("Test Difficulty Presets")]
    public void TestDifficultyPresetsOnly()
    {
        StartCoroutine(TestDifficultyPresets());
    }
    
    /// <summary>
    /// Test configuration manager only
    /// </summary>
    [ContextMenu("Test Configuration Manager")]
    public void TestConfigurationManagerOnly()
    {
        StartCoroutine(TestConfigurationManager());
    }
}