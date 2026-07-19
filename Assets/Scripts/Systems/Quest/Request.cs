// Immutable model of an external request awaiting conversion into a guild quest.

public class Request
{
    public int Id { get; }
    public RequestSource Source { get; }
    public QuestCategory Category { get; }
    public string Objective { get; }
    public QuestDifficulty Difficulty { get; }
    public GuildRank RecommendedRank { get; }
    public int RewardGold { get; }
    public GameDate ExpirationDate { get; }

    public Request(int id, RequestSource source, QuestCategory category, string objective,
        QuestDifficulty difficulty, GuildRank recommendedRank,
        int rewardGold, GameDate expirationDate)
    {
        Id = id;
        Source = source;
        Category = category;
        Objective = objective;
        Difficulty = difficulty;
        RecommendedRank = recommendedRank;
        RewardGold = rewardGold;
        ExpirationDate = expirationDate;
    }
    
    public bool IsExpired(GameDate now)
        => now.CompareTo(ExpirationDate) >= 0;
}