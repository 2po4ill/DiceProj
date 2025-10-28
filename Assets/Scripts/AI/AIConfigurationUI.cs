using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HybridEnemyAI;

/// <summary>
/// Simple UI helper for AI configuration interface
/// Provides easy-to-use UI components for adjusting AI settings
/// </summary>
public class AIConfigurationUI : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject mainConfigPanel;
    public GameObject difficultyPanel;
    public GameObject advancedPanel;
    
    [Header("Difficulty Selection")]
    public Button easyDifficultyButton;
    public Button mediumDifficultyButton;
    public Button hardDifficultyButton;
    public Button customDifficultyButton;
    public TextMeshProUGUI currentDifficultyLabel;
    
    [Header("Quick Settings")]
    public Slider aggressivenessSlider;
    public Slider riskToleranceSlider;
    public Slider speedSlider;
    public TextMeshProUGUI aggressivenessLabel;
    public TextMeshProUGUI riskToleranceLabel;
    public TextMeshProUGUI speedLabel;
    
    [Header("Advanced Settings")]
    public Slider pointsCapAggressiveSlider;
    public Slider pointsCapPassiveSlider;
    public Slider bufferCapSlider;
    public Slider momentumReductionSlider;
    public TextMeshProUGUI pointsCapAggressiveLabel;
    public TextMeshProUGUI pointsCapPassiveLabel;
    public TextMeshProUGUI bufferCapLabel;
    public TextMeshProUGUI momentumReductionLabel;
    
    [Header("Control Buttons")]
    public Button applyButton;
    public Button resetButton;
    public Button closeButton;
    public Button showAdvancedButton;
    public Button hideAdvancedButton;
    
    [Header("AI Components")]
    public AIDifficultyManager difficultyManager;
    public AIConfigurationManager configManager;
    
    [Header("Settings")]
    public bool autoApplyChanges = true;
    public bool showAdvancedByDefault = false;
    
    private AIConfiguration currentConfig;
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeComponents();
        SetupUI();
        LoadCurrentConfiguration();
    }
    
    void InitializeComponents()
    {
        if (difficultyManager == null) difficultyManager = FindObjectOfType<AIDifficultyManager>();
        if (configManager == null) configManager = FindObjectOfType<AIConfigurationManager>();
        
        if (currentConfig == null)
        {
            currentConfig = new AIConfiguration();
        }
    }
    
    void SetupUI()
    {
        // Setup difficulty buttons
        if (easyDifficultyButton != null)
            easyDifficultyButton.onClick.AddListener(() => SetDifficulty(AIDifficultyManager.AIDifficultyLevel.Easy));
        
        if (mediumDifficultyButton != null)
            mediumDifficultyButton.onClick.AddListener(() => SetDifficulty(AIDifficultyManager.AIDifficultyLevel.Medium));
        
        if (hardDifficultyButton != null)
            hardDifficultyButton.onClick.AddListener(() => SetDifficulty(AIDifficultyManager.AIDifficultyLevel.Hard));
        
        if (customDifficultyButton != null)
            customDifficultyButton.onClick.AddListener(() => SetDifficulty(AIDifficultyManager.AIDifficultyLevel.Custom));
        
        // Setup quick settings sliders
        SetupSlider(aggressivenessSlider, 0f, 1f, OnAggressivenessChanged);
        SetupSlider(riskToleranceSlider, 0f, 1f, OnRiskToleranceChanged);
        SetupSlider(speedSlider, 0f, 1f, OnSpeedChanged);
        
        // Setup advanced settings sliders
        SetupSlider(pointsCapAggressiveSlider, 200f, 800f, OnPointsCapAggressiveChanged);
        SetupSlider(pointsCapPassiveSlider, 100f, 400f, OnPointsCapPassiveChanged);
        SetupSlider(bufferCapSlider, 50f, 300f, OnBufferCapChanged);
        SetupSlider(momentumReductionSlider, 0.05f, 0.25f, OnMomentumReductionChanged);
        
        // Setup control buttons
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyConfiguration);
        
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefaults);
        
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseConfigurationPanel);
        
        if (showAdvancedButton != null)
            showAdvancedButton.onClick.AddListener(ShowAdvancedSettings);
        
        if (hideAdvancedButton != null)
            hideAdvancedButton.onClick.AddListener(HideAdvancedSettings);
        
        // Set initial panel visibility
        if (advancedPanel != null)
            advancedPanel.SetActive(showAdvancedByDefault);
        
        if (mainConfigPanel != null)
            mainConfigPanel.SetActive(false);
        
        isInitialized = true;
    }
    
    void SetupSlider(Slider slider, float min, float max, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        
        slider.minValue = min;
        slider.maxValue = max;
        slider.onValueChanged.AddListener(callback);
    }
    
    // ===== PUBLIC METHODS =====
    
    /// <summary>
    /// Shows the configuration panel
    /// </summary>
    public void ShowConfigurationPanel()
    {
        if (mainConfigPanel != null)
        {
            mainConfigPanel.SetActive(true);
            LoadCurrentConfiguration();
            UpdateAllUI();
        }
    }
    
    /// <summary>
    /// Closes the configuration panel
    /// </summary>
    public void CloseConfigurationPanel()
    {
        if (mainConfigPanel != null)
            mainConfigPanel.SetActive(false);
    }
    
    /// <summary>
    /// Shows advanced settings panel
    /// </summary>
    public void ShowAdvancedSettings()
    {
        if (advancedPanel != null)
            advancedPanel.SetActive(true);
    }
    
    /// <summary>
    /// Hides advanced settings panel
    /// </summary>
    public void HideAdvancedSettings()
    {
        if (advancedPanel != null)
            advancedPanel.SetActive(false);
    }
    
    /// <summary>
    /// Applies current configuration to AI components
    /// </summary>
    public void ApplyConfiguration()
    {
        if (configManager != null)
        {
            configManager.SetConfiguration(currentConfig);
        }
        
        Debug.Log("AI Configuration applied");
    }
    
    /// <summary>
    /// Resets all settings to default values
    /// </summary>
    public void ResetToDefaults()
    {
        if (configManager != null)
        {
            configManager.ResetToDefault();
            LoadCurrentConfiguration();
            UpdateAllUI();
        }
        
        Debug.Log("AI Configuration reset to defaults");
    }
    
    // ===== DIFFICULTY METHODS =====
    
    void SetDifficulty(AIDifficultyManager.AIDifficultyLevel difficulty)
    {
        if (difficultyManager != null)
        {
            difficultyManager.SetDifficulty(difficulty);
            LoadCurrentConfiguration();
            UpdateAllUI();
        }
    }
    
    // ===== SLIDER CALLBACKS =====
    
    void OnAggressivenessChanged(float value)
    {
        if (!isInitialized) return;
        
        // Aggressiveness affects points caps and risk tolerance
        float aggressiveMultiplier = 1f + value; // 1.0 to 2.0
        currentConfig.PointsCapAggressive = Mathf.RoundToInt(500 * aggressiveMultiplier);
        currentConfig.PointsCapPassive = Mathf.RoundToInt(250 * aggressiveMultiplier);
        
        UpdateLabel(aggressivenessLabel, $"Aggressiveness: {value:F2}");
        
        if (autoApplyChanges) ApplyConfiguration();
    }
    
    void OnRiskToleranceChanged(float value)
    {
        if (!isInitialized) return;
        
        // Risk tolerance affects momentum reduction and stop chances
        currentConfig.MomentumReductionPerSuccess = 0.05f + (value * 0.15f); // 0.05 to 0.20
        currentConfig.ConservativeHighRiskThreshold = 0.4f + (value * 0.4f); // 0.4 to 0.8
        
        UpdateLabel(riskToleranceLabel, $"Risk Tolerance: {value:F2}");
        
        if (autoApplyChanges) ApplyConfiguration();
    }
    
    void OnSpeedChanged(float value)
    {
        if (!isInitialized) return;
        
        // Speed affects iteration limits and base multipliers
        currentConfig.ConservativeMaxIterations = Mathf.RoundToInt(1 + (value * 3)); // 1 to 4
        currentConfig.AggressiveBaseMultiplier = 0.05f + (value * 0.10f); // 0.05 to 0.15
        
        UpdateLabel(speedLabel, $"AI Speed: {value:F2}");
        
        if (autoApplyChanges) ApplyConfiguration();
    }
    
    void OnPointsCapAggressiveChanged(float value)
    {
        if (!isInitialized) return;
        
        currentConfig.PointsCapAggressive = Mathf.RoundToInt(value);
        UpdateLabel(pointsCapAggressiveLabel, $"Aggressive Cap: {(int)value}");
        
        if (autoApplyChanges) ApplyConfiguration();
    }
    
    void OnPointsCapPassiveChanged(float value)
    {
        if (!isInitialized) return;
        
        currentConfig.PointsCapPassive = Mathf.RoundToInt(value);
        UpdateLabel(pointsCapPassiveLabel, $"Passive Cap: {(int)value}");
        
        if (autoApplyChanges) ApplyConfiguration();
    }
    
    void OnBufferCapChanged(float value)
    {
        if (!isInitialized) return;
        
        currentConfig.InitialBufferCap = Mathf.RoundToInt(value);
        UpdateLabel(bufferCapLabel, $"Buffer Cap: {(int)value}");
        
        if (autoApplyChanges) ApplyConfiguration();
    }
    
    void OnMomentumReductionChanged(float value)
    {
        if (!isInitialized) return;
        
        currentConfig.MomentumReductionPerSuccess = value;
        UpdateLabel(momentumReductionLabel, $"Momentum Reduction: {value:F3}");
        
        if (autoApplyChanges) ApplyConfiguration();
    }
    
    // ===== HELPER METHODS =====
    
    void LoadCurrentConfiguration()
    {
        if (configManager != null)
        {
            currentConfig = configManager.GetCurrentConfiguration();
        }
        else if (difficultyManager != null)
        {
            currentConfig = difficultyManager.GetCurrentConfiguration();
        }
    }
    
    void UpdateAllUI()
    {
        if (!isInitialized || currentConfig == null) return;
        
        // Update difficulty label
        if (difficultyManager != null && currentDifficultyLabel != null)
        {
            currentDifficultyLabel.text = $"Current: {difficultyManager.GetCurrentDifficulty()}";
        }
        
        // Update quick settings sliders
        UpdateSliderValue(aggressivenessSlider, CalculateAggressiveness());
        UpdateSliderValue(riskToleranceSlider, CalculateRiskTolerance());
        UpdateSliderValue(speedSlider, CalculateSpeed());
        
        // Update advanced settings sliders
        UpdateSliderValue(pointsCapAggressiveSlider, currentConfig.PointsCapAggressive);
        UpdateSliderValue(pointsCapPassiveSlider, currentConfig.PointsCapPassive);
        UpdateSliderValue(bufferCapSlider, currentConfig.InitialBufferCap);
        UpdateSliderValue(momentumReductionSlider, currentConfig.MomentumReductionPerSuccess);
        
        // Update labels
        UpdateLabel(aggressivenessLabel, $"Aggressiveness: {CalculateAggressiveness():F2}");
        UpdateLabel(riskToleranceLabel, $"Risk Tolerance: {CalculateRiskTolerance():F2}");
        UpdateLabel(speedLabel, $"AI Speed: {CalculateSpeed():F2}");
        UpdateLabel(pointsCapAggressiveLabel, $"Aggressive Cap: {currentConfig.PointsCapAggressive}");
        UpdateLabel(pointsCapPassiveLabel, $"Passive Cap: {currentConfig.PointsCapPassive}");
        UpdateLabel(bufferCapLabel, $"Buffer Cap: {currentConfig.InitialBufferCap}");
        UpdateLabel(momentumReductionLabel, $"Momentum Reduction: {currentConfig.MomentumReductionPerSuccess:F3}");
    }
    
    void UpdateSliderValue(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(value);
        }
    }
    
    void UpdateLabel(TextMeshProUGUI label, string text)
    {
        if (label != null)
        {
            label.text = text;
        }
    }
    
    float CalculateAggressiveness()
    {
        // Calculate aggressiveness based on points caps (normalized 0-1)
        float normalizedAggressive = (currentConfig.PointsCapAggressive - 200f) / 600f;
        return Mathf.Clamp01(normalizedAggressive);
    }
    
    float CalculateRiskTolerance()
    {
        // Calculate risk tolerance based on momentum reduction (normalized 0-1)
        float normalizedRisk = (currentConfig.MomentumReductionPerSuccess - 0.05f) / 0.15f;
        return Mathf.Clamp01(normalizedRisk);
    }
    
    float CalculateSpeed()
    {
        // Calculate speed based on base multiplier (normalized 0-1)
        float normalizedSpeed = (currentConfig.AggressiveBaseMultiplier - 0.05f) / 0.10f;
        return Mathf.Clamp01(normalizedSpeed);
    }
}