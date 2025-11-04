using UnityEngine;
using TMPro;

/// <summary>
/// Simple UI Check - Just checks if the GameManager text fields are assigned
/// </summary>
public class SimpleUICheck : MonoBehaviour
{
    [ContextMenu("Check GameManager UI Fields")]
    public void CheckGameManagerUIFields()
    {
        Debug.Log("=== SIMPLE UI CHECK ===");
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("❌ GameManager not found!");
            return;
        }
        
        Debug.Log($"GameManager found: {gameManager.gameObject.name}");
        Debug.Log($"Current Game Mode: {gameManager.GetCurrentGameMode()}");
        Debug.Log($"Is AI Opponent Mode: {gameManager.IsAIOpponentMode()}");
        
        // Check the public UI fields
        Debug.Log($"playerScoreText assigned: {gameManager.playerScoreText != null}");
        if (gameManager.playerScoreText != null)
        {
            Debug.Log($"  GameObject: {gameManager.playerScoreText.gameObject.name}");
            Debug.Log($"  Current text: '{gameManager.playerScoreText.text}'");
        }
        else
        {
            Debug.LogError("❌ playerScoreText is NULL! This is why it's not updating!");
        }
        
        Debug.Log($"aiScoreText assigned: {gameManager.aiScoreText != null}");
        if (gameManager.aiScoreText != null)
        {
            Debug.Log($"  GameObject: {gameManager.aiScoreText.gameObject.name}");
            Debug.Log($"  Current text: '{gameManager.aiScoreText.text}'");
        }
        else
        {
            Debug.LogError("❌ aiScoreText is NULL! This is why it's not updating!");
        }
        
        Debug.Log($"currentPlayerText assigned: {gameManager.currentPlayerText != null}");
        if (gameManager.currentPlayerText != null)
        {
            Debug.Log($"  GameObject: {gameManager.currentPlayerText.gameObject.name}");
            Debug.Log($"  Current text: '{gameManager.currentPlayerText.text}'");
        }
        
        // Check turnManager
        GameTurnManager turnManager = FindObjectOfType<GameTurnManager>();
        if (turnManager != null)
        {
            Debug.Log($"TurnManager scores - Player: {turnManager.playerScore}, AI: {turnManager.aiScore}");
        }
    }
    
    [ContextMenu("Find and Assign Text Components")]
    public void FindAndAssignTextComponents()
    {
        Debug.Log("=== FINDING AND ASSIGNING TEXT COMPONENTS ===");
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found!");
            return;
        }
        
        // Find all TextMeshPro components
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();
        Debug.Log($"Found {allTexts.Length} TextMeshProUGUI components in scene");
        
        foreach (var text in allTexts)
        {
            Debug.Log($"Text component: '{text.text}' on {text.gameObject.name}");
            
            if (text.text == "New Text")
            {
                // Try to determine what this should be based on position
                Vector3 worldPos = text.transform.position;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                float screenWidth = Screen.width;
                
                Debug.Log($"  'New Text' component at screen position: {screenPos.x}/{screenWidth}");
                
                if (screenPos.x < screenWidth * 0.4f) // Left side
                {
                    gameManager.playerScoreText = text;
                    text.text = "Player: 0";
                    Debug.Log($"  ✅ Assigned as playerScoreText and set to 'Player: 0'");
                }
                else if (screenPos.x > screenWidth * 0.6f) // Right side
                {
                    gameManager.aiScoreText = text;
                    text.text = "AI: 0";
                    Debug.Log($"  ✅ Assigned as aiScoreText and set to 'AI: 0'");
                }
                else // Center
                {
                    gameManager.currentPlayerText = text;
                    text.text = "Your Turn";
                    Debug.Log($"  ✅ Assigned as currentPlayerText and set to 'Your Turn'");
                }
            }
        }
        
        Debug.Log("✅ Assignment complete! The UI should now update properly.");
    }
    
    [ContextMenu("Force UI Update Now")]
    public void ForceUIUpdateNow()
    {
        Debug.Log("=== FORCING UI UPDATE ===");
        
        GameManager gameManager = FindObjectOfType<GameManager>();
        GameTurnManager turnManager = FindObjectOfType<GameTurnManager>();
        
        if (gameManager == null || turnManager == null)
        {
            Debug.LogError("Missing components!");
            return;
        }
        
        Debug.Log($"Current scores - Player: {turnManager.playerScore}, AI: {turnManager.aiScore}");
        
        // Manually update the text
        if (gameManager.playerScoreText != null)
        {
            gameManager.playerScoreText.text = $"Player: {turnManager.playerScore}";
            Debug.Log($"✅ Updated playerScoreText to: {gameManager.playerScoreText.text}");
        }
        else
        {
            Debug.LogError("❌ playerScoreText is null!");
        }
        
        if (gameManager.aiScoreText != null)
        {
            gameManager.aiScoreText.text = $"AI: {turnManager.aiScore}";
            Debug.Log($"✅ Updated aiScoreText to: {gameManager.aiScoreText.text}");
        }
        else
        {
            Debug.LogError("❌ aiScoreText is null!");
        }
        
        if (gameManager.currentPlayerText != null)
        {
            string currentPlayer = turnManager.isAITurn ? "AI Turn" : "Your Turn";
            gameManager.currentPlayerText.text = currentPlayer;
            Debug.Log($"✅ Updated currentPlayerText to: {gameManager.currentPlayerText.text}");
        }
    }
}