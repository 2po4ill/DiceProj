using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DiceSelector : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask diceLayer = -1; // What layers to check for dice
    public GameObject arrowPrefab; // Drag an arrow model here
    
    [Header("Arrow Settings")]
    public Vector3 arrowOffset = new Vector3(0, 0.5f, 0); // Position above dice
    public Vector3 arrowRotation = new Vector3(180, 0, 0); // Point down at dice
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private List<GameObject> selectedDice = new List<GameObject>();
    private Dictionary<GameObject, GameObject> arrowObjects = new Dictionary<GameObject, GameObject>();
    private Camera playerCamera;
    private bool canSelect = false;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
            

    }
    

    
    void Update()
    {
        // Check for mouse click using Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleDiceClick();
        }
    }
    
    void HandleDiceClick()
    {
        // Only allow selection after alignment phase
        if (!canSelect)
        {
            if (enableDebugLogs)
                Debug.Log("Cannot select dice yet - wait for alignment to complete");
            return;
        }
        
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, diceLayer))
        {
            GameObject clickedObject = hit.collider.gameObject;
            
            // Check if it's a dice (you might want to add a tag or component check)
            if (IsDice(clickedObject))
            {
                ToggleDiceSelection(clickedObject);
            }
        }
    }
    
    bool IsDice(GameObject obj)
    {
        // Check if object is a dice - exclude arrows
        if (obj.name.Contains("_Arrow"))
            return false;
        
        // Check if it's actually a dice
        return obj.name.Contains("Dice") || obj.GetComponent<Rigidbody>() != null;
    }
    
    void ToggleDiceSelection(GameObject dice)
    {
        if (selectedDice.Contains(dice))
        {
            // Deselect dice
            DeselectDice(dice);
        }
        else
        {
            // Select dice
            SelectDice(dice);
        }
        
        if (enableDebugLogs)
            Debug.Log($"Selected dice count: {selectedDice.Count}");
    }
    
    void SelectDice(GameObject dice)
    {
        if (selectedDice.Contains(dice)) return;
        
        selectedDice.Add(dice);
        
        // Create arrow above dice
        CreateArrowForDice(dice);
        
        if (enableDebugLogs)
            Debug.Log($"Selected dice: {dice.name}");
    }
    
    void DeselectDice(GameObject dice)
    {
        if (!selectedDice.Contains(dice)) return;
        
        selectedDice.Remove(dice);
        
        // Remove arrow
        RemoveArrowForDice(dice);
        
        if (enableDebugLogs)
            Debug.Log($"Deselected dice: {dice.name}");
    }
    
    public void ClearAllSelections()
    {
        // Deselect all dice safely
        GameObject[] dicesToDeselect = selectedDice.ToArray();
        foreach (GameObject dice in dicesToDeselect)
        {
            if (dice != null) // Check if dice still exists
            {
                DeselectDice(dice);
            }
        }
        
        // Force clear the list in case some dice were already destroyed
        selectedDice.Clear();
        
        // Clean up any orphaned arrows
        var keysToRemove = new List<GameObject>();
        foreach (var kvp in arrowObjects)
        {
            if (kvp.Key == null || kvp.Value == null)
            {
                if (kvp.Value != null) DestroyImmediate(kvp.Value);
                keysToRemove.Add(kvp.Key);
            }
        }
        
        // Remove the orphaned entries
        foreach (var key in keysToRemove)
        {
            arrowObjects.Remove(key);
        }
    }
    
    public List<GameObject> GetSelectedDice()
    {
        return new List<GameObject>(selectedDice);
    }
    
    public int GetSelectedCount()
    {
        return selectedDice.Count;
    }
    
    public bool IsDiceSelected(GameObject dice)
    {
        return selectedDice.Contains(dice);
    }
    
    public void EnableSelection()
    {
        canSelect = true;
        if (enableDebugLogs)
            Debug.Log("Dice selection enabled");
    }
    
    public void DisableSelection()
    {
        canSelect = false;
        if (enableDebugLogs)
            Debug.Log("Dice selection disabled");
    }
    
    void CreateArrowForDice(GameObject dice)
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning("No arrow prefab assigned! Create a simple arrow or use a primitive.");
            return;
        }
        
        // Create arrow above dice
        GameObject arrow = Instantiate(arrowPrefab);
        arrow.name = dice.name + "_Arrow";
        arrow.transform.position = dice.transform.position + arrowOffset;
        arrow.transform.rotation = Quaternion.Euler(arrowRotation);
        arrow.transform.parent = dice.transform.parent;
        
        // Make arrow non-clickable by removing colliders
        Collider[] arrowColliders = arrow.GetComponentsInChildren<Collider>();
        foreach (Collider col in arrowColliders)
        {
            Destroy(col);
        }
        
        // Store reference
        arrowObjects[dice] = arrow;
        
        // Make arrow follow the dice
        StartCoroutine(FollowDiceWithArrow(dice, arrow));
    }
    
    void RemoveArrowForDice(GameObject dice)
    {
        if (arrowObjects.ContainsKey(dice))
        {
            GameObject arrow = arrowObjects[dice];
            if (arrow != null)
            {
                DestroyImmediate(arrow);
            }
            arrowObjects.Remove(dice);
        }
    }
    
    System.Collections.IEnumerator FollowDiceWithArrow(GameObject dice, GameObject arrow)
    {
        while (dice != null && arrow != null && selectedDice.Contains(dice))
        {
            // Make arrow follow dice position
            arrow.transform.position = dice.transform.position + arrowOffset;
            
            yield return null;
        }
    }
    

}