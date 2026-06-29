using UnityEngine;

public class StandardRequestFactory : IRequestFactory
{
    private readonly System.Random _rng;
    private readonly int _baseExpirationDays;
    private readonly int _daysPerSeason;
    
    public StandardRequestFactory(System.Random rng, int baseExpirationDays, int daysPerSeason)
    {
        _rng = rng;
        _baseExpirationDays = Mathf.Max(1, baseExpirationDays);
        _daysPerSeason = Mathf.Max(1, daysPerSeason);
    }
    
    public Request Create(int id, RequestTemplate template, GameDate now)
    {
        var difficulty = template.RollDifficulty(_rng);
        var reward = template.RollReward(_rng);
        var recommended = RankForDifficulty(difficulty);
        var expiry = now.AddDays(_baseExpirationDays, _daysPerSeason);

        return new Request(id, template.Source, template.Category, template.PickObjective(_rng),
            difficulty, recommended, reward, expiry);
    }
    
    private static GuildRank RankForDifficulty(QuestDifficulty d) => d switch
    {
        QuestDifficulty.Trivial => GuildRank.F,
        QuestDifficulty.Easy => GuildRank.E,
        QuestDifficulty.Moderate => GuildRank.D,
        QuestDifficulty.Hard => GuildRank.C,
        QuestDifficulty.Severe => GuildRank.B,
        QuestDifficulty.Extreme => GuildRank.A,
        QuestDifficulty.Deadly => GuildRank.S,
        QuestDifficulty.Special => GuildRank.National,
        _ => GuildRank.F
    };
}