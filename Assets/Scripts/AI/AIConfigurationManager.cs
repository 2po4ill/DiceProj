using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HybridEnemyAI;

/// <summary>
/// Manages AI configuration and difficulty presets for runtime tuning
/// </summary>
public class AIConfigurationManager : MonoBehaviour
{
    [Header("AI Components")]
    public AITurnExecutor aiTurnExecutor;
    public AIGameStateAnalyzer gameStateAnalyzer;
    public AIRiskCalculator riskCalculator;
    
    [Header("Configuration Presets")]
    public AIDifficultyPreset[] difficultyPresets;
    
    [Header("UI References")]
    public GameObject configurationPanel;
    public TMP_Dropdown difficultyDropdown;
    public Button applyConfigButton;
    public Button resetToDefaultButton;
    public Button closeConfigButton;
    
    [Header("Runtime Adjustment UI")]
    public Slider aggressiveCapSlider;
    public Slider passiveCapSlider;
    public Slider bufferCapSlider;
    public Slider momentumReductionSlider;
    public TextMeshProUGUI aggressiveCapText;
    public TextMeshProUGUI passiveCapText;
    public TextMeshProUGUI bufferCapText;
    public TextMeshProUGUI momentumReductionText;
    
    [Header("Current Configuration")]
    public AIConfiguration currentConfig;
    
    private AIConfiguration defaultConfig;
    
    void Start()
    {
        InitializeConfiguration();
        SetupUI();
    }
    
    void InitializeConfiguration()
    {
        // Get AI components if not assigned
        if (aiTurnExecutor == null) aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        if (gameStateAnalyzer == null) gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
        if (riskCalculator == null) riskCalculator = FindObjectOfType<AIRiskCalculator>();
        
        // Initialize default configuration
        if (currentConfig == null)
        {
            currentConfig = new AIConfiguration();
        }
        
        // Store default configuration
        defaultConfig = CreateConfigurationCopy(currentConfig);
        
        // Initialize difficulty presets if not set
        if (difficultyPresets == null || difficultyPresets.Length == 0)
        {
            CreateDefaultPresets();
        }
    }
    
    void SetupUI()
    {
        // Setup button listeners
        if (applyConfigButton != null)
            applyConfigButton.onClick.AddListener(ApplyCurrentConfiguration);
        
        if (resetToDefaultButton != null)
            resetToDefaultButton.onClick.AddListener(ResetToDefault);
        
        if (closeConfigButton != null)
            closeConfigButton.onClick.AddListener(CloseConfigurationPanel);
        
        // Setup dropdown
        if (difficultyDropdown != null)
        {
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyPresetChanged);
            PopulateDifficultyDropdown();
        }
        
        // Setup sliders
        SetupSliders();
        
        // Hide configuration panel initially
        if (configurationPanel != null)
            configurationPanel.SetActive(false);
        
