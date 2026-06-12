// Singleton that owns all quest-related state and drives the full quest lifecycle.
// Responsibilities: request pool draw, request/quest expiry, board slot management,
// application handling, success/completion formulas, quest resolution, and guild funds.
// Adventurer integration: AdventurerManager subscribes to OnQuestStatusChanged and
// handles adventurer-side rewards (XP, rank points, adventurer gold) via that event.
// QuestManager handles guild-side rewards (treasury gold, reputation) directly here.
// Fallback application simulation runs only when AdventurerManager.Instance is null
// (useful for testing quests without a full adventurer roster in the scene).

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }
    
    #region Inspector
    [Header("Configuration")]
    [Tooltip("Shared quest system config asset. Create via Guild Manager/Quest Config.")]
    [SerializeField] private QuestConfig questConfig;
    
    [Header("Request Pool")]
    [Tooltip("All request templates available for daily draws. " +
             "Assign the temporary QuestRequestData assets from Assets/Data/Quest/Requests.")]
    [SerializeField] private QuestRequestData[] requestPool;
    
    [Header("Guild Funds")]
    [Tooltip("Gold in the guild treasury at game start.")]
    [SerializeField, Min(0)] private int startingGuildFunds = 150;
    
    [Header("Application Simulation — Placeholder")]
    [Tooltip("Probability per in-game hour that a posted quest receives a simulated application. " +
             "Multiplied by the current season modifier.")]
    [SerializeField, Range(0f, 1f)] private float baseApplicationChancePerHour = 0.35f;

    [Tooltip("Application rate multiplier per season. Index matches Season enum: " +
             "0 = Spring, 1 = Summer, 2 = Autumn, 3 = Winter.")]
    [SerializeField] private float[] seasonApplicationRateModifiers = { 1.0f, 0.8f, 1.2f, 0.6f };
    #endregion
    
    #region Runtime State
    // Requests drawn from the pool; available at the reception desk.
    private List<QuestRequest> _availableRequests;
    // Quests created at reception but not yet dragged onto the board.
    private List<QuestData> _unpostedQuests;
    // Fixed-size board. null means the slot is empty.
    private QuestData[] _boardSlots;
    // Quests whose applications have been approved; awaiting resolution.
    private List<QuestData> _inProgressQuests;
    // All quests that have reached a terminal state (Completed, Failed, Expired).
    private List<QuestData> _resolvedQuests;

    private int _guildFunds;
    #endregion
    
    #region Public Properties
    public int GuildFunds => _guildFunds;
    public QuestConfig Config => questConfig;
    public IReadOnlyList<QuestRequest> AvailableRequests => _availableRequests;
    public IReadOnlyList<QuestData> UnpostedQuests => _unpostedQuests;
    public IReadOnlyList<QuestData> InProgressQuests => _inProgressQuests;
    public IReadOnlyList<QuestData> ResolvedQuests => _resolvedQuests;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (!questConfig)
            Debug.LogError("[QuestManager] QuestConfig is not assigned in the inspector.");

        var boardSize = questConfig ? questConfig.MaxBoardSlots : 10;

        _availableRequests = new List<QuestRequest>();
        _unpostedQuests = new List<QuestData>();
        _boardSlots = new QuestData[boardSize];
        _inProgressQuests = new List<QuestData>();
        _resolvedQuests = new List<QuestData>();
        _guildFunds = startingGuildFunds;
    }

    private void OnEnable()
    {
        GameEventRelay.Instance.OnHourChanged.AddListener(HandleHourChanged);
        GameEventRelay.Instance.OnDayChanged.AddListener(HandleDayChanged);
    }

    private void OnDisable()
    {
        GameEventRelay.Instance.OnHourChanged.RemoveListener(HandleHourChanged);
        GameEventRelay.Instance.OnDayChanged.RemoveListener(HandleDayChanged);
    }

    private void Start()
    {
        if (!TimeManager.Instance)
        {
            Debug.LogError("[QuestManager] TimeManager not found. " +
                           "Ensure it exists in the scene and initialises before QuestManager.");
            return;
        }
        // Seed the reception desk with the first day's requests immediately on game start.
        GenerateDailyRequests(TimeManager.Instance.Day);
    }
    #endregion
    
    #region Time Event Handlers
    private void HandleDayChanged(int day)
    {
        // Expire stale requests first, then add fresh one, so day-1 draws don't immediately appear alongside day-4 replacements.
        CheckRequestExpiry(day);
        GenerateDailyRequests(day);
    }

    private void HandleHourChanged(int hour)
    {
        CheckBoardQuestExpiry();
        CheckQuestResolution();
        
        var inApplicationWindow = questConfig
            && hour >= questConfig.ApplicationWindowStartHour
            && hour <= questConfig.ApplicationWindowEndHour;

        if (inApplicationWindow && !AdventurerManager.Instance)
            SimulateFallbackApplications();
    }
    #endregion
    
    #region Game-Time Computation
    // Returns total in-game hours elapsed since Year 1 / Month 1 / Day 1 / 00:00.
    // Used as an absolute timestamp for posting, expiry, and resolution comparisons.
    // Recomputed from source each call to avoid floating-point drift from accumulation.
    private float GetCurrentGameHours()
        => TimeManager.Instance.GetTotalGameHours();
    #endregion
    
    #region Request Management
    // Draws up to RequestsPerDay templates from the pool without repeating within the same draw.
    // Uses a Fisher-Yates shuffle so every template has an equal chance of being selected.
    private void GenerateDailyRequests(int currentDay)
    {
        if (requestPool == null || requestPool.Length == 0)
        {
            Debug.LogWarning("[QuestManager] Request pool is empty. " +
                             "Assign QuestRequestData assets in the inspector.");
            return;
        }
        // Collect valid pool indices.
        var indices = new List<int>(requestPool.Length);
        for (var i = 0; i < requestPool.Length; i++)
            if (requestPool[i])
                indices.Add(i);
        // Fisher-Yates shuffle
        for (var i = indices.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i+1);
            (indices[i],  indices[j]) = (indices[j], indices[i]);
        }

        var drawCount = questConfig ? questConfig.RequestsPerDay : 1;
        var drawn = 0;

        foreach (var index in indices.TakeWhile(_ => drawn < drawCount))
        {
            _availableRequests.Add(new QuestRequest(requestPool[index], currentDay));
            drawn++;
        }
        
        if (drawn > 0)
            GameEventRelay.Instance?.OnAvailableRequestsChanged.Invoke();
    }
    
    // Marks and removes requests that have sat unconverted past their expiry window.
    private void CheckRequestExpiry(int currentDay)
    {
        var expiryDays = questConfig ? questConfig.RequestExpiryDays : 1;
        var anyExpired = false;

        foreach (var request in _availableRequests.Where(request => request.ShouldExpire(currentDay, expiryDays)))
        {
            request.MarkExpired();
            anyExpired = true;
        }

        if (!anyExpired)
            return;
        
        // RemoveAll avoids mutation during the foreach above.
        _availableRequests.RemoveAll(r => r.IsExpired);
        GameEventRelay.Instance?.OnAvailableRequestsChanged.Invoke();
    }
    #endregion
    
    #region Quest Creation
    // Creates a QuestData from a chosen request with the player's rank and reward decisions.
    // Marks the source request as converted so it cannot be used again.
    public QuestData CreateQuest(QuestRequest request, QuestRank chosenRank, int adventurerReward)
    {
        if (request is not { IsAvailable: true })
        {
            Debug.LogWarning("[QuestManager] CreateQuest called with a null or unavailable request.");
            return null;
        }
        if (!request.SourceData)
        {
            Debug.LogError("[QuestManager] Request has no SourceData; cannot create quest.");
            return null;
        }
        // Silently clamp rank to the allowed ±1 window in case the UI passes a bad value.
        if (!request.SourceData.IsRankAllowed(chosenRank))
        {
            Debug.LogWarning($"[QuestManager] Rank {chosenRank} outside ±1 of base rank " +
                             $"{request.BaseRank}. Clamping to valid range.");
            chosenRank = (QuestRank)Mathf.Clamp((int)chosenRank,
                (int)request.GetMinAllowedRank(),
                (int)request.GetMaxAllowedRank());
        }

        var clampedReward = Mathf.Clamp(adventurerReward, 0, request.MaxReward);
        var quest = new QuestData(request.SourceData, chosenRank, clampedReward);
        
        request.MarkConverted();
        _availableRequests.Remove(request);
        _unpostedQuests.Add(quest);

        GameEventRelay.Instance?.OnAvailableRequestsChanged.Invoke();
        GameEventRelay.Instance?.OnUnpostedQuestsChanged.Invoke();
        return quest;
    }

    public void ForceFailQuest(string questId)
    {
        for (var i = _inProgressQuests.Count - 1; i >= 0; i--)
        {
            var quest = _inProgressQuests[i];
            if (quest.QuestId != questId)
                continue;
            _inProgressQuests.RemoveAt(i);
            _resolvedQuests.Add(quest);
            quest.Fail();
            ApplyFailurePenalties(quest);
            GameEventRelay.Instance.OnQuestStatusChanged.Invoke(quest);
            return;
        } 
        Debug.LogWarning($"[QuestManager] ForceFailQuest: '{questId}' not found in InProgress.");
    }

    public bool SubmitApplication(QuestData quest, QuestApplication application)
    {
        if (!quest.AddApplication(application))
            return false;
        GameEventRelay.Instance.OnApplicationSubmitted.Invoke(application);
        Debug.Log("[QuestManager] SubmitApplication called.");
        return true;
    }
    #endregion
    
    #region Board Management
    // Places an unposted quest into a specific board slot and starts its countdown.
    // Called by the board UI when the player drops a quest card onto an empty slot.
    public bool PostQuestToSlot(QuestData quest, int slotIndex)
    {
        if (quest == null)
        {
            Debug.LogWarning("[QuestManager] PostQuestToSlot received a null quest.");
            return false;
        }
        if (quest.Status != QuestStatus.Unposted)
        {
            Debug.LogWarning($"[QuestManager] '{quest.QuestName}' cannot be posted " +
                             $"— status is {quest.Status}.");
            return false;
        }
        if (slotIndex < 0 || slotIndex >= _boardSlots.Length)
        {
            Debug.LogWarning($"[QuestManager] Slot index {slotIndex} is out of range " +
                             $"(board size: {_boardSlots.Length}).");
            return false;
        }
        if (_boardSlots[slotIndex] != null)
        {
            Debug.LogWarning($"[QuestManager] Board slot {slotIndex} is occupied.");
            return false;
        }

        if (!quest.Post(GetCurrentGameHours())) return false;

        _boardSlots[slotIndex] = quest;
        _unpostedQuests.Remove(quest);

        GameEventRelay.Instance?.OnUnpostedQuestsChanged.Invoke();
        GameEventRelay.Instance?.OnBoardChanged.Invoke();
        GameEventRelay.Instance?.OnQuestStatusChanged.Invoke(quest);
        return true;
    }
    // Returns the quest in a given slot or null if the slot is empty or the index is invalid.
    public QuestData GetBoardSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _boardSlots.Length)
            return null;
        return _boardSlots[slotIndex];
    }
    // Returns the number of currently empty board slots.
    public int GetEmptySlotCount()
        => _boardSlots.Count(q => q == null);
    // Finds which slot index a quest occupies, or -1 if it is not on the board.
    private int FindBoardSlotIndex(QuestData quest)
    {
        for (var i = 0; i < _boardSlots.Length; i++)
            if (_boardSlots[i] == quest)
                return i;
        return -1;
    }
    // Clears a quest from its board slot. Does not change quest status.
    private void FreeBoardSlot(QuestData quest)
    {
        var slot = FindBoardSlotIndex(quest);
        if (slot < 0)
            return;
        _boardSlots[slot] = null;
        GameEventRelay.Instance?.OnBoardChanged.Invoke();
    }
    // Checks all Posted quests on the board and expires those whose deadline has passed.
    // Iterates by index to allow safe null-assignment mid-loop.
    private void CheckBoardQuestExpiry()
    {
        var currentHour = GetCurrentGameHours();
        var boardDirty = false;

        for (var i = 0; i < _boardSlots.Length; i++)
        {
            var quest = _boardSlots[i];
            if (quest is not { Status: QuestStatus.Posted })
                continue;
            if (!quest.IsDeadlinePassed(currentHour))
                continue;

            quest.Expire();
            _boardSlots[i] = null;
            _resolvedQuests.Add(quest);
            boardDirty = true;
            // Reputation: expired jobs signal a guild that cannot attract adventurers.
            // Penalty is smaller than an active failure — the job just wasn't taken.
            if (questConfig)
            {
                var expiryPenalty = questConfig.GetRankConfig(quest.Rank).reputationExpiryPenalty;
                if (expiryPenalty > 0)
                    ReputationSystem.Instance?.ChangeReputation(-expiryPenalty);
            }
            
            GameEventRelay.Instance?.OnQuestStatusChanged.Invoke(quest);
        }

        if (boardDirty)
            GameEventRelay.Instance?.OnBoardChanged.Invoke();
    }
    #endregion
    
    #region Application Management
    // Approves an application, dispatches the party, and frees the board slot.
    // All other pending applications on the same quest are automatically rejected.
    public bool ApproveApplication(QuestApplication application)
    {
        if (application == null)
        {
            Debug.LogWarning("[QuestManager] ApproveApplication received a null application.");
            return false;
        }
        if (application.Status != ApplicationStatus.Pending)
        {
            Debug.LogWarning($"[QuestManager] Application {application.ApplicationId} " +
                             $"is not Pending (status: {application.Status}).");
            return false;
        }
        
        var quest = FindPostedQuestById(application.QuestId);
        if (quest == null)
        {
            Debug.LogError($"[QuestManager] No Posted quest found for application " +
                           $"{application.ApplicationId} (quest ID: {application.QuestId}).");
            return false;
        }
        
        var currentHour = GetCurrentGameHours();
        var resolveAtHour = CalculateResolveAtHour(quest, application.PartyStrength, currentHour);
        
        // Dispatch calls application.Approve() and quest.RejectRemainingApplications() internally.
        if (!quest.Dispatch(currentHour, resolveAtHour, application))
            return false;
        
        FreeBoardSlot(quest);
        _inProgressQuests.Add(quest);

        GameEventRelay.Instance?.OnBoardChanged.Invoke();
        GameEventRelay.Instance?.OnQuestStatusChanged.Invoke(quest);
        return true;
    }
    // Manually rejects a pending application. The quest stays on the board.
    public bool RejectApplication(QuestApplication application)
    {
        if (application is not { Status: ApplicationStatus.Pending })
        {
            Debug.LogWarning("[QuestManager] RejectApplication called on a null or " +
                             "non-pending application.");
            return false;
        }
        application.Reject();
        return true;
    }
    #endregion
    
    #region Fallback Application Simulator
    // Runs only when AdventurerManager.Instance is null (e.g. testing quests in isolation).
    // Each posted quest has a season-adjusted chance per hour to receive a dummy application.
    private void SimulateFallbackApplications()
    {
        var seasonMod = GetSeasonApplicationModifier();

        foreach (var quest in _boardSlots)
        {
            if (quest is not { Status: QuestStatus.Posted })
                continue;
            if (Random.value < baseApplicationChancePerHour * seasonMod)
                SubmitFallbackApplication(quest);
        }
    }
    
    private void SubmitFallbackApplication(QuestData quest)
    {
        var threshold = questConfig.GetRankPowerThreshold(quest.Rank);
        var partyStrength = Random.Range(threshold * 0.5f, threshold * 1.5f);
        var partySize = Random.Range(1, Mathf.Min(quest.PartyLimit, 5) + 1);

        var memberIds = new string[partySize];
        for (var i = 0; i < partySize; i++)
            memberIds[i] = $"fallback_adventurer_{Random.Range(1, 1000)}";

        var leaderId = memberIds[0];
        var isTemp = partySize > 1;
        var successChance = CalculateSuccessChance(quest, partyStrength);
        var currentHour = GetCurrentGameHours();

        var application = new QuestApplication(
            quest.QuestId, memberIds, leaderId, isTemp,
            partyStrength, successChance, currentHour
        );

        if (quest.AddApplication(application))
            GameEventRelay.Instance?.OnApplicationSubmitted.Invoke(application);
    }
    
    private float GetSeasonApplicationModifier()
    {
        if (!TimeManager.Instance)
            return 1f;
        var index = (int)TimeManager.Instance.GetCurrentSeason();
        if (seasonApplicationRateModifiers == null || index >= seasonApplicationRateModifiers.Length)
            return 1f;
        return seasonApplicationRateModifiers[index];
    }
    #endregion
    
    #region Formulas
    // Computes the probability of a party succeeding on a quest.
    // Class affinity modifiers are left as zero until adventurer class data exists.
    // This method is public, so the UI can call it when displaying application details.
    public float CalculateSuccessChance(QuestData quest, float partyStrength)
    {
        if (!questConfig)
            return 0.5f;

        var threshold = Mathf.Max(1f, questConfig.GetRankPowerThreshold(quest.Rank));
        var efficiency = Mathf.Clamp(partyStrength / threshold, 0.5f, 2f);
        // Linear interpolation: 0.15 at minimum efficiency (0.5), 0.9 at maximum (2).
        var t = (efficiency - 0.5f) / 1.5f;
        var baseChance = Mathf.Lerp(0.15f, 0.9f, t);

        return Mathf.Clamp(baseChance,
            questConfig.MinSuccessChance,
            questConfig.MaxSuccessChance
        );
    }
    // Computes when the quest will resolve, measured from the moment of dispatch.
    // More efficient parties finish sooner; the result is clamped, so it never equals or exceeds the quest's expiry (which would merge success and deadline failure).
    private float CalculateResolveAtHour(QuestData quest, float partyStrength, float currentHour)
    {
        if (!questConfig)
            return currentHour + quest.TimeLimitHours;
        
        var remainingHours = quest.GetRemainingHours(currentHour);
        var threshold = Mathf.Max(1f, questConfig.GetRankPowerThreshold(quest.Rank));
        var efficiency = Mathf.Clamp(partyStrength / threshold, 0.5f, 2f);
        // Higher efficiency → lower completionRatio → faster resolution.
        var t = (efficiency - 0.5f) / 1.5f;
        var completionRatio = Mathf.Lerp(questConfig.MaxCompletionRatio,
            questConfig.MinCompletionRatio,
            t
        );
        
        var rawHours = remainingHours * completionRatio;
        var variance = rawHours * questConfig.CompletionTimeVariance;
        var finalHours = rawHours + Random.Range(-variance, variance);
        // Clamp: at least a minimal resolution time, at most just under the expiry.
        // Separating the two clamps avoids Mathf.Clamp(x, imn, max) with nim > max when the remainingHours variable is shorter than the minimum floor.
        var safeMin = Mathf.Min(0.1f, remainingHours * 0.5f);
        var safeMax = Mathf.Max(finalHours, remainingHours - 0.5f);
        finalHours = Mathf.Clamp(finalHours, safeMin, safeMax);
        
        return currentHour + finalHours;
    }
    #endregion
    
    #region Quest Resolution
    // Checks all in-progress quests and resolves any that have reached their resolve or whose deadline has passed.
    // Iterates backward for safe list mutation.
    private void CheckQuestResolution()
    {
        var currentHour = GetCurrentGameHours();

        for (var i = _inProgressQuests.Count - 1; i >= 0; i--)
        {
            var quest = _inProgressQuests[i];
            
            var deadlinePassed = quest.IsDeadlinePassed(currentHour);
            var resolveTimeMet = quest.ResolveAtHour > 0f && currentHour >= quest.ResolveAtHour;
            
            if (!deadlinePassed && !resolveTimeMet)
                continue;
            
            _inProgressQuests.RemoveAt(i);
            _resolvedQuests.Add(quest);

            if (deadlinePassed && !resolveTimeMet)
            {
                // The clock ran out before the planned resolve: automatic failure.
                quest.Fail();
                ApplyFailurePenalties(quest);
            }
            else
            {
                ResolveQuestOutcome(quest);
            }
            
            GameEventRelay.Instance?.OnQuestStatusChanged.Invoke(quest);
        }
    }
    
    // Rolls against the stored success chance to determine the final outcome.
    private void ResolveQuestOutcome(QuestData quest)
    {
        var successChance = quest.ApprovedApplication?.SuccessChance ?? 0f;
        
        if (Random.value <= successChance)
        {
            quest.Complete();
            DistributeRewards(quest);
        }
        else
        {
            quest.Fail();
            ApplyFailurePenalties(quest);
        }
    }
    
    // Sends guild and adventurer rewards on a successful completion.
    private void DistributeRewards(QuestData quest)
    {
        if (quest.GuildReward > 0)
            AddGuildFunds(quest.GuildReward);

        if (questConfig)
        {
            var repGain = questConfig.GetRankConfig(quest.Rank).reputationReward;
            if (repGain > 0)
                ReputationSystem.Instance?.ChangeReputation(repGain);
        }

        var partySize = quest.ApprovedApplication?.PartySize ?? 1;
        var perMember = quest.GetGoldPerMember(partySize);
        var leaderBonus = quest.GetLeaderBonus(partySize);
        var bonusNote = leaderBonus > 0 ? $", leader +{leaderBonus}g bonus" : string.Empty;
        var repGainLog = questConfig ? questConfig.GetRankConfig(quest.Rank).reputationReward : 0;
        Debug.Log($"[QuestManager] '{quest.QuestName}' completed. " +
                  $"Guild +{quest.GuildReward}g | " +
                  $"Reputation +{repGainLog} | " +
                  $"Each adventurer +{perMember}g{bonusNote}.");
    }
    
    // Applies guild reputation loss on quest failure.
    // Injury and death consequences are deferred to the injury/death system;
    // AdventurerManager handles adventurer-side failure consequences (party deterioration, rank-up cooldown resets) via its OnQuestStatusChanged subscription.
    private void ApplyFailurePenalties(QuestData quest)
    {
        if (questConfig)
        {
            var repLoss = questConfig.GetRankConfig(quest.Rank).reputationFailurePenalty;
            if (repLoss > 0)
                ReputationSystem.Instance?.ChangeReputation(-repLoss);

            Debug.Log($"[QuestManager] '{quest.QuestName}' failed. " +
                      $"Reputation -{repLoss}. " +
                      $"Adventurer injury/death consequences pending that system.");
        }
        else
        {
            Debug.Log($"[QuestManager] '{quest.QuestName}' failed.");
        }
    }
    #endregion
    
    #region Guild Funds
    public void AddGuildFunds(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[QuestManager] AddGuildFunds called with negative amount ({amount}). " +
                             $"Use SpendGuildFunds to deduct.");
            return;
        }
        _guildFunds += amount;
        GameEventRelay.Instance?.OnGuildFundsChanged.Invoke(_guildFunds);
    }
    
    // Attempts to spend guild funds. Returns false without modifying state if insufficient.
    public bool SpendGuildFunds(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"[QuestManager] SpendGuildFunds called with negative amount ({amount}).");
            return false;
        }
        if (amount > _guildFunds)
        {
            Debug.LogWarning($"[QuestManager] Cannot spend {amount}g — guild only has {_guildFunds}g.");
            return false;
        }
        _guildFunds -= amount;
        GameEventRelay.Instance?.OnGuildFundsChanged.Invoke(_guildFunds);
        return true;
    }
    #endregion
    
    #region Utility
    // Searches board slots for a quest in the Posted state with a matching ID.
    // Only Posted quests are eligible for dispatch; InProgress quests are no longer on the board.
    private QuestData FindPostedQuestById(string questId)
        => _boardSlots.FirstOrDefault(q => q is { Status: QuestStatus.Posted } && q.QuestId == questId);
    #endregion
}