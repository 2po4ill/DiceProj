using UnityEngine;
using TMPro;

/// <summary>
/// UI Connection Fixer - Automatically finds and connects UI text components
/// This fixes the "New Text" issue by ensuring all UI elements are properly connected
/// </summary>
public class UIConnectionFixer : MonoBehaviour
{
    [Header("Auto-Fix Settings")]
    [SerializeField] private bool autoFixOnStart = true;
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Components to Fix")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ScoreUIManager scoreUIManager;
    
    void Start()
    {
        if (autoFixOnStart)
        {
            FixAllUIConnections();
        }
    }
    
    [ContextMenu("Fix All UI Connections")]
    public void FixAllUIConnections()
    {
        Debug.Log("=== FIXING UI CONNECTIONS ===");
        
        // Find components if not assigned
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        
        if (scoreUIManager == null)
            scoreUIManager = FindObjectOfType<ScoreUIManager>();
        
        // Fix GameManager UI connections
        FixGameManagerUI();
        
        // Fix ScoreUIManager connections
        FixScoreUIManagerConnections();
        
        // Force initial UI update
        ForceUIUpdate();
        
        Debug.Log("=== UI CONNECTIONS FIXED ===");
    }
    
    void FixGameManagerUI()
    {
        if (gameManager == null)
        {
            Debug.LogError("UIConnectionFixer: GameManager not found!");
            return;
        }
        
        // Use reflection to access private fields and fix them
        var gameManagerType = typeof(GameManager);
        
        // Find and assign player score text
        if (GetFieldValue<TextMeshProUGUI>(gameManager, "playerScoreText") == null)
        {
            var playerScoreText = FindUITextByName("PlayerScoreText", "Player Score", "PlayerTotal");
            if (playerScoreText != null)
            {
                SetFieldValue(gameManager, "playerScoreText", playerScoreText);
                playerScoreText.text = "Player: 0";
                if (enableDebugLogs)
                    Debug.Log($"Fixed: playerScoreText -> {playerScoreText.name}");
            }
        }
        
        // Find and assign AI score text
        if (GetFieldValue<TextMeshProUGUI>(gameManager, "aiScoreText") == null)
        {
            var aiScoreText = FindUITextByName("AIScoreText", "AI Score", "AITotal");
            if (aiScoreText != null)
            {
                SetFieldValue(gameManager, "aiScoreText", aiScoreText);
                aiScoreText.text = "AI: 0";
                if (enableDebugLogs)
                    Debug.Log($"Fixed: aiScoreText -> {aiScoreText.name}");
            }
        }
        
        // Find and assign current player text
        if (GetFieldValue<TextMeshProUGUI>(gameManager, "currentPlayerText") == null)
        {
            var currentPlayerText = FindUITextByName("CurrentPlayerText", "Current Player", "TurnIndicator");
            if (currentPlayerText != null)
            {
                SetFieldValue(gameManager, "currentPlayerText", currentPlayerText);
                currentPlayerText.text = "Your Turn";
                if (enableDebugLogs)
                    Debug.Log($"Fixed: currentPlayerText -> {currentPlayerText.name}");
            }
        }
    }
    
    void FixScoreUIManagerConnections()
    {
        if (scoreUIManager == null)
        {
            Debug.LogError("UIConnectionFixer: ScoreUIManager not found!");
            return;
        }
        
        // Fix player total score text
        if (GetFieldValue<TextMeshProUGUI>(scoreUIManager, "playerTotalScoreText") == null)
        {
            var playerTotalText = FindUITextByName("PlayerTotalScoreText", "Player Total", "PlayerScore");
            if (playerTotalText != null)
            {
                SetFieldValue(scoreUIManager, "playerTotalScoreText", playerTotalText);
                playerTotalText.text = "Player: 0";
                if (enableDebugLogs)
                    Debug.Log($"Fixed: playerTotalScoreText -> {playerTotalText.name}");
            }
        }
        
        // Fix AI total score text
        if (GetFieldValue<TextMeshProUGUI>(scoreUIManager, "aiTotalScoreText") == null)
        {
            var aiTotalText = FindUITextByName("AITotalScoreText", "AI Total", "AIScore");
            if (aiTotalText != null)
            {
                SetFieldValue(scoreUIManager, "aiTotalScoreText", aiTotalText);
                aiTotalText.text = "AI: 0";
                if (enableDebugLogs)
                    Debug.Log($"Fixed: aiTotalScoreText -> {aiTotalText.name}");
            }
        }
        
        // Fix current player indicator text
        if (GetFieldValue<TextMeshProUGUI>(scoreUIManager, "currentPlayerIndicatorText") == null)
        {
            var indicatorText = FindUITextByName("CurrentPlayerIndicatorText", "Player Indicator", "TurnText");
            if (indicatorText != null)
            {
                SetFieldValue(scoreUIManager, "currentPlayerIndicatorText", indicatorText);
                indicatorText.text = "Your Turn";
                if (enableDebugLogs)
                    Debug.Log($"Fixed: currentPlayerIndicatorText -> {indicatorText.name}");
            }
        }
    }
    
