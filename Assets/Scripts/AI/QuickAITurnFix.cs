using UnityEngine;
using HybridEnemyAI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Simple one-click fix for AI turn issues
/// Add this to GameTurnManager GameObject and click "Fix AI Turn Issues"
/// </summary>
public class QuickAITurnFix : MonoBehaviour
{
    private GameTurnManager turnManager;
    
    void Start()
    {
        // Try to find GameTurnManager component (case-insensitive)
        turnManager = GetComponent<GameTurnManager>();
        
        if (turnManager == null)
        {
            // Try to find any component with "TurnManager" in the name
            var components = GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name.ToLower().Contains("turnmanager"))
                {
                    turnManager = comp as GameTurnManager;
                    if (turnManager != null) break;
                }
            }
        }
    }
    
    [ContextMenu("Fix AI Turn Issues")]
    public void FixAITurnIssues()
    {
        if (turnManager == null)
        {
            // Try to find it again
            turnManager = GetComponent<GameTurnManager>();
            
            if (turnManager == null)
            {
                Debug.LogError("No GameTurnManager component found on this GameObject!");
                Debug.LogError("Make sure this script is on the same GameObject as your GameTurnManager script.");
                
                // List all components on this GameObject for debugging
                var components = GetComponents<MonoBehaviour>();
                Debug.Log($"Components found on {gameObject.name}:");
                foreach (var comp in components)
                {
                    Debug.Log($"  - {comp.GetType().Name}");
                }
                return;
            }
        }
        
        Debug.Log("=== FIXING AI TURN ISSUES ===");
        
        // Fix 1: Enable AI opponent mode
        if (!turnManager.isAIOpponent)
        {
            turnManager.isAIOpponent = true;
            Debug.Log("✅ Fixed: Enabled AI opponent mode");
        }
        else
        {
            Debug.Log("✅ AI opponent mode already enabled");
        }
        
        // Fix 2: Find and assign AI Turn Executor if missing
        if (turnManager.aiTurnExecutor == null)
        {
            AITurnExecutor aiExecutor = FindObjectOfType<AITurnExecutor>();
            if (aiExecutor != null)
            {
                turnManager.aiTurnExecutor = aiExecutor;
                Debug.Log($"✅ Fixed: Assigned AITurnExecutor from {aiExecutor.gameObject.name}");
            }
            else
            {
                Debug.LogError("❌ No AITurnExecutor found in scene! Use AISetupHelper to create AI system.");
                return;
            }
        }
        else
        {
            Debug.Log("✅ AITurnExecutor already assigned");
        }
        
        // Fix 3: Set up AI event listeners
        turnManager.SetAIOpponentMode(true);
        Debug.Log("✅ Fixed: Refreshed AI event listeners");
        
        // Fix 4: Test AI turn execution
        if (turnManager.aiTurnExecutor != null)
        {
            Debug.Log("🧪 Testing AI turn execution...");
            
            // Save current state
            bool wasAITurn = turnManager.isAITurn;
            
            // Test AI turn
            turnManager.isAITurn = true;
            turnManager.aiTurnExecutor.StartAITurn(1);
            
            // Wait a moment then check if AI is active
            StartCoroutine(CheckAITurnAfterDelay(wasAITurn));
        }
        
        Debug.Log("=== FIX COMPLETE ===");
        Debug.Log("Now complete a player turn to test if AI turn starts automatically.");
    }
    
    System.Collections.IEnumerator CheckAITurnAfterDelay(bool originalAITurn)
    {
        yield return new WaitForSeconds(0.5f);
        
        if (turnManager.aiTurnExecutor.IsTurnActive())
        {
            Debug.Log("✅ AI turn test SUCCESSFUL - AI is executing turn");
            
            // Stop the test turn
            turnManager.aiTurnExecutor.ForceEndTurn();
            
            // Restore original state
            turnManager.isAITurn = originalAITurn;
        }
        else
        {
            Debug.LogWarning("⚠️ AI turn test FAILED - AI did not start turn execution");
            Debug.LogWarning("Check Console for AI component errors");
            
            // Restore original state
            turnManager.isAITurn = originalAITurn;
        }
    }
    
    [ContextMenu("Check Current Status")]
    public void CheckCurrentStatus()
    {
        if (turnManager == null) return;
        
        Debug.Log("=== CURRENT STATUS ===");
        Debug.Log($"AI Opponent Mode: {turnManager.isAIOpponent}");
        Debug.Log($"Current Turn: {turnManager.currentTurn}");
        Debug.Log($"Is AI Turn: {turnManager.isAITurn}");
        Debug.Log($"Player Score: {turnManager.playerScore}");
        Debug.Log($"AI Score: {turnManager.aiScore}");
        
        if (turnManager.aiTurnExecutor == null)
        {
            Debug.LogError("❌ AITurnExecutor is NULL");
        }
        else
        {
            Debug.Log($"✅ AITurnExecutor: {turnManager.aiTurnExecutor.gameObject.name}");
            Debug.Log($"   AI Turn Active: {turnManager.aiTurnExecutor.IsTurnActive()}");
        }
    }
    
    [ContextMenu("Force Next Turn to be AI")]
    public void ForceNextTurnAI()
    {
        if (turnManager == null) return;
        
        Debug.Log("🤖 Setting next turn to be AI turn...");
        
        // Enable AI mode
        turnManager.SetAIOpponentMode(true);
        
        // Set next turn to be AI
        turnManager.isAITurn = true;
        
        Debug.Log("Next turn will be AI turn. Complete current turn to test.");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(QuickAITurnFix))]
public class QuickAITurnFixEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        QuickAITurnFix fix = (QuickAITurnFix)target;
        
        GUILayout.Label("AI Turn Debugging Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Fix AI Turn Issues", GUILayout.Height(30)))
        {
            fix.FixAITurnIssues();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Check Current Status"))
        {
            fix.CheckCurrentStatus();
        }
        
        if (GUILayout.Button("Force Next Turn to be AI"))
        {
            fix.ForceNextTurnAI();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "1. Click 'Fix AI Turn Issues' to auto-fix common problems\n" +
            "2. Check Console for results\n" +
            "3. Play game and complete a player turn\n" +
            "4. AI turn should start automatically",
            MessageType.Info);
    }
}
#endif

/* 
USAGE:

1. Add this script to your GameTurnManager GameObject
2. In Inspector, click "Fix AI Turn Issues" button
3. Check Console for results
4. Play the game and complete a player turn
5. AI turn should now start automatically

WHAT THIS FIXES:

✅ Enables AI opponent mode
✅ Finds and assigns AITurnExecutor if missing  
✅ Sets up AI event listeners
✅ Tests AI turn execution
✅ Provides status checking

If this doesn't work, the issue is likely in the AI components themselves.
Use AITestingManager to validate the AI system.
*/