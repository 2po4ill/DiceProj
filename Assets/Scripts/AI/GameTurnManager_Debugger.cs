using UnityEngine;
using HybridEnemyAI;

/// <summary>
/// Debug helper to diagnose GameTurnManager AI integration issues
/// Add this to the same GameObject as GameTurnManager to debug turn switching
/// </summary>
public class GameTurnManager_Debugger : MonoBehaviour
{
    [Header("Debug Controls")]
    public bool enableDetailedLogs = true;
    public bool forceAIOpponentMode = true;
    
    private GameTurnManager turnManager;
    
    void Start()
    {
        turnManager = GetComponent<GameTurnManager>();
        
        if (turnManager == null)
        {
            Debug.LogError("GameTurnManager_Debugger: No GameTurnManager found on this GameObject!");
            return;
        }
        
        // Force enable AI opponent mode if requested
        if (forceAIOpponentMode)
        {
            turnManager.SetAIOpponentMode(true);
            Debug.Log("GameTurnManager_Debugger: Forced AI opponent mode ON");
        }
        
        // Run diagnostics
        RunDiagnostics();
    }
    
    void Update()
    {
        // Press 'D' to run diagnostics
        if (Input.GetKeyDown(KeyCode.D))
        {
            RunDiagnostics();
        }
        
        // Press 'F' to force AI turn (for testing)
        if (Input.GetKeyDown(KeyCode.F))
        {
            ForceAITurn();
        }
        
        // Press 'S' to switch turn manually
        if (Input.GetKeyDown(KeyCode.S))
        {
            ForceSwitchTurn();
        }
    }
    
    [ContextMenu("Run Diagnostics")]
    public void RunDiagnostics()
    {
        if (turnManager == null) return;
        
        Debug.Log("=== GAME TURN MANAGER DIAGNOSTICS ===");
        
        // Check AI opponent mode
        Debug.Log($"AI Opponent Mode: {turnManager.isAIOpponent}");
        Debug.Log($"Is AI Turn: {turnManager.isAITurn}");
        Debug.Log($"Current Turn: {turnManager.currentTurn}");
        
        // Check scores
        Debug.Log($"Player Score: {turnManager.playerScore}");
        Debug.Log($"AI Score: {turnManager.aiScore}");
        
        // Check AI components
        if (turnManager.aiTurnExecutor == null)
        {
            Debug.LogError("❌ AITurnExecutor is NULL! Assign it in the Inspector.");
        }
        else
        {
            Debug.Log($"✅ AITurnExecutor found: {turnManager.aiTurnExecutor.name}");
            Debug.Log($"   AI Turn Active: {turnManager.aiTurnExecutor.IsTurnActive()}");
        }
        
        // Check other components
        CheckComponent("DiceController", turnManager.diceController);
        CheckComponent("DiceSelector", turnManager.diceSelector);
        CheckComponent("CombinationDetector", turnManager.combinationDetector);
        CheckComponent("ScoreManager", turnManager.scoreManager);
        CheckComponent("UIManager", turnManager.uiManager);
        
        Debug.Log("=== END DIAGNOSTICS ===");
    }
    
    void CheckComponent(string name, Component component)
    {
        if (component == null)
        {
            Debug.LogWarning($"⚠️ {name} is NULL");
        }
        else
        {
            Debug.Log($"✅ {name} found: {component.name}");
        }
    }
    
    [ContextMenu("Force AI Turn")]
    public void ForceAITurn()
    {
        if (turnManager == null) return;
        
        Debug.Log("🤖 FORCING AI TURN...");
        
        // Enable AI opponent mode
        turnManager.SetAIOpponentMode(true);
        
        // Set to AI turn
        turnManager.isAITurn = true;
        
        // Force end current turn to trigger turn switch
        if (turnManager.aiTurnExecutor != null)
        {
            turnManager.aiTurnExecutor.StartAITurn(turnManager.currentTurn);
        }
        else
        {
            Debug.LogError("Cannot force AI turn - AITurnExecutor is null!");
        }
    }
    
    [ContextMenu("Force Switch Turn")]
    public void ForceSwitchTurn()
    {
        if (turnManager == null) return;
        
        Debug.Log("🔄 FORCING TURN SWITCH...");
        
        // Enable AI opponent mode
        turnManager.SetAIOpponentMode(true);
        
        // Switch turn
        turnManager.isAITurn = !turnManager.isAITurn;
        
        string nextPlayer = turnManager.isAITurn ? "AI" : "Player";
        Debug.Log($"Switched to {nextPlayer} turn");
        
        // If switching to AI, start AI turn directly
        if (turnManager.isAITurn && turnManager.aiTurnExecutor != null)
        {
            turnManager.aiTurnExecutor.StartAITurn(turnManager.currentTurn);
        }
    }
    
    void OnGUI()
    {
        if (!enableDetailedLogs) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== TURN MANAGER DEBUG ===");
        
        if (turnManager != null)
        {
            GUILayout.Label($"AI Opponent: {turnManager.isAIOpponent}");
            GUILayout.Label($"AI Turn: {turnManager.isAITurn}");
            GUILayout.Label($"Turn: {turnManager.currentTurn}");
            GUILayout.Label($"Player: {turnManager.playerScore} | AI: {turnManager.aiScore}");
            
            if (turnManager.aiTurnExecutor != null)
            {
                GUILayout.Label($"AI Active: {turnManager.aiTurnExecutor.IsTurnActive()}");
            }
            else
            {
                GUILayout.Label("AI Executor: NULL");
            }
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Controls:");
        GUILayout.Label("D - Run Diagnostics");
        GUILayout.Label("F - Force AI Turn");
        GUILayout.Label("S - Switch Turn");
        
        GUILayout.EndArea();
    }
}

/* 
USAGE INSTRUCTIONS:

1. Add this script to the same GameObject as your GameTurnManager
2. Enable "Force AI Opponent Mode" in Inspector
3. Play the game and check the debug overlay
4. Use keyboard shortcuts to test:
   - D: Run diagnostics
   - F: Force AI turn
   - S: Switch turn manually

COMMON ISSUES TO CHECK:

1. In GameTurnManager Inspector:
   - ✅ "Is AI Opponent" should be checked
   - ✅ "AI Turn Executor" field should be assigned
   
2. In AI_Manager GameObject:
   - ✅ AITurnExecutor component should be present
   - ✅ All required fields should be assigned
   
3. Check Console for error messages during turn switching
*/