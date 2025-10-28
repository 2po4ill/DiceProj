using UnityEngine;

public enum Rule
{
    Zonk,           // No combination listed
    One,            // 1 or 5 in dice values, singular combination
    Pair,           // Two of same value
    TwoPair,       // Two different pairs
    LowStraight,    // three consecutive numbers
    MiddleStraight,   //  four consecutive numbers
    ThreeOfKind,    // Three of same value
    FullHouse,      // Three + pair
    FourOfKind,     // Four of same value
    ThreePairs,     // Three different pairs
    Straight,       // Five consequtive numbers
    MaxStraight,    // Six consequtive numbers
    TwoSets,        // Two different three of a kind
}

public enum Count
{
    Zonk,           // No combination listed
    One,            // 1 or 5 in dice values, singular combination
    Pair,           // Two of same value
    TwoPair,       // Two different pairs
    LowStraight,    // three consecutive numbers
    MiddleStraight,   //  four consecutive numbers
    ThreeOfKind,    // Three of same value
    FullHouse,      // Three + pair
    FourOfKind,     // Four of same value
    ThreePairs,     // Three different pairs
    Straight,       // Five consequtive numbers
    MaxStraight,    // Six consequtive numbers
    TwoSets,        // Two different three of a kind
}

[System.Serializable]
public class CombinationResult
{
    public Rule rule;
    public int points;
    public string description;
    public float multiplier;
    
    public CombinationResult(Rule ruleType, int score, string desc, float mult = 1f)
    {
        rule = ruleType;
        points = score;
        description = desc;
        multiplier = mult;
    }
}