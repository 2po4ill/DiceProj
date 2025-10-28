using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

namespace HybridEnemyAI
{
    /// <summary>
    /// AI-specific dice display system for visualizing AI dice without physics
    /// Requirements: 8.5, 10.4
    /// </summary>
    public class AIDiceDisplaySystem : MonoBehaviour
    {
        [Header("AI Dice Display Area")]
        [SerializeField] private Transform aiDiceContainer;
        [SerializeField] private GameObject aiDicePrefab; // UI prefab for displaying dice values
        
        [Header("Display Settings")]
        [SerializeField] private float diceSpacing = 60f;
        [SerializeField] private float updateAnimationSpeed = 2f;
        [SerializeField] private bool enableInstantUpdates = true;
        
        [Header("Visual Differentiation")]
        [SerializeField] private Color aiDiceColor = Color.cyan;
        [SerializeField] private Color playerDiceColor = Color.white;
        [SerializeField] private string aiDicePrefix = "AI: ";
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // Current dice display objects
        private List<GameObject> currentDiceDisplays = new List<GameObject>();
        private List<int> currentDiceValues = new List<int>();
        
        // Animation coroutines
        private Coroutine updateCoroutine;
        
        private void Awake()
        {
            SetupAIDiceContainer();
        }
        
        /// <summary>
        /// Display AI dice values instantly
        /// </summary>
        /// <param name="diceValues">List of dice values to display</param>
        public void DisplayAIDice(List<int> diceValues)
        {
            if (diceValues == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("AIDiceDisplaySystem: Null dice values provided");
                return;
            }
            
            currentDiceValues = new List<int>(diceValues);
            
            if (enableInstantUpdates)
            {
                UpdateDiceDisplayImmediate();
            }
            else
            {
                if (updateCoroutine != null)
                    StopCoroutine(updateCoroutine);
                updateCoroutine = StartCoroutine(UpdateDiceDisplayAnimated());
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceDisplaySystem: Displaying {diceValues.Count} AI dice: [{string.Join(", ", diceValues)}]");
            }
        }
        
        /// <summary>
        /// Clear all AI dice displays
        /// </summary>
        public void ClearAIDiceDisplay()
        {
            foreach (GameObject display in currentDiceDisplays)
            {
                if (display != null)
                    DestroyImmediate(display);
            }
            
            currentDiceDisplays.Clear();
            currentDiceValues.Clear();
            
            if (enableDebugLogs)
                Debug.Log("AIDiceDisplaySystem: Cleared AI dice display");
        }
        
        /// <summary>
        /// Update dice display to show remaining dice after combination selection
        /// </summary>
        /// <param name="remainingDice">Dice values still available for rolling</param>
        public void UpdateRemainingDice(List<int> remainingDice)
        {
            DisplayAIDice(remainingDice);
        }
        
