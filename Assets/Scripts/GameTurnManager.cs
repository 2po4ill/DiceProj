using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HybridEnemyAI;

public class GameTurnManager : MonoBehaviour
{
    [Header("Components")]
    public DiceController diceController;
    public DiceSelector diceSelector;
    public DiceCombinationDetector combinationDetector;
    public TurnScoreManager scoreManager;
    public ScoreUIManager uiManager;
    
    [Header("AI Components")]
    public AITurnExecutor aiTurnExecutor;
    
    [Header("UI")]
    public UnityEngine.UI.Button submitCombinationButton;
    
    [Header("Game State")]
    public int currentTurn = 1;
    public int totalScore = 0;
    public int currentTurnScore = 0; // Points earned this turn
    public float currentMultiplier = 1f;
    
    [Header("Player vs AI")]
    public bool isAIOpponent = false;
    public bool isAITurn = false;
    public int playerScore = 0;
    public int aiScore = 0;
    
    [Header("UI")]
    public UnityEngine.UI.Button endTurnButton;
    public UnityEngine.UI.Button continueTurnButton;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private bool waitingForSubmission = false;
    private bool waitingForTurnChoice = false;
    
    void Start()
    {
        // Get components if not assigned
        if (diceController == null) diceController = FindObjectOfType<DiceController>();
        if (diceSelector == null) diceSelector = FindObjectOfType<DiceSelector>();
        if (combinationDetector == null) combinationDetector = FindObjectOfType<DiceCombinationDetector>();
        if (scoreManager == null) scoreManager = GetComponent<TurnScoreManager>();
        if (scoreManager == null) scoreManager = gameObject.AddComponent<TurnScoreManager>();
        if (uiManager == null) uiManager = FindObjectOfType<ScoreUIManager>();
        if (aiTurnExecutor == null) aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        
        // Setup buttons
        if (submitCombinationButton != null)
        {
            submitCombinationButton.onClick.AddListener(SubmitCombination);
            submitCombinationButton.gameObject.SetActive(false);
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(EndTurn);
            endTurnButton.gameObject.SetActive(false);
        }
        
        if (continueTurnButton != null)
        {
            continueTurnButton.onClick.AddListener(ContinueTurn);
            continueTurnButton.gameObject.SetActive(false);
        }
        
        // Setup AI event listeners if AI opponent is enabled
        if (isAIOpponent && aiTurnExecutor != null)
        {
            SetupAIEventListeners();
        }
        
        StartNewTurn();
    }
    
