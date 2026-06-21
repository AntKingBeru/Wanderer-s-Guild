// Builder pattern — assembles an AdventurerData step by step and validates required fields
// before construction. Used internally by every AdventurerFactory so creation always goes
// through one consistent, validated path.

using UnityEngine;

public class AdventurerBuilder
{
    private readonly AdventurerConfig _config;

    private string _id;
    private string _name;
    private ClassData _classData;
    private QuestRank _rank = QuestRank.F;
    private int _level = 1;
    private int? _gold;
    
    public AdventurerBuilder(AdventurerConfig config) => _config = config;

    public AdventurerBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public AdventurerBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AdventurerBuilder WithClass(ClassData classData)
    {
        _classData = classData;
        return this;
    }
    public AdventurerBuilder WithRank(QuestRank rank)
    {
        _rank = rank;
        return this;
    }

    public AdventurerBuilder WithLevel(int level)
    {
        _level = Mathf.Max(1, level);
        return this;
    }

    public AdventurerBuilder WithGold(int gold)
    {
        _gold = gold;
        return this;
    }
    
    // Validates required fields and constructs the AdventurerData. Returns null on failure.
    public AdventurerData Build()
    {
        if (!_config)
        {
            Debug.LogError("[AdventurerBuilder] AdventurerConfig is missing.");
            return null;
        }
        if (!_classData)
        {
            Debug.LogError("[AdventurerBuilder] ClassData is missing — call WithClass() before Build().");
            return null;
        }
        if (string.IsNullOrWhiteSpace(_name))
        {
            Debug.LogError("[AdventurerBuilder] Name is missing — call WithName() before Build().");
            return null;
        }

        var id = string.IsNullOrEmpty(_id) ? System.Guid.NewGuid().ToString() : _id;
        var gold = _gold ?? _config.StartingGold;
        var level = Mathf.Clamp(_level, 1, _config.MaxLevel);

        return new AdventurerData(id, _name, _classData, _rank, level, gold, _config);
    }
}
