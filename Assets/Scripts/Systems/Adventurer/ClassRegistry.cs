// Singleton that tracks which classes are currently unlocked: started with, automatically by guild rank
// (RankUp method) or manually via training (Training method).
// Recomputes whenever the guild's rank changes.
// RandomAdventurerFactory queries this for eligible classes when rolling a new adventurer.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ClassRegistry : MonoBehaviour
{
    public static ClassRegistry Instance { get; private set; }
    
    private ClassDatabase _database;
    private readonly HashSet<AdventurerClass> _trainedUnlocks = new();
    private readonly List<ClassData> _unlockedClasses = new();
    
    public IReadOnlyList<ClassData> UnlockedClasses => _unlockedClasses;
    
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _database = GameManager.Instance ? GameManager.Instance.ClassDatabase : null;
        if (!_database)
            Debug.LogError("[ClassRegistry] No ClassDatabase found on GameManager.");
    }
    
    private void OnEnable()
    {
        if (GameEventRelay.Instance)
            GameEventRelay.Instance.OnProgressionRankChanged.AddListener(OnGuildRankChanged);
    }

    private void OnDisable()
    {
        if (GameEventRelay.Instance)
            GameEventRelay.Instance.OnProgressionRankChanged.RemoveListener(OnGuildRankChanged);
    }
    
    // Deferred to Start() so it always runs after ProgressionSystem's Awake has set Instance,
    // regardless of script execution order between the two.
    private void Start() => RecomputeUnlockedClasses();
    
    #region Public API
    // Looks up a class by enum value regardless of unlock state.
    public ClassData GetClassData(AdventurerClass adventurerClass)
        => _database ? _database.GetClassData(adventurerClass) : null;

    // Picks a random unlocked class that qualifies for the given rank and level.
    // Returns null if nothing qualifies (caller should treat this as "skip this spawn").
    public ClassData GetRandomUnlockedClassData(QuestRank rank, int level)
    {
        var candidates = _unlockedClasses
            .Where(c => c.MinimumRank <= rank && c.MinimumLevel <= level)
            .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[ClassRegistry] No unlocked class qualifies for rank {rank}, level {level}.");
            return null;
        }
        return candidates[Random.Range(0, candidates.Count)];
    }

    // Manually unlocks a Training-method class. Called by the Build system once a training
    // room reaches the right tier (not yet implemented — this is the integration point).
    public void UnlockClassViaTraining(AdventurerClass adventurerClass)
    {
        if (_trainedUnlocks.Add(adventurerClass))
            RecomputeUnlockedClasses();
    }
    #endregion
    
    #region Private Helpers
    private void OnGuildRankChanged(int _) => RecomputeUnlockedClasses();

    private void RecomputeUnlockedClasses()
    {
        if (!_database)
            return;

        var guildRank = ProgressionSystem.Instance ? ProgressionSystem.Instance.GuildRank : QuestRank.F;
        var newlyUnlocked = new List<ClassData>();

        foreach (var classData in _database.AllClasses)
        {
            if (!classData || _unlockedClasses.Contains(classData))
                continue;

            var isUnlocked = classData.UnlockMethod == ClassUnlockMethod.RankUp
                ? guildRank >= classData.MinimumRank
                : _trainedUnlocks.Contains(classData.AdventurerClass);

            if (isUnlocked)
                newlyUnlocked.Add(classData);
        }

        foreach (var classData in newlyUnlocked)
        {
            _unlockedClasses.Add(classData);
            GameEventRelay.Instance?.OnClassUnlocked.Invoke(classData);
        }
    }
    #endregion
}