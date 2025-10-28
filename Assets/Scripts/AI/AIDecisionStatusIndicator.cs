using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using HybridEnemyAI;

/// <summary>
/// AI Decision Status Indicator - provides visual feedback for AI decision-making process
/// Requirements: 10.4, 6.5
/// </summary>
public class AIDecisionStatusIndicator : MonoBehaviour
{
    [Header("Status Display Elements")]
    [SerializeField] private Image statusIcon;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI decisionReasonText;
    [SerializeField] private Slider confidenceSlider;
    [SerializeField] private TextMeshProUGUI confidenceText;
    
    [Header("Decision Process Visualization")]
    [SerializeField] private Transform decisionFactorsContainer;
    [SerializeField] private GameObject decisionFactorPrefab;
    [SerializeField] private int maxFactorsDisplayed = 5;
    
    [Header("Status Icons")]
    [SerializeField] private Sprite thinkingIcon;
    [SerializeField] private Sprite analyzingIcon;
    [SerializeField] private Sprite decidingIcon;
    [SerializeField] private Sprite continueIcon;
    [SerializeField] private Sprite stopIcon;
    [SerializeField] private Sprite zonkIcon;
    
    [Header("Status Colors")]
    [SerializeField] private Color thinkingColor = Color.yellow;
    [SerializeField] private Color analyzingColor = Color.blue;
    [SerializeField] private Color decidingColor = Color.orange;
    [SerializeField] private Color continueColor = Color.green;
    [SerializeField] private Color stopColor = Color.red;
    [SerializeField] private Color zonkColor = Color.magenta;
    [SerializeField] private Color inactiveColor = Color.gray;
    
    [Header("Animation Settings")]
    [SerializeField] private float statusTransitionSpeed = 2f;
    [SerializeField] private bool enablePulseAnimation = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showDetailedFactors = true;
    
    // Component references
    private AITurnExecutor aiTurnExecutor;
    private AIDecisionEngine decisionEngine;
    
    // Current state
    private AIDecisionStatus currentStatus = AIDecisionStatus.Inactive;
    private AIStopDecision lastDecision;
    private float currentConfidence = 0f;
    
    // Animation tracking
    private Coroutine statusAnimationCoroutine;
    private Coroutine pulseAnimationCoroutine;
    
    // Decision factors display
    private System.Collections.Generic.List<GameObject> factorDisplays = new System.Collections.Generic.List<GameObject>();
    
