// Concrete factory (Factory Method pattern) that clones a designer-authored AdventurerPreset
// (Prototype pattern) into a real AdventurerData. Used for the starting roster and for
// scripted arrivals at specific story beats.

using UnityEngine;

public class SetAdventurerFactory : AdventurerFactory
{
    private readonly AdventurerPreset _preset;
    private readonly AdventurerConfig _config;
    private readonly RandomNameGenerator _nameGenerator;

    public SetAdventurerFactory(AdventurerPreset preset, AdventurerConfig config, RandomNameGenerator nameGenerator)
    {
        _preset = preset;
        _config = config;
        _nameGenerator = nameGenerator;
        if (!_preset)
            Debug.LogError("[SetAdventurerFactory] AdventurerPreset is null.");
    }

    public override AdventurerData CreateAdventurer(AdventurerCreationContext context)
    {
        if (!_preset || !_config)
            return null;
        if (!ClassRegistry.Instance)
        {
            Debug.LogError("[SetAdventurerFactory] ClassRegistry not found in scene.");
            return null;
        }

        var classData = ClassRegistry.Instance.GetClassData(_preset.ClassType);
        if (!classData)
        {
            Debug.LogError($"[SetAdventurerFactory] No ClassData found for '{_preset.ClassType}'.");
            return null;
        }

        if (_preset.Level < classData.MinimumLevel)
            Debug.LogWarning($"[SetAdventurerFactory] Preset level {_preset.Level} is below " +
                             $"'{classData.DisplayName}' minimum level {classData.MinimumLevel}. Using it anyway — designer override.");

        var name = string.IsNullOrWhiteSpace(_preset.PresetName) ? _nameGenerator.GenerateName() : _preset.PresetName;

        var builder = new AdventurerBuilder(_config)
            .WithId(GenerateID())
            .WithName(name)
            .WithClass(classData)
            .WithRank(_preset.Rank)
            .WithLevel(_preset.Level);

        if (_preset.GoldOverride >= 0)
            builder.WithGold(_preset.GoldOverride);

        return builder.Build();
    }
}