    TextMeshProUGUI FindUITextByName(params string[] possibleNames)
    {
        // Find all TextMeshProUGUI components in the scene
        var allTexts = FindObjectsOfType<TextMeshProUGUI>();
        
        foreach (var text in allTexts)
        {
            foreach (var name in possibleNames)
            {
                if (text.name.Contains(name) || text.gameObject.name.Contains(name))
                {
                    return text;
                }
            }
        }
        
        // If not found by name, look for texts with "New Text" content
        foreach (var text in allTexts)
        {
            if (text.text == "New Text")
            {
                // Check parent names to determine purpose
                Transform parent = text.transform.parent;
                if (parent != null)
                {
                    string parentName = parent.name.ToLower();
                    
                    foreach (var name in possibleNames)
                    {
                        if (parentName.Contains(name.ToLower()))
                        {
                            if (enableDebugLogs)
                                Debug.Log($"Found 'New Text' component for {name} in parent: {parent.name}");
                            return text;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    void ForceUIUpdate()
    {
        // Force GameManager to update UI
        if (gameManager != null && gameManager.IsAIOpponentMode())
        {
            // Call the private UpdatePlayerVsAIUI method using reflection
            var method = typeof(GameManager).GetMethod("UpdatePlayerVsAIUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(gameManager, null);
                if (enableDebugLogs)
                    Debug.Log("Forced GameManager UI update");
            }
        }
        
        // Force ScoreUIManager to update
        if (scoreUIManager != null)
        {
            scoreUIManager.UpdateAllUI();
            if (enableDebugLogs)
                Debug.Log("Forced ScoreUIManager UI update");
        }
    }
    
    // Utility methods for reflection
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
    
    [ContextMenu("Find All New Text Components")]
    public void FindAllNewTextComponents()
    {
        Debug.Log("=== FINDING ALL 'NEW TEXT' COMPONENTS ===");
        
        var allTexts = FindObjectsOfType<TextMeshProUGUI>();
        int newTextCount = 0;
        
        foreach (var text in allTexts)
        {
            if (text.text == "New Text")
            {
                newTextCount++;
                string path = GetGameObjectPath(text.gameObject);
                Debug.Log($"Found 'New Text' at: {path}");
            }
        }
        
        Debug.Log($"Total 'New Text' components found: {newTextCount}");
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
    
    [ContextMenu("Set All New Text to Placeholder")]
    public void SetAllNewTextToPlaceholder()
    {
        var allTexts = FindObjectsOfType<TextMeshProUGUI>();
        int fixedCount = 0;
        
        foreach (var text in allTexts)
        {
            if (text.text == "New Text")
            {
                // Try to determine what this text should display based on its name/parent
                string newText = DetermineTextContent(text.gameObject);
                text.text = newText;
                fixedCount++;
                
                if (enableDebugLogs)
                    Debug.Log($"Set '{text.gameObject.name}' text to: {newText}");
            }
        }
        
        Debug.Log($"Fixed {fixedCount} 'New Text' components");
    }
    
    string DetermineTextContent(GameObject textObj)
    {
        string name = textObj.name.ToLower();
        string parentName = textObj.transform.parent?.name.ToLower() ?? "";
        
        // Check for score-related text
        if (name.Contains("player") || parentName.Contains("player"))
        {
            if (name.Contains("score") || parentName.Contains("score"))
                return "Player: 0";
        }
        
        if (name.Contains("ai") || parentName.Contains("ai"))
        {
            if (name.Contains("score") || parentName.Contains("score"))
                return "AI: 0";
        }
        
        if (name.Contains("turn") || parentName.Contains("turn"))
        {
            return "Your Turn";
        }
        
        if (name.Contains("current") || parentName.Contains("current"))
        {
            return "Turn 1";
        }
        
        // Default placeholder
        return "0";
    }
}