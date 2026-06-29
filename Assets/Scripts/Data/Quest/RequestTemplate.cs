// ScriptableObject prototype defining a kind of request; randomized into Request instances.

using UnityEngine;

[CreateAssetMenu(fileName = "RequestTemplate", menuName = "Wanderer's Guild/Request Template")]
public class RequestTemplate : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private RequestSource source;
    [SerializeField] private QuestCategory category;
    [Tooltip("Objective lines — one is picked at random per generated request.")]
    [TextArea] [SerializeField] private string[] objectivePool;
    
    [Header("Difficulty & Reward (inclusive ranges)")]
    [SerializeField] private QuestDifficulty minDifficulty = QuestDifficulty.Trivial;
    [SerializeField] private QuestDifficulty maxDifficulty = QuestDifficulty.Easy;
    [SerializeField] private int minRewardGold = 50;
    [SerializeField] private int maxRewardGold = 150;
    
    [Header("Availability")]
    [Tooltip("Reputation required before this template can be selected.")]
    [SerializeField] private int minReputation;
    [Tooltip("Relative odds of being chosen among eligible templates.")]
    [Min(0.01f)] [SerializeField] private float selectionWeight = 1f;
    
    public RequestSource Source => source;
    public QuestCategory Category => category;
    public int MinReputation => minReputation;
    public float SelectionWeight => selectionWeight;
    
    public string PickObjective(System.Random rng) =>
        objectivePool is { Length: > 0 }
            ? objectivePool[rng.Next(objectivePool.Length)]
            : "(unspecified objective)";
    
    public QuestDifficulty RollDifficulty(System.Random rng)
    {
        int low = (int)minDifficulty, hi = (int)maxDifficulty;
        if (hi < low)
            (low, hi) = (hi, low);
        return (QuestDifficulty)rng.Next(low, hi + 1);
    }
    
    public int RollReward(System.Random rng)
    {
        var low = Mathf.Min(minRewardGold, maxRewardGold);
        var hi = Mathf.Max(minRewardGold, maxRewardGold);
        return rng.Next(low, hi + 1);
    }
}