        /// <summary>
        /// Highlight specific dice (for showing selected combinations)
        /// </summary>
        /// <param name="diceIndices">Indices of dice to highlight</param>
        /// <param name="highlightColor">Color to use for highlighting</param>
        public void HighlightDice(List<int> diceIndices, Color highlightColor)
        {
            for (int i = 0; i < currentDiceDisplays.Count; i++)
            {
                GameObject display = currentDiceDisplays[i];
                if (display == null) continue;
                
                Image diceImage = display.GetComponent<Image>();
                if (diceImage != null)
                {
                    if (diceIndices.Contains(i))
                    {
                        diceImage.color = highlightColor;
                    }
                    else
                    {
                        diceImage.color = aiDiceColor;
                    }
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceDisplaySystem: Highlighted dice at indices: [{string.Join(", ", diceIndices)}]");
            }
        }
        
        /// <summary>
        /// Remove highlighted dice from display (after combination selection)
        /// </summary>
        /// <param name="diceIndices">Indices of dice to remove</param>
        public void RemoveHighlightedDice(List<int> diceIndices)
        {
            // Sort indices in descending order to avoid index shifting issues
            diceIndices.Sort((a, b) => b.CompareTo(a));
            
            foreach (int index in diceIndices)
            {
                if (index >= 0 && index < currentDiceDisplays.Count)
                {
                    GameObject display = currentDiceDisplays[index];
                    if (display != null)
                        DestroyImmediate(display);
                    
                    currentDiceDisplays.RemoveAt(index);
                    
                    if (index < currentDiceValues.Count)
                        currentDiceValues.RemoveAt(index);
                }
            }
            
            // Reposition remaining dice
            RepositionDiceDisplays();
            
            if (enableDebugLogs)
            {
                Debug.Log($"AIDiceDisplaySystem: Removed dice at indices: [{string.Join(", ", diceIndices)}]");
            }
        }
        
        /// <summary>
        /// Get current displayed dice values
        /// </summary>
        /// <returns>List of currently displayed dice values</returns>
        public List<int> GetCurrentDiceValues()
        {
            return new List<int>(currentDiceValues);
        }
        
        /// <summary>
        /// Get number of currently displayed dice
        /// </summary>
        /// <returns>Count of displayed dice</returns>
        public int GetDiceCount()
        {
            return currentDiceValues.Count;
        }
        
        private void SetupAIDiceContainer()
        {
            if (aiDiceContainer == null)
            {
                // Create AI dice container if not assigned
                GameObject container = new GameObject("AIDiceContainer");
                container.transform.SetParent(transform);
                
                // Add layout component for automatic positioning
                HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = diceSpacing;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                
                aiDiceContainer = container.transform;
                
                if (enableDebugLogs)
                    Debug.Log("AIDiceDisplaySystem: Created AI dice container with layout");
            }
        }
        
        private void UpdateDiceDisplayImmediate()
        {
            ClearAIDiceDisplay();
            
            for (int i = 0; i < currentDiceValues.Count; i++)
            {
                CreateDiceDisplay(currentDiceValues[i], i);
            }
        }
        
        private IEnumerator UpdateDiceDisplayAnimated()
        {
            // Clear existing displays
            ClearAIDiceDisplay();
            
            // Create new displays with animation
            for (int i = 0; i < currentDiceValues.Count; i++)
            {
                CreateDiceDisplay(currentDiceValues[i], i);
                
                // Animate appearance
                GameObject display = currentDiceDisplays[i];
                if (display != null)
                {
                    Vector3 originalScale = display.transform.localScale;
                    display.transform.localScale = Vector3.zero;
                    
                    float elapsed = 0f;
                    float duration = 1f / updateAnimationSpeed;
                    
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / duration;
                        display.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
                        yield return null;
                    }
                    
                    display.transform.localScale = originalScale;
                }
                
                // Small delay between dice appearances
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private void CreateDiceDisplay(int diceValue, int index)
        {
            GameObject display;
            
            if (aiDicePrefab != null)
            {
                display = Instantiate(aiDicePrefab, aiDiceContainer);
            }
            else
            {
                // Create simple UI element if no prefab assigned
                display = CreateSimpleDiceDisplay();
            }
            
            // Set dice value text
            TextMeshProUGUI text = display.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = aiDicePrefix + diceValue.ToString();
                text.color = aiDiceColor;
            }
            
            // Set visual differentiation
            Image image = display.GetComponent<Image>();
            if (image != null)
            {
                image.color = aiDiceColor;
            }
            
            // Name for debugging
            display.name = $"AIDice_{index}_{diceValue}";
            
            currentDiceDisplays.Add(display);
        }
        
        private GameObject CreateSimpleDiceDisplay()
        {
            GameObject display = new GameObject("AIDiceDisplay");
            display.transform.SetParent(aiDiceContainer);
            
            // Add Image component for background
            Image image = display.AddComponent<Image>();
            image.color = aiDiceColor;
            
            // Add RectTransform and set size
            RectTransform rect = display.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(50, 50);
            
            // Add text for dice value
            GameObject textObj = new GameObject("DiceText");
            textObj.transform.SetParent(display.transform);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "1";
            text.fontSize = 24;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return display;
        }
        
        private void RepositionDiceDisplays()
        {
            // If using layout group, it will handle positioning automatically
            // Otherwise, manually position dice
            if (aiDiceContainer.GetComponent<HorizontalLayoutGroup>() == null)
            {
                for (int i = 0; i < currentDiceDisplays.Count; i++)
                {
                    GameObject display = currentDiceDisplays[i];
                    if (display != null)
                    {
                        RectTransform rect = display.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                            rect.anchoredPosition = new Vector2(i * diceSpacing, 0);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the AI dice container reference (for external setup)
        /// </summary>
        /// <param name="container">Transform to use as dice container</param>
        public void SetAIDiceContainer(Transform container)
        {
            aiDiceContainer = container;
            if (enableDebugLogs)
                Debug.Log("AIDiceDisplaySystem: AI dice container set externally");
        }
        
        /// <summary>
        /// Set the AI dice prefab (for external setup)
        /// </summary>
        /// <param name="prefab">GameObject to use as dice display prefab</param>
        public void SetAIDicePrefab(GameObject prefab)
        {
            aiDicePrefab = prefab;
            if (enableDebugLogs)
                Debug.Log("AIDiceDisplaySystem: AI dice prefab set externally");
        }
        
        /// <summary>
        /// Toggle between instant and animated updates
        /// </summary>
        /// <param name="instant">True for instant updates, false for animated</param>
        public void SetInstantUpdates(bool instant)
        {
            enableInstantUpdates = instant;
            if (enableDebugLogs)
                Debug.Log($"AIDiceDisplaySystem: Instant updates set to {instant}");
        }
        
        /// <summary>
        /// Set visual colors for AI dice differentiation
        /// </summary>
        /// <param name="aiColor">Color for AI dice</param>
        /// <param name="playerColor">Color for player dice (for reference)</param>
        public void SetDiceColors(Color aiColor, Color playerColor)
        {
            aiDiceColor = aiColor;
            playerDiceColor = playerColor;
            
            // Update existing displays
            foreach (GameObject display in currentDiceDisplays)
            {
                if (display != null)
                {
                    Image image = display.GetComponent<Image>();
                    if (image != null)
                        image.color = aiDiceColor;
                    
                    TextMeshProUGUI text = display.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                        text.color = aiDiceColor;
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"AIDiceDisplaySystem: Colors updated - AI: {aiColor}, Player: {playerColor}");
        }
    }
}