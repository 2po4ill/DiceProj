using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// AI Turn Feedback System - provides real-time visual feedback during AI decision making
/// Requirements: 10.4, 6.5
/// </summary>
public class AITurnFeedbackSystem : MonoBehaviour
{
    [Header("Combination Selection Display")]
    [SerializeField] private TextMeshProUGUI combinationSelectionText;
    [SerializeField] private Transform combinationHistoryContainer;
    [SerializeField] private GameObject combinationEntryPrefab;
    [SerializeField] private int maxHistoryEntries = 5;
    
    [Header("Turn Progress Indicators")]
    [SerializeField] private Slider iterationProgressSlider;
    [SerializeField] private TextMeshProUGUI iterationCounterText;
    [SerializeField] private Image[] iterationDots; // Visual dots for each iteration
    [SerializeField] private Color completedIterationColor = Color.green;
    [SerializeField] private Color currentIterationColor = Color.yellow;
    [SerializeField] private Color pendingIterationColor = Color.gray;
    
    [Header("Decision Making Visualization")]
    [SerializeField] private TextMeshProUGUI decisionProcessText;
    [SerializeField] private Slider momentumSlider;
    [SerializeField] private Slider capSlider;
    [SerializeField] private Slider combinedProbabilitySlider;
    [SerializeField] private TextMeshProUGUI probabilityBreakdownText;
    
    [Header("Real-time Status")]
    [SerializeField] private TextMeshProUGUI currentActionText;
    [SerializeField] private Image statusIndicator;
    [SerializeField] private TextMeshProUGUI diceCountText;
    [SerializeField] private TextMeshProUGUI riskLevelText;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem successEffect;
    [SerializeField] private ParticleSystem zonkEffect;
    [SerializeField] private AudioSource feedbackAudioSource;
    [SerializeField] private AudioClip combinationSound;
    [SerializeField] private AudioClip decisionSound;
    [SerializeField] private AudioClip zonkSound;
    
    [Header("Animation Settings")]
    [SerializeField] private float textUpdateSpeed = 2f;
    [SerializeField] private float sliderUpdateSpeed = 3f;
    [SerializeField] private float feedbackDisplayDuration = 2f;
    [SerializeField] private bool enableSmoothAnimations = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showDetailedFeedback = true;
    
    // Component references
    private AITurnExecutor aiTurnExecutor;
    private AIDecisionEngine decisionEngine;
    
    // State tracking
    private List<CombinationResult> combinationHistory = new List<CombinationResult>();
    private AITurnState currentTurnState;
    private AIStopDecision lastDecision;
    private bool isShowingFeedback = false;
    
    // Animation coroutines
    private Coroutine feedbackCoroutine;
    private Coroutine statusUpdateCoroutine;
    