    void StartNewTurn()
    {
        if (enableDebugLogs)
        {
            string turnType = isAIOpponent ? (isAITurn ? "AI" : "PLAYER") : "PLAYER";
            Debug.Log($"=== TURN {currentTurn} START ({turnType}) ===");
        }
        
        // Start new turn in score manager
        if (enableDebugLogs)
            Debug.Log($"🎯 GameTurnManager calling scoreManager.StartNewTurn({currentTurn})");
        scoreManager.StartNewTurn(currentTurn);
        
        // Update UI
        if (uiManager != null)
            uiManager.OnNewTurnStarted();
        waitingForSubmission = false;
        waitingForTurnChoice = false;
        
        HideAllButtons();
        
        // Check if this is an AI turn
        if (enableDebugLogs)
            Debug.Log($"🤖 StartNewTurn check: isAIOpponent={isAIOpponent}, isAITurn={isAITurn}");
        
        if (isAIOpponent && isAITurn)
        {
            if (enableDebugLogs)
                Debug.Log($"🤖 Conditions met for AI turn - calling StartAITurn()");
            StartAITurn();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"👤 Starting player turn - rolling dice");
            // Roll all remaining dice for player turn
            diceController.RollDiceFromUI();
        }
    }
    
    public void OnDiceAligned()
    {
        // Called by DiceController when dice finish aligning
        if (enableDebugLogs)
            Debug.Log("Dice aligned! Checking for available combinations...");
        
        // Check if any combinations are possible
        if (HasValidCombinations())
        {
            waitingForSubmission = true;
            if (submitCombinationButton != null)
                submitCombinationButton.gameObject.SetActive(true);
                
            if (enableDebugLogs)
                Debug.Log("Valid combinations available! Player can select dice.");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("ZONK! No valid combinations available!");
            HandleZonk();
        }
    }
    
    public void SubmitCombination()
    {
        if (!waitingForSubmission)
        {
            Debug.Log("Cannot submit - not ready!");
            return;
        }
        
        List<GameObject> selectedDice = diceSelector.GetSelectedDice();
        
        if (selectedDice.Count == 0)
        {
            if (enableDebugLogs)
                Debug.Log("No dice selected! Please select dice to form a combination.");
            return;
        }
        
        // Get dice values from face detector
        if (enableDebugLogs)
        {
            Debug.Log($"Selected dice objects:");
            for (int i = 0; i < selectedDice.Count; i++)
            {
                Debug.Log($"  {i}: {selectedDice[i].name} (ID: {selectedDice[i].GetInstanceID()})");
            }
        }
        
        List<int> selectedValues = diceController.GetDiceValues(selectedDice);
        
        if (selectedValues.Count != selectedDice.Count)
        {
            if (enableDebugLogs)
                Debug.Log("Error reading dice values! Try again.");
            return;
        }
        
        // Check if selected dice form a valid combination
        CombinationResult result = combinationDetector.CheckForBestCombination(selectedValues);
        
        if (result == null)
        {
            if (enableDebugLogs)
                Debug.Log($"Selected dice [{string.Join(",", selectedValues)}] do not form a valid combination!");
            return;
        }
        
        // Valid combination found!
        ProcessCombination(result, selectedDice);
    }
    
    void ProcessCombination(CombinationResult result, List<GameObject> selectedDice)
    {
        // Add combination to score manager
        scoreManager.AddCombination(result);
        
        // Update UI
        if (uiManager != null)
            uiManager.OnCombinationAdded();
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== COMBINATION FOUND ===");
            Debug.Log($"Type: {result.rule}");
            Debug.Log($"Description: {result.description}");
            Debug.Log($"Points: {result.points}");
            Debug.Log($"Turn Score: {scoreManager.GetCurrentTurnScore()}");
            Debug.Log($"Projected Final: {scoreManager.GetProjectedFinalScore()} (with {scoreManager.GetCurrentTurnMultiplier():F2}x multiplier)");
        }
        
        // Remove selected dice from scene
        RemoveSelectedDice(selectedDice);
        
        // Check if any dice remain
        List<GameObject> remainingDice = diceController.GetRemainingDice();
        
        if (remainingDice.Count == 0)
        {
            if (enableDebugLogs)
                Debug.Log("All dice used! Completing turn and starting fresh.");
            
            // Complete current turn
            scoreManager.CompleteTurn();
            totalScore = scoreManager.totalGameScore;
            
            // Update UI
            if (uiManager != null)
                uiManager.OnTurnCompleted();
            
            // Handle turn switching for AI vs Player mode
            if (isAIOpponent)
            {
                SwitchTurn();
            }
            
            currentTurn++;
            diceController.SpawnNewDice();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"{remainingDice.Count} dice remaining. Player can choose to continue or end turn.");
            
            // Show turn choice buttons
            waitingForTurnChoice = true;
            ShowTurnChoiceButtons();
        }
    }
    
    bool HasValidCombinations()
    {
        List<GameObject> allDice = diceController.GetRemainingDice();
        List<int> allValues = diceController.GetDiceValues(allDice);
        
        // Use the new method that checks for potential combinations
        bool hasCombinations = combinationDetector.HasAnyCombination(allValues);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Checking dice values [{string.Join(",", allValues)}] for combinations: {hasCombinations}");
        }
        
        return hasCombinations;
    }
    
    void HandleZonk()
    {
        // Get Zonk result for display
        CombinationResult zonkResult = combinationDetector.GetZonkResult();
        
        // Add to score manager (will show in UI)
        scoreManager.AddCombination(zonkResult);
        
        // Update UI to show Zonk
        if (uiManager != null)
            uiManager.OnCombinationAdded();
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== ZONK OCCURRED ===");
            Debug.Log($"All turn progress lost!");
            Debug.Log($"Streak reset, multipliers lost!");
        }
        
        // Wait a moment for player to see the Zonk, then end turn
        StartCoroutine(ZonkDelayThenEndTurn());
    }
    
    System.Collections.IEnumerator ZonkDelayThenEndTurn()
    {
        // Give player time to see the Zonk result
        yield return new WaitForSeconds(2f);
        
        // Process Zonk in score manager
        scoreManager.ZonkTurn();
        totalScore = scoreManager.totalGameScore;
        
        // Update UI
        if (uiManager != null)
            uiManager.OnTurnCompleted();
        
        // Handle turn switching for AI vs Player mode
        if (isAIOpponent)
        {
            if (enableDebugLogs)
                Debug.Log($"🔄 About to switch turn (EndTurn). Current: isAITurn={isAITurn}");
            SwitchTurn();
            if (enableDebugLogs)
                Debug.Log($"🔄 After switch turn (EndTurn). New: isAITurn={isAITurn}");
        }
        
        // Start next turn
        currentTurn++;
        
        // Check if next turn will be AI turn
        if (isAIOpponent && isAITurn)
        {
            if (enableDebugLogs)
                Debug.Log($"🤖 Next turn is AI turn {currentTurn} - calling StartNewTurn directly");
            StartNewTurn();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"🎲 About to spawn new dice for turn {currentTurn} (EndTurn)");
            diceController.SpawnNewDice();
        }
    }
    
    public void EndTurn()
    {
        if (!waitingForTurnChoice) return;
        
        // Complete current turn
        scoreManager.CompleteTurn();
        totalScore = scoreManager.totalGameScore;
        
        // Update UI
        if (uiManager != null)
            uiManager.OnTurnCompleted();
        
        // Handle turn switching for AI vs Player mode
        if (isAIOpponent)
        {
            if (enableDebugLogs)
                Debug.Log($"🔄 About to switch turn. Current: isAITurn={isAITurn}");
            SwitchTurn();
            if (enableDebugLogs)
                Debug.Log($"🔄 After switch turn. New: isAITurn={isAITurn}");
        }
        
        currentTurn++;
        
        // Check if next turn will be AI turn
        if (isAIOpponent && isAITurn)
        {
            if (enableDebugLogs)
                Debug.Log($"🤖 Next turn is AI turn {currentTurn} - calling StartNewTurn directly");
            StartNewTurn();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"🎲 About to spawn new dice for turn {currentTurn}");
            diceController.SpawnNewDice();
        }
    }
    
    public void ContinueTurn()
    {
        if (!waitingForTurnChoice) return;
        
        if (enableDebugLogs)
            Debug.Log("Player continues turn - rerolling remaining dice...");
        
        waitingForTurnChoice = false;
        HideAllButtons();
        
        // Reroll remaining dice
        diceController.RollDiceFromUI();
    }
    
    void HideAllButtons()
    {
        if (submitCombinationButton != null)
            submitCombinationButton.gameObject.SetActive(false);
        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(false);
        if (continueTurnButton != null)
            continueTurnButton.gameObject.SetActive(false);
    }
    
    void ShowTurnChoiceButtons()
    {
        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(true);
        if (continueTurnButton != null)
            continueTurnButton.gameObject.SetActive(true);
    }
    
    void RemoveSelectedDice(List<GameObject> selectedDice)
    {
        if (enableDebugLogs)
            Debug.Log($"Removing {selectedDice.Count} selected dice from scene...");
        
        // Clear selections FIRST before destroying objects
        diceSelector.ClearAllSelections();
        
        // Then remove the dice
        foreach (GameObject dice in selectedDice)
        {
            diceController.RemoveDice(dice);
        }
    }
    
    // ===== AI INTEGRATION METHODS =====
    
    /// <summary>
    /// Sets up event listeners for AI turn executor
    /// </summary>
    void SetupAIEventListeners()
    {
        if (aiTurnExecutor == null) return;
        
        aiTurnExecutor.OnTurnStarted += OnAITurnStarted;
        aiTurnExecutor.OnCombinationSelected += OnAICombinationSelected;
        aiTurnExecutor.OnDecisionMade += OnAIDecisionMade;
        aiTurnExecutor.OnTurnCompleted += OnAITurnCompleted;
        aiTurnExecutor.OnZonkOccurred += OnAIZonkOccurred;
        
        if (enableDebugLogs)
            Debug.Log("GameTurnManager: AI event listeners setup complete");
    }
    
    /// <summary>
    /// Starts an AI turn
    /// </summary>
    void StartAITurn()
    {
        if (aiTurnExecutor == null)
        {
            Debug.LogError("GameTurnManager: AI turn requested but no AITurnExecutor found!");
            // Fallback to player turn
            isAITurn = false;
            diceController.RollDiceFromUI();
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"Starting AI turn {currentTurn}");
        
        // Disable player UI during AI turn
        HideAllButtons();
        
        // Start AI turn execution
        aiTurnExecutor.StartAITurn(currentTurn);
    }
    
    /// <summary>
    /// Switches between player and AI turns
    /// </summary>
    void SwitchTurn()
    {
        if (!isAIOpponent) return;
        
        if (enableDebugLogs)
            Debug.Log($"🔄 SwitchTurn called! Current: isAITurn={isAITurn}");
        
        // Complete current turn and update scores
        if (enableDebugLogs)
            Debug.Log($"🔄 About to get turn score from scoreManager");
        
        try
        {
            if (isAITurn)
            {
                // AI turn completed - update AI score
                int turnScore = scoreManager.GetCurrentTurnScore();
                aiScore += turnScore;
                if (enableDebugLogs)
                    Debug.Log($"AI turn completed. Turn Score: {turnScore}, Total AI Score: {aiScore}");
            }
            else
            {
                // Player turn completed - update player score
                int turnScore = scoreManager.GetCurrentTurnScore();
                playerScore += turnScore;
                if (enableDebugLogs)
                    Debug.Log($"Player turn completed. Turn Score: {turnScore}, Total Player Score: {playerScore}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error getting turn score: {e.Message}");
            Debug.LogError($"❌ Stack trace: {e.StackTrace}");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"🔄 About to flip isAITurn from {isAITurn} to {!isAITurn}");
        
        // Switch to other player
        isAITurn = !isAITurn;
        
        if (enableDebugLogs)
            Debug.Log($"🔄 isAITurn is now: {isAITurn}");
        
        // Note: Don't reset score manager here - StartNewTurn() will handle it
        if (enableDebugLogs)
            Debug.Log($"🔄 Skipping score manager reset - StartNewTurn will handle it");
        
        if (enableDebugLogs)
        {
            string nextPlayer = isAITurn ? "AI" : "Player";
            Debug.Log($"✅ Switched to {nextPlayer} turn. Scores - Player: {playerScore}, AI: {aiScore}");
        }
    }
    
    /// <summary>
    /// Gets current player's total score
    /// </summary>
    public int GetCurrentPlayerScore()
    {
        if (!isAIOpponent) return totalScore;
        return isAITurn ? aiScore : playerScore;
    }
    
    /// <summary>
    /// Gets opponent's total score
    /// </summary>
    public int GetOpponentScore()
    {
        if (!isAIOpponent) return 0;
        return isAITurn ? playerScore : aiScore;
    }
    
    // ===== AI EVENT HANDLERS =====
    
    void OnAITurnStarted(AITurnState turnState)
    {
        if (enableDebugLogs)
            Debug.Log($"AI Turn Started - Mode: {turnState.CurrentMode}, Cap: {turnState.PointsPerTurnCap}");
        
        // Update UI to show AI is thinking
        if (uiManager != null)
        {
            // This would need UI support for AI turn indication
            // uiManager.ShowAITurnIndicator(true);
        }
    }
    
    void OnAICombinationSelected(CombinationResult combination)
    {
        if (enableDebugLogs)
            Debug.Log($"AI Selected: {combination.rule} - {combination.description} (+{combination.points} points)");
        
        // Update UI to show AI's combination selection
        if (uiManager != null)
        {
            uiManager.OnCombinationAdded();
        }
    }
    
    void OnAIDecisionMade(AIStopDecision decision)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"AI Decision: {(decision.ShouldStop ? "STOP" : "CONTINUE")} - {decision.DecisionReason}");
            Debug.Log($"  Combined Probability: {decision.CombinedStopChance:P1}");
        }
    }
    
    void OnAITurnCompleted(AITurnState turnState)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"AI Turn Completed - Final Score: {turnState.CurrentTurnScore}");
            Debug.Log($"  Iterations: {turnState.IterationCount}, Combinations: {turnState.SuccessfulCombinationsCount}");
        }
        
        // Complete the turn in score manager
        scoreManager.CompleteTurn();
        totalScore = scoreManager.totalGameScore;
        
        // Update UI
        if (uiManager != null)
        {
            uiManager.OnTurnCompleted();
        }
        
        // Switch turns and start next turn
        if (isAIOpponent)
        {
            if (enableDebugLogs)
                Debug.Log($"🔄 About to switch turn (AI Complete). Current: isAITurn={isAITurn}");
            SwitchTurn();
            if (enableDebugLogs)
                Debug.Log($"🔄 After switch turn (AI Complete). New: isAITurn={isAITurn}");
        }
        
        currentTurn++;
        
        // Start next turn after a brief delay
        StartCoroutine(DelayedNextTurn());
    }
    
    void OnAIZonkOccurred()
    {
        if (enableDebugLogs)
            Debug.Log("AI Zonked! All progress lost.");
        
        // Handle Zonk in score manager
        scoreManager.ZonkTurn();
        totalScore = scoreManager.totalGameScore;
        
        // Update UI
        if (uiManager != null)
        {
            uiManager.OnTurnCompleted();
        }
    }
    
    /// <summary>
    /// Adds a small delay before starting the next turn for better UX
    /// </summary>
    System.Collections.IEnumerator DelayedNextTurn()
    {
        yield return new WaitForSeconds(1.5f);
        
        // Check if next turn will be AI turn
        if (isAIOpponent && isAITurn)
        {
            if (enableDebugLogs)
                Debug.Log($"🤖 Delayed start - AI turn {currentTurn}");
            StartNewTurn();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"🎲 Delayed start - spawning dice for turn {currentTurn}");
            diceController.SpawnNewDice();
        }
    }
    
    /// <summary>
    /// Enables or disables AI opponent mode
    /// </summary>
    public void SetAIOpponentMode(bool enabled)
    {
        isAIOpponent = enabled;
        
        if (enabled)
        {
            // Initialize AI vs Player game
            isAITurn = false; // Player goes first
            playerScore = 0;
            aiScore = 0;
            
            if (aiTurnExecutor != null)
            {
                SetupAIEventListeners();
            }
            
            if (enableDebugLogs)
                Debug.Log("AI Opponent mode enabled. Player goes first.");
        }
        else
        {
            // Clean up AI event listeners
            if (aiTurnExecutor != null)
            {
                aiTurnExecutor.OnTurnStarted -= OnAITurnStarted;
                aiTurnExecutor.OnCombinationSelected -= OnAICombinationSelected;
                aiTurnExecutor.OnDecisionMade -= OnAIDecisionMade;
                aiTurnExecutor.OnTurnCompleted -= OnAITurnCompleted;
                aiTurnExecutor.OnZonkOccurred -= OnAIZonkOccurred;
            }
            
            if (enableDebugLogs)
                Debug.Log("AI Opponent mode disabled.");
        }
    }
    
    /// <summary>
    /// Forces AI turn to end (for debugging or emergency stops)
    /// </summary>
    public void ForceEndAITurn()
    {
        if (isAITurn && aiTurnExecutor != null)
        {
            aiTurnExecutor.ForceEndTurn();
        }
    }
}