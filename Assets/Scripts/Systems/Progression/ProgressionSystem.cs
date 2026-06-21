// Singleton that tracks the guild's overall rank (F → Special) and XP.
// Thresholds come from ProgressionConfig (via GameManager) instead of being hardcoded.
// Fires onProgressionXpChanged (every XP change) and onProgressionRankChanged (rank changes only).
// Event payloads stay as plain ints for UI backwards-compatibility, even though rank is
// stored internally as QuestRank for type safety.

using UnityEngine;

public class ProgressionSystem : MonoBehaviour
{
    private const int MaxRankIndex = (int)QuestRank.Special;

    public static ProgressionSystem Instance { get; private set; }

    private ProgressionConfig _config;
    private QuestRank _currentRank;
    private int _currentXp;
    
    #region Public Accessors
    public QuestRank CurrentRank => _currentRank;
    public int CurrentXp => _currentXp;

    // XP threshold to reach the NEXT rank. 0 at max rank.
    public int CurrentThreshold => IsHighestRank() || !_config ? 0 : _config.GetThreshold(_currentRank);

    // Alias kept for readability at call sites that think in "guild rank" terms.
    public QuestRank GuildRank => _currentRank;

    // Max rank a newly-arriving adventurer may have.
    public QuestRank MaxAdventurerArrivalRank => _currentRank;

    // Max quest base-rank that may appear in the daily request draw. (Consumed by the Quest system.)
    public QuestRank MaxRequestRank => _currentRank;

    // Active quest board slots: rank + 3, clamped 3-10. (Consumed by the Quest system.)
    public int ActiveBoardSlots => Mathf.Clamp((int)_currentRank + 3, 3, 10);
    #endregion
    
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _config = GameManager.Instance ? GameManager.Instance.ProgressionConfig : null;
        if (!_config)
            Debug.LogError("[ProgressionSystem] No ProgressionConfig found on GameManager.");

        BroadcastXp();
    }
    
    #region Public API
    public void GainXp(int xp)
    {
        if (IsHighestRank() || xp <= 0 || !_config)
            return;
        _currentXp += xp;
        ProcessRankUps();
    }

    // Returns the maximum rank an adventurer may rank up TO, given their current rank.
    //   - A-rank and above → capped at the guild's current rank.
    //   - Below A-rank → capped at guild rank + 1.
    public QuestRank GetMaxAdventurerRankUpTarget(QuestRank currentAdventurerRank)
    {
        var isHighRank = currentAdventurerRank >= QuestRank.A;
        var cap = isHighRank ? (int)_currentRank : (int)_currentRank + 1;
        cap = Mathf.Min(cap, MaxRankIndex);
        return (QuestRank)cap;
    }
    #endregion
    
    #region Private Helpers
    private void ProcessRankUps()
    {
        var lastRank = _currentRank;

        while (!IsHighestRank() && _currentXp >= _config.GetThreshold(_currentRank))
        {
            _currentXp -= _config.GetThreshold(_currentRank);
            _currentRank = (QuestRank)((int)_currentRank + 1);
        }

        if (IsHighestRank())
            _currentXp = 0;

        if (_currentRank != lastRank)
            GameEventRelay.Instance?.OnProgressionRankChanged.Invoke((int)_currentRank);

        BroadcastXp();
    }

    private void BroadcastXp()
    {
        var threshold = IsHighestRank() || !_config ? 1 : _config.GetThreshold(_currentRank);
        GameEventRelay.Instance?.OnProgressionXpChanged.Invoke(_currentXp, threshold);
    }

    private bool IsHighestRank() => (int)_currentRank >= MaxRankIndex;
    #endregion
}