using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreUIManager : MonoBehaviour
{
    [Header("Score Display")]
    public TextMeshProUGUI currentTurnScoreText;
    public TextMeshProUGUI projectedFinalScoreText;
    public TextMeshProUGUI totalGameScoreText;
    public TextMeshProUGUI turnNumberText;
    public TextMeshProUGUI turnMultiplierText;
    
    [Header("AI vs Player Mode")]
    public GameObject aiVsPlayerPanel;
    public TextMeshProUGUI playerTotalScoreText;
    public TextMeshProUGUI aiTotalScoreText;
    public TextMeshProUGUI currentPlayerIndicatorText;
    public GameObject aiThinkingPanel;
    
    [Header("Streak Display")]
    public TextMeshProUGUI consecutiveStreakText;
    public TextMeshProUGUI nextTurnBonusText;
    
    [Header("Combination History")]
    public Transform combinationHistoryParent;
    public GameObject combinationEntryPrefab; // Simple text prefab for combination entries
    
    [Header("Animation")]
    public float scoreUpdateSpeed = 2f;
    public bool animateScoreChanges = true;
    
    private TurnScoreManager scoreManager;
    private GameTurnManager turnManager;
    private GameManager gameManager;
    private int displayedTurnScore = 0;
    private int displayedTotalScore = 0;
    private int displayedPlayerScore = 0;
    private int displayedAIScore = 0;
    
    void Start()
    {
        // Find required managers
        scoreManager = FindObjectOfType<TurnScoreManager>();
        turnManager = FindObjectOfType<GameTurnManager>();
        gameManager = FindObjectOfType<GameManager>();
        
        if (scoreManager == null)
        {
            Debug.LogError("ScoreUIManager: No TurnScoreManager found!");
            return;
        }
        
        // Initialize display
        UpdateAllUI();
    }
    
    void Update()
    {
        if (scoreManager == null) return;
        
        // Update UI based on game mode
        bool isAIMode = gameManager != null && gameManager.IsAIOpponentMode();
        
        // Show/hide appropriate UI panels
        if (aiVsPlayerPanel != null)
            aiVsPlayerPanel.SetActive(isAIMode);
        
        // Animate score changes
        if (animateScoreChanges)
        {
            AnimateScoreUpdates();
        }
        else
        {
            UpdateScoreDisplays();
        }
        
        // Update AI vs Player specific UI
        if (isAIMode)
        {
            UpdateAIVsPlayerUI();
        }
    }
    
    void AnimateScoreUpdates()
    {
        // Animate turn score
        int targetTurnScore = scoreManager.GetCurrentTurnScore();
        if (displayedTurnScore != targetTurnScore)
        {
            displayedTurnScore = Mathf.RoundToInt(Mathf.Lerp(displayedTurnScore, targetTurnScore, Time.deltaTime * scoreUpdateSpeed));
            if (currentTurnScoreText != null)
                currentTurnScoreText.text = displayedTurnScore.ToString();
        }
        
        // Animate total score
        int targetTotalScore = scoreManager.totalGameScore;
        if (displayedTotalScore != targetTotalScore)
        {
            displayedTotalScore = Mathf.RoundToInt(Mathf.Lerp(displayedTotalScore, targetTotalScore, Time.deltaTime * scoreUpdateSpeed));
            if (totalGameScoreText != null)
                totalGameScoreText.text = displayedTotalScore.ToString();
        }
        
        // Update other UI elements (no animation needed)
        UpdateStaticUI();
    }
    
    void UpdateScoreDisplays()
    {
        // Direct updates without animation
        displayedTurnScore = scoreManager.GetCurrentTurnScore();
        displayedTotalScore = scoreManager.totalGameScore;
        
        if (currentTurnScoreText != null)
            currentTurnScoreText.text = displayedTurnScore.ToString();
        
        if (totalGameScoreText != null)
            totalGameScoreText.text = displayedTotalScore.ToString();
        
        UpdateStaticUI();
    }
    
    void UpdateStaticUI()
    {
        // Projected final score
        if (projectedFinalScoreText != null)
        {
            int projected = scoreManager.GetProjectedFinalScore();
            projectedFinalScoreText.text = projected.ToString();
            
            // Color coding: green if higher than current, white if same
            if (projected > displayedTurnScore)
                projectedFinalScoreText.color = Color.green;
            else
                projectedFinalScoreText.color = Color.white;
        }
        
        // Turn number
        if (turnNumberText != null)
        {
            GameTurnManager turnManager = FindObjectOfType<GameTurnManager>();
            if (turnManager != null)
                turnNumberText.text = $"Turn {turnManager.currentTurn}";
        }
        
        // Turn multiplier
        if (turnMultiplierText != null)
        {
            float multiplier = scoreManager.GetCurrentTurnMultiplier();
            turnMultiplierText.text = $"{multiplier:F2}x";
            
            // Color coding: yellow if bonus active
            if (multiplier > 1f)
                turnMultiplierText.color = Color.yellow;
            else
                turnMultiplierText.color = Color.white;
        }
        
        // Consecutive streak
        if (consecutiveStreakText != null)
        {
            consecutiveStreakText.text = $"Streak: {scoreManager.consecutiveSuccessfulTurns}";
        }
        
        // Next turn bonus preview
        if (nextTurnBonusText != null)
        {
            float nextBonus = scoreManager.baseTurnMultiplier + ((scoreManager.consecutiveSuccessfulTurns + 1) * scoreManager.consecutiveTurnBonus);
            nextTurnBonusText.text = $"Next: {nextBonus:F2}x";
        }
    }
    
    public void UpdateAllUI()
    {
        if (scoreManager == null) return;
        
        UpdateScoreDisplays();
        UpdateCombinationHistory();
    }
    
    public void UpdateCombinationHistory()
    {
        if (combinationHistoryParent == null || scoreManager.currentTurn == null) return;
        
        // Clear existing entries
        foreach (Transform child in combinationHistoryParent)
        {
            Destroy(child.gameObject);
        }
        
        // Add current turn combinations
        foreach (var combination in scoreManager.currentTurn.combinations)
        {
            CreateCombinationEntry(combination);
        }
    }
    
    void CreateCombinationEntry(CombinationResult combination)
    {
        GameObject entry;
        
        if (combinationEntryPrefab != null)
        {
            entry = Instantiate(combinationEntryPrefab, combinationHistoryParent);
        }
        else
        {
            // Create simple text entry if no prefab assigned
            entry = new GameObject("CombinationEntry");
            entry.transform.SetParent(combinationHistoryParent);
            entry.AddComponent<TextMeshProUGUI>();
        }
        
        // Set the text
        TextMeshProUGUI text = entry.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            if (combination.rule == Rule.Zonk)
            {
                text.text = "ZONK! - All progress lost!";
                text.fontSize = 16;
                text.color = Color.red;
                text.fontStyle = FontStyles.Bold;
            }
            else
            {
                text.text = $"{combination.rule}: {combination.points} pts";
                text.fontSize = 14;
                text.color = Color.white;
            }
        }
    }
    
    // Public methods for external updates
    public void OnCombinationAdded()
    {
        UpdateCombinationHistory();
    }
    
    public void OnTurnCompleted()
    {
        // Force immediate update when turn completes
        if (animateScoreChanges)
        {
            displayedTurnScore = 0; // Reset for next turn
            displayedTotalScore = scoreManager.totalGameScore;
        }
        UpdateAllUI();
    }
    
    public void OnNewTurnStarted()
    {
        displayedTurnScore = 0;
        UpdateAllUI();
    }
    
    // Utility methods
    public void ToggleScoreAnimation()
    {
        animateScoreChanges = !animateScoreChanges;
    }
    
    public void SetScoreAnimationSpeed(float speed)
    {
        scoreUpdateSpeed = Mathf.Clamp(speed, 0.1f, 10f);
    }
    
    // ===== AI VS PLAYER UI METHODS =====
    
    void UpdateAIVsPlayerUI()
    {
        if (turnManager == null) return;
        
        // Animate player and AI scores
        if (animateScoreChanges)
        {
            // Animate player score
            if (displayedPlayerScore != turnManager.playerScore)
            {
                displayedPlayerScore = Mathf.RoundToInt(Mathf.Lerp(displayedPlayerScore, turnManager.playerScore, Time.deltaTime * scoreUpdateSpeed));
                if (playerTotalScoreText != null)
                    playerTotalScoreText.text = $"Player: {displayedPlayerScore}";
            }
            
            // Animate AI score
            if (displayedAIScore != turnManager.aiScore)
            {
                displayedAIScore = Mathf.RoundToInt(Mathf.Lerp(displayedAIScore, turnManager.aiScore, Time.deltaTime * scoreUpdateSpeed));
                if (aiTotalScoreText != null)
                    aiTotalScoreText.text = $"AI: {displayedAIScore}";
            }
        }
        else
        {
            // Direct updates
            displayedPlayerScore = turnManager.playerScore;
            displayedAIScore = turnManager.aiScore;
            
            if (playerTotalScoreText != null)
                playerTotalScoreText.text = $"Player: {displayedPlayerScore}";
            
            if (aiTotalScoreText != null)
                aiTotalScoreText.text = $"AI: {displayedAIScore}";
        }
        
        // Update current player indicator
        if (currentPlayerIndicatorText != null)
        {
            string currentPlayer = turnManager.isAITurn ? "AI's Turn" : "Your Turn";
            currentPlayerIndicatorText.text = currentPlayer;
            
            // Color coding
            currentPlayerIndicatorText.color = turnManager.isAITurn ? Color.red : Color.green;
        }
        
        // Show/hide AI thinking indicator
        if (aiThinkingPanel != null)
        {
            bool aiIsThinking = turnManager.isAITurn && turnManager.aiTurnExecutor != null && 
                               turnManager.aiTurnExecutor.IsTurnActive();
            aiThinkingPanel.SetActive(aiIsThinking);
        }
    }
    
    /// <summary>
    /// Called when switching between player and AI turns
    /// </summary>
    public void OnTurnSwitched()
    {
        if (gameManager != null && gameManager.IsAIOpponentMode())
        {
            UpdateAIVsPlayerUI();
        }
    }
    
    /// <summary>
    /// Shows winner announcement for AI vs Player games
    /// </summary>
    public void ShowGameWinner(bool playerWon)
    {
        // This could be expanded to show a winner panel
        string winner = playerWon ? "Player Wins!" : "AI Wins!";
        Debug.Log($"Game Over: {winner}");
        
        // You could add UI elements here to show the winner
    }
}