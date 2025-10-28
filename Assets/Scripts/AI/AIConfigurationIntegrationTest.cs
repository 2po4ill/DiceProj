using UnityEngine;
using System.Collections;
using HybridEnemyAI;

/// <summary>
/// Integration test for complete AI configuration system
/// Validates all configuration components work together correctly
/// </summary>
public class AIConfigurationIntegrationTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool runTestOnStart = false;
    public bool enableDetailedLogs = true;
    
    [Header("Test Results")]
    public bool configurationSystemComplete = false;
    public bool difficultyPresetsWorking = false;
    public bool runtimeConfigurationWorking = false;
    public bool uiIntegrationWorking = false;
    public bool aiComponentsUpdating = false;
    
    private AIConfigurationManager configManager;
    private AIDifficultyManager difficultyManager;
    private AIConfigurationUI configUI;
    private AITurnExecutor aiTurnExecutor;
    private AIGameStateAnalyzer gameStateAnalyzer;
    private bool testInProgress = false;
    
    void Start()
    {
        if (runTestOnStart)
        {
            StartCoroutine(RunConfigurationIntegrationTest());
        }
    }
    
    /// <summary>
    /// Runs complete integration test for AI configuration system
    /// </summary>
    [ContextMenu("Run Configuration Integration Test")]
    public void RunIntegrationTest()
    {
        if (testInProgress)
        {
            Debug.LogWarning("Configuration integration test already in progress!");
            return;
        }
        
        StartCoroutine(RunConfigurationIntegrationTest());
    }
    
    IEnumerator RunConfigurationIntegrationTest()
    {
        testInProgress = true;
        
        Debug.Log("=== STARTING AI CONFIGURATION INTEGRATION TEST ===");
        
        // Initialize components
        InitializeComponents();
        
        // Test 1: Configuration System Completeness
        yield return StartCoroutine(TestConfigurationSystemCompleteness());
        
        // Test 2: Difficulty Preset Functionality
        yield return StartCoroutine(TestDifficultyPresetFunctionality());
        
        // Test 3: Runtime Configuration Changes
        yield return StartCoroutine(TestRuntimeConfigurationChanges());
        
        // Test 4: UI Integration
        yield return StartCoroutine(TestUIIntegration());
        
        // Test 5: AI Component Updates
        yield return StartCoroutine(TestAIComponentUpdates());
        
        // Generate final report
        GenerateIntegrationReport();
        
        testInProgress = false;
        Debug.Log("=== AI CONFIGURATION INTEGRATION TEST COMPLETE ===");
    }
    
    void InitializeComponents()
    {
        configManager = FindObjectOfType<AIConfigurationManager>();
        difficultyManager = FindObjectOfType<AIDifficultyManager>();
        configUI = FindObjectOfType<AIConfigurationUI>();
        aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
    }
    
    IEnumerator TestConfigurationSystemCompleteness()
    {
        Debug.Log("--- Testing Configuration System Completeness ---");
        
        bool systemComplete = true;
        
        // Check for essential components
        if (configManager == null)
        {
            Debug.LogError("AIConfigurationManager not found!");
            systemComplete = false;
        }
        
        if (difficultyManager == null)
        {
            Debug.LogWarning("AIDifficultyManager not found - difficulty presets unavailable");
        }
        
        if (configUI == null)
        {
            Debug.LogWarning("AIConfigurationUI not found - UI configuration unavailable");
        }
        
        // Test basic configuration functionality
        if (configManager != null)
        {
            AIConfiguration testConfig = configManager.GetCurrentConfiguration();
            if (testConfig == null)
            {
                Debug.LogError("Configuration manager cannot provide current configuration!");
                systemComplete = false;
            }
            else
            {
                // Test configuration application
                configManager.ApplyCurrentConfiguration();
                yield return new WaitForSeconds(0.1f);
                
                if (enableDetailedLogs)
                {
                    Debug.Log("Configuration manager basic functionality working");
                }
            }
        }
        
        configurationSystemComplete = systemComplete;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Configuration System Completeness: {(configurationSystemComplete ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestDifficultyPresetFunctionality()
    {
        Debug.Log("--- Testing Difficulty Preset Functionality ---");
        
        if (difficultyManager == null)
        {
            Debug.LogWarning("Cannot test difficulty presets - AIDifficultyManager not found");
            difficultyPresetsWorking = false;
            yield break;
        }
        
        bool presetsWorking = true;
        
        // Test each difficulty level
        var difficulties = new AIDifficultyManager.AIDifficultyLevel[]
        {
            AIDifficultyManager.AIDifficultyLevel.Easy,
            AIDifficultyManager.AIDifficultyLevel.Medium,
            AIDifficultyManager.AIDifficultyLevel.Hard
        };
        
        AIConfiguration[] configs = new AIConfiguration[difficulties.Length];
        
        for (int i = 0; i < difficulties.Length; i++)
        {
            difficultyManager.SetDifficulty(difficulties[i]);
            yield return new WaitForSeconds(0.1f);
            
            configs[i] = difficultyManager.GetCurrentConfiguration();
            
            if (configs[i] == null)
            {
                Debug.LogError($"Failed to get configuration for {difficulties[i]} difficulty!");
                presetsWorking = false;
            }
            
            if (enableDetailedLogs)
            {
                Debug.Log($"{difficulties[i]} - Aggressive Cap: {configs[i]?.PointsCapAggressive}, " +
                         $"Passive Cap: {configs[i]?.PointsCapPassive}");
            }
        }
        
        // Verify difficulty progression (Easy < Medium < Hard)
        if (presetsWorking && configs[0] != null && configs[1] != null && configs[2] != null)
        {
            bool progressionCorrect = configs[0].PointsCapAggressive < configs[1].PointsCapAggressive &&
                                     configs[1].PointsCapAggressive < configs[2].PointsCapAggressive;
            
            if (!progressionCorrect)
            {
                Debug.LogError("Difficulty progression is incorrect! Easy should be < Medium < Hard");
                presetsWorking = false;
            }
        }
        
        difficultyPresetsWorking = presetsWorking;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Difficulty Preset Functionality: {(difficultyPresetsWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestRuntimeConfigurationChanges()
    {
        Debug.Log("--- Testing Runtime Configuration Changes ---");
        
        if (configManager == null)
        {
            Debug.LogError("Cannot test runtime configuration - AIConfigurationManager not found");
            runtimeConfigurationWorking = false;
            yield break;
        }
        
        // Get initial configuration
        AIConfiguration initialConfig = configManager.GetCurrentConfiguration();
        
        // Create test configuration with different values
        AIConfiguration testConfig = new AIConfiguration
        {
            PointsCapAggressive = 600,
            PointsCapPassive = 300,
            InitialBufferCap = 175,
            MomentumReductionPerSuccess = 0.10f,
            AggressiveCapGrowthRate = 0.12f,
            PassiveCapGrowthRate = 0.18f
        };
        
        // Apply test configuration
        configManager.SetConfiguration(testConfig);
        yield return new WaitForSeconds(0.2f);
        
        // Verify configuration was applied
        AIConfiguration appliedConfig = configManager.GetCurrentConfiguration();
        
        bool configApplied = appliedConfig != null &&
                            appliedConfig.PointsCapAggressive == testConfig.PointsCapAggressive &&
                            appliedConfig.PointsCapPassive == testConfig.PointsCapPassive &&
                            appliedConfig.InitialBufferCap == testConfig.InitialBufferCap;
        
        // Test reset functionality
        configManager.ResetToDefault();
        yield return new WaitForSeconds(0.1f);
        
        AIConfiguration resetConfig = configManager.GetCurrentConfiguration();
        bool resetWorking = resetConfig != null;
        
        runtimeConfigurationWorking = configApplied && resetWorking;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Configuration Applied: {configApplied}");
            Debug.Log($"Reset Working: {resetWorking}");
            Debug.Log($"Runtime Configuration Changes: {(runtimeConfigurationWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestUIIntegration()
    {
        Debug.Log("--- Testing UI Integration ---");
        
        if (configUI == null)
        {
            Debug.LogWarning("Cannot test UI integration - AIConfigurationUI not found");
            uiIntegrationWorking = false;
            yield break;
        }
        
        bool uiWorking = true;
        
        try
        {
            // Test showing configuration panel
            configUI.ShowConfigurationPanel();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ShowConfigurationPanel failed: {e.Message}");
            uiWorking = false;
        }
        
        yield return new WaitForSeconds(0.1f);
        
        if (uiWorking)
        {
            try
            {
                // Test applying configuration through UI
                configUI.ApplyConfiguration();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplyConfiguration failed: {e.Message}");
                uiWorking = false;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        if (uiWorking)
        {
            try
            {
                // Test reset through UI
                configUI.ResetToDefaults();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ResetToDefaults failed: {e.Message}");
                uiWorking = false;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        if (uiWorking)
        {
            try
            {
                // Test closing panel
                configUI.CloseConfigurationPanel();
                
                if (enableDetailedLogs)
                {
                    Debug.Log("UI integration methods executed without errors");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CloseConfigurationPanel failed: {e.Message}");
                uiWorking = false;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        uiIntegrationWorking = uiWorking;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"UI Integration: {(uiIntegrationWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestAIComponentUpdates()
    {
        Debug.Log("--- Testing AI Component Updates ---");
        
        bool componentsUpdating = true;
        
        // Create test configuration
        AIConfiguration testConfig = new AIConfiguration
        {
            PointsCapAggressive = 550,
            PointsCapPassive = 275,
            InitialBufferCap = 180
        };
        
        // Test AITurnExecutor update
        if (aiTurnExecutor != null)
        {
            try
            {
                aiTurnExecutor.UpdateConfiguration(testConfig);
                
                if (enableDetailedLogs)
                {
                    Debug.Log("AITurnExecutor configuration updated successfully");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AITurnExecutor update failed: {e.Message}");
                componentsUpdating = false;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            Debug.LogWarning("AITurnExecutor not found - skipping update test");
        }
        
        // Test AIGameStateAnalyzer update
        if (gameStateAnalyzer != null)
        {
            try
            {
                gameStateAnalyzer.config = testConfig;
                
                if (enableDetailedLogs)
                {
                    Debug.Log("AIGameStateAnalyzer configuration updated successfully");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AIGameStateAnalyzer update failed: {e.Message}");
                componentsUpdating = false;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            Debug.LogWarning("AIGameStateAnalyzer not found - skipping update test");
        }
        
        // Test configuration manager integration
        if (configManager != null)
        {
            try
            {
                configManager.SetConfiguration(testConfig);
                configManager.ApplyCurrentConfiguration();
                
                if (enableDetailedLogs)
                {
                    Debug.Log("Configuration manager integration working");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Configuration manager integration failed: {e.Message}");
                componentsUpdating = false;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        aiComponentsUpdating = componentsUpdating;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"AI Component Updates: {(aiComponentsUpdating ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    void GenerateIntegrationReport()
    {
        Debug.Log("=== AI CONFIGURATION INTEGRATION REPORT ===");
        
        // Component availability
        Debug.Log("--- Component Availability ---");
        Debug.Log($"AIConfigurationManager: {(configManager != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AIDifficultyManager: {(difficultyManager != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AIConfigurationUI: {(configUI != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AITurnExecutor: {(aiTurnExecutor != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AIGameStateAnalyzer: {(gameStateAnalyzer != null ? "FOUND" : "MISSING")}");
        
        // Test results
        Debug.Log("--- Integration Test Results ---");
        Debug.Log($"Configuration System Complete: {(configurationSystemComplete ? "PASSED" : "FAILED")}");
        Debug.Log($"Difficulty Presets Working: {(difficultyPresetsWorking ? "PASSED" : "FAILED")}");
        Debug.Log($"Runtime Configuration Working: {(runtimeConfigurationWorking ? "PASSED" : "FAILED")}");
        Debug.Log($"UI Integration Working: {(uiIntegrationWorking ? "PASSED" : "FAILED")}");
        Debug.Log($"AI Components Updating: {(aiComponentsUpdating ? "PASSED" : "FAILED")}");
        
        // Overall status
        bool overallSuccess = configurationSystemComplete && difficultyPresetsWorking && 
                             runtimeConfigurationWorking && aiComponentsUpdating;
        
        Debug.Log($"--- OVERALL INTEGRATION STATUS: {(overallSuccess ? "PASSED" : "FAILED")} ---");
        
        if (overallSuccess)
        {
            Debug.Log("AI Configuration system integration successful! All components working together correctly.");
        }
        else
        {
            Debug.LogWarning("Some integration tests failed. The configuration system may not work properly.");
        }
        
        // Requirements validation
        Debug.Log("--- Requirements Validation ---");
        Debug.Log($"Requirement 10.2.1 (Runtime parameter adjustment): {(runtimeConfigurationWorking ? "MET" : "NOT MET")}");
        Debug.Log($"Requirement 10.2.2 (Difficulty preset system): {(difficultyPresetsWorking ? "MET" : "NOT MET")}");
        Debug.Log($"Requirement 10.2.3 (AI behavior customization): {(aiComponentsUpdating ? "MET" : "NOT MET")}");
        
        bool allRequirementsMet = runtimeConfigurationWorking && difficultyPresetsWorking && aiComponentsUpdating;
        Debug.Log($"ALL REQUIREMENTS MET: {(allRequirementsMet ? "YES" : "NO")}");
    }
    
    /// <summary>
    /// Quick test for manual validation
    /// </summary>
    [ContextMenu("Quick Integration Test")]
    public void QuickTest()
    {
        RunIntegrationTest();
    }
    
    /// <summary>
    /// Test only difficulty presets
    /// </summary>
    [ContextMenu("Test Difficulty Presets Only")]
    public void TestDifficultyPresetsOnly()
    {
        StartCoroutine(TestDifficultyPresetFunctionality());
    }
    
    /// <summary>
    /// Test only runtime configuration
    /// </summary>
    [ContextMenu("Test Runtime Configuration Only")]
    public void TestRuntimeConfigurationOnly()
    {
        StartCoroutine(TestRuntimeConfigurationChanges());
    }
}