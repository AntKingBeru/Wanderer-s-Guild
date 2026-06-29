// Builder pattern: fluently configures and validates a Quest from a source Request.

using UnityEngine;

public class QuestBuilder
{
    private readonly Request _source;
    private readonly int _daysPerSeason;
    private int _guildPercent;
    private GuildRank _minRank;
    private GuildRank _maxRank;
    private int _minPartySize;
    private int _maxPartySize;
    private int _lifetimeDays;
    
    public QuestBuilder(Request source)
    {
        _source = source;
        var config = GameConfig.Instance;

        _guildPercent  = config.Quest.defaultGuildRewardPercent;
        _lifetimeDays  = config.Quest.postedQuestLifetimeDays;
        _daysPerSeason = config.Time.daysPerSeason;

        // Sensible starting bounds: recommended rank as the floor, the low-rank party window.
        _minRank = source.RecommendedRank;
        _maxRank = GuildRank.National;
        _minPartySize = config.Party.lowRankSize.min;
        _maxPartySize = config.Party.lowRankSize.max;
    }

    public QuestBuilder WithGuildRewardPercent(int percent)
    {
        _guildPercent = Mathf.Clamp(percent, 0, 100);
        return this;
    }
    
    public QuestBuilder WithRankRange(GuildRank min, GuildRank max)
    {
        if ((int)max < (int)min) (min, max) = (max, min);
        _minRank = min;
        _maxRank = max;
        return this;
    }
    
    public QuestBuilder WithPartySize(int min, int max)
    {
        min = Mathf.Max(1, min);
        max = Mathf.Max(min, max);
        _minPartySize = min; _maxPartySize = max;
        return this;
    }

    public QuestBuilder WithLifetimeDays(int days)
    {
        _lifetimeDays = Mathf.Max(1, days);
        return this;
    }
    
    public bool Validate(out string error)
    {
        if (_source == null)
        {
            error = "No source request.";
            return false;
        }

        if ((int)_maxRank < (int)_minRank)
        {
            error = "Max rank is below min rank.";
            return false;
        }

        if (_maxPartySize < _minPartySize)
        {
            error = "Max party size is below min.";
            return false;
        }

        if (_minPartySize < 1)
        {
            error = "Party must allow at least one member.";
            return false;
        }
        error = null;
        return true;
    }
    
    public Quest Build(int questId, GameDate now)
    {
        if (!Validate(out _))
            return null;

        var config = new QuestConfiguration(
            new RewardSplit(_guildPercent), _minRank, _maxRank, _minPartySize, _maxPartySize);

        var expiry = now.AddDays(_lifetimeDays, _daysPerSeason);
        return new Quest(questId, _source, config, expiry);
    }
}