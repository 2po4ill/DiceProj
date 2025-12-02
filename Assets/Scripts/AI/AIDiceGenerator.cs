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
        
        [Header("Rigged Combination Probabilities")]
        [Tooltip("Chance to generate a 4-dice combination when 4 dice remain")]
        [Range(0f, 1f)]
        [SerializeField] private float fourDiceComboProbability = 0.5f;
        [Tooltip("Chance to generate a 3-dice combination (fallback from 4)")]
        [Range(0f, 1f)]
        [SerializeField] private float threeDiceComboProbability = 0.6f;
        [Tooltip("Chance to generate a 2-dice combination (fallback from 3)")]
        [Range(0f, 1f)]
        [SerializeField] private float twoDiceComboProbability = 0.7f;
        [Tooltip("Chance to generate a 1-dice combination (final fallback)")]
        [Range(0f, 1f)]
        [SerializeField] private float oneDiceComboProbability = 0.9f;
        
        [Header("Distribution Validation")]
        [SerializeField] private int validationSampleSize = 10000;
        [SerializeField] private float acceptableVariance = 0.05f; // 5% variance from expected 1/6
        
        // Track generation statistics for validation
        private Dictionary<int, int> generationStats = new Dictionary<int, int>();
        private int totalGenerations = 0;
        
        // Combination templates for rigged dice
        private readonly int[][] fourDiceTemplates = new int[][]
        {
            new int[] {1,2,3,4}, new int[] {2,3,4,5}, new int[] {3,4,5,6}, // Middle straights
            new int[] {1,1,2,2}, new int[] {3,3,4,4}, new int[] {5,5,6,6}, // Two pairs
            new int[] {1,1,1,1}, new int[] {2,2,2,2}, new int[] {3,3,3,3}, // Four of a kind
            new int[] {4,4,4,4}, new int[] {5,5,5,5}, new int[] {6,6,6,6}
        };
        
        private readonly int[][] threeDiceTemplates = new int[][]
        {
            new int[] {1,2,3}, new int[] {2,3,4}, new int[] {3,4,5}, new int[] {4,5,6}, // Low straights
            new int[] {1,1,1}, new int[] {2,2,2}, new int[] {3,3,3}, // Three of a kind
            new int[] {4,4,4}, new int[] {5,5,5}, new int[] {6,6,6}
        };
        
        private readonly int[][] twoDiceTemplates = new int[][]
        {
            new int[] {1,1}, new int[] {2,2}, new int[] {3,3}, // Pairs
            new int[] {4,4}, new int[] {5,5}, new int[] {6,6}
        };
        
        private readonly int[][] oneDiceTemplates = new int[][]
        {
            new int[] {1}, new int[] {5} // Single scoring dice (50/50)
        };
        
        private void Awake()
        {
            InitializeStats();
        }
        
        /// <summary>
        /// Generate random dice values with template-based rigging for AI safety
        /// </summary>
        public List<int> GenerateRandomDice(int count)
        {
            return GenerateRandomDice(count, enableRiggedDice);
        }
        
        /// <summary>
        /// Generate random dice values with optional rigging control
        /// </summary>
        public List<int> GenerateRandomDice(int count, bool useRiggedDice)
        {
            if (count <= 0 || count > 6)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"AIDiceGenerator: Invalid dice count: {count}");
                return new List<int>();
            }
            
            // Try rigged generation for 1-4 dice (only if enabled)
            if (useRiggedDice && count >= 1 && count <= 4)
            {
                var riggedDice = TryGenerateRiggedCombination(count);
                if (riggedDice != null)
                {
                    foreach (int val in riggedDice) UpdateGenerationStats(val);
                    // Always log rigged dice (not just when enableDebugLogs is true)
                    Debug.Log($"🎰 AIDiceGenerator: RIGGED {count} dice: [{string.Join(", ", riggedDice)}]");
                    return riggedDice;
                }
            }
            else if (count >= 1 && count <= 4)
            {
                // Log when rigged dice are NOT used for 1-4 dice
                Debug.Log($"🎲 AIDiceGenerator: RANDOM {count} dice (rigged disabled: useRiggedDice={useRiggedDice})");
            }
            
            // Normal random generation
            List<int> diceValues = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int value = Random.Range(1, 7);
                diceValues.Add(value);
                UpdateGenerationStats(value);
            }
            
            if (enableDebugLogs)
                Debug.Log($"AIDiceGenerator: Generated {count} dice: [{string.Join(", ", diceValues)}]");
            
            return diceValues;
        }
        
        /// <summary>
        /// Tries to generate a rigged combination using templates and fallback chain
        /// </summary>
        List<int> TryGenerateRiggedCombination(int count)
        {
            // Try primary tier (matching dice count)
            if (count == 4 && Random.Range(0f, 1f) < fourDiceComboProbability)
                return ShuffleTemplate(fourDiceTemplates);
            if (count == 3 && Random.Range(0f, 1f) < threeDiceComboProbability)
                return ShuffleTemplate(threeDiceTemplates);
            if (count == 2 && Random.Range(0f, 1f) < twoDiceComboProbability)
                return ShuffleTemplate(twoDiceTemplates);
            
            // Fallback chain
            if (count >= 3 && Random.Range(0f, 1f) < threeDiceComboProbability)
                return PadTemplate(ShuffleTemplate(threeDiceTemplates), count);
            if (count >= 2 && Random.Range(0f, 1f) < twoDiceComboProbability)
                return PadTemplate(ShuffleTemplate(twoDiceTemplates), count);
            if (Random.Range(0f, 1f) < oneDiceComboProbability)
                return PadTemplate(ShuffleTemplate(oneDiceTemplates), count);
            
            return null; // All rigging failed
        }
        
        /// <summary>
        /// Picks random template and shuffles it
        /// </summary>
        List<int> ShuffleTemplate(int[][] templates)
        {
            var template = templates[Random.Range(0, templates.Length)];
            var shuffled = new List<int>(template);
            
            // Fisher-Yates shuffle
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
            
            return shuffled;
        }
        
        /// <summary>
        /// Pads template with random dice to reach target count
        /// </summary>
        List<int> PadTemplate(List<int> template, int targetCount)
        {
            while (template.Count < targetCount)
            {
                template.Add(Random.Range(1, 7));
            }
            return template;
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