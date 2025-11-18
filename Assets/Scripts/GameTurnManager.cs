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
    
    [Header("Game Rules")]
    [Tooltip("Score needed to win the game (first player to reach this score wins)")]
    public int victoryScore = 5000;
    
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
        
        // Connect DiceController to AI components
        if (aiTurnExecutor != null && diceController != null)
        {
            aiTurnExecutor.diceController = diceController;
        }
        
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
    
    public void StartNewTurn()
    {
        // Don't start new turns if game is over
        if (IsGameOver())
        {
            if (enableDebugLogs)
                Debug.Log($"Game is over ({GetWinner()} won). Not starting new turn.");
            return;
        }
        
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
                Debug.Log($"👤 Starting player turn - spawning fresh dice");
            // Always start player turn with fresh 6 dice
            diceController.SpawnNewDice();
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
            
            // SELECTION PHASE: Only show submit button
            // Player must select and submit a combination first
            if (submitCombinationButton != null)
                submitCombinationButton.gameObject.SetActive(true);
            
            // Do NOT show end turn button here - only after submission
            if (endTurnButton != null)
                endTurnButton.gameObject.SetActive(false);
            if (continueTurnButton != null)
                continueTurnButton.gameObject.SetActive(false);
                
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
        
        // Disable selection during submission processing
        if (diceSelector != null)
            diceSelector.DisableSelection();
        
        List<GameObject> selectedDice = diceSelector.GetSelectedDice();
        
        if (selectedDice.Count == 0)
        {
            if (enableDebugLogs)
                Debug.Log("No dice selected! Please select dice to form a combination.");
            
            // Re-enable selection so player can try again
            if (diceSelector != null)
                diceSelector.EnableSelection();
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
            
            // Clear selections and re-enable so player can try again
            if (diceSelector != null)
            {
                diceSelector.ClearAllSelections();
                diceSelector.EnableSelection();
            }
            return;
        }
        
        // Check if selected dice form a valid combination
        CombinationResult result = combinationDetector.CheckForBestCombination(selectedValues);
        
        if (result == null)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ Selected dice [{string.Join(",", selectedValues)}] do not form a valid combination!");
            
            // Clear invalid selections and re-enable so player can try again
            if (diceSelector != null)
            {
                diceSelector.ClearAllSelections();
                diceSelector.EnableSelection();
            }
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
                Debug.Log("🔥 HOT STREAK! All dice used - spawning fresh set!");
            
            // All dice used - spawn new set and continue turn automatically
            // No choice given - hot streak always continues
            diceController.SpawnNewDice();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"{remainingDice.Count} dice remaining. Player can choose to continue or end turn.");
            
            // DECISION PHASE: Show only end/continue buttons
            // Disable selection and clear any existing selections
            waitingForTurnChoice = true;
            waitingForSubmission = false;
            
            if (diceSelector != null)
            {
                diceSelector.DisableSelection();
                diceSelector.ClearAllSelections();
            }
            
            if (submitCombinationButton != null)
                submitCombinationButton.gameObject.SetActive(false);
            
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
                Debug.Log($"🔄 About to switch turn (Zonk). Current: isAITurn={isAITurn}");
            SwitchTurn();
            if (enableDebugLogs)
                Debug.Log($"🔄 After switch turn (Zonk). New: isAITurn={isAITurn}");
            
            // Only increment turn when both players have played
            if (isAITurn) // We just switched to AI, so this completes the round
            {
                currentTurn++;
                if (enableDebugLogs)
                    Debug.Log($"🔄 Round complete, incrementing to turn {currentTurn}");
            }
        }
        else
        {
            // Single player mode - increment turn
            currentTurn++;
        }
        
        StartNewTurn();
    }
    
    public void EndTurn()
    {
        // Allow ending turn at any time during player's turn (not just when waitingForTurnChoice)
        if (isAIOpponent && isAITurn)
        {
            if (enableDebugLogs)
                Debug.Log("Cannot end turn - it's AI's turn!");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log("Player manually ending turn");
        
        // Disable selection and clear selections when ending turn
        if (diceSelector != null)
        {
            diceSelector.DisableSelection();
            diceSelector.ClearAllSelections();
        }
        
        // Complete current turn
        scoreManager.CompleteTurn();
        totalScore = scoreManager.totalGameScore;
        
        // Update UI
        if (uiManager != null)
            uiManager.OnTurnCompleted();
        
        // Reset UI state
        waitingForSubmission = false;
        waitingForTurnChoice = false;
        HideAllButtons();
        
        // Clear any remaining dice when ending turn
        if (enableDebugLogs)
            Debug.Log("Clearing remaining dice as turn ends");
        // The dice will be cleared automatically when the next turn starts with SpawnNewDice()
        
        // Handle turn switching for AI vs Player mode
        if (isAIOpponent)
        {
            if (enableDebugLogs)
                Debug.Log($"🔄 About to switch turn. Current: isAITurn={isAITurn}");
            SwitchTurn();
            if (enableDebugLogs)
                Debug.Log($"🔄 After switch turn. New: isAITurn={isAITurn}");
            
            // Only increment turn number when both players have played (after AI turn)
            if (!isAITurn) // We just switched to AI, so increment turn after AI plays
            {
                // Don't increment here, let AI completion handle it
            }
        }
        else
        {
            // Single player mode - increment turn
            currentTurn++;
        }
        
        // Start next turn (AI or new player turn)
        StartNewTurn();
    }
    
    public void ContinueTurn()
    {
        if (!waitingForTurnChoice) return;
        
        if (enableDebugLogs)
            Debug.Log("Player continues turn - rerolling remaining dice...");
        
        waitingForTurnChoice = false;
        HideAllButtons();
        
        // Disable selection during reroll
        if (diceSelector != null)
        {
            diceSelector.DisableSelection();
            diceSelector.ClearAllSelections();
        }
        
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
        // DECISION PHASE: Show only end turn and continue buttons
        // Submit button should already be hidden
        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(true);
        if (continueTurnButton != null)
            continueTurnButton.gameObject.SetActive(true);
        if (submitCombinationButton != null)
            submitCombinationButton.gameObject.SetActive(false);
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
        
        // Clear any existing AI dice from previous turn
        if (diceController != null)
        {
            diceController.ClearAIDice();
        }
        
        // Start AI turn execution
        aiTurnExecutor.StartAITurn(currentTurn);
    }
    
    /// <summary>
    /// Switches between player and AI turns
    /// FIXED: Now gets the final calculated score with multipliers
    /// </summary>
    void SwitchTurn()
    {
        if (!isAIOpponent) return;
        
        if (enableDebugLogs)
            Debug.Log($"🔄 SwitchTurn called! Current: isAITurn={isAITurn}");
        
        // Get the FINAL calculated score (with multipliers) from the last completed turn
        if (enableDebugLogs)
            Debug.Log($"🔄 About to get FINAL turn score from scoreManager");
        
        try
        {
            int finalTurnScore = 0;
            
            // Get the final score from the most recently completed turn
            if (scoreManager.turnHistory.Count > 0)
            {
                var lastTurn = scoreManager.turnHistory[scoreManager.turnHistory.Count - 1];
                finalTurnScore = lastTurn.finalScore;
                
                if (enableDebugLogs)
                    Debug.Log($"🔄 Got final score from turn history: {finalTurnScore} (base: {lastTurn.baseScore}, multiplier: {lastTurn.turnMultiplier:F2}x)");
            }
            else
            {
                // Fallback: calculate final score if no history available
                finalTurnScore = scoreManager.GetProjectedFinalScore();
                
                if (enableDebugLogs)
                    Debug.Log($"🔄 No turn history, using projected final score: {finalTurnScore}");
            }
            
            if (isAITurn)
            {
                // AI turn completed - update AI score with FINAL score
                aiScore += finalTurnScore;
                if (enableDebugLogs)
                    Debug.Log($"✅ AI turn completed. Final Turn Score: {finalTurnScore}, Total AI Score: {aiScore}");
            }
            else
            {
                // Player turn completed - update player score with FINAL score
                playerScore += finalTurnScore;
                if (enableDebugLogs)
                    Debug.Log($"✅ Player turn completed. Final Turn Score: {finalTurnScore}, Total Player Score: {playerScore}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error getting final turn score: {e.Message}");
            Debug.LogError($"❌ Stack trace: {e.StackTrace}");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"🔄 About to flip isAITurn from {isAITurn} to {!isAITurn}");
        
        // Switch to other player
        isAITurn = !isAITurn;
        
        if (enableDebugLogs)
            Debug.Log($"🔄 isAITurn is now: {isAITurn}");
        
        if (enableDebugLogs)
        {
            string nextPlayer = isAITurn ? "AI" : "Player";
            Debug.Log($"✅ Switched to {nextPlayer} turn. Final Scores - Player: {playerScore}, AI: {aiScore}");
        }
        
        // Check for victory condition (5000 points)
        CheckForVictory();
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
    
    /// <summary>
    /// Checks if the game is over (someone reached victory score)
    /// </summary>
    public bool IsGameOver()
    {
        if (!isAIOpponent) return false;
        return playerScore >= victoryScore || aiScore >= victoryScore;
    }
    
    /// <summary>
    /// Gets the winner (null if game not over)
    /// </summary>
    public string GetWinner()
    {
        if (!IsGameOver()) return null;
        
        if (playerScore >= victoryScore) return "Player";
        if (aiScore >= victoryScore) return "AI";
        return null;
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
        
        // Clear AI dice after a delay to let player see final result
        if (diceController != null)
        {
            StartCoroutine(DelayedClearAIDice());
        }
        
        // Switch turns and start next turn
        if (isAIOpponent)
        {
            if (enableDebugLogs)
                Debug.Log($"🔄 About to switch turn (AI Complete). Current: isAITurn={isAITurn}");
            SwitchTurn();
            if (enableDebugLogs)
                Debug.Log($"🔄 After switch turn (AI Complete). New: isAITurn={isAITurn}");
            
            // Only increment turn when switching back to player (completing the round)
            if (!isAITurn) // We just switched to player, so this completes the round
            {
                currentTurn++;
                if (enableDebugLogs)
                    Debug.Log($"🔄 Round complete, incrementing to turn {currentTurn}");
            }
        }
        else
        {
            // Single player mode - increment turn
            currentTurn++;
        }
        
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
        if (enableDebugLogs)
            Debug.Log($"⏰ DelayedNextTurn started. isAITurn={isAITurn}, currentTurn={currentTurn}");
        
        yield return new WaitForSeconds(1.5f);
        
        // Always start the new turn first
        StartNewTurn();
    }
    
    /// <summary>
    /// Clears AI dice after a delay to let player see the final result
    /// </summary>
    System.Collections.IEnumerator DelayedClearAIDice()
    {
        if (enableDebugLogs)
            Debug.Log("Keeping AI dice visible for final review...");
        
        // Wait to let player see the final AI dice state
        yield return new WaitForSeconds(2.0f);
        
        if (diceController != null)
        {
            diceController.ClearAIDice();
            if (enableDebugLogs)
                Debug.Log("AI dice cleared");
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
    
    /// <summary>
    /// Checks if either player has reached victory score and ends the game
    /// </summary>
    void CheckForVictory()
    {
        if (!isAIOpponent) return; // Only check in AI vs Player mode
        
        if (playerScore >= victoryScore)
        {
            if (enableDebugLogs)
                Debug.Log($"🏆 VICTORY: Player wins with {playerScore} points!");
            
            // Stop any active AI turn
            if (isAITurn && aiTurnExecutor != null)
            {
                aiTurnExecutor.ForceEndTurn();
            }
            
            // Disable further gameplay
            waitingForSubmission = false;
            waitingForTurnChoice = false;
            HideAllButtons();
            
            // Notify UI manager if available
            if (uiManager != null)
            {
                // Could add a victory notification method to ScoreUIManager
                uiManager.UpdateAllUI();
            }
        }
        else if (aiScore >= victoryScore)
        {
            if (enableDebugLogs)
                Debug.Log($"🏆 VICTORY: AI wins with {aiScore} points!");
            
            // Disable further gameplay
            waitingForSubmission = false;
            waitingForTurnChoice = false;
            HideAllButtons();
            
            // Notify UI manager if available
            if (uiManager != null)
            {
                // Could add a victory notification method to ScoreUIManager
                uiManager.UpdateAllUI();
            }
        }
    }
}