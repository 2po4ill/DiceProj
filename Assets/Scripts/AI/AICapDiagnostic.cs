using UnityEngine;
using HybridEnemyAI;

/// <summary>
/// Diagnostic tool to check why AI cap is stuck at 500
/// </summary>
public class AICapDiagnostic : MonoBehaviour
{
    [ContextMenu("Diagnose Cap Issue")]
    public void DiagnoseCapIssue()
    {
        Debug.Log("=== AI CAP DIAGNOSTIC ===");
        
        // Find all AI components
        var gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
        var turnExecutor = FindObjectOfType<AITurnExecutor>();
        var decisionEngine = FindObjectOfType<AIDecisionEngine>();
        var dualProbCap = FindObjectOfType<AIDualProbabilityCapSystem>();
        
        Debug.Log("\n--- COMPONENT CHECK ---");
        Debug.Log($"AIGameStateAnalyzer: {(gameStateAnalyzer != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AITurnExecutor: {(turnExecutor != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AIDecisionEngine: {(decisionEngine != null ? "FOUND" : "MISSING")}");
        Debug.Log($"AIDualProbabilityCapSystem: {(dualProbCap != null ? "FOUND" : "MISSING")}");
        
        if (gameStateAnalyzer != null)
        {
            Debug.Log("\n--- AIGameStateAnalyzer CONFIG ---");
            if (gameStateAnalyzer.config == null)
            {
                Debug.LogError("❌ AIGameStateAnalyzer.config is NULL!");
                Debug.Log("This is why it's using default values (500/250)");
            }
            else
            {
                Debug.Log($"✓ Config assigned");
                Debug.Log($"  PointsCapAggressive: {gameStateAnalyzer.config.PointsCapAggressive}");
                Debug.Log($"  PointsCapPassive: {gameStateAnalyzer.config.PointsCapPassive}");
                
                // Test the method
                int aggressiveCap = gameStateAnalyzer.GetPointsPerTurnCap(BehaviorMode.AGGRESSIVE);
                int passiveCap = gameStateAnalyzer.GetPointsPerTurnCap(BehaviorMode.PASSIVE);
                Debug.Log($"  GetPointsPerTurnCap(AGGRESSIVE): {aggressiveCap}");
                Debug.Log($"  GetPointsPerTurnCap(PASSIVE): {passiveCap}");
            }
        }
        
        if (turnExecutor != null)
        {
            Debug.Log("\n--- AITurnExecutor CONFIG ---");
            if (turnExecutor.aiConfig == null)
            {
                Debug.LogWarning("⚠ AITurnExecutor.aiConfig is NULL");
            }
            else
            {
                Debug.Log($"✓ Config assigned");
                Debug.Log($"  PointsCapAggressive: {turnExecutor.aiConfig.PointsCapAggressive}");
                Debug.Log($"  PointsCapPassive: {turnExecutor.aiConfig.PointsCapPassive}");
            }
            
            if (turnExecutor.gameStateAnalyzer == null)
            {
                Debug.LogError("❌ AITurnExecutor.gameStateAnalyzer is NULL!");
                Debug.Log("This means InitializeTurnState will fail to get proper cap!");
            }
            else
            {
                Debug.Log($"✓ gameStateAnalyzer reference assigned");
            }
        }
        
        if (decisionEngine != null)
        {
            Debug.Log("\n--- AIDecisionEngine CONFIG ---");
            if (decisionEngine.config == null)
            {
                Debug.LogWarning("⚠ AIDecisionEngine.config is NULL");
            }
            else
            {
                Debug.Log($"✓ Config assigned");
                Debug.Log($"  PointsCapAggressive: {decisionEngine.config.PointsCapAggressive}");
                Debug.Log($"  PointsCapPassive: {decisionEngine.config.PointsCapPassive}");
            }
            
            if (decisionEngine.gameStateAnalyzer == null)
            {
                Debug.LogError("❌ AIDecisionEngine.gameStateAnalyzer is NULL!");
                Debug.Log("This means it will use fallback values (500/250)!");
            }
            else
            {
                Debug.Log($"✓ gameStateAnalyzer reference assigned");
            }
        }
        
        if (dualProbCap != null)
        {
            Debug.Log("\n--- AIDualProbabilityCapSystem CONFIG ---");
            if (dualProbCap.config == null)
            {
                Debug.LogWarning("⚠ AIDualProbabilityCapSystem.config is NULL");
            }
            else
            {
                Debug.Log($"✓ Config assigned");
                Debug.Log($"  PointsCapAggressive: {dualProbCap.config.PointsCapAggressive}");
                Debug.Log($"  PointsCapPassive: {dualProbCap.config.PointsCapPassive}");
            }
            
            var state = dualProbCap.GetCurrentState();
            if (state != null)
            {
                Debug.Log($"  Current State:");
                Debug.Log($"    CurrentPointsPerTurnCap: {state.CurrentPointsPerTurnCap}");
                Debug.Log($"    AggressiveCapMin: {state.AggressiveCapMin}");
                Debug.Log($"    AggressiveCapMax: {state.AggressiveCapMax}");
                Debug.Log($"    PassiveCapMin: {state.PassiveCapMin}");
                Debug.Log($"    PassiveCapMax: {state.PassiveCapMax}");
            }
        }
        
        Debug.Log("\n=== DIAGNOSIS COMPLETE ===");
        Debug.Log("\nSOLUTION:");
        Debug.Log("1. Create an AIConfiguration ScriptableObject or GameObject");
        Debug.Log("2. Set PointsCapAggressive and PointsCapPassive to your desired values");
        Debug.Log("3. Assign this config to ALL AI components in the Inspector");
        Debug.Log("4. Make sure AITurnExecutor.gameStateAnalyzer is assigned");
        Debug.Log("5. Make sure AIDecisionEngine.gameStateAnalyzer is assigned");
    }
}
