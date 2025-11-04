using UnityEngine;
using TMPro;

/// <summary>
/// UI Connection Diagnostic - Checks why ScoreUIManager text components are not updating
/// </summary>
public class UIConnectionDiagnostic : MonoBehaviour
{
    [Header("Components to Check")]
    [SerializeField] private ScoreUIManager scoreUIManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameTurnManager turnManager;
    
    void Start()
    {
        FindComponents();
    }
    
    void FindComponents()
    {
        if (scoreUIManager == null)
            scoreUIManager = FindObjectOfType<ScoreUIManager>();
        
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        
        if (turnManager == null)
            turnManager = FindObjectOfType<GameTurnManager>();
    }
    
    [ContextMenu("Diagnose UI Connections")]
    public void DiagnoseUIConnections()
    {
        Debug.Log("=== UI CONNECTION DIAGNOSTIC ===");
        
        if (scoreUIManager == null)
        {
            Debug.LogError("❌ ScoreUIManager not found!");
            return;
        }
        
        // Check ScoreUIManager fields using reflection
        CheckScoreUIManagerFields();
        
        // Check if UpdateAIVsPlayerUI is being called
        CheckUpdateCalls();
        
        // Check game mode
        CheckGameMode();
        
        // Find all TextMeshPro components in scene
        FindAllTextComponents();
    }
    
    void CheckScoreUIManagerFields()
    {
        Debug.Log("--- ScoreUIManager Field Check ---");
        
        // Use reflection to check private fields
        var playerTotalScoreText = GetFieldValue<TextMeshProUGUI>(scoreUIManager, "playerTotalScoreText");
        var aiTotalScoreText = GetFieldValue<TextMeshProUGUI>(scoreUIManager, "aiTotalScoreText");
        var currentPlayerIndicatorText = GetFieldValue<TextMeshProUGUI>(scoreUIManager, "currentPlayerIndicatorText");
        
        Debug.Log($"playerTotalScoreText: {(playerTotalScoreText != null ? "✅ Assigned" : "❌ NULL")}");
        if (playerTotalScoreText != null)
        {
            Debug.Log($"  Current text: '{playerTotalScoreText.text}'");
            Debug.Log($"  GameObject: {playerTotalScoreText.gameObject.name}");
        }
        
        Debug.Log($"aiTotalScoreText: {(aiTotalScoreText != null ? "✅ Assigned" : "❌ NULL")}");
        if (aiTotalScoreText != null)
        {
            Debug.Log($"  Current text: '{aiTotalScoreText.text}'");
            Debug.Log($"  GameObject: {aiTotalScoreText.gameObject.name}");
        }
        
        Debug.Log($"currentPlayerIndicatorText: {(currentPlayerIndicatorText != null ? "✅ Assigned" : "❌ NULL")}");
        if (currentPlayerIndicatorText != null)
        {
            Debug.Log($"  Current text: '{currentPlayerIndicatorText.text}'");
            Debug.Log($"  GameObject: {currentPlayerIndicatorText.gameObject.name}");
        }
    }
    
    void CheckUpdateCalls()
    {
        Debug.Log("--- Update Call Check ---");
        
        if (gameManager != null)
        {
            bool isAIMode = gameManager.IsAIOpponentMode();
            Debug.Log($"Game Mode: {(isAIMode ? "AI vs Player" : "Single Player")}");
            
            if (!isAIMode)
            {
                Debug.LogWarning("⚠️ Game is not in AI vs Player mode - UpdateAIVsPlayerUI won't be called!");
            }
        }
        
        if (turnManager != null)
        {
            Debug.Log($"Turn Manager - isAIOpponent: {turnManager.isAIOpponent}, isAITurn: {turnManager.isAITurn}");
            Debug.Log($"Player Score: {turnManager.playerScore}, AI Score: {turnManager.aiScore}");
        }
    }
    
    void CheckGameMode()
    {
        Debug.Log("--- Game Mode Check ---");
        
        if (gameManager != null)
        {
            var currentMode = GetFieldValue<GameManager.GameMode>(gameManager, "currentGameMode");
            Debug.Log($"Current Game Mode: {currentMode}");
            
            if (currentMode != GameManager.GameMode.AIOpponent)
            {
                Debug.LogError("❌ Game is not in AIOpponent mode! This is why AI vs Player UI isn't updating.");
                Debug.Log("💡 Solution: Start the game in AI vs Player mode from the main menu.");
            }
        }
    }
    
    void FindAllTextComponents()
    {
        Debug.Log("--- All TextMeshPro Components ---");
        
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        
        Debug.Log($"Found {allTexts.Length} TextMeshProUGUI components:");
        
        foreach (var text in allTexts)
        {
            string path = GetGameObjectPath(text.gameObject);
            Debug.Log($"  '{text.text}' at {path}");
            
            if (text.text == "New Text")
            {
                Debug.LogWarning($"    ⚠️ This component shows 'New Text' - likely unassigned!");
            }
        }
    }
    
    [ContextMenu("Force Assign Text Components")]
    public void ForceAssignTextComponents()
    {
        Debug.Log("=== FORCE ASSIGNING TEXT COMPONENTS ===");
        
        if (scoreUIManager == null)
        {
            Debug.LogError("ScoreUIManager not found!");
            return;
        }
        
        // Find and assign text components by position/name
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        
        foreach (var text in allTexts)
        {
            if (text.text == "New Text")
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(text.transform.position);
                float screenWidth = Screen.width;
                
                // Left side = Player score
                if (screenPos.x < screenWidth * 0.4f)
                {
                    SetFieldValue(scoreUIManager, "playerTotalScoreText", text);
                    text.text = "Player: 0";
                    Debug.Log($"✅ Assigned playerTotalScoreText to {GetGameObjectPath(text.gameObject)}");
                }
                // Right side = AI score
                else if (screenPos.x > screenWidth * 0.6f)
                {
                    SetFieldValue(scoreUIManager, "aiTotalScoreText", text);
                    text.text = "AI: 0";
                    Debug.Log($"✅ Assigned aiTotalScoreText to {GetGameObjectPath(text.gameObject)}");
                }
                // Center = Current player indicator
                else
                {
                    SetFieldValue(scoreUIManager, "currentPlayerIndicatorText", text);
                    text.text = "Your Turn";
                    Debug.Log($"✅ Assigned currentPlayerIndicatorText to {GetGameObjectPath(text.gameObject)}");
                }
            }
        }
        
        Debug.Log("✅ Text component assignment complete!");
    }
    
    [ContextMenu("Test UI Update")]
    public void TestUIUpdate()
    {
        Debug.Log("=== TESTING UI UPDATE ===");
        
        if (scoreUIManager == null || turnManager == null)
        {
            Debug.LogError("Missing components for test!");
            return;
        }
        
        // Set test scores
        turnManager.playerScore = 1500;
        turnManager.aiScore = 1200;
        
        Debug.Log($"Set test scores - Player: {turnManager.playerScore}, AI: {turnManager.aiScore}");
        
        // Force UI update
        scoreUIManager.UpdateAllUI();
        
        // Also call the AI vs Player update directly
        var method = typeof(ScoreUIManager).GetMethod("UpdateAIVsPlayerUI", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(scoreUIManager, null);
            Debug.Log("✅ Called UpdateAIVsPlayerUI directly");
        }
        
        Debug.Log("UI update test complete - check if text components updated");
    }
    
    // Utility methods
    T GetFieldValue<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        
        return default(T);
    }
    
    void SetFieldValue(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }
    
    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}