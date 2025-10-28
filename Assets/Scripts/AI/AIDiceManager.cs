using UnityEngine;
using System.Collections.Generic;

namespace HybridEnemyAI
{
    /// <summary>
    /// Manager component that integrates AI dice generation with display system
    /// Provides unified interface for AI dice operations
    /// Requirements: 8.1, 8.2, 8.3, 8.4, 8.5
    /// </summary>
    public class AIDiceManager : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private AIDiceGenerator diceGenerator;
        [SerializeField] private AIDiceDisplaySystem displaySystem;
        
        [Header("AI Dice Settings")]
        [SerializeField] private int maxDiceCount = 6;
        [SerializeField] private bool autoDisplayGenerated = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // Current AI dice state
        private List<int> currentAIDice = new List<int>();
        
        private void Awake()
        {
            SetupComponents();
        }
        
        /// <summary>
        /// Generate and display new AI dice
        /// </summary>
        /// <param name="count">Number of dice to generate</param>
        /// <returns>Generated dice values</returns>
        public List<int> GenerateAndDisplayDice(int count)
        {
            if (count <= 0 || count > maxDiceCount)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"AIDiceManager: Invalid dice count: {count}. Must be 1-{maxDiceCount}");
                return new List<int>();
            }
            
            // Generate dice values
            List<int> newDice = diceGenerator.GenerateRandomDice(count);
            currentAIDice = new List<int>(newDice);
            
            // Display if auto-display is enabled
            if (autoDisplayGenerated && displaySystem != null)
            {
                displaySystem.DisplayAIDice(currentAIDice);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceManager: Generated and displayed {count} dice: [{string.Join(", ", newDice)}]");
            }
            
            return new List<int>(newDice);
        }
        
        /// <summary>
        /// Generate dice without displaying (for internal calculations)
        /// </summary>
        /// <param name="count">Number of dice to generate</param>
        /// <returns>Generated dice values</returns>
        public List<int> GenerateDiceOnly(int count)
        {
            if (count <= 0 || count > maxDiceCount)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"AIDiceManager: Invalid dice count: {count}. Must be 1-{maxDiceCount}");
                return new List<int>();
            }
            
            List<int> newDice = diceGenerator.GenerateRandomDice(count);
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceManager: Generated {count} dice (no display): [{string.Join(", ", newDice)}]");
            }
            
            return newDice;
        }
        
        /// <summary>
        /// Update display with specific dice values
        /// </summary>
        /// <param name="diceValues">Dice values to display</param>
        public void DisplayDice(List<int> diceValues)
        {
            if (displaySystem == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("AIDiceManager: No display system available");
                return;
            }
            
            currentAIDice = new List<int>(diceValues);
            displaySystem.DisplayAIDice(currentAIDice);
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceManager: Displayed {diceValues.Count} dice: [{string.Join(", ", diceValues)}]");
            }
        }
        
        /// <summary>
        /// Remove specific dice from current set (after combination selection)
        /// </summary>
        /// <param name="diceIndices">Indices of dice to remove</param>
        /// <returns>Remaining dice values</returns>
        public List<int> RemoveDice(List<int> diceIndices)
        {
            if (diceIndices == null || diceIndices.Count == 0)
                return new List<int>(currentAIDice);
            
            // Highlight dice before removal (for visual feedback)
            if (displaySystem != null)
            {
                displaySystem.HighlightDice(diceIndices, Color.red);
            }
            
            // Sort indices in descending order to avoid shifting issues
            diceIndices.Sort((a, b) => b.CompareTo(a));
            
            // Remove from current dice list
            foreach (int index in diceIndices)
            {
                if (index >= 0 && index < currentAIDice.Count)
                {
                    currentAIDice.RemoveAt(index);
                }
            }
            
            // Update display
            if (displaySystem != null)
            {
                displaySystem.RemoveHighlightedDice(diceIndices);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceManager: Removed dice at indices [{string.Join(", ", diceIndices)}]. Remaining: [{string.Join(", ", currentAIDice)}]");
            }
            
            return new List<int>(currentAIDice);
        }
        
        /// <summary>
        /// Clear all AI dice
        /// </summary>
        public void ClearAllDice()
        {
            currentAIDice.Clear();
            
            if (displaySystem != null)
            {
                displaySystem.ClearAIDiceDisplay();
            }
            
            if (enableDebugLogs)
                Debug.Log("AIDiceManager: Cleared all AI dice");
        }
        
        /// <summary>
        /// Get current AI dice values
        /// </summary>
        /// <returns>Current dice values</returns>
        public List<int> GetCurrentDice()
        {
            return new List<int>(currentAIDice);
        }
        
        /// <summary>
        /// Get number of current AI dice
        /// </summary>
        /// <returns>Current dice count</returns>
        public int GetCurrentDiceCount()
        {
            return currentAIDice.Count;
        }
        
        /// <summary>
        /// Generate specific dice values (for testing)
        /// </summary>
        /// <param name="targetValues">Specific values to generate</param>
        /// <returns>The target values</returns>
        public List<int> GenerateTargetDice(List<int> targetValues)
        {
            List<int> newDice = diceGenerator.GenerateTargetDice(targetValues);
            currentAIDice = new List<int>(newDice);
            
            if (autoDisplayGenerated && displaySystem != null)
            {
                displaySystem.DisplayAIDice(currentAIDice);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceManager: Generated target dice: [{string.Join(", ", newDice)}]");
            }
            
            return new List<int>(newDice);
        }
        
        /// <summary>
        /// Validate dice generation distribution
        /// </summary>
        /// <returns>True if distribution is valid</returns>
        public bool ValidateDistribution()
        {
            if (diceGenerator == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("AIDiceManager: No dice generator available for validation");
                return false;
            }
            
            return diceGenerator.ValidateDistribution();
        }
        
        /// <summary>
        /// Get generation statistics from dice generator
        /// </summary>
        /// <returns>Dictionary with face counts</returns>
        public Dictionary<int, int> GetGenerationStats()
        {
            if (diceGenerator == null)
                return new Dictionary<int, int>();
            
            return diceGenerator.GetGenerationStats();
        }
        
        /// <summary>
        /// Reset generation statistics
        /// </summary>
        public void ResetGenerationStats()
        {
            if (diceGenerator != null)
            {
                diceGenerator.ResetStats();
                if (enableDebugLogs)
                    Debug.Log("AIDiceManager: Reset generation statistics");
            }
        }
        
        /// <summary>
        /// Run distribution test on dice generator
        /// </summary>
        public void RunDistributionTest()
        {
            if (diceGenerator != null)
            {
                diceGenerator.RunDistributionTest();
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning("AIDiceManager: No dice generator available for testing");
            }
        }
        
        private void SetupComponents()
        {
            // Get or create dice generator
            if (diceGenerator == null)
            {
                diceGenerator = GetComponent<AIDiceGenerator>();
                if (diceGenerator == null)
                {
                    diceGenerator = gameObject.AddComponent<AIDiceGenerator>();
                    if (enableDebugLogs)
                        Debug.Log("AIDiceManager: Created AIDiceGenerator component");
                }
            }
            
            // Get or create display system
            if (displaySystem == null)
            {
                displaySystem = GetComponent<AIDiceDisplaySystem>();
                if (displaySystem == null)
                {
                    displaySystem = gameObject.AddComponent<AIDiceDisplaySystem>();
                    if (enableDebugLogs)
                        Debug.Log("AIDiceManager: Created AIDiceDisplaySystem component");
                }
            }
        }
        
        /// <summary>
        /// Set component references externally
        /// </summary>
        /// <param name="generator">Dice generator component</param>
        /// <param name="display">Display system component</param>
        public void SetComponents(AIDiceGenerator generator, AIDiceDisplaySystem display)
        {
            diceGenerator = generator;
            displaySystem = display;
            
            if (enableDebugLogs)
                Debug.Log("AIDiceManager: Components set externally");
        }
        
        /// <summary>
        /// Enable or disable auto-display of generated dice
        /// </summary>
        /// <param name="autoDisplay">True to auto-display, false otherwise</param>
        public void SetAutoDisplay(bool autoDisplay)
        {
            autoDisplayGenerated = autoDisplay;
            if (enableDebugLogs)
                Debug.Log($"AIDiceManager: Auto-display set to {autoDisplay}");
        }
        
        /// <summary>
        /// Set maximum dice count
        /// </summary>
        /// <param name="maxCount">Maximum number of dice (1-6)</param>
        public void SetMaxDiceCount(int maxCount)
        {
            maxDiceCount = Mathf.Clamp(maxCount, 1, 6);
            if (enableDebugLogs)
                Debug.Log($"AIDiceManager: Max dice count set to {maxDiceCount}");
        }
    }
}