using UnityEngine;
using UnityEngine.UI;

public class UIButtonController : MonoBehaviour
{
    [Header("Button References")]
    public Button[] gameButtons;
    
    private GameManager gameManager;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        SetupButtons();
    }
    
    void SetupButtons()
    {
        // Auto-assign button listeners
        for (int i = 0; i < gameButtons.Length; i++)
        {
            int buttonIndex = i; // Capture for closure
            gameButtons[i].onClick.AddListener(() => OnButtonClick(buttonIndex));
        }
    }
    
    void OnButtonClick(int buttonIndex)
    {
        if (gameManager != null)
        {
            gameManager.OnButtonPressed($"Button_{buttonIndex}");
        }
        
        // Add visual feedback
        StartCoroutine(ButtonPressEffect(gameButtons[buttonIndex]));
    }
    
    System.Collections.IEnumerator ButtonPressEffect(Button button)
    {
        // Simple scale effect
        Vector3 originalScale = button.transform.localScale;
        button.transform.localScale = originalScale * 0.95f;
        
        yield return new WaitForSeconds(0.1f);
        
        button.transform.localScale = originalScale;
    }
}