    void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        InitializeDisplay();
    }
    
    void InitializeComponents()
    {
        // Find AI components
        if (aiTurnExecutor == null)
            aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        
        if (decisionEngine == null)
            decisionEngine = FindObjectOfType<AIDecisionEngine>();
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIDecisionStatusIndicator: Components initialized - " +
                     $"TurnExecutor: {aiTurnExecutor != null}, " +
                     $"DecisionEngine: {decisionEngine != null}");
        }
    }
    
    void SetupEventListeners()
    {
        if (aiTurnExecutor != null)
        {
            aiTurnExecutor.OnTurnStarted += HandleTurnStarted;
            aiTurnExecutor.OnCombinationSelected += HandleCombinationSelected;
            aiTurnExecutor.OnDecisionMade += HandleDecisionMade;
            aiTurnExecutor.OnTurnCompleted += HandleTurnCompleted;
            aiTurnExecutor.OnZonkOccurred += HandleZonkOccurred;
            
            if (enableDebugLogs)
                Debug.Log("AIDecisionStatusIndicator: Event listeners set up");
        }
    }
    
    void InitializeDisplay()
    {
        SetStatus(AIDecisionStatus.Inactive);
        UpdateConfidence(0f);
        ClearDecisionFactors();
        
        if (enableDebugLogs)
            Debug.Log("AIDecisionStatusIndicator: Display initialized");
    }
    
    #region Event Handlers
    
    void HandleTurnStarted(AITurnState turnState)
    {
        SetStatus(AIDecisionStatus.Thinking);
        UpdateStatusText("AI turn started - analyzing options...");
        UpdateConfidence(0.1f);
        
        if (enableDebugLogs)
            Debug.Log($"AIDecisionStatusIndicator: Turn started - Mode: {turnState.CurrentMode}");
    }
    
    void HandleCombinationSelected(CombinationResult combination)
    {
        if (combination == null) return;
        
        SetStatus(AIDecisionStatus.Analyzing);
        UpdateStatusText($"Selected {combination.rule} - analyzing next move...");
        UpdateConfidence(0.5f);
        
        if (enableDebugLogs)
            Debug.Log($"AIDecisionStatusIndicator: Combination selected - {combination.rule}");
    }
    
    void HandleDecisionMade(AIStopDecision decision)
    {
        if (decision == null) return;
        
        lastDecision = decision;
        
        // Set status based on decision
        if (decision.ShouldStop)
        {
            SetStatus(AIDecisionStatus.Stop);
            UpdateStatusText("Decision: STOP TURN");
        }
        else
        {
            SetStatus(AIDecisionStatus.Continue);
            UpdateStatusText("Decision: CONTINUE");
        }
        
        // Update confidence based on decision certainty
        float confidence = CalculateDecisionConfidence(decision);
        UpdateConfidence(confidence);
        
        // Update decision reason
        UpdateDecisionReason(decision.DecisionReason);
        
        // Show decision factors if enabled
        if (showDetailedFactors)
        {
            DisplayDecisionFactors(decision);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIDecisionStatusIndicator: Decision made - {(decision.ShouldStop ? "STOP" : "CONTINUE")}, " +
                     $"Confidence: {confidence:P1}");
        }
    }
    
    void HandleTurnCompleted(AITurnState turnState)
    {
        SetStatus(AIDecisionStatus.Inactive);
        UpdateStatusText($"Turn complete - scored {turnState.CurrentTurnScore} points");
        UpdateConfidence(1f);
        
        // Clear decision factors after a delay
        StartCoroutine(ClearDecisionFactorsDelayed(3f));
        
        if (enableDebugLogs)
            Debug.Log($"AIDecisionStatusIndicator: Turn completed - Score: {turnState.CurrentTurnScore}");
    }
    
    void HandleZonkOccurred()
    {
        SetStatus(AIDecisionStatus.Zonk);
        UpdateStatusText("ZONK! All progress lost!");
        UpdateConfidence(0f);
        UpdateDecisionReason("Zonk occurred - no valid combinations");
        
        if (enableDebugLogs)
            Debug.Log("AIDecisionStatusIndicator: Zonk occurred");
    }
    
    #endregion
    
    #region Status Update Methods
    
    void SetStatus(AIDecisionStatus status)
    {
        if (currentStatus == status) return;
        
        currentStatus = status;
        
        // Update visual elements based on status
        UpdateStatusIcon(status);
        UpdateStatusColor(status);
        
        // Start pulse animation for active states
        if (enablePulseAnimation && IsActiveStatus(status))
        {
            StartPulseAnimation();
        }
        else
        {
            StopPulseAnimation();
        }
        
        if (enableDebugLogs)
            Debug.Log($"AIDecisionStatusIndicator: Status changed to {status}");
    }
    
    void UpdateStatusIcon(AIDecisionStatus status)
    {
        if (statusIcon == null) return;
        
        Sprite targetSprite = GetStatusSprite(status);
        if (targetSprite != null)
        {
            statusIcon.sprite = targetSprite;
            statusIcon.enabled = true;
        }
        else
        {
            statusIcon.enabled = false;
        }
    }
    
    void UpdateStatusColor(AIDecisionStatus status)
    {
        Color targetColor = GetStatusColor(status);
        
        if (statusIcon != null)
        {
            if (statusAnimationCoroutine != null)
                StopCoroutine(statusAnimationCoroutine);
            
            statusAnimationCoroutine = StartCoroutine(AnimateColorTransition(statusIcon, targetColor));
        }
        
        if (statusText != null)
        {
            statusText.color = targetColor;
        }
    }
    
    void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }
    
    void UpdateDecisionReason(string reason)
    {
        if (decisionReasonText != null)
        {
            decisionReasonText.text = $"Reason: {reason}";
        }
    }
    
    void UpdateConfidence(float confidence)
    {
        currentConfidence = Mathf.Clamp01(confidence);
        
        if (confidenceSlider != null)
        {
            StartCoroutine(AnimateSliderUpdate(confidenceSlider, currentConfidence));
        }
        
        if (confidenceText != null)
        {
            confidenceText.text = $"Confidence: {currentConfidence:P0}";
        }
    }
    
    void DisplayDecisionFactors(AIStopDecision decision)
    {
        ClearDecisionFactors();
        
        if (decisionFactorsContainer == null) return;
        
        // Create factor displays
        var factors = GetDecisionFactorsList(decision);
        
        for (int i = 0; i < Mathf.Min(factors.Count, maxFactorsDisplayed); i++)
        {
            CreateDecisionFactorDisplay(factors[i]);
        }
    }
    
    void CreateDecisionFactorDisplay(DecisionFactor factor)
    {
        GameObject factorDisplay;
        
        if (decisionFactorPrefab != null)
        {
            factorDisplay = Instantiate(decisionFactorPrefab, decisionFactorsContainer);
        }
        else
        {
            factorDisplay = CreateSimpleFactorDisplay();
        }
        
        // Set factor information
        TextMeshProUGUI factorText = factorDisplay.GetComponentInChildren<TextMeshProUGUI>();
        if (factorText != null)
        {
            factorText.text = $"{factor.Name}: {factor.Value}";
            factorText.color = factor.IsPositive ? Color.green : Color.red;
        }
        
        // Set factor weight visualization
        Slider factorSlider = factorDisplay.GetComponentInChildren<Slider>();
        if (factorSlider != null)
        {
            factorSlider.value = factor.Weight;
            factorSlider.interactable = false;
        }
        
        factorDisplays.Add(factorDisplay);
    }
    
    GameObject CreateSimpleFactorDisplay()
    {
        GameObject display = new GameObject("DecisionFactor");
        display.transform.SetParent(decisionFactorsContainer);
        
        // Add text component
        TextMeshProUGUI text = display.AddComponent<TextMeshProUGUI>();
        text.fontSize = 12;
        text.color = Color.white;
        
        // Add rect transform
        RectTransform rect = display.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 30);
        
        return display;
    }
    
    void ClearDecisionFactors()
    {
        foreach (GameObject display in factorDisplays)
        {
            if (display != null)
                DestroyImmediate(display);
        }
        
        factorDisplays.Clear();
    }
    
    #endregion
    
    #region Helper Methods
    
    Sprite GetStatusSprite(AIDecisionStatus status)
    {
        switch (status)
        {
            case AIDecisionStatus.Thinking: return thinkingIcon;
            case AIDecisionStatus.Analyzing: return analyzingIcon;
            case AIDecisionStatus.Deciding: return decidingIcon;
            case AIDecisionStatus.Continue: return continueIcon;
            case AIDecisionStatus.Stop: return stopIcon;
            case AIDecisionStatus.Zonk: return zonkIcon;
            default: return null;
        }
    }
    
    Color GetStatusColor(AIDecisionStatus status)
    {
        switch (status)
        {
            case AIDecisionStatus.Thinking: return thinkingColor;
            case AIDecisionStatus.Analyzing: return analyzingColor;
            case AIDecisionStatus.Deciding: return decidingColor;
            case AIDecisionStatus.Continue: return continueColor;
            case AIDecisionStatus.Stop: return stopColor;
            case AIDecisionStatus.Zonk: return zonkColor;
            default: return inactiveColor;
        }
    }
    
    bool IsActiveStatus(AIDecisionStatus status)
    {
        return status != AIDecisionStatus.Inactive;
    }
    
    float CalculateDecisionConfidence(AIStopDecision decision)
    {
        // Calculate confidence based on how decisive the probabilities are
        float combinedChance = decision.CombinedStopChance;
        
        // High confidence when probability is very high or very low
        if (combinedChance > 0.8f || combinedChance < 0.2f)
            return 0.9f;
        else if (combinedChance > 0.6f || combinedChance < 0.4f)
            return 0.7f;
        else
            return 0.5f; // Moderate confidence for middle probabilities
    }
    
    System.Collections.Generic.List<DecisionFactor> GetDecisionFactorsList(AIStopDecision decision)
    {
        var factors = new System.Collections.Generic.List<DecisionFactor>();
        
        factors.Add(new DecisionFactor
        {
            Name = "Momentum Chance",
            Value = $"{decision.MomentumStopChance:P1}",
            Weight = decision.MomentumStopChance,
            IsPositive = decision.MomentumStopChance < 0.5f
        });
        
        factors.Add(new DecisionFactor
        {
            Name = "Cap Chance",
            Value = $"{decision.CapStopChance:P1}",
            Weight = decision.CapStopChance,
            IsPositive = decision.CapStopChance < 0.5f
        });
        
        factors.Add(new DecisionFactor
        {
            Name = "Combined Chance",
            Value = $"{decision.CombinedStopChance:P1}",
            Weight = decision.CombinedStopChance,
            IsPositive = decision.CombinedStopChance < 0.5f
        });
        
        factors.Add(new DecisionFactor
        {
            Name = "Momentum Roll",
            Value = decision.MomentumRollResult.ToString(),
            Weight = decision.MomentumRollResult ? 1f : 0f,
            IsPositive = !decision.MomentumRollResult
        });
        
        factors.Add(new DecisionFactor
        {
            Name = "Cap Roll",
            Value = decision.CapRollResult.ToString(),
            Weight = decision.CapRollResult ? 1f : 0f,
            IsPositive = !decision.CapRollResult
        });
        
        return factors;
    }
    
    #endregion
    
    #region Animation Methods
    
    void StartPulseAnimation()
    {
        if (pulseAnimationCoroutine != null)
            StopCoroutine(pulseAnimationCoroutine);
        
        pulseAnimationCoroutine = StartCoroutine(PulseAnimation());
    }
    
    void StopPulseAnimation()
    {
        if (pulseAnimationCoroutine != null)
        {
            StopCoroutine(pulseAnimationCoroutine);
            pulseAnimationCoroutine = null;
        }
        
        // Reset to normal scale
        if (statusIcon != null)
            statusIcon.transform.localScale = Vector3.one;
    }
    
    IEnumerator PulseAnimation()
    {
        if (statusIcon == null) yield break;
        
        Vector3 originalScale = statusIcon.transform.localScale;
        
        while (true)
        {
            // Pulse up
            float elapsed = 0f;
            float duration = 1f / pulseSpeed;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 1f + pulseIntensity, elapsed / duration);
                statusIcon.transform.localScale = originalScale * scale;
                yield return null;
            }
            
            // Pulse down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f + pulseIntensity, 1f, elapsed / duration);
                statusIcon.transform.localScale = originalScale * scale;
                yield return null;
            }
        }
    }
    
    IEnumerator AnimateColorTransition(Image image, Color targetColor)
    {
        if (image == null) yield break;
        
        Color startColor = image.color;
        float duration = 1f / statusTransitionSpeed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        image.color = targetColor;
    }
    
    IEnumerator AnimateSliderUpdate(Slider slider, float targetValue)
    {
        if (slider == null) yield break;
        
        float startValue = slider.value;
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            slider.value = Mathf.Lerp(startValue, targetValue, t);
            yield return null;
        }
        
        slider.value = targetValue;
    }
    
    IEnumerator ClearDecisionFactorsDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearDecisionFactors();
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Manually set status (for external control)
    /// </summary>
    public void SetStatusManually(AIDecisionStatus status, string statusText = null)
    {
        SetStatus(status);
        if (!string.IsNullOrEmpty(statusText))
            UpdateStatusText(statusText);
    }
    
    /// <summary>
    /// Toggle detailed factors display
    /// </summary>
    public void SetShowDetailedFactors(bool show)
    {
        showDetailedFactors = show;
        
        if (!show)
            ClearDecisionFactors();
        
        if (enableDebugLogs)
            Debug.Log($"AIDecisionStatusIndicator: Detailed factors display set to {show}");
    }
    
    /// <summary>
    /// Toggle pulse animation
    /// </summary>
    public void SetEnablePulseAnimation(bool enable)
    {
        enablePulseAnimation = enable;
        
        if (!enable)
            StopPulseAnimation();
        else if (IsActiveStatus(currentStatus))
            StartPulseAnimation();
        
        if (enableDebugLogs)
            Debug.Log($"AIDecisionStatusIndicator: Pulse animation set to {enable}");
    }
    
    /// <summary>
    /// Get current status
    /// </summary>
    public AIDecisionStatus GetCurrentStatus()
    {
        return currentStatus;
    }
    
    /// <summary>
    /// Get current confidence level
    /// </summary>
    public float GetCurrentConfidence()
    {
        return currentConfidence;
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (aiTurnExecutor != null)
        {
            aiTurnExecutor.OnTurnStarted -= HandleTurnStarted;
            aiTurnExecutor.OnCombinationSelected -= HandleCombinationSelected;
            aiTurnExecutor.OnDecisionMade -= HandleDecisionMade;
            aiTurnExecutor.OnTurnCompleted -= HandleTurnCompleted;
            aiTurnExecutor.OnZonkOccurred -= HandleZonkOccurred;
        }
        
        // Stop animations
        StopAllCoroutines();
        
        // Clean up factor displays
        ClearDecisionFactors();
    }
}

/// <summary>
/// AI Decision Status enumeration
/// </summary>
public enum AIDecisionStatus
{
    Inactive,
    Thinking,
    Analyzing,
    Deciding,
    Continue,
    Stop,
    Zonk
}

/// <summary>
/// Decision factor for visualization
/// </summary>
[System.Serializable]
public class DecisionFactor
{
    public string Name;
    public string Value;
    public float Weight;
    public bool IsPositive;
}