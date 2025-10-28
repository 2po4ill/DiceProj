using UnityEngine;

[CreateAssetMenu(fileName = "DiceCombinationRules", menuName = "Dice Game/Combination Rules")]
public class DiceCombinationRules : ScriptableObject
{
    [System.Serializable]
    public class CombinationRule
    {
        public string name;
        [TextArea(2, 3)]
        public string description;
        public Rule rule;
        public Count count;
        public float multiplier = 1f;
        public int points = 0;
    }
    
    [Header("Combination Rules")]
    public CombinationRule[] combinations = new CombinationRule[]
    {
    };
    
    public CombinationRule GetRule(Rule ruleType)
    {
        foreach (var rule in combinations)
        {
            if (rule.rule == ruleType)
                return rule;
        }
        return null;
    }
}