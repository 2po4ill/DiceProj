using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HybridEnemyAI
{
    /// <summary>
    /// Non-physics dice generation system for AI turns
    /// Generates dice values mathematically with proper random distribution
    /// Requirements: 8.1, 8.2, 8.3
    /// </summary>
    public class AIDiceGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool validateDistribution = true;
        
        [Header("Rigged Probability (AI Safety Net)")]
        [Tooltip("Enable rigged dice when AI has 1-4 dice remaining")]
        [SerializeField] private bool enableRiggedDice = true;
        [Tooltip("Probability that ONE dice will be 1 or 5 when rigging is active (0.9 = 90%)")]
        [Range(0f, 1f)]
        [SerializeField] private float riggedSafetyProbability = 0.9f;
        
        [Header("Distribution Validation")]
        [SerializeField] private int validationSampleSize = 10000;
        [SerializeField] private float acceptableVariance = 0.05f; // 5% variance from expected 1/6
        
        // Track generation statistics for validation
        private Dictionary<int, int> generationStats = new Dictionary<int, int>();
        private int totalGenerations = 0;
        
        private void Awake()
        {
            InitializeStats();
        }
        
        /// <summary>
        /// Generate random dice values instantly without physics
        /// Applies rigged probability when count is 1-4 (AI safety net)
        /// </summary>
        /// <param name="count">Number of dice to generate</param>
        /// <returns>List of dice values (1-6)</returns>
        public List<int> GenerateRandomDice(int count)
        {
            if (count <= 0)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("AIDiceGenerator: Invalid dice count requested: " + count);
                return new List<int>();
            }
            
            List<int> diceValues = new List<int>();
            
            // RIGGED PROBABILITY: When 1-4 dice, guarantee at least one 1 or 5 (90% chance)
            bool shouldRig = enableRiggedDice && count >= 1 && count <= 4;
            bool rigApplied = false;
            
            if (shouldRig && Random.Range(0f, 1f) < riggedSafetyProbability)
            {
                // Pick a random position to place the safety dice
                int safetyPosition = Random.Range(0, count);
                
                for (int i = 0; i < count; i++)
                {
                    int value;
                    
                    if (i == safetyPosition && !rigApplied)
                    {
                        // Place a 1 or 5 at this position (50/50 chance)
                        value = Random.Range(0, 2) == 0 ? 1 : 5;
                        rigApplied = true;
                        
                        if (enableDebugLogs)
                            Debug.Log($"AIDiceGenerator: RIGGED - Placed safety dice ({value}) at position {i}");
                    }
                    else
                    {
                        // Normal random dice
                        value = Random.Range(1, 7);
                    }
                    
                    diceValues.Add(value);
                    UpdateGenerationStats(value);
                }
            }
            else
            {
                // Normal generation (no rigging or rigging failed)
                for (int i = 0; i < count; i++)
                {
                    int value = Random.Range(1, 7);
                    diceValues.Add(value);
                    UpdateGenerationStats(value);
                }
            }
            
            if (enableDebugLogs)
            {
                string rigStatus = shouldRig ? (rigApplied ? " [RIGGED]" : " [RIG FAILED]") : "";
                Debug.Log($"AIDiceGenerator: Generated {count} dice: [{string.Join(", ", diceValues)}]{rigStatus}");
            }
            
            return diceValues;
        }
        
        /// <summary>
        /// Generate a single random dice value
        /// </summary>
        /// <returns>Dice value (1-6)</returns>
        public int GenerateSingleDice()
        {
            int value = Random.Range(1, 7);
            UpdateGenerationStats(value);
            
            if (enableDebugLogs)
                Debug.Log($"AIDiceGenerator: Generated single dice: {value}");
                
            return value;
        }
        
        /// <summary>
        /// Generate dice with weighted probabilities (for testing purposes)
        /// </summary>
        /// <param name="count">Number of dice to generate</param>
        /// <param name="weights">Array of 6 weights for faces 1-6</param>
        /// <returns>List of dice values</returns>
        public List<int> GenerateWeightedDice(int count, float[] weights = null)
        {
            if (weights == null)
            {
                // Default to equal weights (normal dice)
                return GenerateRandomDice(count);
            }
            
            if (weights.Length != 6)
            {
                Debug.LogError("AIDiceGenerator: Weights array must have exactly 6 elements for faces 1-6");
                return GenerateRandomDice(count);
            }
            
            List<int> diceValues = new List<int>();
            
            // Normalize weights
            float totalWeight = weights.Sum();
            if (totalWeight <= 0)
            {
                Debug.LogError("AIDiceGenerator: Total weight must be positive");
                return GenerateRandomDice(count);
            }
            
            for (int i = 0; i < count; i++)
            {
                float randomValue = Random.Range(0f, totalWeight);
                float cumulativeWeight = 0f;
                int selectedFace = 1;
                
                for (int face = 0; face < 6; face++)
                {
                    cumulativeWeight += weights[face];
                    if (randomValue <= cumulativeWeight)
                    {
                        selectedFace = face + 1; // Convert 0-5 to 1-6
                        break;
                    }
                }
                
                diceValues.Add(selectedFace);
                UpdateGenerationStats(selectedFace);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceGenerator: Generated {count} weighted dice: [{string.Join(", ", diceValues)}]");
            }
            
            return diceValues;
        }
        
        /// <summary>
        /// Validate that dice generation follows proper random distribution
        /// </summary>
        /// <returns>True if distribution is within acceptable variance</returns>
        public bool ValidateDistribution()
        {
            if (totalGenerations < validationSampleSize)
            {
                if (enableDebugLogs)
                    Debug.Log($"AIDiceGenerator: Need {validationSampleSize - totalGenerations} more samples for validation");
                return true; // Not enough data yet
            }
            
            float expectedProbability = 1f / 6f; // Each face should appear ~16.67% of the time
            bool isValid = true;
            
            if (enableDebugLogs)
                Debug.Log("=== AIDiceGenerator Distribution Validation ===");
            
            for (int face = 1; face <= 6; face++)
            {
                int count = generationStats.ContainsKey(face) ? generationStats[face] : 0;
                float actualProbability = (float)count / totalGenerations;
                float variance = Mathf.Abs(actualProbability - expectedProbability);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Face {face}: {count}/{totalGenerations} = {actualProbability:P2} " +
                             $"(expected {expectedProbability:P2}, variance {variance:P2})");
                }
                
                if (variance > acceptableVariance)
                {
                    isValid = false;
                    Debug.LogWarning($"AIDiceGenerator: Face {face} variance {variance:P2} exceeds acceptable {acceptableVariance:P2}");
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceGenerator: Distribution validation " + (isValid ? "PASSED" : "FAILED"));
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Reset generation statistics
        /// </summary>
        public void ResetStats()
        {
            InitializeStats();
            if (enableDebugLogs)
                Debug.Log("AIDiceGenerator: Statistics reset");
        }
        
        /// <summary>
        /// Get current generation statistics
        /// </summary>
        /// <returns>Dictionary with face counts</returns>
        public Dictionary<int, int> GetGenerationStats()
        {
            return new Dictionary<int, int>(generationStats);
        }
        
        /// <summary>
        /// Get total number of dice generated
        /// </summary>
        /// <returns>Total generation count</returns>
        public int GetTotalGenerations()
        {
            return totalGenerations;
        }
        
        private void InitializeStats()
        {
            generationStats.Clear();
            for (int i = 1; i <= 6; i++)
            {
                generationStats[i] = 0;
            }
            totalGenerations = 0;
        }
        
        private void UpdateGenerationStats(int value)
        {
            if (value >= 1 && value <= 6)
            {
                generationStats[value]++;
                totalGenerations++;
                
                // Validate distribution periodically
                if (validateDistribution && totalGenerations % 1000 == 0)
                {
                    ValidateDistribution();
                }
            }
        }
        
        /// <summary>
        /// Generate dice values that match a specific target (for testing)
        /// </summary>
        /// <param name="targetValues">Specific values to generate</param>
        /// <returns>The exact target values</returns>
        public List<int> GenerateTargetDice(List<int> targetValues)
        {
            // Validate target values
            foreach (int value in targetValues)
            {
                if (value < 1 || value > 6)
                {
                    Debug.LogError($"AIDiceGenerator: Invalid target dice value: {value}");
                    return GenerateRandomDice(targetValues.Count);
                }
            }
            
            // Update stats for target values
            foreach (int value in targetValues)
            {
                UpdateGenerationStats(value);
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceGenerator: Generated target dice: [{string.Join(", ", targetValues)}]");
            }
            
            return new List<int>(targetValues);
        }
        
        /// <summary>
        /// Test the generator with a large sample to verify distribution
        /// </summary>
        [ContextMenu("Run Distribution Test")]
        public void RunDistributionTest()
        {
            Debug.Log("AIDiceGenerator: Starting distribution test...");
            
            ResetStats();
            
            // Generate large sample
            for (int i = 0; i < validationSampleSize; i++)
            {
                GenerateSingleDice();
            }
            
            // Validate results
            bool isValid = ValidateDistribution();
            Debug.Log($"AIDiceGenerator: Distribution test completed. Result: {(isValid ? "PASSED" : "FAILED")}");
        }
    }
}