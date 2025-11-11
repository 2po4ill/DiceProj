using UnityEngine;
using UnityEditor;
using HybridEnemyAI;

/// <summary>
/// Helper script to automatically set up AI GameObjects in the scene
/// Use this to quickly create the basic AI structure
/// </summary>
public class AISetupHelper : MonoBehaviour
{
    [Header("Auto-Setup Options")]
    [SerializeField] private bool createCoreAI = true;
    [SerializeField] private bool createDiceSystem = true;
    [SerializeField] private bool createConfiguration = true;
    [SerializeField] private bool createUI = true;
    [SerializeField] private bool createTesting = false;
    
    [ContextMenu("Setup AI System")]
    public void SetupAISystem()
    {
        if (createCoreAI)
        {
            CreateCoreAIGameObject();
        }
        
        if (createDiceSystem)
        {
            CreateDiceSystemGameObject();
        }
        
        if (createConfiguration)
        {
            CreateConfigurationGameObject();
        }
        
        if (createUI)
        {
            CreateUIGameObjects();
        }
        
        if (createTesting)
        {
            CreateTestingGameObjects();
        }
        
        Debug.Log("AI System setup complete! Check the installation guide for component configuration.");
    }
    
    private void CreateCoreAIGameObject()
    {
        GameObject aiManager = new GameObject("AI_Manager");
        aiManager.AddComponent<AITurnExecutor>();
        aiManager.AddComponent<AIDecisionEngine>();
        aiManager.AddComponent<AIRiskCalculator>();
        aiManager.AddComponent<AIGameStateAnalyzer>();
        aiManager.AddComponent<AICombinationStrategy>();
        aiManager.AddComponent<AIMinimumDiceSelector>();
        
        Debug.Log("Created AI_Manager with core components");
    }
    
    private void CreateDiceSystemGameObject()
    {
        GameObject diceSystem = new GameObject("AI_DiceSystem");
        diceSystem.AddComponent<AIDiceGenerator>();
        // diceSystem.AddComponent<AIDiceManager>();  // Redundant - disabled
        // diceSystem.AddComponent<AIDiceDisplaySystem>();  // Redundant - disabled
        
        Debug.Log("Created AI_DiceSystem with dice generator (using DiceController for display)");
    }
    
    private void CreateConfigurationGameObject()
    {
        GameObject config = new GameObject("AI_Configuration");
        config.AddComponent<AIConfigurationManager>();
        config.AddComponent<AIDifficultyManager>();
        config.AddComponent<AIDualProbabilityCapSystem>();
        config.AddComponent<AIAggressiveRerollStrategy>();
        
        Debug.Log("Created AI_Configuration with config components");
    }
    
    private void CreateUIGameObjects()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create AI UI
        GameObject aiUI = new GameObject("AI_UI");
        aiUI.transform.SetParent(canvas.transform);
        aiUI.AddComponent<AIUIManager>();
        aiUI.AddComponent<AIScoreDisplay>();
        aiUI.AddComponent<AIDecisionStatusIndicator>();
        aiUI.AddComponent<AITurnFeedbackSystem>();
        
        // Create AI Config UI
        GameObject configUI = new GameObject("AI_ConfigUI");
        configUI.transform.SetParent(canvas.transform);
        configUI.AddComponent<AIConfigurationUI>();
        
        Debug.Log("Created AI UI components");
    }
    
    private void CreateTestingGameObjects()
    {
        GameObject testing = new GameObject("AI_Testing");
        testing.AddComponent<AITestingManager>();
        testing.AddComponent<AIBehaviorDebugger>();
        testing.AddComponent<AIPerformanceTestFramework>();
        testing.AddComponent<AIBalanceValidator>();
        
        GameObject advancedTesting = new GameObject("AI_AdvancedTesting");
        advancedTesting.AddComponent<AIIntegrationTest>();
        advancedTesting.AddComponent<AIConfigurationIntegrationTest>();
        advancedTesting.AddComponent<AIAggressiveRerollTester>();
        advancedTesting.AddComponent<AITestingFrameworkValidator>();
        
        Debug.Log("Created AI testing components");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AISetupHelper))]
public class AISetupHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        AISetupHelper helper = (AISetupHelper)target;
        
        if (GUILayout.Button("Setup AI System", GUILayout.Height(30)))
        {
            helper.SetupAISystem();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This will create GameObjects with AI components. " +
            "After setup, follow the Installation Guide to configure component references.",
            MessageType.Info);
    }
}
#endif