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
    
    [Header("AI Area Position")]
    public Transform aiDiceArea; // Where AI dice spawn
    public Vector3 aiAreaOffset = new Vector3(0, 0.1f, -1.0f); // Opposite side of table
    
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
    private List<GameObject> aiDice = new List<GameObject>();
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
        
        // Create AI dice area if not assigned
        if (aiDiceArea == null)
        {
            GameObject aiArea = new GameObject("AIDiceArea");
            aiArea.transform.position = aiAreaOffset; // Opposite side of table
            aiDiceArea = aiArea.transform;
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
    
    // ===== AI DICE METHODS =====
    
    /// <summary>
    /// Spawns AI dice with specific values (no physics, instant placement)
    /// </summary>
    public void SpawnAIDice(List<int> diceValues)
    {
        StartCoroutine(SpawnAIDiceCoroutine(diceValues));
    }
    
    IEnumerator SpawnAIDiceCoroutine(List<int> diceValues)
    {
        if (enableDebugLogs)
            Debug.Log($"Spawning {diceValues.Count} AI dice with values: [{string.Join(",", diceValues)}]");
        
        // Clear existing AI dice
        ClearAIDice();
        
        // Check if we have a dice prefab
        if (dicePrefab == null)
        {
            Debug.LogError("No dice prefab assigned for AI dice!");
            yield break;
        }
        
        // Spawn dice with specific values
        for (int i = 0; i < diceValues.Count; i++)
        {
            // Position AI dice in a line on their side of the table
            Vector3 spawnPos = aiDiceArea.position + new Vector3((i - (diceValues.Count - 1) * 0.5f) * 0.4f, alignmentHeight, 0);
            GameObject newDice = Instantiate(dicePrefab, spawnPos, Quaternion.identity);
            
            // Disable physics for AI dice (they don't roll)
            Rigidbody rb = newDice.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
            
            // Set the dice to show the correct face
            SetDiceFace(newDice, diceValues[i]);
            
            // Add visual indicator that this is an AI dice
            AddAIDiceVisualIndicator(newDice);
            
            aiDice.Add(newDice);
            
            if (enableDebugLogs)
                Debug.Log($"Spawned AI dice {i} with value {diceValues[i]} at position: {spawnPos}");
        }
        
        if (enableDebugLogs)
            Debug.Log($"AI dice spawning complete. Total: {aiDice.Count}");
    }
    
    /// <summary>
    /// Sets a dice to show a specific face value
    /// </summary>
    void SetDiceFace(GameObject dice, int value)
    {
        // Rotate the dice to show the correct face
        // This assumes standard dice face orientations
        Quaternion targetRotation = GetRotationForDiceValue(value);
        dice.transform.rotation = targetRotation;
        
        // Store the dice value for later retrieval
        if (diceValues.ContainsKey(dice))
            diceValues[dice] = value;
        else
            diceValues.Add(dice, value);
        
        // Also tag the dice with its value for easy identification
        dice.name = $"AI_Dice_{value}";
        
        if (enableDebugLogs)
            Debug.Log($"Set dice {dice.name} to show face {value} with rotation {targetRotation.eulerAngles}");
    }
    
    /// <summary>
    /// Gets the rotation needed to show a specific dice face
    /// </summary>
    Quaternion GetRotationForDiceValue(int value)
    {
        // Standard dice face rotations (adjust based on your dice model)
        switch (value)
        {
            case 1: return Quaternion.Euler(0, 0, 0);      // Face 1 up
            case 2: return Quaternion.Euler(90, 0, 0);     // Face 2 up  
            case 3: return Quaternion.Euler(0, 0, 90);     // Face 3 up
            case 4: return Quaternion.Euler(0, 0, -90);    // Face 4 up
            case 5: return Quaternion.Euler(-90, 0, 0);    // Face 5 up
            case 6: return Quaternion.Euler(180, 0, 0);    // Face 6 up
            default: return Quaternion.identity;
        }
    }
    
    /// <summary>
    /// Adds visual indicator to show this is an AI dice
    /// </summary>
    void AddAIDiceVisualIndicator(GameObject dice)
    {
        // Add a visual indicator to show this is an AI dice
        Renderer renderer = dice.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a new material instance to avoid affecting other dice
            Material material = new Material(renderer.material);
            
            // Make AI dice red-tinted to distinguish from player dice
            Color originalColor = material.color;
            material.color = new Color(1.0f, originalColor.g * 0.7f, originalColor.b * 0.7f, originalColor.a);
            
            // Apply the new material
            renderer.material = material;
            
            if (enableDebugLogs)
                Debug.Log($"Applied AI visual indicator to {dice.name}");
        }
        
        // Add a tag to identify AI dice
        dice.tag = "AIDice";
    }
    
    /// <summary>
    /// Removes selected AI dice (when AI uses them in combinations)
    /// </summary>
    public void RemoveAIDice(List<int> indices)
    {
        if (enableDebugLogs)
            Debug.Log($"Removing AI dice at indices: [{string.Join(",", indices)}] (AI used these in combination)");
        
        // Sort indices in descending order to avoid index shifting issues
        indices.Sort((a, b) => b.CompareTo(a));
        
        foreach (int index in indices)
        {
            if (index >= 0 && index < aiDice.Count)
            {
                GameObject dice = aiDice[index];
                
                // Get the dice value before removing
                int diceValue = diceValues.ContainsKey(dice) ? diceValues[dice] : 0;
                
                // Remove from tracking
                if (diceValues.ContainsKey(dice))
                    diceValues.Remove(dice);
                
                aiDice.RemoveAt(index);
                DestroyImmediate(dice);
                
                if (enableDebugLogs)
                    Debug.Log($"✅ Removed AI dice at index {index} (value: {diceValue}) - AI used this in combination");
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"AI dice removal complete. Remaining AI dice: {aiDice.Count}");
    }
    

    
    /// <summary>
    /// Clears all AI dice from the scene
    /// </summary>
    public void ClearAIDice()
    {
        foreach (GameObject dice in aiDice)
        {
            if (dice != null)
            {
                // Remove from dice values dictionary
                if (diceValues.ContainsKey(dice))
                    diceValues.Remove(dice);
                
                DestroyImmediate(dice);
            }
        }
        aiDice.Clear();
        
        if (enableDebugLogs)
            Debug.Log("Cleared all AI dice");
    }
    
    /// <summary>
    /// Gets the current AI dice values
    /// </summary>
    public List<int> GetAIDiceValues()
    {
        List<int> values = new List<int>();
        
        for (int i = 0; i < aiDice.Count; i++)
        {
            // Since AI dice are set to specific faces, we can determine value from rotation
            // Or store the values when we create them
            int value = GetDiceValueFromRotation(aiDice[i]);
            values.Add(value);
        }
        
        return values;
    }
    
    /// <summary>
    /// Determines dice value from its rotation
    /// </summary>
    int GetDiceValueFromRotation(GameObject dice)
    {
        // This is a simplified approach - you might need to adjust based on your dice model
        Vector3 euler = dice.transform.rotation.eulerAngles;
        
        // Normalize angles to 0-360 range
        float x = ((euler.x % 360) + 360) % 360;
        float z = ((euler.z % 360) + 360) % 360;
        
        // Match rotations to values (adjust based on your dice model)
        if (Mathf.Approximately(x, 0) && Mathf.Approximately(z, 0)) return 1;
        if (Mathf.Approximately(x, 90) && Mathf.Approximately(z, 0)) return 2;
        if (Mathf.Approximately(x, 0) && Mathf.Approximately(z, 90)) return 3;
        if (Mathf.Approximately(x, 0) && Mathf.Approximately(z, 270)) return 4;
        if (Mathf.Approximately(x, 270) && Mathf.Approximately(z, 0)) return 5;
        if (Mathf.Approximately(x, 180) && Mathf.Approximately(z, 0)) return 6;
        
        // Fallback
        return 1;
    }
}