using UnityEngine;
using TMPro;

/// <summary>
/// UI Text Assignment Fixer - Fixes the "New Text" issue by properly assigning TextMeshPro components
/// </summary>
public class UITextAssignmentFixer : MonoBehaviour
{
    [Header("Auto-Fix Settings")]
    [SerializeField] private bool fixOnStart = true;
    
    void Start()
    {
        if (fixOnStart)
        {
            FixUITextAssignments();
        }
    }
    
    [ContextMenu("Fix UI Text Assignments")]
    public void FixUITextAssignments()
    {
        Debug.Log("=== FIXING UI TEXT ASSIGNMENTS ===");
        
        // Find the managers
        GameManager gameManager = FindObjectOfType<GameManager>();
        ScoreUIManager scoreUIManager = FindObjectOfType<ScoreUIManager>();
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found!");
            return;
        }
        
        if (scoreUIManager == null)
        {
            Debug.LogError("ScoreUIManager not found!");
            return;
        }
        
        // Find all TextMeshPro components showing "New Text"
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        
        TextMeshProUGUI leftText = null;
        TextMeshProUGUI rightText = null;
        TextMeshProUGUI centerText = null;
        
        // Identify text components by screen position
        foreach (var text in allTexts)
        {
            if (text.text == "New Text")
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(text.transform.position);
                float screenWidth = Screen.width;
                
                if (screenPos.x < screenWidth * 0.4f) // Left side
                {
                    leftText = text;
                }
                else if (screenPos.x > screenWidth * 0.6f) // Right side
                {
                    rightText = text;
                }
                else // Center
                {
                    centerText = text;
                }
            }
        }
        
        // Assign to GameManager (public fields)
        if (leftText != null)
        {
            gameManager.playerScoreText = leftText;
            leftText.text = "Player: 0";
            Debug.Log($"✅ Assigned GameManager.playerScoreText to {leftText.gameObject.name}");
        }
        
        if (rightText != null)
        {
            gameManager.aiScoreText = rightText;
            rightText.text = "AI: 0";
            Debug.Log($"✅ Assigned GameManager.aiScoreText to {rightText.gameObject.name}");
        }
        
        if (centerText != null)
        {
            gameManager.currentPlayerText = centerText;
            centerText.text = "Your Turn";
            Debug.Log($"✅ Assigned GameManager.currentPlayerText to {centerText.gameObject.name}");
        }
        
        // Also assign to ScoreUIManager (private fields using reflection)
        if (leftText != null)
        {
            SetPrivateField(scoreUIManager, "playerTotalScoreText", leftText);
            Debug.Log($"✅ Assigned ScoreUIManager.playerTotalScoreText to {leftText.gameObject.name}");
        }
        
        if (rightText != null)
        {
            SetPrivateField(scoreUIManager, "aiTotalScoreText", rightText);
            Debug.Log($"✅ Assigned ScoreUIManager.aiTotalScoreText to {rightText.gameObject.name}");
        }
        
        if (centerText != null)
        {
            SetPrivateField(scoreUIManager, "currentPlayerIndicatorText", centerText);
            Debug.Log($"✅ Assigned ScoreUIManager.currentPlayerIndicatorText to {centerText.gameObject.name}");
        }
        
        // Force immediate UI update
        ForceUIUpdate(gameManager, scoreUIManager);
        
        Debug.Log("✅ UI TEXT ASSIGNMENT COMPLETE!");
    }
    
    void ForceUIUpdate(GameManager gameManager, ScoreUIManager scoreUIManager)
    {
        Debug.Log("--- Forcing UI Update ---");
        
        // Force GameManager UI update
        if (gameManager != null)
        {
            var method = typeof(GameManager).GetMethod("UpdatePlayerVsAIUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(gameManager, null);
                Debug.Log("✅ Called GameManager.UpdatePlayerVsAIUI()");
            }
        }
        
        // Force ScoreUIManager UI update
        if (scoreUIManager != null)
        {
            scoreUIManager.UpdateAllUI();
            
            var method = typeof(ScoreUIManager).GetMethod("UpdateAIVsPlayerUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(scoreUIManager, null);
                Debug.Log("✅ Called ScoreUIManager.UpdateAIVsPlayerUI()");
            }
        }
    }
    
    void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else
        {
            Debug.LogWarning($"Field '{fieldName}' not found in {obj.GetType().Name}");
        }
    }
    
    [ContextMenu("Check Current Assignments")]
    public void CheckCurrentAssignments()
    {
        Debug.Log("=== CURRENT UI ASSIGNMENTS ===");
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        ScoreUIManager scoreUIManager = FindObjectOfType<ScoreUIManager>();
        
        if (gameManager != null)
        {
            Debug.Log("GameManager assignments:");
            Debug.Log($"  playerScoreText: {(gameManager.playerScoreText != null ? gameManager.playerScoreText.gameObject.name : "NULL")}");
            Debug.Log($"  aiScoreText: {(gameManager.aiScoreText != null ? gameManager.aiScoreText.gameObject.name : "NULL")}");
            Debug.Log($"  currentPlayerText: {(gameManager.currentPlayerText != null ? gameManager.currentPlayerText.gameObject.name : "NULL")}");
            
            if (gameManager.playerScoreText != null)
                Debug.Log($"    Player text content: '{gameManager.playerScoreText.text}'");
            if (gameManager.aiScoreText != null)
                Debug.Log($"    AI text content: '{gameManager.aiScoreText.text}'");
        }
        
        if (scoreUIManager != null)
        {
            Debug.Log("ScoreUIManager assignments:");
            var playerText = GetPrivateField<TextMeshProUGUI>(scoreUIManager, "playerTotalScoreText");
            var aiText = GetPrivateField<TextMeshProUGUI>(scoreUIManager, "aiTotalScoreText");
            
            Debug.Log($"  playerTotalScoreText: {(playerText != null ? playerText.gameObject.name : "NULL")}");
            Debug.Log($"  aiTotalScoreText: {(aiText != null ? aiText.gameObject.name : "NULL")}");
            
            if (playerText != null)
                Debug.Log($"    Player text content: '{playerText.text}'");
            if (aiText != null)
                Debug.Log($"    AI text content: '{aiText.text}'");
        }
    }
    
    T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        
        return default(T);
    }
}