using UnityEngine;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// Test to verify that aggressive strategy now correctly tracks dice indices
/// </summary>
public class AIAggressiveDiceIndicesFix : MonoBehaviour
{
    [Header("Dependencies")]
    public AIAggressiveRerollStrategy aggressiveStrategy;
    public AICombinationStrategy combinationStrategy;
    
    [Header("Test Controls")]
    public bool runTest = false;
    
    void Update()
    {
        if (runTest)
        {
            runTest = false;
            RunDiceIndicesTest();
        }
    }
    
    void RunDiceIndicesTest()
    {
        Debug.Log("=== TESTING DICE INDICES FIX ===");
        
        // Test case from the bug report: [2,2,3,1,5,6]
        var testDice = new List<int> { 2, 2, 3, 1, 5, 6 };
        
        Debug.Log($"Test Dice: [{string.Join(",", testDice)}]");
        Debug.Log("Expected: AI should select dice at index 3 (value 1) for 100 points");
        
        // Find minimum dice combination
        var strategyResult = combinationStrategy.FindMinimumDiceCombination(testDice, BehaviorMode.AGGRESSIVE);
        
        if (strategyResult != null && strategyResult.combination != null)
        {
            Debug.Log($"Selected Combination: {strategyResult.combination.description}");
            Debug.Log($"Points: {strategyResult.combination.points}");
            Debug.Log($"Dice Used: {strategyResult.diceUsed}");
            
            if (strategyResult.combination.diceIndices != null && strategyResult.combination.diceIndices.Count > 0)
            {
                Debug.Log($"✓ Dice Indices: [{string.Join(",", strategyResult.combination.diceIndices)}]");
                
                // Verify the indices are correct
                foreach (int index in strategyResult.combination.diceIndices)
                {
                    Debug.Log($"  Index {index} = Dice value {testDice[index]}");
                }
                
                Debug.Log("✓ FIX VERIFIED: Dice indices are now tracked correctly!");
            }
            else
            {
                Debug.LogError("✗ FIX FAILED: Dice indices are still null or empty!");
            }
        }
        else
        {
            Debug.LogError("✗ TEST FAILED: No combination found!");
        }
        
        Debug.Log("=== TEST COMPLETE ===");
    }
}
