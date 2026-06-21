// Singleton facade for the adventurer roster. Single responsibility: own the roster
// dictionary and orchestrate creation/progression calls — it never decides whether a
// transition is legal (that lives on AdventurerData) and never decides HOW an adventurer
// is built (that lives in the factories).

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AdventurerManager : MonoBehaviour
{
    public static AdventurerManager Instance { get; private set; }

    private readonly Dictionary<string, AdventurerData> _roster = new();

    private AdventurerConfig _config;
    private RandomNameGenerator _nameGenerator;
    private RandomAdventurerFactory _randomFactory;
    
    public IReadOnlyList<AdventurerData> Adventurers => _roster.Values.ToList();

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _config = GameManager.Instance ? GameManager.Instance.AdventurerConfig : null;
        if (!_config)
            Debug.LogError("[AdventurerManager] No AdventurerConfig found on GameManager.");

        _nameGenerator = new RandomNameGenerator(GameManager.Instance ? GameManager.Instance.NameDatabase : null);
        _randomFactory = new RandomAdventurerFactory(_config, _nameGenerator);
    }
    
    #region Creation
    // Spontaneous arrival — rank/level/class/name are all randomized.
    public AdventurerData CreateRandomAdventurer()
    {
        var adventurer = _randomFactory.CreateAdventurer(BuildContext());
        if (adventurer != null)
            Register(adventurer);
        return adventurer;
    }

    // Designer-defined arrival — values come from a preset asset (starting roster, story beats).
    public AdventurerData CreateSetAdventurer(AdventurerPreset preset)
    {
        if (!preset)
        {
            Debug.LogError("[AdventurerManager] CreateSetAdventurer called with a null preset.");
            return null;
        }

        var factory = new SetAdventurerFactory(preset, _config, _nameGenerator);
        var adventurer = factory.CreateAdventurer(BuildContext());
        if (adventurer != null)
            Register(adventurer);
        return adventurer;
    }

    private void Register(AdventurerData adventurer)
    {
        _roster[adventurer.Id] = adventurer;
        GameEventRelay.Instance?.OnAdventurerCreated.Invoke(adventurer);
        GameEventRelay.Instance?.OnRosterChanged.Invoke();
    }

    private AdventurerCreationContext BuildContext()
    {
        var context = new AdventurerCreationContext
        {
            GuildRankCap = ProgressionSystem.Instance ? ProgressionSystem.Instance.GuildRank : QuestRank.F,
            TotalAdventurerCount = _roster.Count
        };

        foreach (var adventurer in _roster.Values)
        {
            context.AdventurersPerClass[(int)adventurer.ClassType]++;
            context.AdventurersPerRank[(int)adventurer.Rank]++;
        }
        return context;
    }
    #endregion
    
    #region Roster Queries
    public AdventurerData GetAdventurer(string id)
        => _roster.GetValueOrDefault(id);

    public bool RemoveAdventurer(string id)
    {
        if (!_roster.Remove(id, out var adventurer))
            return false;

        GameEventRelay.Instance?.OnAdventurerRemoved.Invoke(adventurer);
        GameEventRelay.Instance?.OnRosterChanged.Invoke();
        return true;
    }
    #endregion
    
    #region Progression
    public bool AddExperience(string id, int amount)
    {
        var adventurer = GetAdventurer(id);
        var classData = adventurer != null ? ClassRegistry.Instance?.GetClassData(adventurer.ClassType) : null;
        if (adventurer == null || !classData || !_config)
            return false;

        var leveledUp = adventurer.AddExperience(amount, classData, _config);
        if (leveledUp)
        {
            GameEventRelay.Instance?.OnAdventurerLeveledUp.Invoke(adventurer);
            GameEventRelay.Instance?.OnRosterChanged.Invoke();
        }
        return leveledUp;
    }

    public bool AddRankPoints(string id, int amount)
    {
        var adventurer = GetAdventurer(id);
        if (adventurer == null || !_config)
            return false;

        var becameEligible = adventurer.AddRankPoints(amount, _config);
        if (becameEligible)
            GameEventRelay.Instance?.OnAdventurerRankUpEligible.Invoke(adventurer);
        return becameEligible;
    }

    // Hook for the future Quest system: call once a rank-up quest succeeds.
    public bool PromoteRank(string id)
    {
        var adventurer = GetAdventurer(id);
        if (adventurer == null || !adventurer.PromoteRank())
            return false;

        GameEventRelay.Instance?.OnAdventurerRankedUp.Invoke(adventurer);
        GameEventRelay.Instance?.OnRosterChanged.Invoke();
        return true;
    }
    #endregion
    
    #region Gold
    public void AddGold(string id, int amount)
    {
        var adventurer = GetAdventurer(id);
        if (adventurer == null) return;
        adventurer.AddGold(amount);
        GameEventRelay.Instance?.OnAdventurerGoldChanged.Invoke(adventurer);
    }

    public bool SpendGold(string id, int amount)
    {
        var adventurer = GetAdventurer(id);
        if (adventurer == null) return false;
        var success = adventurer.SpendGold(amount);
        if (success)
            GameEventRelay.Instance?.OnAdventurerGoldChanged.Invoke(adventurer);
        return success;
    }
    #endregion
}