    void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        InitializeFeedbackSystem();
    }
    
    void InitializeComponents()
    {
        // Find AI components
        if (aiTurnExecutor == null)
            aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        
        if (decisionEngine == null)
            decisionEngine = FindObjectOfType<AIDecisionEngine>();
        
        // Initialize UI elements
        InitializeIterationDots();
        InitializeProbabilitySliders();
        
        if (enableDebugLogs)
        {
            Debug.Log($"AITurnFeedbackSystem: Components initialized - " +
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
                Debug.Log("AITurnFeedbackSystem: Event listeners set up");
        }
    }
    
    void InitializeFeedbackSystem()
    {
        // Set initial states
        ClearAllFeedback();
        UpdateCurrentAction("Waiting for AI turn...");
        UpdateStatusIndicator(Color.gray, "Inactive");
        
        if (enableDebugLogs)
            Debug.Log("AITurnFeedbackSystem: Feedback system initialized");
    }
    
    void InitializeIterationDots()
    {
        if (iterationDots != null)
        {
            foreach (Image dot in iterationDots)
            {
                if (dot != null)
                    dot.color = pendingIterationColor;
            }
        }
    }
    
    void InitializeProbabilitySliders()
    {
        if (momentumSlider != null)
        {
            momentumSlider.value = 0f;
            momentumSlider.interactable = false;
        }
        
        if (capSlider != null)
        {
            capSlider.value = 0f;
            capSlider.interactable = false;
        }
        
        if (combinedProbabilitySlider != null)
        {
            combinedProbabilitySlider.value = 0f;
            combinedProbabilitySlider.interactable = false;
        }
    }
    
    #region Event Handlers
    
    void HandleTurnStarted(AITurnState turnState)
    {
        currentTurnState = turnState;
        combinationHistory.Clear();
        
        UpdateCurrentAction($"AI Turn Started - {turnState.CurrentMode} Mode");
        UpdateStatusIndicator(Color.blue, "Active");
        UpdateIterationProgress(0, turnState.MaxIterations);
        UpdateDiceCount(turnState.CurrentDice?.Count ?? 0);
        UpdateRiskLevel("Analyzing...", Color.yellow);
        
        ClearCombinationHistory();
        
        if (enableDebugLogs)
        {
            Debug.Log($"AITurnFeedbackSystem: Turn started - Mode: {turnState.CurrentMode}, " +
                     $"Max Iterations: {turnState.MaxIterations}");
        }
    }
    
    void HandleCombinationSelected(CombinationResult combination)
    {
        if (combination == null) return;
        
        // Add to history
        combinationHistory.Add(combination);
        
        // Update combination display
        UpdateCombinationSelection(combination);
        AddCombinationToHistory(combination);
        
        // Update current action
        UpdateCurrentAction($"Selected: {combination.rule} ({combination.points} pts)");
        
        // Update dice count
        if (currentTurnState != null)
        {
            UpdateDiceCount(currentTurnState.CurrentDice?.Count ?? 0);
        }
        
        // Play sound effect
        PlayFeedbackSound(combinationSound);
        
        // Show success effect
        if (successEffect != null && combination.rule != Rule.Zonk)
        {
            successEffect.Play();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"AITurnFeedbackSystem: Combination selected - {combination.rule}: {combination.points} pts");
        }
    }
    
    void HandleDecisionMade(AIStopDecision decision)
    {
        if (decision == null) return;
        
        lastDecision = decision;
        
        // Update decision visualization
        UpdateDecisionVisualization(decision);
        UpdateProbabilityBreakdown(decision);
        
        // Update current action
        string actionText = decision.ShouldStop ? "Decision: STOP TURN" : "Decision: CONTINUE";
        UpdateCurrentAction(actionText);
        
        // Update status indicator
        Color statusColor = decision.ShouldStop ? Color.red : Color.green;
        string statusText = decision.ShouldStop ? "Stopping" : "Continuing";
        UpdateStatusIndicator(statusColor, statusText);
        
        // Update iteration progress
        if (currentTurnState != null)
        {
            UpdateIterationProgress(currentTurnState.IterationCount, currentTurnState.MaxIterations);
        }
        
        // Play decision sound
        PlayFeedbackSound(decisionSound);
        
        if (enableDebugLogs)
        {
            Debug.Log($"AITurnFeedbackSystem: Decision made - {(decision.ShouldStop ? "STOP" : "CONTINUE")}, " +
                     $"Combined: {decision.CombinedStopChance:P1}");
        }
    }
    
    void HandleTurnCompleted(AITurnState turnState)
    {
        currentTurnState = turnState;
        
        UpdateCurrentAction($"Turn Complete - Final Score: {turnState.CurrentTurnScore}");
        UpdateStatusIndicator(Color.gray, "Complete");
        UpdateIterationProgress(turnState.IterationCount, turnState.MaxIterations);
        
        // Show final summary
        if (showDetailedFeedback)
        {
            ShowTurnSummary(turnState);
        }
        
        // Start cleanup timer
        StartCoroutine(ClearFeedbackDelayed(feedbackDisplayDuration));
        
        if (enableDebugLogs)
        {
            Debug.Log($"AITurnFeedbackSystem: Turn completed - Score: {turnState.CurrentTurnScore}, " +
                     $"Iterations: {turnState.IterationCount}");
        }
    }
    
    void HandleZonkOccurred()
    {
        UpdateCurrentAction("ZONK! All progress lost!");
        UpdateStatusIndicator(Color.red, "Zonk");
        UpdateCombinationSelection(new CombinationResult(Rule.Zonk, 0, "ZONK - All progress lost!", 0f));
        
        // Play zonk sound and effect
        PlayFeedbackSound(zonkSound);
        if (zonkEffect != null)
        {
            zonkEffect.Play();
        }
        
        if (enableDebugLogs)
            Debug.Log("AITurnFeedbackSystem: Zonk occurred");
    }
    
    #endregion
    
    #region UI Update Methods
    
    void UpdateCurrentAction(string action)
    {
        if (currentActionText != null)
        {
            if (enableSmoothAnimations)
            {
                StartCoroutine(AnimateTextUpdate(currentActionText, action));
            }
            else
            {
                currentActionText.text = action;
            }
        }
    }
    
    void UpdateStatusIndicator(Color color, string status)
    {
        if (statusIndicator != null)
        {
            if (enableSmoothAnimations)
            {
                StartCoroutine(AnimateColorChange(statusIndicator, color));
            }
            else
            {
                statusIndicator.color = color;
            }
        }
        
        // Update status text if available
        TextMeshProUGUI statusText = statusIndicator?.GetComponentInChildren<TextMeshProUGUI>();
        if (statusText != null)
        {
            statusText.text = status;
        }
    }
    
    void UpdateCombinationSelection(CombinationResult combination)
    {
        if (combinationSelectionText != null && combination != null)
        {
            string displayText;
            Color textColor;
            
            if (combination.rule == Rule.Zonk)
            {
                displayText = "ZONK!";
                textColor = Color.red;
            }
            else
            {
                displayText = $"{combination.rule}\n{combination.points} points\n{combination.description}";
                textColor = Color.white;
            }
            
            if (enableSmoothAnimations)
            {
                StartCoroutine(AnimateTextUpdate(combinationSelectionText, displayText, textColor));
            }
            else
            {
                combinationSelectionText.text = displayText;
                combinationSelectionText.color = textColor;
            }
        }
    }
    
    void UpdateIterationProgress(int currentIteration, int maxIterations)
    {
        // Update slider
        if (iterationProgressSlider != null)
        {
            float progress = maxIterations > 0 ? (float)currentIteration / maxIterations : 0f;
            
            if (enableSmoothAnimations)
            {
                StartCoroutine(AnimateSliderUpdate(iterationProgressSlider, progress));
            }
            else
            {
                iterationProgressSlider.value = progress;
            }
        }
        
        // Update counter text
        if (iterationCounterText != null)
        {
            iterationCounterText.text = $"Iteration: {currentIteration}/{maxIterations}";
        }
        
        // Update iteration dots
        UpdateIterationDots(currentIteration, maxIterations);
    }
    
    void UpdateIterationDots(int currentIteration, int maxIterations)
    {
        if (iterationDots == null) return;
        
        for (int i = 0; i < iterationDots.Length; i++)
        {
            if (iterationDots[i] == null) continue;
            
            Color dotColor;
            if (i < currentIteration)
                dotColor = completedIterationColor;
            else if (i == currentIteration)
                dotColor = currentIterationColor;
            else
                dotColor = pendingIterationColor;
            
            if (enableSmoothAnimations)
            {
                StartCoroutine(AnimateColorChange(iterationDots[i], dotColor));
            }
            else
            {
                iterationDots[i].color = dotColor;
            }
        }
    }
    
    void UpdateDecisionVisualization(AIStopDecision decision)
    {
        if (decisionProcessText != null)
        {
            string processText = $"Decision Process:\n" +
                               $"Action: {(decision.ShouldStop ? "STOP" : "CONTINUE")}\n" +
                               $"Reason: {decision.DecisionReason}";
            
            if (showDetailedFeedback)
            {
                processText += $"\n\nProbability Breakdown:\n" +
                              $"Momentum: {decision.MomentumStopChance:P1}\n" +
                              $"Cap: {decision.CapStopChance:P1}\n" +
                              $"Combined: {decision.CombinedStopChance:P1}";
            }
            
            decisionProcessText.text = processText;
            decisionProcessText.color = decision.ShouldStop ? Color.red : Color.green;
        }
        
        // Update probability sliders
        UpdateProbabilitySliders(decision);
    }
    
    void UpdateProbabilitySliders(AIStopDecision decision)
    {
        if (enableSmoothAnimations)
        {
            if (momentumSlider != null)
                StartCoroutine(AnimateSliderUpdate(momentumSlider, decision.MomentumStopChance));
            
            if (capSlider != null)
                StartCoroutine(AnimateSliderUpdate(capSlider, decision.CapStopChance));
            
            if (combinedProbabilitySlider != null)
                StartCoroutine(AnimateSliderUpdate(combinedProbabilitySlider, decision.CombinedStopChance));
        }
        else
        {
            if (momentumSlider != null)
                momentumSlider.value = decision.MomentumStopChance;
            
            if (capSlider != null)
                capSlider.value = decision.CapStopChance;
            
            if (combinedProbabilitySlider != null)
                combinedProbabilitySlider.value = decision.CombinedStopChance;
        }
    }
    
    void UpdateProbabilityBreakdown(AIStopDecision decision)
    {
        if (probabilityBreakdownText != null)
        {
            string breakdownText = $"Probability Analysis:\n\n" +
                                  $"Momentum Roll: {decision.MomentumRollResult} " +
                                  $"({decision.MomentumStopChance:P1} chance)\n" +
                                  $"Cap Roll: {decision.CapRollResult} " +
                                  $"({decision.CapStopChance:P1} chance)\n\n" +
                                  $"Combined Chance: {decision.CombinedStopChance:P1}\n" +
                                  $"Final Decision: {(decision.ShouldStop ? "STOP" : "CONTINUE")}";
            
            probabilityBreakdownText.text = breakdownText;
        }
    }
    
    void UpdateDiceCount(int diceCount)
    {
        if (diceCountText != null)
        {
            diceCountText.text = $"Dice: {diceCount}";
            
            // Color code based on risk
            if (diceCount <= 1)
                diceCountText.color = Color.red;
            else if (diceCount <= 2)
                diceCountText.color = Color.yellow;
            else
                diceCountText.color = Color.white;
        }
    }
    
    void UpdateRiskLevel(string riskText, Color riskColor)
    {
        if (riskLevelText != null)
        {
            riskLevelText.text = $"Risk: {riskText}";
            riskLevelText.color = riskColor;
        }
    }
    
    void AddCombinationToHistory(CombinationResult combination)
    {
        if (combinationHistoryContainer == null) return;
        
        // Create history entry
        GameObject entry = CreateHistoryEntry(combination);
        if (entry != null)
        {
            entry.transform.SetParent(combinationHistoryContainer);
            entry.transform.SetAsFirstSibling(); // Add to top
        }
        
        // Limit history entries
        while (combinationHistoryContainer.childCount > maxHistoryEntries)
        {
            Transform lastChild = combinationHistoryContainer.GetChild(combinationHistoryContainer.childCount - 1);
            DestroyImmediate(lastChild.gameObject);
        }
    }
    
    GameObject CreateHistoryEntry(CombinationResult combination)
    {
        GameObject entry;
        
        if (combinationEntryPrefab != null)
        {
            entry = Instantiate(combinationEntryPrefab);
        }
        else
        {
            // Create simple entry
            entry = new GameObject("CombinationHistoryEntry");
            entry.AddComponent<TextMeshProUGUI>();
        }
        
        // Set entry text
        TextMeshProUGUI text = entry.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = entry.GetComponentInChildren<TextMeshProUGUI>();
        
        if (text != null)
        {
            if (combination.rule == Rule.Zonk)
            {
                text.text = "ZONK!";
                text.color = Color.red;
            }
            else
            {
                text.text = $"{combination.rule}: {combination.points} pts";
                text.color = Color.white;
            }
            
            text.fontSize = 14;
        }
        
        return entry;
    }
    
    void ShowTurnSummary(AITurnState turnState)
    {
        if (decisionProcessText != null)
        {
            string summaryText = $"TURN SUMMARY\n\n" +
                               $"Final Score: {turnState.CurrentTurnScore}\n" +
                               $"Iterations Used: {turnState.IterationCount}/{turnState.MaxIterations}\n" +
                               $"Successful Combinations: {turnState.SuccessfulCombinationsCount}\n" +
                               $"Behavior Mode: {turnState.CurrentMode}\n" +
                               $"Points Cap: {turnState.PointsPerTurnCap}";
            
            decisionProcessText.text = summaryText;
            decisionProcessText.color = Color.cyan;
        }
    }
    
    void ClearAllFeedback()
    {
        if (combinationSelectionText != null)
            combinationSelectionText.text = "";
        
        if (decisionProcessText != null)
            decisionProcessText.text = "";
        
        if (probabilityBreakdownText != null)
            probabilityBreakdownText.text = "";
        
        if (currentActionText != null)
            currentActionText.text = "";
        
        if (diceCountText != null)
            diceCountText.text = "Dice: 0";
        
        if (riskLevelText != null)
            riskLevelText.text = "Risk: None";
        
        ClearCombinationHistory();
        InitializeProbabilitySliders();
        InitializeIterationDots();
    }
    
    void ClearCombinationHistory()
    {
        if (combinationHistoryContainer != null)
        {
            foreach (Transform child in combinationHistoryContainer)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        combinationHistory.Clear();
    }
    
    #endregion
    
    #region Animation Methods
    
    IEnumerator AnimateTextUpdate(TextMeshProUGUI textComponent, string newText, Color? newColor = null)
    {
        if (textComponent == null) yield break;
        
        // Fade out
        Color originalColor = textComponent.color;
        float fadeTime = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            Color fadeColor = originalColor;
            fadeColor.a = alpha;
            textComponent.color = fadeColor;
            yield return null;
        }
        
        // Update text
        textComponent.text = newText;
        
        // Fade in with new color
        Color targetColor = newColor ?? originalColor;
        elapsed = 0f;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
            Color fadeColor = targetColor;
            fadeColor.a = alpha;
            textComponent.color = fadeColor;
            yield return null;
        }
        
        textComponent.color = targetColor;
    }
    
    IEnumerator AnimateSliderUpdate(Slider slider, float targetValue)
    {
        if (slider == null) yield break;
        
        float startValue = slider.value;
        float duration = 1f / sliderUpdateSpeed;
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
    
    IEnumerator AnimateColorChange(Image image, Color targetColor)
    {
        if (image == null) yield break;
        
        Color startColor = image.color;
        float duration = 0.3f;
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
    
    IEnumerator ClearFeedbackDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearAllFeedback();
        UpdateCurrentAction("Waiting for next turn...");
        UpdateStatusIndicator(Color.gray, "Inactive");
    }
    
    #endregion
    
    #region Audio and Effects
    
    void PlayFeedbackSound(AudioClip clip)
    {
        if (feedbackAudioSource != null && clip != null)
        {
            feedbackAudioSource.PlayOneShot(clip);
        }
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Toggle detailed feedback display
    /// </summary>
    public void SetShowDetailedFeedback(bool show)
    {
        showDetailedFeedback = show;
        if (enableDebugLogs)
            Debug.Log($"AITurnFeedbackSystem: Detailed feedback set to {show}");
    }
    
    /// <summary>
    /// Toggle smooth animations
    /// </summary>
    public void SetEnableSmoothAnimations(bool enable)
    {
        enableSmoothAnimations = enable;
        if (enableDebugLogs)
            Debug.Log($"AITurnFeedbackSystem: Smooth animations set to {enable}");
    }
    
    /// <summary>
    /// Set feedback display duration
    /// </summary>
    public void SetFeedbackDisplayDuration(float duration)
    {
        feedbackDisplayDuration = Mathf.Clamp(duration, 0.5f, 10f);
        if (enableDebugLogs)
            Debug.Log($"AITurnFeedbackSystem: Feedback duration set to {duration}s");
    }
    
    /// <summary>
    /// Get current combination history
    /// </summary>
    public List<CombinationResult> GetCombinationHistory()
    {
        return new List<CombinationResult>(combinationHistory);
    }
    
    /// <summary>
    /// Check if feedback is currently being displayed
    /// </summary>
    public bool IsShowingFeedback()
    {
        return isShowingFeedback;
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
        
        // Stop any running coroutines
        StopAllCoroutines();
    }
}