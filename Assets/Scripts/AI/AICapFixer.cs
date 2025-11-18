using UnityEngine;
using HybridEnemyAI;

/// <summary>
/// Fixes the AI cap issue by ensuring all components share the same configuration
/// </summary>
public class AICapFixer : MonoBehaviour
{
    [Header("Cap Settings")]
    [Tooltip("Points per turn cap for aggressive mode")]
    public int AggressivePointsCap = 500;
    
    [Tooltip("Points per turn cap for passive mode")]
    public int PassivePointsCap = 250;
    
    [Header("Auto-Fix on Start")]
    public bool autoFixOnStart = true;
    
    void Start()
    {
        if (autoFixOnStart)
        {
            FixCapConfiguration();
        }
    }
    
    [ContextMenu("Fix AI Cap Configuration")]
    public void FixCapConfiguration()
    {
        Debug.Log("=== FIXING AI CAP CONFIGURATION ===");
        
        // Create a shared configuration
        AIConfiguration sharedConfig = new AIConfiguration();
        sharedConfig.PointsCapAggressive = AggressivePointsCap;
        sharedConfig.PointsCapPassive = PassivePointsCap;
        
        Debug.Log($"Created shared config: Aggressive={AggressivePointsCap}, Passive={PassivePointsCap}");
        
        // Find all AI components and assign the config
        var gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
        var turnExecutor = FindObjectOfType<AITurnExecutor>();
        var decisionEngine = FindObjectOfType<AIDecisionEngine>();
        var dualProbCap = FindObjectOfType<AIDualProbabilityCapSystem>();
        var riskCalculator = FindObjectOfType<AIRiskCalculator>();
        var combinationStrategy = FindObjectOfType<AICombinationStrategy>();
        var aggressiveReroll = FindObjectOfType<AIAggressiveRerollStrategy>();
        
        int fixedCount = 0;
        
        if (gameStateAnalyzer != null)
        {
            gameStateAnalyzer.config = sharedConfig;
            Debug.Log("✓ Fixed AIGameStateAnalyzer.config");
            fixedCount++;
        }
        
        if (turnExecutor != null)
        {
            turnExecutor.aiConfig = sharedConfig;
            
            // Also ensure gameStateAnalyzer reference is set
            if (turnExecutor.gameStateAnalyzer == null && gameStateAnalyzer != null)
            {
                turnExecutor.gameStateAnalyzer = gameStateAnalyzer;
                Debug.Log("✓ Fixed AITurnExecutor.gameStateAnalyzer reference");
            }
            
            Debug.Log("✓ Fixed AITurnExecutor.aiConfig");
            fixedCount++;
        }
        
        if (decisionEngine != null)
        {
            decisionEngine.config = sharedConfig;
            
            // Also ensure gameStateAnalyzer reference is set
            if (decisionEngine.gameStateAnalyzer == null && gameStateAnalyzer != null)
            {
                decisionEngine.gameStateAnalyzer = gameStateAnalyzer;
                Debug.Log("✓ Fixed AIDecisionEngine.gameStateAnalyzer reference");
            }
            
            Debug.Log("✓ Fixed AIDecisionEngine.config");
            fixedCount++;
        }
        
        if (dualProbCap != null)
        {
            dualProbCap.config = sharedConfig;
            
            // Update the internal state to use new cap values
            var state = dualProbCap.GetCurrentState();
            if (state != null)
            {
                state.AggressiveCapMin = AggressivePointsCap - 100;
                state.AggressiveCapMax = AggressivePointsCap + 100;
                state.PassiveCapMin = PassivePointsCap - 50;
                state.PassiveCapMax = PassivePointsCap + 50;
                state.CurrentPointsPerTurnCap = AggressivePointsCap;
                Debug.Log($"✓ Updated AIDualProbabilityCapSystem state ranges");
            }
            
            Debug.Log("✓ Fixed AIDualProbabilityCapSystem.config");
            fixedCount++;
        }
        
        if (riskCalculator != null)
        {
            riskCalculator.config = sharedConfig;
            Debug.Log("✓ Fixed AIRiskCalculator.config");
            fixedCount++;
        }
        
        if (combinationStrategy != null)
        {
            combinationStrategy.config = sharedConfig;
            Debug.Log("✓ Fixed AICombinationStrategy.config");
            fixedCount++;
        }
        
        if (aggressiveReroll != null)
        {
            aggressiveReroll.config = sharedConfig;
            Debug.Log("✓ Fixed AIAggressiveRerollStrategy.config");
            fixedCount++;
        }
        
        Debug.Log($"\n=== FIX COMPLETE: {fixedCount} components updated ===");
        Debug.Log($"AI will now use cap values: Aggressive={AggressivePointsCap}, Passive={PassivePointsCap}");
        Debug.Log("\nTo change caps in the future:");
        Debug.Log("1. Adjust AggressivePointsCap and PassivePointsCap in THIS component's Inspector");
        Debug.Log("2. Right-click this component → 'Fix AI Cap Configuration'");
        Debug.Log("3. Or just restart the game (autoFixOnStart is enabled)");
    }
    
    [ContextMenu("Test Current Cap Values")]
    public void TestCurrentCapValues()
    {
        Debug.Log("=== TESTING CURRENT CAP VALUES ===");
        
        var gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
        if (gameStateAnalyzer != null)
        {
            int aggressiveCap = gameStateAnalyzer.GetPointsPerTurnCap(BehaviorMode.AGGRESSIVE);
            int passiveCap = gameStateAnalyzer.GetPointsPerTurnCap(BehaviorMode.PASSIVE);
            
            Debug.Log($"Current caps from AIGameStateAnalyzer:");
            Debug.Log($"  AGGRESSIVE: {aggressiveCap}");
            Debug.Log($"  PASSIVE: {passiveCap}");
            
            if (aggressiveCap == AggressivePointsCap && passiveCap == PassivePointsCap)
            {
                Debug.Log("✓ Caps match the configured values!");
            }
            else
            {
                Debug.LogWarning($"⚠ Caps don't match! Expected Aggressive={AggressivePointsCap}, Passive={PassivePointsCap}");
                Debug.LogWarning("Run 'Fix AI Cap Configuration' to update");
            }
        }
        else
        {
            Debug.LogError("❌ AIGameStateAnalyzer not found!");
        }
    }
}
