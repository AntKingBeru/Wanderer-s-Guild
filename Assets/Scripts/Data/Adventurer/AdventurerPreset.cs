// ScriptableObject acting as a Prototype — a designer-authored template for a specific
// adventurer (starting roster, story-triggered arrivals, etc.). SetAdventurerFactory clones
// these values into a real AdventurerData via AdventurerBuilder.

using UnityEngine;

[CreateAssetMenu(fileName = "AdventurerPreset_New", menuName = "Guild Manager/Adventurer/Adventurer Preset")]
public class AdventurerPreset : ScriptableObject
{
    [Tooltip("Leave blank to have a name rolled by the random name generator instead.")]
    [SerializeField] private string presetName;

    [SerializeField] private AdventurerClass classType = AdventurerClass.Fighter;
    [SerializeField] private QuestRank rank = QuestRank.F;
    [SerializeField, Min(1)] private int level = 1;

    [Tooltip("Leave at -1 to use AdventurerConfig's default starting gold instead.")]
    [SerializeField, Min(-1)] private int goldOverride = -1;

    public string PresetName => presetName;
    public AdventurerClass ClassType => classType;
    public QuestRank Rank => rank;
    public int Level => level;
    public int GoldOverride => goldOverride;
}