        // Update UI with current values
        UpdateUIFromConfiguration();
    }
    
    void SetupSliders()
    {
        if (aggressiveCapSlider != null)
        {
            aggressiveCapSlider.minValue = 200f;
            aggressiveCapSlider.maxValue = 800f;
            aggressiveCapSlider.onValueChanged.AddListener(OnAggressiveCapChanged);
        }
        
        if (passiveCapSlider != null)
        {
            passiveCapSlider.minValue = 100f;
            passiveCapSlider.maxValue = 400f;
            passiveCapSlider.onValueChanged.AddListener(OnPassiveCapChanged);
        }
        
        if (bufferCapSlider != null)
        {
            bufferCapSlider.minValue = 50f;
            bufferCapSlider.maxValue = 300f;
            bufferCapSlider.onValueChanged.AddListener(OnBufferCapChanged);
        }
        
        if (momentumReductionSlider != null)
        {
            momentumReductionSlider.minValue = 0.05f;
            momentumReductionSlider.maxValue = 0.25f;
            momentumReductionSlider.onValueChanged.AddListener(OnMomentumReductionChanged);
        }
    }
    
    void CreateDefaultPresets()
    {
        difficultyPresets = new AIDifficultyPreset[3];
        
        // Easy preset
        difficultyPresets[0] = new AIDifficultyPreset
        {
            name = "Easy",
            description = "Conservative AI with lower caps and higher risk avoidance",
            config = new AIConfiguration
            {
                PointsCapAggressive = 350,
                PointsCapPassive = 200,
                InitialBufferCap = 250,
                MomentumReductionPerSuccess = 0.15f,
                AggressiveCapGrowthRate = 0.15f,
                PassiveCapGrowthRate = 0.25f
            }
        };
        
        // Medium preset (default)
        difficultyPresets[1] = new AIDifficultyPreset
        {
            name = "Medium",
            description = "Balanced AI with standard settings",
            config = new AIConfiguration
            {
                PointsCapAggressive = 500,
                PointsCapPassive = 250,
                InitialBufferCap = 200,
                MomentumReductionPerSuccess = 0.12f,
                AggressiveCapGrowthRate = 0.10f,
                PassiveCapGrowthRate = 0.20f
            }
        };
        
        // Hard preset
        difficultyPresets[2] = new AIDifficultyPreset
        {
            name = "Hard",
            description = "Aggressive AI with higher caps and better risk management",
            config = new AIConfiguration
            {
                PointsCapAggressive = 650,
                PointsCapPassive = 300,
                InitialBufferCap = 150,
                MomentumReductionPerSuccess = 0.08f,
                AggressiveCapGrowthRate = 0.08f,
                PassiveCapGrowthRate = 0.15f
            }
        };
    }
    
    void PopulateDifficultyDropdown()
    {
        if (difficultyDropdown == null || difficultyPresets == null) return;
        
        difficultyDropdown.ClearOptions();
        
        var options = new System.Collections.Generic.List<string>();
        foreach (var preset in difficultyPresets)
        {
            options.Add(preset.name);
        }
        
        difficultyDropdown.AddOptions(options);
        difficultyDropdown.value = 1; // Default to Medium
    }
    
    // ===== UI EVENT HANDLERS =====
    
    void OnDifficultyPresetChanged(int index)
    {
        if (index >= 0 && index < difficultyPresets.Length)
        {
            ApplyDifficultyPreset(difficultyPresets[index]);
        }
    }
    
    void OnAggressiveCapChanged(float value)
    {
        currentConfig.PointsCapAggressive = (int)value;
        if (aggressiveCapText != null)
            aggressiveCapText.text = $"Aggressive Cap: {(int)value}";
    }
    
    void OnPassiveCapChanged(float value)
    {
        currentConfig.PointsCapPassive = (int)value;
        if (passiveCapText != null)
            passiveCapText.text = $"Passive Cap: {(int)value}";
    }
    
    void OnBufferCapChanged(float value)
    {
        currentConfig.InitialBufferCap = (int)value;
        if (bufferCapText != null)
            bufferCapText.text = $"Buffer Cap: {(int)value}";
    }
    
    void OnMomentumReductionChanged(float value)
    {
        currentConfig.MomentumReductionPerSuccess = value;
        if (momentumReductionText != null)
            momentumReductionText.text = $"Momentum Reduction: {value:F2}";
    }
    
    // ===== PUBLIC METHODS =====
    
    /// <summary>
    /// Shows the configuration panel
    /// </summary>
    public void ShowConfigurationPanel()
    {
        if (configurationPanel != null)
        {
            configurationPanel.SetActive(true);
            UpdateUIFromConfiguration();
        }
    }
    
    /// <summary>
    /// Closes the configuration panel
    /// </summary>
    public void CloseConfigurationPanel()
    {
        if (configurationPanel != null)
            configurationPanel.SetActive(false);
    }
    
    /// <summary>
    /// Applies a difficulty preset
    /// </summary>
    public void ApplyDifficultyPreset(AIDifficultyPreset preset)
    {
        if (preset?.config == null) return;
        
        currentConfig = CreateConfigurationCopy(preset.config);
        UpdateUIFromConfiguration();
        ApplyCurrentConfiguration();
        
        Debug.Log($"Applied difficulty preset: {preset.name}");
    }
    
    /// <summary>
    /// Applies current configuration to AI components
    /// </summary>
    public void ApplyCurrentConfiguration()
    {
        if (aiTurnExecutor != null)
            aiTurnExecutor.UpdateConfiguration(currentConfig);
        
        if (gameStateAnalyzer != null && gameStateAnalyzer.config != null)
        {
            // Update game state analyzer configuration
            gameStateAnalyzer.config = CreateConfigurationCopy(currentConfig);
        }
        
        if (riskCalculator != null)
            riskCalculator.UpdateConfiguration(currentConfig);
        
        Debug.Log("AI Configuration applied to all components");
    }
    
    /// <summary>
    /// Resets configuration to default values
    /// </summary>
    public void ResetToDefault()
    {
        currentConfig = CreateConfigurationCopy(defaultConfig);
        UpdateUIFromConfiguration();
        ApplyCurrentConfiguration();
        
        Debug.Log("AI Configuration reset to default");
    }
    
    /// <summary>
    /// Gets current AI configuration
    /// </summary>
    public AIConfiguration GetCurrentConfiguration()
    {
        return CreateConfigurationCopy(currentConfig);
    }
    
    /// <summary>
    /// Sets AI configuration from external source
    /// </summary>
    public void SetConfiguration(AIConfiguration config)
    {
        if (config == null) return;
        
        currentConfig = CreateConfigurationCopy(config);
        UpdateUIFromConfiguration();
        ApplyCurrentConfiguration();
    }
    
    // ===== HELPER METHODS =====
    
    void UpdateUIFromConfiguration()
    {
        if (aggressiveCapSlider != null)
        {
            aggressiveCapSlider.value = currentConfig.PointsCapAggressive;
            OnAggressiveCapChanged(currentConfig.PointsCapAggressive);
        }
        
        if (passiveCapSlider != null)
        {
            passiveCapSlider.value = currentConfig.PointsCapPassive;
            OnPassiveCapChanged(currentConfig.PointsCapPassive);
        }
        
        if (bufferCapSlider != null)
        {
            bufferCapSlider.value = currentConfig.InitialBufferCap;
            OnBufferCapChanged(currentConfig.InitialBufferCap);
        }
        
        if (momentumReductionSlider != null)
        {
            momentumReductionSlider.value = currentConfig.MomentumReductionPerSuccess;
            OnMomentumReductionChanged(currentConfig.MomentumReductionPerSuccess);
        }
    }
    
    AIConfiguration CreateConfigurationCopy(AIConfiguration source)
    {
        return new AIConfiguration
        {
            PointsCapAggressive = source.PointsCapAggressive,
            PointsCapPassive = source.PointsCapPassive,
            StateBufferThreshold = source.StateBufferThreshold,
            InitialBufferCap = source.InitialBufferCap,
            BufferReductionPerRound = source.BufferReductionPerRound,
            RoundsPerReduction = source.RoundsPerReduction,
            MinimumBufferCap = source.MinimumBufferCap,
            AggressiveBaseMultiplier = source.AggressiveBaseMultiplier,
            PassiveBaseMultiplier = source.PassiveBaseMultiplier,
            MomentumReductionPerSuccess = source.MomentumReductionPerSuccess,
            MinimumMomentumMultiplier = source.MinimumMomentumMultiplier,
            DiceRiskExponent = source.DiceRiskExponent,
            DiceRiskMultiplier = source.DiceRiskMultiplier,
            IterationPressureIncrease = source.IterationPressureIncrease,
            MaxMomentumStopChance = source.MaxMomentumStopChance,
            BaseCapStopChance = source.BaseCapStopChance,
            AggressiveCapGrowthRate = source.AggressiveCapGrowthRate,
            PassiveCapGrowthRate = source.PassiveCapGrowthRate,
            CapGrowthInterval = source.CapGrowthInterval,
            MaxCapStopChance = source.MaxCapStopChance,
            AggressiveInitialThreshold = source.AggressiveInitialThreshold,
            AggressiveThresholdReduction = source.AggressiveThresholdReduction,
            PassiveInitialThreshold = source.PassiveInitialThreshold,
            PassiveThresholdReduction = source.PassiveThresholdReduction,
            ConservativeMaxIterations = source.ConservativeMaxIterations,
            ConservativeHighRiskThreshold = source.ConservativeHighRiskThreshold,
            ConservativeEarlySatisfactionThreshold = source.ConservativeEarlySatisfactionThreshold,
            ConservativeTwoDiceStopChance = source.ConservativeTwoDiceStopChance,
            ConservativeOneDiceStopChance = source.ConservativeOneDiceStopChance,
            EnableDebugLogs = source.EnableDebugLogs,
            ShowProbabilityCalculations = source.ShowProbabilityCalculations
        };
    }
}

/// <summary>
/// Difficulty preset for AI configuration
/// </summary>
[System.Serializable]
public class AIDifficultyPreset
{
    public string name;
    public string description;
    public AIConfiguration config;
}