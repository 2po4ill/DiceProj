using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HybridEnemyAI;

/// <summary>
/// Manages AI difficulty presets and provides easy difficulty selection interface
/// </summary>
public class AIDifficultyManager : MonoBehaviour
{
    [Header("Difficulty Presets")]
    public AIDifficultyLevel currentDifficulty = AIDifficultyLevel.Medium;
    
    [Header("UI References")]
    public GameObject difficultySelectionPanel;
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    public Button customButton;
    public TextMeshProUGUI currentDifficultyText;
    public TextMeshProUGUI difficultyDescriptionText;
    
    [Header("AI Components")]
    public AIConfigurationManager configManager;
    public AITurnExecutor aiTurnExecutor;
    public AIGameStateAnalyzer gameStateAnalyzer;
    
    [Header("Difficulty Descriptions")]
    [TextArea(3, 5)]
    public string easyDescription = "Conservative AI that plays it safe. Lower scoring caps and higher risk avoidance make for a more forgiving opponent.";
    [TextArea(3, 5)]
    public string mediumDescription = "Balanced AI with standard settings. Good mix of aggression and caution provides challenging but fair gameplay.";
    [TextArea(3, 5)]
    public string hardDescription = "Aggressive AI that takes calculated risks. Higher scoring caps and better risk management create a formidable opponent.";
    [TextArea(3, 5)]
    public string customDescription = "Fully customizable AI settings. Adjust all parameters to create your perfect opponent.";
    
