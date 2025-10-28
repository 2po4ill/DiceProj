using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DiceController : MonoBehaviour
{
    [Header("Dice Setup")]
    public GameObject dicePrefab; // Drag your dice model here
    public Transform playerDiceArea; // Where dice spawn on player side
    public int numberOfDice = 6;
    
    [Header("Player Area Position")]
    public Vector3 playerAreaOffset = new Vector3(0, 0.1f, 1.0f); // Closest to camera/player
    
    [Header("Rolling Settings")]
    public float horizontalForce = 300f;
    public float upwardForce = 200f; // Small upward force for realistic bounce
    public float rollTorque = 300f;
    public float settleTime = 3f;
    
    [Header("Force Randomization")]
    [Range(0f, 0.5f)]
    public float forceVariation = 0.2f; // ±20% variation
    [Range(0f, 0.5f)]
    public float torqueVariation = 0.3f; // ±30% variation
    

    
    [Header("Alignment Settings")]
    public float alignmentSpeed = 2f;
    public float alignmentHeight = 0.2f;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private List<GameObject> playerDice = new List<GameObject>();
    private bool isRolling = false;
    private DiceSelector diceSelector;
    public DiceFaceDetector faceDetector;
    
    // Store dice values after detection
    private Dictionary<GameObject, int> diceValues = new Dictionary<GameObject, int>();
    
    void Start()
    {
        // Create player dice area if not assigned
        if (playerDiceArea == null)
        {
            GameObject area = new GameObject("PlayerDiceArea");
            area.transform.position = playerAreaOffset; // Use configurable offset
            playerDiceArea = area.transform;
        }
        

        
        // Get or add DiceSelector component
        diceSelector = GetComponent<DiceSelector>();
        if (diceSelector == null)
            diceSelector = gameObject.AddComponent<DiceSelector>();
            
        // Get or add DiceFaceDetector component
        faceDetector = GetComponent<DiceFaceDetector>();
        if (faceDetector == null)
        {
            Debug.Log("DiceFaceDetector not found, adding component...");
            faceDetector = gameObject.AddComponent<DiceFaceDetector>();
        }
        else
        {
            Debug.Log("DiceFaceDetector component found!");
        }
        
        if (enableDebugLogs)
            Debug.Log($"FaceDetector assigned: {faceDetector != null}");
            
        StartCoroutine(SpawnPlayerDiceCoroutine());
    }
    
    IEnumerator SpawnPlayerDiceCoroutine()
    {
        isRolling = true; // Prevent rolling during spawn
        
        // Clear existing dice
        foreach (GameObject dice in playerDice)
        {
            if (dice != null) DestroyImmediate(dice);
        }
        playerDice.Clear();
        
        // Check if we have a dice prefab
        if (dicePrefab == null)
        {
            Debug.LogError("No dice prefab assigned! Drag your dice model to Dice Prefab slot.");
            isRolling = false;
            yield break;
        }
        
        // Check if we have a spawn area
        if (playerDiceArea == null)
        {
            Debug.LogError("No player dice area found!");
            isRolling = false;
            yield break;
        }
        
        // Spawn 6 dice with physics enabled (they will fall and settle)
        for (int i = 0; i < numberOfDice; i++)
        {
            // Spawn slightly above the table with random rotation (centered for 6 dice)
            Vector3 spawnPos = playerDiceArea.position + new Vector3((i - 2.5f) * 0.4f, 0.5f, 0);
            GameObject newDice = Instantiate(dicePrefab, spawnPos, Random.rotation);
            
            if (enableDebugLogs)
                Debug.Log($"Spawned dice {i} at position: {spawnPos}");
            
            // Get existing physics components (should be on prefab)
            Rigidbody rb = newDice.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Allow physics so dice fall and settle naturally
                rb.constraints = RigidbodyConstraints.None;
                rb.isKinematic = false;
            }
            
            playerDice.Add(newDice);
        }
        
        if (enableDebugLogs)
            Debug.Log($"Spawned {playerDice.Count} dice total - waiting for settle...");
        
        // Wait for dice to settle after spawning
        yield return new WaitForSeconds(settleTime);
        
        // Align dice in a neat line after settling
        yield return StartCoroutine(AlignDiceInLine());
        
        if (enableDebugLogs)
            Debug.Log("Dice spawned, settled, and aligned!");
        
        // Read dice values after spawning/alignment
        ReadDiceValues();
        
        // Enable dice selection after alignment
        if (diceSelector != null)
            diceSelector.EnableSelection();
            
        // Notify turn manager that dice are ready
        GameTurnManager turnManager = FindObjectOfType<GameTurnManager>();
        if (turnManager != null)
            turnManager.OnDiceAligned();
            
        isRolling = false; // Now ready for rolling
    }
    
    public void RollDice()
    {
        if (isRolling) return;
        
        // Disable selection during rolling
        if (diceSelector != null)
            diceSelector.DisableSelection();
        
        StartCoroutine(RollDiceCoroutine());
    }
    
    IEnumerator RollDiceCoroutine()
    {
        isRolling = true;
        
        // Get selected dice from selector
        List<GameObject> selectedDice = new List<GameObject>();
        if (diceSelector != null)
        {
            selectedDice = diceSelector.GetSelectedDice();
            // Clear selections before rolling
            diceSelector.ClearAllSelections();
        }
        
        // If no dice selected, roll all dice
        if (selectedDice.Count == 0)
        {
            selectedDice = new List<GameObject>(playerDice);
            if (enableDebugLogs)
                Debug.Log("No dice selected - rolling all dice");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"Rolling {selectedDice.Count} selected dice");
        }
        
        // Unlock only selected dice for rolling
        foreach (GameObject dice in selectedDice)
        {
            Rigidbody rb = dice.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None; // Allow free movement
                rb.isKinematic = false; // Enable physics
            }
        }
        
        // Apply random forces only to selected dice
        foreach (GameObject dice in selectedDice)
        {
            Rigidbody rb = dice.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Randomize forces for each dice individually
                float randomHorizontal = horizontalForce * Random.Range(1f - forceVariation, 1f + forceVariation);
                float randomUpward = upwardForce * Random.Range(1f - forceVariation, 1f + forceVariation);
                float randomTorqueStrength = rollTorque * Random.Range(1f - torqueVariation, 1f + torqueVariation);
                
                // Mostly horizontal force with small upward component
                Vector3 force = new Vector3(
                    Random.Range(-randomHorizontal, randomHorizontal), // Left/right movement
                    randomUpward, // Randomized upward force
                    Random.Range(-randomHorizontal/3, randomHorizontal/3) // Forward/back movement (smaller)
                );
                
                Vector3 torque = new Vector3(
                    Random.Range(-randomTorqueStrength, randomTorqueStrength),
                    Random.Range(-randomTorqueStrength, randomTorqueStrength),
                    Random.Range(-randomTorqueStrength, randomTorqueStrength)
                );
                
                rb.AddForce(force);
                rb.AddTorque(torque);
            }
        }
        
        // Wait for dice to settle
        yield return new WaitForSeconds(settleTime);
        
        // Align dice in a neat line
        yield return StartCoroutine(AlignDiceInLine());
        
        // Read dice values (you'll implement this based on your dice model)
        ReadDiceValues();
        
        // Enable dice selection after alignment
        if (diceSelector != null)
            diceSelector.EnableSelection();
        
        // Notify turn manager that dice are ready
        GameTurnManager turnManager = FindObjectOfType<GameTurnManager>();
        if (turnManager != null)
            turnManager.OnDiceAligned();
        
        isRolling = false;
    }
    
    IEnumerator AlignDiceInLine()
    {
        // Stop all dice physics
        foreach (GameObject dice in playerDice)
        {
            Rigidbody rb = dice.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // Disable physics during alignment
            }
        }
        
        // Smoothly move dice to aligned positions
        float alignmentTime = 1f;
        float elapsed = 0f;
        
        // Store starting positions and rotations
        Vector3[] startPositions = new Vector3[playerDice.Count];
        Quaternion[] startRotations = new Quaternion[playerDice.Count];
        
        for (int i = 0; i < playerDice.Count; i++)
        {
            startPositions[i] = playerDice[i].transform.position;
            startRotations[i] = playerDice[i].transform.rotation;
        }
        
        while (elapsed < alignmentTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / alignmentTime;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth animation curve
            
            for (int i = 0; i < playerDice.Count; i++)
            {
                // Target position: neat line (centered for 6 dice)
                Vector3 targetPos = playerDiceArea.position + new Vector3((i - 2.5f) * 0.4f, alignmentHeight, 0);
                
                // Keep the current rotation (preserve the rolled face)
                // Only move position, don't change rotation
                playerDice[i].transform.position = Vector3.Lerp(startPositions[i], targetPos, t);
                // Remove rotation lerp to preserve dice face
            }
            
            yield return null;
        }
        
        // Re-enable physics but keep them kinematic for now (no more rolling)
        foreach (GameObject dice in playerDice)
        {
            Rigidbody rb = dice.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeAll; // Lock in place
            }
        }
    }
    
    void ReadDiceValues()
    {
        Debug.Log("=== ReadDiceValues() called ===");
        
        if (enableDebugLogs)
            Debug.Log("Dice rolled! Reading values...");
        
        if (faceDetector != null)
        {
            if (enableDebugLogs)
                Debug.Log($"FaceDetector exists, reading {playerDice.Count} dice values...");
            
            // Store dice values for later use
            diceValues.Clear();
            foreach (GameObject dice in playerDice)
            {
                if (enableDebugLogs)
                    Debug.Log($"Calling GetDiceValue for {dice.name}...");
                    
                int value = faceDetector.GetDiceValue(dice);
                diceValues[dice] = value;
                if (enableDebugLogs)
                    Debug.Log($"Stored: {dice.name} = {value}");
            }
            
            // Display all dice values
            if (enableDebugLogs)
                faceDetector.DisplayDiceValues(playerDice);
        }
        else
        {
            Debug.LogError("FaceDetector is null! Cannot read dice values.");
        }
        
        if (enableDebugLogs)
            Debug.Log("Dice are now aligned and ready to read!");
    }
    

    
    // Call this method from UI buttons or other scripts
    public void RollDiceFromUI()
    {
        if (!isRolling)
        {
            RollDice();
        }
    }
    
    public List<GameObject> GetRemainingDice()
    {
        return new List<GameObject>(playerDice);
    }
    
    public void RemoveDice(GameObject dice)
    {
        if (dice == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("Trying to remove null dice!");
            return;
        }
        
        if (playerDice.Contains(dice))
        {
            string diceName = dice.name; // Store name before destroying
            playerDice.Remove(dice);
            
            // Remove from stored values too
            if (diceValues.ContainsKey(dice))
                diceValues.Remove(dice);
                
            DestroyImmediate(dice);
            if (enableDebugLogs)
                Debug.Log($"Removed dice: {diceName}");
        }
    }
    
    public void SpawnNewDice()
    {
        // Reset to 6 dice and spawn fresh set
        numberOfDice = 6;
        StartCoroutine(SpawnPlayerDiceCoroutine());
    }
    
    public int GetDiceValue(GameObject dice)
    {
        if (enableDebugLogs)
            Debug.Log($"GetDiceValue called for {dice.name}. Stored values count: {diceValues.Count}");
        
        // First try stored values (more reliable)
        if (diceValues.ContainsKey(dice))
        {
            if (enableDebugLogs)
                Debug.Log($"Using stored value for {dice.name}: {diceValues[dice]}");
            return diceValues[dice];
        }
        
        if (enableDebugLogs)
            Debug.Log($"No stored value found for {dice.name}. Available keys: {string.Join(", ", System.Linq.Enumerable.Select(diceValues.Keys, k => k.name))}");
        
        // Fallback to live detection
        if (faceDetector != null)
        {
            if (enableDebugLogs)
                Debug.Log($"No stored value, detecting live for {dice.name}");
            return faceDetector.GetDiceValue(dice);
        }
        
        if (enableDebugLogs)
            Debug.LogWarning($"No way to get value for {dice.name}");
        return 0;
    }
    
    public List<int> GetDiceValues(List<GameObject> dice)
    {
        List<int> values = new List<int>();
        if (enableDebugLogs)
            Debug.Log($"Getting values for {dice.Count} selected dice...");
        
        foreach (GameObject d in dice)
        {
            int value = GetDiceValue(d);
            if (enableDebugLogs)
                Debug.Log($"Dice {d.name} has value: {value}");
            if (value > 0)
                values.Add(value);
        }
        
        if (enableDebugLogs)
            Debug.Log($"Successfully read {values.Count} dice values: [{string.Join(",", values)}]");
        return values;
    }
}