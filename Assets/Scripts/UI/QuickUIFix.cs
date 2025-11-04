using UnityEngine;
using TMPro;

/// <summary>
/// Quick UI Fix - Immediately fixes "New Text" issues in the scene
/// Run this in the Inspector to instantly fix UI display problems
/// </summary>
public class QuickUIFix : MonoBehaviour
{
    [Header("Quick Fix")]
    [SerializeField] private bool fixOnStart = true;
    
    void Start()
    {
        if (fixOnStart)
        {
            FixNewTextIssue();
        }
    }
    
    [ContextMenu("Fix New Text Issue Now")]
    public void FixNewTextIssue()
    {
        Debug.Log("=== QUICK UI FIX: Fixing 'New Text' Issue ===");
        
        // Find all TextMeshProUGUI components
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        int fixedCount = 0;
        
        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text.text == "New Text")
            {
                // Determine what this text should show based on its position and name
                string correctText = GetCorrectTextContent(text);
                text.text = correctText;
                fixedCount++;
                
                Debug.Log($"Fixed: {GetPath(text.gameObject)} -> '{correctText}'");
            }
        }
        
        Debug.Log($"✅ Fixed {fixedCount} 'New Text' components");
        
        // Also force update any score managers
        ForceUpdateScoreDisplays();
    }
    
    string GetCorrectTextContent(TextMeshProUGUI textComponent)
    {
        string objName = textComponent.gameObject.name.ToLower();
        string parentName = textComponent.transform.parent?.name.ToLower() ?? "";
        Vector3 position = textComponent.transform.position;
        
        // Check object names for clues
        if (objName.Contains("player") || parentName.Contains("player"))
        {
            return "Player: 0";
        }
        
        if (objName.Contains("ai") || parentName.Contains("ai"))
        {
            return "AI: 0";
        }
        
        if (objName.Contains("turn") || parentName.Contains("turn"))
        {
            return "Your Turn";
        }
        
        if (objName.Contains("score"))
        {
            // Check position to determine if it's left (player) or right (AI)
            if (position.x < 0)
                return "Player: 0";
            else if (position.x > 0)
                return "AI: 0";
            else
                return "0";
        }
        
        // Check by screen position (left side = player, right side = AI)
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(position);
            float screenWidth = Screen.width;
            
            if (screenPos.x < screenWidth * 0.3f) // Left side
            {
                return "Player: 0";
            }
            else if (screenPos.x > screenWidth * 0.7f) // Right side
            {
                return "AI: 0";
            }
        }
        
        // Default fallback
        return "0";
    }
    
    void ForceUpdateScoreDisplays()
    {
        // Update GameManager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            // Force update the UI by calling the update method
            var method = typeof(GameManager).GetMethod("UpdatePlayerVsAIUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(gameManager, null);
                Debug.Log("✅ Forced GameManager UI update");
            }
        }
        
        // Update ScoreUIManager
        ScoreUIManager scoreManager = FindObjectOfType<ScoreUIManager>();
        if (scoreManager != null)
        {
            scoreManager.UpdateAllUI();
            Debug.Log("✅ Forced ScoreUIManager update");
        }
        
        // Update GameTurnManager scores
        GameTurnManager turnManager = FindObjectOfType<GameTurnManager>();
        if (turnManager != null)
        {
            // Ensure scores are initialized
            if (turnManager.isAIOpponent)
            {
                // Make sure scores are set to 0 if not initialized
                Debug.Log($"Current scores - Player: {turnManager.playerScore}, AI: {turnManager.aiScore}");
            }
        }
    }
    
    string GetPath(GameObject obj)
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
    
    [ContextMenu("List All Text Components")]
    public void ListAllTextComponents()
    {
        Debug.Log("=== ALL TEXT COMPONENTS IN SCENE ===");
        
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        
        foreach (TextMeshProUGUI text in allTexts)
        {
            string path = GetPath(text.gameObject);
            Debug.Log($"Text: '{text.text}' at {path}");
        }
        
        Debug.Log($"Total TextMeshProUGUI components: {allTexts.Length}");
    }
    
    [ContextMenu("Force Set Specific Scores")]
    public void ForceSetSpecificScores()
    {
        Debug.Log("=== FORCE SETTING SPECIFIC SCORES ===");
        
        // Find and set player score text
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        
        foreach (TextMeshProUGUI text in allTexts)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(text.transform.position);
            float screenWidth = Screen.width;
            
            // Left side text = Player score
            if (screenPos.x < screenWidth * 0.4f && screenPos.y > Screen.height * 0.7f)
            {
                text.text = "Player: 0";
                text.color = Color.green;
                Debug.Log($"Set LEFT text to 'Player: 0' at {GetPath(text.gameObject)}");
            }
            // Right side text = AI score  
            else if (screenPos.x > screenWidth * 0.6f && screenPos.y > Screen.height * 0.7f)
            {
                text.text = "AI: 0";
                text.color = Color.red;
                Debug.Log($"Set RIGHT text to 'AI: 0' at {GetPath(text.gameObject)}");
            }
        }
    }
}