    public enum AIDifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        Custom
    }
    
    void Start()
    {
        InitializeComponents();
        SetupUI();
        ApplyCurrentDifficulty();
    }
    
    void InitializeComponents()
    {
        if (configManager == null) configManager = FindObjectOfType<AIConfigurationManager>();
        if (aiTurnExecutor == null) aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        if (gameStateAnalyzer == null) gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
    }
    
    void SetupUI()
    {
        // Setup button listeners
        if (easyButton != null)
            easyButton.onClick.AddListener(() => SetDifficulty(AIDifficultyLevel.Easy));
        
        if (mediumButton != null)
            mediumButton.onClick.AddListener(() => SetDifficulty(AIDifficultyLevel.Medium));
        
        if (hardButton != null)
            hardButton.onClick.AddListener(() => SetDifficulty(AIDifficultyLevel.Hard));
        
        if (customButton != null)
            customButton.onClick.AddListener(() => SetDifficulty(AIDifficultyLevel.Custom));
        
        // Hide difficulty panel initially
        if (difficultySelectionPanel != null)
            difficultySelectionPanel.SetActive(false);
        
        UpdateUI();
    }
    
    /// <summary>
    /// Sets AI difficulty level and applies corresponding configuration
    /// </summary>
    public void SetDifficulty(AIDifficultyLevel difficulty)
    {
        currentDifficulty = difficulty;
        
        AIConfiguration config = CreateConfigurationForDifficulty(difficulty);
        
        // Apply configuration through the configuration manager
        if (configManager != null)
        {
            configManager.SetConfiguration(config);
        }
        else
        {
            // Apply directly to components if no configuration manager
            ApplyConfigurationDirectly(config);
        }
        
        UpdateUI();
        
        Debug.Log($"AI Difficulty set to: {difficulty}");
    }
    
    /// <summary>
    /// Shows the difficulty selection panel
    /// </summary>
    public void ShowDifficultySelection()
    {
        if (difficultySelectionPanel != null)
        {
            difficultySelectionPanel.SetActive(true);
            UpdateUI();
        }
    }
    
    /// <summary>
    /// Hides the difficulty selection panel
    /// </summary>
    public void HideDifficultySelection()
    {
        if (difficultySelectionPanel != null)
            difficultySelectionPanel.SetActive(false);
    }
    
    /// <summary>
    /// Opens custom configuration panel
    /// </summary>
    public void OpenCustomConfiguration()
    {
        if (configManager != null)
        {
            configManager.ShowConfigurationPanel();
        }
        else
        {
            Debug.LogWarning("No AIConfigurationManager found - cannot open custom configuration");
        }
    }
    
    AIConfiguration CreateConfigurationForDifficulty(AIDifficultyLevel difficulty)
    {
        AIConfiguration config = new AIConfiguration();
        
        switch (difficulty)
        {
            case AIDifficultyLevel.Easy:
                // Conservative settings - easier for player to win
                config.PointsCapAggressive = 350;
                config.PointsCapPassive = 200;
                config.InitialBufferCap = 250;
                config.MomentumReductionPerSuccess = 0.15f; // Less momentum = stops sooner
                config.AggressiveCapGrowthRate = 0.15f;     // Faster growth = stops sooner
                config.PassiveCapGrowthRate = 0.25f;
                config.AggressiveBaseMultiplier = 0.12f;    // Higher base = stops sooner
                config.PassiveBaseMultiplier = 0.18f;
                config.ConservativeMaxIterations = 2;       // Fewer iterations
                config.ConservativeHighRiskThreshold = 0.5f; // Lower risk tolerance
                break;
                
            case AIDifficultyLevel.Medium:
                // Balanced settings - default configuration
                config.PointsCapAggressive = 500;
                config.PointsCapPassive = 250;
                config.InitialBufferCap = 200;
                config.MomentumReductionPerSuccess = 0.12f;
                config.AggressiveCapGrowthRate = 0.10f;
                config.PassiveCapGrowthRate = 0.20f;
                config.AggressiveBaseMultiplier = 0.10f;
                config.PassiveBaseMultiplier = 0.15f;
                config.ConservativeMaxIterations = 2;
                config.ConservativeHighRiskThreshold = 0.6f;
                break;
                
            case AIDifficultyLevel.Hard:
                // Aggressive settings - challenging for player
                config.PointsCapAggressive = 650;
                config.PointsCapPassive = 300;
                config.InitialBufferCap = 150;
                config.MomentumReductionPerSuccess = 0.08f; // More momentum = continues longer
                config.AggressiveCapGrowthRate = 0.08f;     // Slower growth = continues longer
                config.PassiveCapGrowthRate = 0.15f;
                config.AggressiveBaseMultiplier = 0.08f;    // Lower base = continues longer
                config.PassiveBaseMultiplier = 0.12f;
                config.ConservativeMaxIterations = 3;       // More iterations
                config.ConservativeHighRiskThreshold = 0.7f; // Higher risk tolerance
                break;
                
            case AIDifficultyLevel.Custom:
                // Use current configuration from configuration manager
                if (configManager != null)
                {
                    config = configManager.GetCurrentConfiguration();
                }
                break;
        }
        
        return config;
    }
    
    void ApplyConfigurationDirectly(AIConfiguration config)
    {
        // Apply to AI components directly if no configuration manager
        if (aiTurnExecutor != null)
            aiTurnExecutor.UpdateConfiguration(config);
        
        if (gameStateAnalyzer != null && gameStateAnalyzer.config != null)
            gameStateAnalyzer.config = config;
    }
    
    void UpdateUI()
    {
        // Update current difficulty text
        if (currentDifficultyText != null)
            currentDifficultyText.text = $"Difficulty: {currentDifficulty}";
        
        // Update description text
        if (difficultyDescriptionText != null)
        {
            string description = GetDescriptionForDifficulty(currentDifficulty);
            difficultyDescriptionText.text = description;
        }
        
        // Update button states (highlight current selection)
        UpdateButtonStates();
    }
    
    void UpdateButtonStates()
    {
        // Reset all button colors
        ResetButtonColor(easyButton);
        ResetButtonColor(mediumButton);
        ResetButtonColor(hardButton);
        ResetButtonColor(customButton);
        
        // Highlight current difficulty button
        Button currentButton = GetButtonForDifficulty(currentDifficulty);
        if (currentButton != null)
        {
            HighlightButton(currentButton);
        }
    }
    
    Button GetButtonForDifficulty(AIDifficultyLevel difficulty)
    {
        switch (difficulty)
        {
            case AIDifficultyLevel.Easy: return easyButton;
            case AIDifficultyLevel.Medium: return mediumButton;
            case AIDifficultyLevel.Hard: return hardButton;
            case AIDifficultyLevel.Custom: return customButton;
            default: return null;
        }
    }
    
    string GetDescriptionForDifficulty(AIDifficultyLevel difficulty)
    {
        switch (difficulty)
        {
            case AIDifficultyLevel.Easy: return easyDescription;
            case AIDifficultyLevel.Medium: return mediumDescription;
            case AIDifficultyLevel.Hard: return hardDescription;
            case AIDifficultyLevel.Custom: return customDescription;
            default: return "";
        }
    }
    
    void ResetButtonColor(Button button)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        button.colors = colors;
    }
    
    void HighlightButton(Button button)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        colors.normalColor = Color.yellow;
        button.colors = colors;
    }
    
    /// <summary>
    /// Gets current difficulty level
    /// </summary>
    public AIDifficultyLevel GetCurrentDifficulty()
    {
        return currentDifficulty;
    }
    
    /// <summary>
    /// Applies current difficulty (useful for refreshing after changes)
    /// </summary>
    public void ApplyCurrentDifficulty()
    {
        SetDifficulty(currentDifficulty);
    }
    
    /// <summary>
    /// Gets configuration for current difficulty
    /// </summary>
    public AIConfiguration GetCurrentConfiguration()
    {
        return CreateConfigurationForDifficulty(currentDifficulty);
    }
    
    /// <summary>
    /// Resets to default difficulty (Medium)
    /// </summary>
    public void ResetToDefault()
    {
        SetDifficulty(AIDifficultyLevel.Medium);
    }
    
    // Context menu methods for testing
    [ContextMenu("Set Easy")]
    public void SetEasy() => SetDifficulty(AIDifficultyLevel.Easy);
    
    [ContextMenu("Set Medium")]
    public void SetMedium() => SetDifficulty(AIDifficultyLevel.Medium);
    
    [ContextMenu("Set Hard")]
    public void SetHard() => SetDifficulty(AIDifficultyLevel.Hard);
    
    [ContextMenu("Show Difficulty Panel")]
    public void ShowPanel() => ShowDifficultySelection();
}