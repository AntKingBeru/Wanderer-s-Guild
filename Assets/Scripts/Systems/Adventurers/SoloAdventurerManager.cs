// Owns the adventurer roster and drives all per-adventurer logic:
//   - Factory timer (spawning new arrivals)
//   - Quest application simulation for solo adventurers and party leaders
//   - Quest outcome distribution (XP, rank points, gold)
//   - Early quest failure checks
//   - Rank-up application lifecycle
//   - Nightly maintenance (sleep/food)
// Party-specific operations are delegated to PartyManager.
// All outbound events are raised through GameEventRelay.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoloAdventurerManager : MonoBehaviour
{
    public static SoloAdventurerManager Instance { get; private set; }

    #region Inspector
    [Header("Configuration")]
    [Tooltip("Adventurer system config asset.")]
    [SerializeField] private AdventurerConfig config;

    [Tooltip("Quest system config — rank thresholds, XP rewards, success chance bounds.")]
    [SerializeField] private QuestConfig questConfig;
    #endregion

    #region Runtime State
    // Master roster of all registered adventurers.
    private readonly List<AdventurerData> _adventurers = new();

    // All rank-up applications (pending and resolved) for history and lookup.
    private readonly List<RankUpApplicationData> _rankUpApplications = new();

    // Active adventurer factory — swap to change generation behavior.
    private RandomAdventurerFactory _factory;

    // Absolute game-hour at which the next adventurer will arrive.
    private float _nextArrivalHour = -1f;
    #endregion

    #region Public Accessors
    public AdventurerConfig Config => config;
    public IReadOnlyList<AdventurerData> Adventurers => _adventurers;

    // Returns only applications that are still awaiting player review.
    public IReadOnlyList<RankUpApplicationData> PendingRankUpApplications
        => _rankUpApplications.Where(a => a.Status == ApplicationStatus.Pending).ToList();
    #endregion
    
    #region Lifecycle
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (!GameEventRelay.Instance)
            return;
        // Subscribe to time ticks through the relay — never miss a tick regardless of enable order.
        GameEventRelay.Instance.OnHourChanged.AddListener(HandleHourChanged);
        GameEventRelay.Instance.OnDayChanged.AddListener(HandleDayChanged);
        // React to quest state transitions to update adventurer statuses and reward members.
        GameEventRelay.Instance.OnQuestStatusChanged.AddListener(HandleQuestStatusChanged);
    }

    private void OnDisable()
    {
        if (!GameEventRelay.Instance)
            return;
        GameEventRelay.Instance.OnHourChanged.RemoveListener(HandleHourChanged);
        GameEventRelay.Instance.OnDayChanged.RemoveListener(HandleDayChanged);
        GameEventRelay.Instance.OnQuestStatusChanged.RemoveListener(HandleQuestStatusChanged);
    }

    private void Start()
    {
        if (!config || !questConfig)
        {
            Debug.LogError("[SoloAdventurerManager] Config or QuestConfig not assigned. Disabling.");
            enabled = false;
            return;
        }

        _factory = new RandomAdventurerFactory(config);
        _factory.OnFactoryActivated();
        ScheduleNextArrival(GetCurrentGameHours());
    }
    #endregion
    
    #region Time Event Handlers
    private void HandleHourChanged(int hour)
    {
        var currentHour = GetCurrentGameHours();

        CheckFactoryTime(currentHour);
        CheckEarlyQuestFailures();
        CheckRankUpQuestCompletions(currentHour);
        CheckRankUpReapplyEligible(currentHour);

        // Application simulation runs only during the configured window hours.
        var inWindow = hour >= questConfig.ApplicationWindowStartHour
                       && hour <= questConfig.ApplicationWindowEndHour;
        if (inWindow)
            SimulateApplications(currentHour);
    }

    private void HandleDayChanged(int day)
        => PerformMaintenanceChecks();
    #endregion
    
    #region Game-Time Helper
    // Centralized now on TimeManager; this is a thin pass-through kept here
    // so callers inside this file remain readable.
    private static float GetCurrentGameHours()
        => TimeManager.Instance ? TimeManager.Instance.GetTotalGameHours() : 0f;
    #endregion

    #region Factory — Adventurer Spawning
    // Forces the factory to spawn one adventurer immediately.
    // Used by AdventurerWorldManager for the two Day-1 starter adventurers.
    // After starters are spawned, the regular timer cycle continues normally.
    public void SpawnStarterAdventurer()
    {
        var context  = BuildCreationContext();
        var newcomer = _factory?.CreateAdventurer(context);
        if (newcomer != null)
            RegisterAdventurer(newcomer);
    }
    
    // Checks whether it is time for the next arrival and spawns one if so.
    private void CheckFactoryTime(float currentHour)
    {
        if (_nextArrivalHour < 0f || currentHour < _nextArrivalHour)
            return;

        var context  = BuildCreationContext();
        var newcomer = _factory?.CreateAdventurer(context);
        if (newcomer != null)
            RegisterAdventurer(newcomer);

        ScheduleNextArrival(currentHour);
    }

    // Schedules the next arrival based on the configurable random day range.
    private void ScheduleNextArrival(float fromHour)
    {
        var days = UnityEngine.Random.Range(config.ArrivalRateMinDays, config.ArrivalRateMaxDays);
        _nextArrivalHour = fromHour + days * 24f;
    }

    // Builds a snapshot of the current guild roster for factory weighting.
    private AdventurerCreationContext BuildCreationContext()
    {
        var context = new AdventurerCreationContext
        {
            GuildRankCap = config.GuildRankCap,
            TotalAdventurerCount = _adventurers.Count
        };
        foreach (var adv in _adventurers)
        {
            var ci = (int)adv.Class;
            var ri = (int)adv.Rank;
            if (ci < context.AdventurersPerClass.Length) context.AdventurersPerClass[ci]++;
            if (ri < context.AdventurersPerRank.Length)  context.AdventurersPerRank[ri]++;
        }
        return context;
    }

    // Adds the adventurer to the roster and broadcasts arrival.
    private void RegisterAdventurer(AdventurerData adventurer)
    {
        _adventurers.Add(adventurer);
        if (GameEventRelay.Instance)
        {
            GameEventRelay.Instance.OnAdventurerArrived.Invoke(adventurer);
            GameEventRelay.Instance.OnRosterChanged.Invoke();
        }
    }
    #endregion
    
    #region Quest Application Simulation
    // Each in-window hour, idle adventurers and party leaders roll to submit applications.
    private void SimulateApplications(float currentHour)
    {
        if (!QuestManager.Instance)
            return;

        var seasonMod = config.GetSeasonApplicationModifier(TimeManager.Instance.GetCurrentSeason());
        var baseChance = config.BaseApplicationChancePerHour * seasonMod;

        // Track parties already processed this tick so only the leader submits once.
        var processedParties = new HashSet<string>();

        foreach (var adv in _adventurers)
        {
            if (adv.Status != AdventurerStatus.Idle)
                continue;

            var isLeader = !string.IsNullOrEmpty(adv.PartyId) && adv.IsPartyLeader;
            var isSolo = string.IsNullOrEmpty(adv.PartyId);

            if (isLeader)
            {
                if (!processedParties.Add(adv.PartyId))
                    continue;
                var party = PartyManager.Instance?.GetParty(adv.PartyId);
                if (party == null)
                    continue;
                if (UnityEngine.Random.value < baseChance)
                    TryApplyAsParty(adv, party, currentHour);
            }
            else if (isSolo)
            {
                if (UnityEngine.Random.value < baseChance)
                    TryApplySolo(adv, currentHour);
            }
        }
    }

    // Submits a solo application for the given adventurer. Public so the player can
    // also trigger manual applications from the UI if needed.
    public bool TryApplySolo(AdventurerData adventurer, float currentHour)
    {
        var quest = FindSuitableQuest(adventurer.Rank, 1, adventurer.Class);
        if (quest == null)
            return false;

        var power = adventurer.CalculatePower(config);
        var successChance = CalculateSuccessChance(quest, new[] { adventurer });
        var application = new QuestApplication(
            quest.QuestId,
            new[] { adventurer.Id },
            adventurer.Id,
            false,
            power,
            successChance,
            currentHour
        );

        if (!QuestManager.Instance.SubmitApplication(quest, application))
            return false;
        adventurer.ApplyToQuest(application.ApplicationId);
        // Notify the world layer so the adventurer walks to the board visually.
        GameEventRelay.Instance.OnAdventurerApplicationSubmitted.Invoke(adventurer.Id);
        return true;
    }

    // Submits a party application; skips if any member is not idle.
    private void TryApplyAsParty(AdventurerData leader, PartyData party, float currentHour)
    {
        var members = PartyManager.Instance.GetPartyMembers(party.PartyId);
        if (members.Any(m => m.Status != AdventurerStatus.Idle))
            return;
        if (members.Count == 0)
            return;

        var totalRankInt = 0;
        var lowestRank = members[0].Rank;
        foreach (var m in members)
        {
            totalRankInt += (int)m.Rank;
            if ((int)m.Rank < (int)lowestRank) lowestRank = m.Rank;
        }
        var avgRank = (QuestRank)(totalRankInt / members.Count);
        var maxPartySize = config.GetPartyMaxSize(lowestRank);
        if (members.Count > maxPartySize)
            return;

        var quest = FindSuitableQuest(avgRank, members.Count, leader.Class);
        if (quest == null)
            return;

        var partyStrength = members.Sum(m => m.CalculatePower(config));
        var successChance = CalculateSuccessChance(quest, members);
        var memberIds = members.Select(m => m.Id).ToArray();
        var application = new QuestApplication(
            quest.QuestId,
            memberIds,
            leader.Id,
            party.IsTemporary,
            partyStrength,
            successChance,
            currentHour
        );

        if (!QuestManager.Instance.SubmitApplication(quest, application))
            return;
        foreach (var m in members)
        {
            m.ApplyToQuest(application.ApplicationId);
            GameEventRelay.Instance.OnAdventurerApplicationSubmitted.Invoke(m.Id);
        }
            
    }

    // Weighted random quest selection from the board that fits rank and party size.
    private QuestData FindSuitableQuest(QuestRank effectiveRank, int partySize, AdventurerClass advClass)
    {
        var posted = GetAllPostedQuests();
        var classData = config.GetClassData(advClass);
        var eligible = new List<(QuestData quest, float weight)>();

        foreach (var quest in posted)
        {
            var qr = (int)quest.Rank;
            var er = (int)effectiveRank;
            if (qr < er || qr > er + 1)
                continue;
            if (quest.ApprovedApplication != null)
                continue;
            if (partySize > quest.PartyLimit)
                continue;

            var weight = 1f;
            if (classData)
            {
                weight = classData.GetAffinity(quest.Category) switch
                {
                    CategoryAffinity.Preferred => 3f,
                    CategoryAffinity.Disliked => 0.5f,
                    _ => 1f
                };
            }
            eligible.Add((quest, weight));
        }

        if (eligible.Count == 0)
            return null;

        var total = eligible.Sum(e => e.weight);
        var roll = UnityEngine.Random.Range(0f, total);
        var cumulative = 0f;
        foreach (var (quest, weight) in eligible)
        {
            cumulative += weight;
            if (roll < cumulative)
                return quest;
        }
        return eligible[^1].quest;
    }
    #endregion
    
    #region Application Management
    // Forwards approval to QuestManager and transitions member statuses.
    public bool ApproveQuestApplication(QuestApplication application)
        => QuestManager.Instance && QuestManager.Instance.ApproveApplication(application);

    // Rejects the application and cancels pending state on all members.
    public bool RejectQuestApplication(QuestApplication application)
    {
        if (!QuestManager.Instance)
            return false;
        if (!QuestManager.Instance.RejectApplication(application))
            return false;

        foreach (var id in application.PartyMemberIds)
        {
            var adv = GetAdventurer(id);
            if (adv?.CurrentApplicationId == application.ApplicationId)
                adv?.CancelQuestApplication();
        }
        return true;
    }
    #endregion
    
    #region Quest Status Handlers
    private void HandleQuestStatusChanged(QuestData quest)
    {
        switch (quest.Status)
        {
            case QuestStatus.InProgress:
                OnQuestDispatched(quest);
                break;
            case QuestStatus.Completed:
                OnQuestCompleted(quest);
                break;
            case QuestStatus.Failed:
                OnQuestFailed(quest);
                break;
            case QuestStatus.Expired:
                OnQuestExpired(quest);
                break;
        }
    }

    // Dispatches all approved applicants to the quest.
    private void OnQuestDispatched(QuestData quest)
    {
        if (quest.ApprovedApplication == null)
            return;
        quest.ApprovedApplication.ResetEarlyFailureMarks();
        foreach (var id in quest.ApprovedApplication.PartyMemberIds)
        {
            var adv = GetAdventurer(id);
            if (adv?.Status == AdventurerStatus.AppliedToQuest)
                adv.DispatchToQuest(quest.QuestId);
        }
    }

    // Distributes rewards, returns adventurers, and triggers party deterioration.
    private void OnQuestCompleted(QuestData quest)
    {
        var members = GetQuestMembers(quest);
        if (members.Count == 0)
            return;

        DistributeQuestRewards(quest, members);
        ReturnAdventurersFromQuest(members, quest.QuestId);

        var party = GetPartyFromApplication(quest.ApprovedApplication);
        if (party != null)
        {
            party.RecordSuccess();
            PartyManager.Instance?.CheckPartyDeterioration(party, true);
        }
        else
        {
            // Solo adventurers still need their success counter ticked.
            foreach (var m in members)
                m.OnRegularQuestSucceeded();
        }

        GameEventRelay.Instance?.OnRosterChanged.Invoke();
    }

    private void OnQuestFailed(QuestData quest)
    {
        var members = GetQuestMembers(quest);
        ReturnAdventurersFromQuest(members, quest.QuestId);

        // TODO: apply injury/death penalties here.

        var party = GetPartyFromApplication(quest.ApprovedApplication);
        if (party != null)
        {
            party.RecordFailure();
            PartyManager.Instance?.CheckPartyDeterioration(party, false);
        }

        GameEventRelay.Instance?.OnRosterChanged.Invoke();
    }

    // Cancels pending applications from all members of an expired quest.
    private void OnQuestExpired(QuestData quest)
    {
        foreach (var app in quest.Applications)
        {
            if (app.Status != ApplicationStatus.Pending)
                continue;
            foreach (var id in app.PartyMemberIds)
            {
                var adv = GetAdventurer(id);
                if (adv?.CurrentApplicationId == app.ApplicationId)
                    adv?.CancelQuestApplication();
            }
        }
    }

    // Returns all adventurers who were dispatched on this quest.
    private List<AdventurerData> GetQuestMembers(QuestData quest)
    {
        var members = new List<AdventurerData>();
        if (quest.ApprovedApplication == null)
            return members;
        members.AddRange(
            quest.ApprovedApplication.PartyMemberIds
                .Select(GetAdventurer)
                .Where(a => a != null)
        );
        return members;
    }

    // Returns all dispatched members for this quest ID back to Idle status.
    private void ReturnAdventurersFromQuest(List<AdventurerData> members, string questId)
    {
        foreach (var m in members.Where(m => m.Status == AdventurerStatus.OnQuest && m.CurrentQuestId == questId))
            m.ReturnFromQuest();
    }

    // Calculates and distributes gold, XP, rank points to each quest member.
    private void DistributeQuestRewards(QuestData quest, List<AdventurerData> members)
    {
        var leaderId = quest.ApprovedApplication?.LeaderId;
        var partySize = members.Count;
        var goldEach = quest.GetGoldPerMember(partySize);
        var leaderBonus = quest.GetLeaderBonus(partySize);
        var rankPoints = CalculateQuestRankPoints(quest);

        foreach (var m in members)
        {
            m.AddGold(goldEach + (m.Id == leaderId ? leaderBonus : 0));

            var classData = config.GetClassData(m.Class);
            var xp = CalculateQuestXp(quest, m, classData);
            var leveledUp = classData && m.AddExperience(xp, classData, config);

            if (leveledUp)
                GameEventRelay.Instance?.OnAdventurerLeveledUp.Invoke(m);

            var becameEligible = m.AddRankPoints(rankPoints, config);
            if (becameEligible)
            {
                CreateRankUpApplication(m, GetCurrentGameHours());
                GameEventRelay.Instance?.OnRankUpEligibilityGained.Invoke(m);
            }
            m.OnRegularQuestSucceeded();
        }
    }
    #endregion
    
    #region Early Quest Failure
    // Each hour, in-progress quests have a chance to accumulate failure marks.
    // Too many marks forces an immediate failure.
    private void CheckEarlyQuestFailures()
    {
        if (!QuestManager.Instance) return;
        var inProgress = new List<QuestData>(QuestManager.Instance.InProgressQuests);
        foreach (var quest in from quest in inProgress
                 where quest.ApprovedApplication != null let chance = (1f - quest.ApprovedApplication.SuccessChance) * config.EarlyFailureCoefficient
                 where UnityEngine.Random.value < chance select quest)
        {
            quest.ApprovedApplication.AddEarlyFailureMark();
            if (quest.ApprovedApplication.EarlyFailureMarks >= config.EarlyFailureMarksRequired)
                QuestManager.Instance.ForceFailQuest(quest.QuestId);
        }
    }
    #endregion
    
    #region Rank-Up Quest
    // Creates a rank-up application if the adventurer doesn't already have one pending.
    private void CreateRankUpApplication(AdventurerData adventurer, float currentHour)
    {
        if (!string.IsNullOrEmpty(adventurer.RankApplicationId))
            return;

        var classData = config.GetClassData(adventurer.Class);
        var targetRank = (QuestRank)Mathf.Min((int)adventurer.Rank + 1, Enum.GetNames(typeof(QuestRank)).Length - 1);
        var category = classData?.RankUpCategory ?? QuestCategory.Combat;
        var duration = classData?.GetRankUpDuration(adventurer.Rank) ?? 24f;
        var successChance = CalculateRankUpSuccessChance(adventurer, targetRank, classData);

        var application = new RankUpApplicationData(
            adventurer.Id, adventurer.Rank, category, duration, successChance, currentHour
        );
        _rankUpApplications.Add(application);
        adventurer.SetRankUpApplication(application.ApplicationId);
        GameEventRelay.Instance?.OnRankUpApplicationCreated.Invoke(application);
    }

    // Player approves a rank-up application; dispatches the adventurer to their quest.
    public bool ApproveRankUpApplication(string applicationId)
    {
        var application = FindRankUpApplication(applicationId);
        if (application == null)
            return false;
        var adventurer = GetAdventurer(application.AdventurerId);
        if (adventurer == null)
            return false;

        var currentHour = GetCurrentGameHours();
        if (!application.Approve(currentHour))
            return false;

        adventurer.DispatchToRankUpQuest(application.StartHour, application.EndHour);
        GameEventRelay.Instance?.OnRankUpApplicationResolved.Invoke(application);
        return true;
    }

    // Player rejects a rank-up application; removes it and resets adventurer state.
    public bool RejectRankUpApplication(string applicationId)
    {
        var application = FindRankUpApplication(applicationId);
        if (application == null)
            return false;
        if (!application.Reject())
            return false;

        GetAdventurer(application.AdventurerId)?.ClearRankUpApplication();
        _rankUpApplications.Remove(application);
        GameEventRelay.Instance?.OnRankUpApplicationResolved.Invoke(application);
        return true;
    }

    // Hourly check: resolve rank-up quests whose end hour has passed.
    private void CheckRankUpQuestCompletions(float currentHour)
    {
        foreach (var adv in _adventurers)
        {
            if (!adv.OnRankUpQuest)
                continue;
            if (!adv.IsRankUpQuestComplete(currentHour))
                continue;

            var classData = config.GetClassData(adv.Class);
            var targetRank = (QuestRank)Mathf.Min((int)adv.Rank + 1, Enum.GetNames(typeof(QuestRank)).Length - 1);
            var chance = CalculateRankUpSuccessChance(adv, targetRank, classData);

            if (UnityEngine.Random.value < chance)
            {
                adv.CompleteRankUpQuest();
                if (classData)
                {
                    var xp = questConfig.GetRankBaseXp(targetRank);
                    var levelUp = adv.AddExperience(xp, classData, config);
                    if (levelUp)
                        GameEventRelay.Instance?.OnAdventurerLeveledUp.Invoke(adv);
                }
                GameEventRelay.Instance?.OnAdventurerRankUp.Invoke(adv);
                Debug.Log($"[SoloAdventurerManager] {adv.Name} ranked up to {adv.Rank}!");
            }
            else
            {
                var cooldownDays  = TimeManager.Instance
                    ? TimeManager.Instance.DaysPerMonth * config.RankUpRetryCooldownMonthFraction
                    : 15f;
                adv.FailRankUpQuest(currentHour + cooldownDays * 24f, config);
                GameEventRelay.Instance?.OnAdventurerRankUpFailed.Invoke(adv);
                Debug.Log($"[SoloAdventurerManager] {adv.Name} failed their rank-up quest.");
            }
            GameEventRelay.Instance?.OnRosterChanged.Invoke();
        }
    }

    // Hourly check: re-enable rank-up eligibility for adventurers past their cooldown.
    private void CheckRankUpReapplyEligible(float currentHour)
    {
        foreach (var adv in from adv in _adventurers
                 where !adv.RankUpEligible where adv.CanReapplyForRankUp(currentHour)
                 where adv.RankPoints >= config.GetRankPointThreshold(adv.Rank) select adv)
        {
            adv.SetRankUpEligible(true);
            CreateRankUpApplication(adv, currentHour);
            GameEventRelay.Instance?.OnRankUpEligibilityGained.Invoke(adv);
        }
    }

    private float CalculateRankUpSuccessChance(AdventurerData adv, QuestRank targetRank, ClassData classData)
    {
        var power = adv.CalculatePower(config);
        var threshold = Mathf.Max(1f, questConfig.GetRankPowerThreshold(targetRank));
        var efficiency = Mathf.Clamp(power / threshold, 0.5f, 2f);
        var t = (efficiency - 0.5f) / 1.5f;
        var baseChance = Mathf.Lerp(0.15f, 0.9f, t);

        if (classData)
        {
            var affinity = classData.GetAffinity(classData.RankUpCategory);
            switch (affinity)
            {
                case CategoryAffinity.Preferred:
                    baseChance += config.PreferredClassBonus;
                    break;
                case CategoryAffinity.Disliked:
                    baseChance -= config.DislikedClassPenalty;
                    break;
            }
        }

        baseChance -= adv.GetMaintenancePenalty(config);
        return Mathf.Clamp(baseChance, questConfig.MinSuccessChance, questConfig.MaxSuccessChance);
    }
    #endregion
    
    #region Maintenance
    // Nightly: apply sleep penalties to unhoused adventurers; reset food state.
    private void PerformMaintenanceChecks()
    {
        foreach (var adv in _adventurers.Where(a => a.Status != AdventurerStatus.Dead))
        {
            if (adv.LodgingState == LodgingState.Nowhere)
                adv.RecordSleepMissed();
            else
                adv.ResetSleep();
            // TODO: add food checks once the tavern system is built.
            adv.ResetFood();
        }
    }
    
    // Called by BuildManager when a room with stat boosts finishes construction.
    // Recalculates every adventurer's effective stats using the new cumulative boosts.
    public void RecalculateAllStatBoosts()
    {
        // TODO: iterate all adventurers, re-apply BuildManager.Instance.GetTotalStatBoosts()
        // on top of base ClassData stats. Implement fully when stat-boost UI is built.
        Debug.Log("[AdventurerManager] RecalculateAllStatBoosts called — stub pending full implementation.");
    }

    // Called by BuildManager when new bed slots become available.
    // Assigns homeless (LodgingState.Nowhere) adventurers to in-guild lodging.
    public void OnBedsAdded(int newBeds)
    {
        // TODO: sort Nowhere adventurers by seniority or rank and assign beds up to the new cap.
        Debug.Log($"[AdventurerManager] OnBedsAdded({newBeds}) called — stub pending bed-assignment logic.");
    }
    #endregion

    #region Success Chance Calculation
    // Public so UI and external systems can preview success odds.
    public float CalculateSuccessChance(QuestData quest, IEnumerable<AdventurerData> members)
    {
        if (quest == null)
            return 0f;

        var partyStrength = 0f;
        var affinityModifier = 0f;
        var maintenancePenalty = 0f;
        var count = 0;

        foreach (var m in members)
        {
            partyStrength += m.CalculatePower(config);
            maintenancePenalty += m.GetMaintenancePenalty(config);
            var classData = config.GetClassData(m.Class);
            if (classData)
            {
                var affinity = classData.GetAffinity(quest.Category);
                switch (affinity)
                {
                    case CategoryAffinity.Preferred:
                        affinityModifier += config.PreferredClassBonus;
                        break;
                    case CategoryAffinity.Disliked:
                        affinityModifier -= config.DislikedClassPenalty;
                        break;
                }
            }
            count++;
        }

        if (count == 0)
            return 0f;
        maintenancePenalty /= count;

        var threshold = Mathf.Max(1f, questConfig.GetRankPowerThreshold(quest.Rank));
        var efficiency = Mathf.Clamp(partyStrength / threshold, 0.5f, 2f);
        var t = (efficiency - 0.5f) / 1.5f;
        var baseChance = Mathf.Lerp(0.15f, 0.9f, t);
        var final = baseChance + affinityModifier - maintenancePenalty;
        return Mathf.Clamp(final, questConfig.MinSuccessChance, questConfig.MaxSuccessChance);
    }

    // Overload accepting IDs for convenience when you only have member IDs.
    public float CalculateSuccessChance(QuestData quest, IEnumerable<string> memberIds)
    {
        var members = memberIds.Select(GetAdventurer).Where(a => a != null).ToList();
        return CalculateSuccessChance(quest, members);
    }

    private int CalculateQuestXp(QuestData quest, AdventurerData adv, ClassData classData)
    {
        var baseXp = questConfig.GetRankBaseXp(quest.Rank);
        var threshold = Mathf.Max(1f, questConfig.GetRankPowerThreshold(quest.Rank));
        var efficiency = Mathf.Clamp(adv.CalculatePower(config) / threshold, 0.5f, 2f);
        var modifier = Mathf.Lerp(1.5f, 0.5f, (efficiency - 0.5f) / 1.5f);
        if (classData && classData.GetAffinity(quest.Category) == CategoryAffinity.Preferred)
            modifier *= 1.1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseXp * modifier));
    }

    private int CalculateQuestRankPoints(QuestData quest)
        => Mathf.Max(1, questConfig.GetRankBaseXp(quest.Rank) / 10);
    #endregion
    
    #region Public Queries
    // Looks up an adventurer by their unique ID.
    public AdventurerData GetAdventurer(string id)
        => _adventurers.FirstOrDefault(a => a.Id == id);

    // Convenience: resolve the party that submitted the given application.
    public PartyData GetPartyFromApplication(QuestApplication application)
    {
        if (application == null || !PartyManager.Instance)
            return null;
        var leader = GetAdventurer(application.LeaderId);
        return leader != null && !string.IsNullOrEmpty(leader.PartyId)
            ? PartyManager.Instance.GetParty(leader.PartyId)
            : null;
    }
    #endregion

    #region Private Helpers
    private List<QuestData> GetAllPostedQuests()
    {
        var posted = new List<QuestData>();
        if (!QuestManager.Instance) return posted;
        for (var i = 0; i < questConfig.MaxBoardSlots; i++)
        {
            var quest = QuestManager.Instance.GetBoardSlot(i);
            if (quest is { Status: QuestStatus.Posted })
                posted.Add(quest);
        }
        return posted;
    }

    private RankUpApplicationData FindRankUpApplication(string applicationId)
        => _rankUpApplications.FirstOrDefault(a => a.ApplicationId == applicationId);
    #endregion
}