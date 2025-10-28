using UnityEngine;
using System.Collections.Generic;

namespace HybridEnemyAI
{
    /// <summary>
    /// Simple test component for AI dice system validation
    /// </summary>
    public class AIDiceSystemTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private int testDiceCount = 6;
        
        private AIDiceManager diceManager;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                RunBasicTest();
            }
        }
        
        [ContextMenu("Run Basic Test")]
        public void RunBasicTest()
        {
            Debug.Log("=== AI Dice System Basic Test ===");
            
            // Get or create dice manager
            diceManager = GetComponent<AIDiceManager>();
            if (diceManager == null)
            {
                diceManager = gameObject.AddComponent<AIDiceManager>();
            }
            
            // Test 1: Generate and display dice
            Debug.Log("Test 1: Generate and display dice");
            List<int> dice = diceManager.GenerateAndDisplayDice(testDiceCount);
            Debug.Log($"Generated dice: [{string.Join(", ", dice)}]");
            
            // Test 2: Generate specific target dice
            Debug.Log("Test 2: Generate target dice");
            List<int> targetDice = new List<int> { 1, 2, 3, 4, 5, 6 };
            List<int> generatedTarget = diceManager.GenerateTargetDice(targetDice);
            Debug.Log($"Target dice: [{string.Join(", ", generatedTarget)}]");
            
            // Test 3: Remove some dice
            Debug.Log("Test 3: Remove dice");
            List<int> indicesToRemove = new List<int> { 0, 2 }; // Remove first and third dice
            List<int> remaining = diceManager.RemoveDice(indicesToRemove);
            Debug.Log($"Remaining after removal: [{string.Join(", ", remaining)}]");
            
            // Test 4: Clear all dice
            Debug.Log("Test 4: Clear all dice");
            diceManager.ClearAllDice();
            Debug.Log($"Dice count after clear: {diceManager.GetCurrentDiceCount()}");
            
            Debug.Log("=== AI Dice System Test Complete ===");
        }
        
        [ContextMenu("Run Distribution Test")]
        public void RunDistributionTest()
        {
            if (diceManager == null)
            {
                diceManager = GetComponent<AIDiceManager>();
                if (diceManager == null)
                {
                    diceManager = gameObject.AddComponent<AIDiceManager>();
                }
            }
            
            Debug.Log("Running distribution test...");
            diceManager.RunDistributionTest();
        }
        
        [ContextMenu("Show Generation Stats")]
        public void ShowGenerationStats()
        {
            if (diceManager == null)
            {
                diceManager = GetComponent<AIDiceManager>();
                if (diceManager == null)
                {
                    Debug.LogWarning("No dice manager found");
                    return;
                }
            }
            
            Dictionary<int, int> stats = diceManager.GetGenerationStats();
            Debug.Log("=== Generation Statistics ===");
            
            foreach (var kvp in stats)
            {
                Debug.Log($"Face {kvp.Key}: {kvp.Value} times");
            }
        }
    }
}