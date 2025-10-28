using UnityEngine;
using System.Collections.Generic;

public class DiceFaceDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public float raycastHeight = 2f; // How high above dice to start raycast
    public LayerMask faceLayer = -1; // Layer for face detection (optional)
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    public int GetDiceValue(GameObject dice)
    {
        Debug.Log($"=== GetDiceValue called for {dice.name} ===");
        
        // Cast ray from above the dice downward
        Vector3 rayStart = dice.transform.position + Vector3.up * raycastHeight;
        Vector3 rayDirection = Vector3.down;
        
        if (enableDebugLogs)
            Debug.Log($"Casting ray from {rayStart} downward for dice {dice.name}");
        
        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, raycastHeight + 1f, faceLayer))
        {
            // Check if we hit a face child object
            GameObject hitObject = hit.collider.gameObject;
            if (enableDebugLogs)
                Debug.Log($"Raycast hit object: {hitObject.name}");
            
            // Try to get face value from object name
            int faceValue = GetFaceValueFromName(hitObject.name);
            
            if (faceValue > 0)
            {
                if (enableDebugLogs)
                    Debug.Log($"Dice {dice.name} shows value: {faceValue}");
                return faceValue;
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"Hit object {hitObject.name} but couldn't determine face value");
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"Raycast missed dice {dice.name} - no face detected. Check:");
                Debug.LogWarning($"- Dice has Face1, Face2, etc. child objects?");
                Debug.LogWarning($"- Face children have trigger colliders?");
                Debug.LogWarning($"- Raycast height ({raycastHeight}) is sufficient?");
            }
        }
        
        return 0; // No value detected
    }
    
    int GetFaceValueFromName(string objectName)
    {
        // Parse face value from object name (Face1, Face2, etc.)
        if (objectName.StartsWith("Face"))
        {
            string numberPart = objectName.Substring(4); // Remove "Face" prefix
            if (int.TryParse(numberPart, out int value))
            {
                if (value >= 1 && value <= 6)
                {
                    return value;
                }
            }
        }
        
        return 0; // Invalid face name
    }
    
    public List<int> GetAllDiceValues(List<GameObject> diceList)
    {
        List<int> values = new List<int>();
        
        foreach (GameObject dice in diceList)
        {
            int value = GetDiceValue(dice);
            values.Add(value);
        }
        
        return values;
    }
    
    public void DisplayDiceValues(List<GameObject> diceList)
    {
        List<int> values = GetAllDiceValues(diceList);
        string result = "Dice Values: ";
        
        for (int i = 0; i < values.Count; i++)
        {
            result += $"Dice{i + 1}={values[i]} ";
        }
        
        if (enableDebugLogs)
            Debug.Log(result);
    }
}