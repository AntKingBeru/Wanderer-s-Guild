// Singleton that tracks guild XP and rank (F → National, internally 0–7).
// Fires two events via GameEventRelay:
//   onProgressionXpChanged  — every XP change (xp, threshold)
//   onProgressionRankChanged — only when the rank level changes
// Exposes derived board/quest/adventurer cap helpers consumed by other managers.

using System.Collections.Generic;
using UnityEngine;

public class ProgressionSystem : MonoBehaviour
{
    // Rank indices: 0=F, 1=E, 2=D, 3=C, 4=B, 5=A, 6=S, 7=National(max)
    private const int MaxProgressionRank = 7;
    
    // Making this class singleton
    public static ProgressionSystem Instance { get; private set; }
    
    private int _currentRank;
    private int _currentXp;
    
    // XP threshold for each rank transition: index = current rank (0=F→E … 6=S→National).
    // Rank 7 (National/max) has no threshold; that key is intentionally absent.
    private readonly Dictionary<int, int> _rankThresholds = new()
    {
        { 0, 500 },    // F → E
        { 1, 1000 },   // E → D
        { 2, 1750 },   // D → C
        { 3, 2750 },   // C → B
        { 4, 4000 },   // B → A
        { 5, 6000 },   // A → S
        { 6, 10000 },  // S → National
    };
    
    #region Public Accessors
    public int CurrentRank => _currentRank;
    public int CurrentXp => _currentXp;
    
    // XP threshold to reach the NEXT rank. Returns 0 at max rank (no more thresholds).
    public int CurrentThreshold
        => IsHighestRank() ? 0 : _rankThresholds[_currentRank];
    
    // Guild rank as a QuestRank enum — used by adventurer/quest systems.
    public QuestRank GuildRank
        => (QuestRank)_currentRank;
    
    // Max rank a newly-arriving adventurer may have (= guild rank, capped at Special).
    public QuestRank MaxAdventurerArrivalRank
        => (QuestRank)Mathf.Min(_currentRank, (int)QuestRank.S);
    
    // Max quest base-rank that may appear in the daily request draw.
    public QuestRank MaxRequestRank => GuildRank;
    
    // Active board slots: rank + 3, minimum 3, maximum 10 (all slots open at National rank).
    // Formula: F=3, E=4, D=5, C=6, B=7, A=8, S=9, National=10
    public int ActiveBoardSlots
        => Mathf.Clamp(_currentRank + 3, 3, 10);
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

        // Broadcast initial state so listeners that register early get a valid baseline.
        BroadcastXp();
    }
    
    #region Public API
    public void GainXp(int xp)
    {
        if (IsHighestRank() || xp <= 0)
            return;
        _currentXp += xp;
        ProcessRankUps();
    }
    
    // Returns the maximum rank an adventurer may rank up TO, given the guild rank.
    // Rules:
    //   - A-rank and above → can only rank up to the guild's current rank (not above).
    //   - Below A-rank → can rank up to guild rank + 1.
    public QuestRank GetMaxAdventurerRankUpTarget(QuestRank currentAdventurerRank)
    {
        var isHighRank = (int)currentAdventurerRank >= (int)QuestRank.A;
        var cap = isHighRank ? _currentRank : _currentRank + 1;
        // Never exceed the absolute max rank (National/Special).
        cap = Mathf.Min(cap, MaxProgressionRank);
        return (QuestRank)cap;
    }
    #endregion
    
    #region Private Helpers
    // Drains XP across as many rank-up thresholds as available, then broadcasts.
    private void ProcessRankUps()
    {
        var lastRank = _currentRank;

        while (!IsHighestRank() && _currentXp >= _rankThresholds[_currentRank])
        {
            _currentXp -= _rankThresholds[_currentRank];
            _currentRank++;
        }

        // Clamp leftover XP at max rank — no reason to store excess.
        if (IsHighestRank())
            _currentXp = 0;

        if (_currentRank != lastRank)
            GameEventRelay.Instance?.onProgressionRankChanged.Invoke(_currentRank);

        BroadcastXp();
    }

    private void BroadcastXp()
    {
        var threshold = IsHighestRank() ? 1 : _rankThresholds[_currentRank];
        GameEventRelay.Instance?.onProgressionXpChanged.Invoke(_currentXp, threshold);
    }

    private bool IsHighestRank()
        => _currentRank >= MaxProgressionRank;
    #endregion
}