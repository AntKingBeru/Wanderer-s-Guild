// Singleton owning all adventurers and party states. Responsibilities:
//   - Factory timer: generate new adventurers on a configurable schedule.
//   - Quest application simulation: idle adventurers/parties apply to posted quests.
//   - Quest outcome: distribute XP, rank points, and gold; return adventurers.
//   - Early quest failure: hourly chance-based mark accumulation.
//   - Rank-up eligibility: create applications when the point threshold is crossed.
//   - Rank-up quest resolution: approve/reject and resolve rank-up quests.
//   - Maintenance: nightly sleep and daily food checks.
//   - Party management: deterioration checks after each quest.
// NOTE: QuestConfig.GetRankPowerThreshold values were calibrated for placeholder
// party strength. Now that CalculatePower() produces real values, those thresholds
// should be updated on the balance sheet to match actual adventurer output.

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class AdventurerManager : MonoBehaviour
{
    public static AdventurerManager Instance { get; private set; }
    
    #region Inspector
    [Header("Configuration")]
    [Tooltip("Adventurer system config asset.")]
    [SerializeField] private AdventurerConfig config;

    [Tooltip("Quest system config — used for rank thresholds, XP rewards, and success chance bounds.")]
    [SerializeField] private QuestConfig questConfig;
    #endregion
    
    #region Runtime State
    private readonly List<AdventurerData> _adventurers = new();
    private readonly Dictionary<string, PartyData> _parties = new();
    private readonly List<RankUpApplicationData> _rankUpApplications = new();
    private AdventurerFactory _factory;
    private float _nextArrivalHour = -1f;
    #endregion
    
    #region Events
    public event Action<AdventurerData> OnAdventurerArrived;
    public event Action<AdventurerData> OnAdventurerLeveledUp;
    public event Action<AdventurerData> OnRankUpEligibilityGained;
    public event Action<AdventurerData> OnAdventurerRankUp;
    public event Action<AdventurerData> OnAdventurerRankUpFailed;
    public event Action<RankUpApplicationData> OnRankUpApplicationCreated;
    public event Action<RankUpApplicationData> OnRankUpApplicationResolved;
    public event Action<PartyData, PartyChangeReason> OnPartyChanged;
    public event Action OnRosterChanged;
    #endregion
    
    #region Public Accessors
    public AdventurerConfig Config => config;
    public IReadOnlyList<AdventurerData> Adventurers => _adventurers;
    public IReadOnlyDictionary<string, PartyData> Parties => _parties;
    public IReadOnlyList<RankUpApplicationData> PendingRankUpApplications
    {
        get
        {
            return _rankUpApplications.Where(application => application.Status == ApplicationStatus.Pending).ToList();
        }
    }
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
        if (TimeManager.Instance)
        {
            Debug.Log("Enabled");
            TimeManager.Instance.OnHourChanged += HandleHourChanged;
            TimeManager.Instance.OnDayChanged += HandleDayChanged;
        }
        if (QuestManager.Instance)
            QuestManager.Instance.OnQuestStatusChanged += HandleQuestStatusChanged;
    }

    private void OnDisable()
    {
        if (TimeManager.Instance)
        {
            Debug.Log("Disabled");
            TimeManager.Instance.OnHourChanged -= HandleHourChanged;
            TimeManager.Instance.OnDayChanged -= HandleDayChanged;
        }
        if (QuestManager.Instance)
            QuestManager.Instance.OnQuestStatusChanged -= HandleQuestStatusChanged;
    }

    private void Start()
    {
        if (!config || !questConfig)
        {
            Debug.LogError("[AdventurerManager] Config or QuestConfig not assigned. " +
                           "Assign both in the inspector.");
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
        Debug.Log("HOUR CHANGED");
        var currentHour = GetCurrentGameHours();
        CheckFactoryTime(0f);
        CheckEarlyQuestFailures();
        CheckRankUpQuestCompletions(currentHour);
        CheckRankUpReapplyEligible(currentHour);
        
        var inWindow = hour >= questConfig.ApplicationWindowStartHour
                       && hour <= questConfig.ApplicationWindowEndHour;
        if (inWindow)
            SimulateApplications(currentHour);
    }

    private void HandleDayChanged(int day)
    {
        PerformMaintenanceChecks();
    }
    #endregion
    
    #region Game-Time Helpers
    // Computes total in-game hours from Year 1 / Month 1 / Day 1 / 00:00.
    // Duplicated from QuestManager; both should be centralized on TimeManager later.
    private float GetCurrentGameHours()
    {
        if (!TimeManager.Instance)
            return 0f;
        var tm = TimeManager.Instance;
        return (tm.Year - 1) * tm.MonthsPerYear * tm.DaysPerMonth * 24f
            + (tm.Month - 1) * tm.DaysPerMonth * 24f
            + (tm.Day - 1) * 24f
            + tm.Hour
            + tm.Minute / 60f;
    }
    #endregion
    
    #region Factory
    private void CheckFactoryTime(float currentHour)
    {
        Debug.Log("SPAWN ADV");
        if (_nextArrivalHour < 0f || currentHour < _nextArrivalHour)
            return;

        var context = BuildCreationContext();
        var newcomer = _factory?.CreateAdventurer(context);
        if (newcomer != null)
            RegisterAdventurer(newcomer);
        
        ScheduleNextArrival(currentHour);
    }

    private void ScheduleNextArrival(float fromHour)
    {
        var days = UnityEngine.Random.Range(config.ArrivalRateMinDays, config.ArrivalRateMaxDays);
        _nextArrivalHour = fromHour + days * 24f;
    }

    private AdventurerCreationContext BuildCreationContext()
    {
        var context = new AdventurerCreationContext
        {
            GuildRankCap = config.GuildRankCap,
            TotalAdventurerCount = _adventurers.Count
        };
        foreach (var adventurer in _adventurers)
        {
            var ci = (int)adventurer.Class;
            var ri = (int)adventurer.Rank;
            if (ci < context.AdventurersPerClass.Length)
                context.AdventurersPerClass[ci]++;
            if (ri < context.AdventurersPerRank.Length)
                context.AdventurersPerRank[ri]++;
        }
        return context;
    }

    private void RegisterAdventurer(AdventurerData adventurer)
    {
        _adventurers.Add(adventurer);
        OnAdventurerArrived?.Invoke(adventurer);
        OnRosterChanged?.Invoke();
        Debug.Log($"[AdventurerManager] New adventurer arrived: {adventurer.Name} " +
                  $"({adventurer.Class}, Rank {adventurer.Rank}, Level {adventurer.Level})");
    }
    #endregion
    
    #region Quest Application Simulation
    private void SimulateApplications(float currentHour)
    {
        if (!QuestManager.Instance)
            return;
        var seasonMod = config.GetSeasonApplicationModifier(TimeManager.Instance.GetCurrentSeason());
        var baseChance = config.BaseApplicationChancePerHour * seasonMod;
        var processedParties = new HashSet<string>();
        foreach (var adventurer in _adventurers)
        {
            if (adventurer.Status != AdventurerStatus.Idle)
                continue;
            var isLeader = !string.IsNullOrEmpty(adventurer.PartyId) && adventurer.IsPartyLeader;
            var isSolo = string.IsNullOrEmpty(adventurer.PartyId);

            if (isLeader)
            {
                if (!processedParties.Add(adventurer.PartyId))
                    continue;

                var party = GetParty(adventurer.PartyId);
                if (party == null)
                    continue;
                if (UnityEngine.Random.value < baseChance)
                    TryApplyAsParty(adventurer, party, currentHour);
            }
            else if (isSolo)
            {
                if (UnityEngine.Random.value < baseChance)
                    TryApplySolo(adventurer, currentHour);
            }
        }
    }

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
        return true;
    }

    private void TryApplyAsParty(AdventurerData leader, PartyData party, float currentHour)
    {
        var members = GetPartyMembers(party.PartyId);
        if (members.Any(member => member.Status != AdventurerStatus.Idle))
        {
            return;
        }
        var totalRankInt = 0;
        var lowestRank = members[0].Rank;
        foreach (var member in members)
        {
            totalRankInt += (int)member.Rank;
            if ((int)member.Rank < (int)lowestRank)
                lowestRank = member.Rank;
        }
        var avgRank = (QuestRank)(totalRankInt / members.Count);
        var maxPartySize = config.GetPartyMaxSize(lowestRank);
        if (members.Count > maxPartySize) return;
        var quest = FindSuitableQuest(avgRank, members.Count, leader.Class);
        if (quest == null) return;
        var partyStrength = members.Sum(member => member.CalculatePower(config));
        var successChance = CalculateSuccessChance(quest, members);
        var memberIds = new string[members.Count];
        for (var i = 0; i < members.Count; i++)
            memberIds[i] = members[i].Id;
        var application = new QuestApplication(
            quest.QuestId,
            memberIds,
            leader.Id,
            party.IsTemporary,
            partyStrength,
            successChance,
            currentHour
        );
        if (!QuestManager.Instance.SubmitApplication(quest, application)) return;
        foreach (var member in members)
            member.ApplyToQuest(application.ApplicationId);
    }

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
                var affinity = classData.GetAffinity(quest.Category);
                weight = affinity switch
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
    public bool ApproveQuestApplication(QuestApplication application) 
        => QuestManager.Instance && QuestManager.Instance.ApproveApplication(application);

    public bool RejectQuestApplication(QuestApplication application)
    {
        if (!QuestManager.Instance)
            return false;
        if (!QuestManager.Instance.RejectApplication(application))
            return false;
        foreach (var memberId in application.PartyMemberIds)
        {
            var adventurer = GetAdventurer(memberId);
            if (adventurer?.CurrentApplicationId == application.ApplicationId)
                adventurer?.CancelQuestApplication();
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

    private void OnQuestDispatched(QuestData quest)
    {
        if (quest.ApprovedApplication == null)
            return;
        quest.ApprovedApplication.ResetEarlyFailureMarks();
        foreach (var memberId in quest.ApprovedApplication.PartyMemberIds)
        {
            var adventurer = GetAdventurer(memberId);
            if (adventurer?.Status == AdventurerStatus.AppliedToQuest)
                adventurer.DispatchToQuest(quest.QuestId);
        }
    }

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
            CheckPartyDeterioration(party, true);
        }
        else
        {
            foreach (var member in members)
                member.OnRegularQuestSucceeded();
        }
        OnRosterChanged?.Invoke();
    }

    private void OnQuestFailed(QuestData quest)
    {
        var members = GetQuestMembers(quest);
        ReturnAdventurersFromQuest(members, quest.QuestId);
        
        // TODO: apply injury/death penalties here
        
        var party = GetPartyFromApplication(quest.ApprovedApplication);
        if (party != null)
        {
            party.RecordFailure();
            CheckPartyDeterioration(party, false);
        }
        OnRosterChanged?.Invoke();
    }

    private void OnQuestExpired(QuestData quest)
    {
        foreach (var application in quest.Applications)
        {
            if (application.Status != ApplicationStatus.Pending)
                continue;
            foreach (var memberId in application.PartyMemberIds)
            {
                var adventurer = GetAdventurer(memberId);
                if (adventurer?.CurrentApplicationId == application.ApplicationId)
                    adventurer?.CancelQuestApplication();
            }
        }
    }

    private List<AdventurerData> GetQuestMembers(QuestData quest)
    {
        var members = new List<AdventurerData>();
        if (quest.ApprovedApplication == null)
            return members;
        members.AddRange(quest.ApprovedApplication.PartyMemberIds.Select(GetAdventurer).Where(adventurer => adventurer != null));
        return members;
    }

    private void ReturnAdventurersFromQuest(List<AdventurerData> members, string questId)
    {
        foreach (var member in members.Where(member => member.Status == AdventurerStatus.OnQuest && member.CurrentQuestId == questId))
            member.ReturnFromQuest();
    }

    private void DistributeQuestRewards(QuestData quest, List<AdventurerData> members)
    {
        var leaderId = quest.ApprovedApplication?.LeaderId;
        var partySize = members.Count;
        var goldEach = quest.GetGoldPerMember(partySize);
        var leaderBonus = quest.GetLeaderBonus(partySize);
        var rankPoints = CalculateQuestRankPoints(quest);
        foreach (var member in members)
        {
            member.AddGold(goldEach + (member.Id == leaderId ? leaderBonus : 0));
            var classData = config.GetClassData(member.Class);
            var xp = CalculateQuestXp(quest, member, classData);
            var lvUp = classData && member.AddExperience(xp, classData, config);
            if (lvUp)
                OnAdventurerLeveledUp?.Invoke(member);
            var becameEligible = member.AddRankPoints(rankPoints, config);
            if (becameEligible)
            {
                CreateRankUpApplication(member, GetCurrentGameHours());
                OnRankUpEligibilityGained?.Invoke(member);
            }
            member.OnRegularQuestSucceeded();
        }
    }
    #endregion
    
    #region Early Quest Failure
    private void CheckEarlyQuestFailures()
    {
        if (!QuestManager.Instance)
            return;
        var inProgress = new List<QuestData>(QuestManager.Instance.InProgressQuests);
        foreach (var quest in from quest in inProgress where quest.ApprovedApplication != null let chance = (1f - quest.ApprovedApplication.SuccessChance)
                     * config.EarlyFailureCoefficient where UnityEngine.Random.value < chance select quest)
        {
            quest.ApprovedApplication.AddEarlyFailureMark();
            if (quest.ApprovedApplication.EarlyFailureMarks >= config.EarlyFailureMarksRequired)
                QuestManager.Instance.ForceFailQuest(quest.QuestId);
        }
    }
    #endregion
    
    #region Rank-Up Quest
    private void CreateRankUpApplication(AdventurerData adventurer, float currentHour)
    {
        if (!string.IsNullOrEmpty(adventurer.RankApplicationId))
            return;
        var classData = config.GetClassData(adventurer.Class);
        var targetRank = (QuestRank)Mathf.Min((int)adventurer.Rank + 1, Enum.GetNames(typeof(QuestRank)).Length);
        var category = classData?.RankUpCategory ?? QuestCategory.Combat;
        var duration = classData?.GetRankUpDuration(adventurer.Rank) ?? 24f;
        var successChance = CalculateRankUpSuccessChance(adventurer, targetRank, classData);
        var application = new RankUpApplicationData(
            adventurer.Id,
            adventurer.Rank,
            category,
            duration,
            successChance,
            currentHour
        );
        _rankUpApplications.Add(application);
        adventurer.SetRankUpApplication(application.ApplicationId);
        OnRankUpApplicationCreated?.Invoke(application);
    }

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
        OnRankUpApplicationResolved?.Invoke(application);
        return true;
    }

    public bool RejectRankUpApplication(string applicationId)
    {
        var application = FindRankUpApplication(applicationId);
        if (application == null)
            return false;
        var adventurer = GetAdventurer(application.AdventurerId);
        if (!application.Reject())
            return false;
        adventurer?.ClearRankUpApplication();
        _rankUpApplications.Remove(application);
        OnRankUpApplicationResolved?.Invoke(application);
        return true;
    }

    private void CheckRankUpQuestCompletions(float currentHour)
    {
        foreach (var adventurer in _adventurers)
        {
            if (!adventurer.OnRankUpQuest)
                continue;
            if (!adventurer.IsRankUpQuestComplete(currentHour))
                continue;
            var classData = config.GetClassData(adventurer.Class);
            var targetRank = (QuestRank)Mathf.Min((int)adventurer.Rank + 1, Enum.GetNames(typeof(QuestRank)).Length);
            var successChance = CalculateRankUpSuccessChance(adventurer, targetRank, classData);
            if (UnityEngine.Random.value < successChance)
            {
                adventurer.CompleteRankUpQuest();
                if (classData)
                {
                    var xp = questConfig.GetRankBaseXp(targetRank);
                    var lvUp = adventurer.AddExperience(xp, classData, config);
                    if (lvUp)
                        OnAdventurerLeveledUp?.Invoke(adventurer);
                }
                OnAdventurerRankUp?.Invoke(adventurer);
                Debug.Log($"[AdventurerManager] {adventurer.Name} ranked up to {adventurer.Rank}!");
            }
            else
            {
                var cooldownDays = TimeManager.Instance
                    ? TimeManager.Instance.DaysPerMonth * config.RankUpRetryCooldownMonthFraction
                    : 15f;
                var cooldownHours = cooldownDays * 24f;
                adventurer.FailRankUpQuest(currentHour + cooldownHours, config);
                OnAdventurerRankUpFailed?.Invoke(adventurer);
                Debug.Log($"[AdventurerManager] {adventurer.Name} failed their rank-up quest.");
            }
            OnRosterChanged?.Invoke();
        }
    }

    private void CheckRankUpReapplyEligible(float currentHour)
    {
        foreach (var adventurer in from adventurer in _adventurers where !adventurer.RankUpEligible where adventurer.CanReapplyForRankUp(currentHour)
                 where adventurer.RankPoints >= config.GetRankPointThreshold(adventurer.Rank) select adventurer)
        {
            adventurer.SetRankUpEligible(true);
            CreateRankUpApplication(adventurer, currentHour);
            OnRankUpEligibilityGained?.Invoke(adventurer);
        }
    }

    private float CalculateRankUpSuccessChance(AdventurerData adventurer, QuestRank targetRank, ClassData classData)
    {
        var power = adventurer.CalculatePower(config);
        var threshold = Mathf.Max(1f, questConfig.GetRankPowerThreshold(targetRank));
        var efficiency = Mathf.Clamp(power / threshold, 0.5f, 2f);
        var t = (efficiency - 0.5f) / 1.5f;
        var baseChance = Mathf.Lerp(0.15f, 0.9f, t);
        if (classData)
        {
            var affinity = classData.GetAffinity(classData.RankUpCategory);
            if (affinity == CategoryAffinity.Preferred)
                baseChance += config.PreferredClassBonus;
            else if (affinity == CategoryAffinity.Disliked)
                baseChance -= config.DislikedClassPenalty;
        }
        baseChance -= adventurer.GetMaintenancePenalty(config);
        return Mathf.Clamp(baseChance, questConfig.MinSuccessChance, questConfig.MaxSuccessChance);
    }
    #endregion
    
    #region Maintenance
    private void PerformMaintenanceChecks()
    {
        foreach (var adventurer in _adventurers.Where(adventurer => adventurer.Status != AdventurerStatus.Dead))
        {
            if (adventurer.LodgingState == LodgingState.Nowhere)
                adventurer.RecordSleepMissed();
            else
                adventurer.ResetSleep();
            // TODO: add food checks after adding tavern
            adventurer.ResetFood();
        }
    }
    #endregion
    
    #region Party Management
    public PartyData CreateParty(string leaderId, IEnumerable<string> memberIds, bool isTemporary)
    {
        var partyId = Guid.NewGuid().ToString();
        var existingMembers = memberIds.ToList();
        var party = new PartyData(partyId, leaderId, existingMembers, isTemporary);
        _parties[partyId] = party;
        var leader = GetAdventurer(leaderId);
        leader?.SetParty(partyId, true);
        foreach (var member in from memberId in existingMembers
                 where memberId != leaderId select GetAdventurer(memberId))
        {
            member?.SetParty(partyId, false);
        }
        TriggerPartyEvent(party, PartyChangeReason.Formed);
        return party;
    }

    private void CheckPartyDeterioration(PartyData party, bool isSuccess)
    {
        if (party == null)
            return;
        var members = GetPartyMembers(party.PartyId);
        if (members.Count < 2)
            return;
        QuestRank highest = members[0].Rank, lowest = members[0].Rank;
        foreach (var member in members)
        {
            if ((int)member.Rank > (int)highest)
                highest = member.Rank;
            if ((int)member.Rank < (int)lowest)
                lowest = member.Rank;
        }
        if ((int)highest - (int)lowest >= config.RankGapSplitThreshold)
        {
            if (UnityEngine.Random.value < config.RankGapSplitChance)
            {
                var leaver = members.Find(
                    member => member.Rank == lowest && member.Id != party.LeaderId
                );
                if (leaver != null)
                {
                    SplitMembersFromParty(party, new List<string> { leaver.Id }, PartyChangeReason.RankDifference);
                    if (!_parties.ContainsKey(party.PartyId))
                        return;
                }
            }
        }
        if (!isSuccess)
        {
            var extra = party.ConsecutiveFailures - config.ConsecutiveFailSplitThreshold;
            if (extra > 0)
            {
                var splitChance = extra * config.ConsecutiveFailSplitChancePerExtra;
                if (UnityEngine.Random.value < splitChance)
                {
                    DisbandParty(party, PartyChangeReason.ConsecutiveFailures);
                    return;
                }
            }
        }
        if (party.IsTemporary && party.QuestsCompletedTogether >= config.TemporaryPartyQuestsToMakePermanent)
        {
            party.MakePermanent();
            TriggerPartyEvent(party, PartyChangeReason.TemporaryMadePermanent);
            Debug.Log($"[AdventurerManager] Party {party.PartyId} is now permanent.");
        }
    }

    private void DisbandParty(PartyData party, PartyChangeReason reason)
    {
        var members = GetPartyMembers(party.PartyId);
        foreach (var member in members)
            member.ClearParty();
        _parties.Remove(party.PartyId);
        TriggerPartyEvent(party, reason);
    }

    private void SplitMembersFromParty(PartyData party, List<string> memberIdsToRemove, PartyChangeReason reason)
    {
        foreach (var id in memberIdsToRemove)
        {
            GetAdventurer(id)?.ClearParty();
            party.RemoveMember(id);
        }

        if (party.MemberIds.Count <= 1)
        {
            DisbandParty(party, PartyChangeReason.Disbanded);
            return;
        }
        TriggerPartyEvent(party, reason);
    }

    private void TriggerPartyEvent(PartyData party, PartyChangeReason reason)
    {
        OnPartyChanged?.Invoke(party, reason);
        OnRosterChanged?.Invoke();
    }
    #endregion
    
    #region Success Chance Calculation
    public float CalculateSuccessChance(QuestData quest, IEnumerable<AdventurerData> members)
    {
        if (quest == null)
            return 0f;
        var partyStrength = 0f;
        var affinityModifier = 0f;
        var maintenancePenalty = 0f;
        var count = 0;
        foreach (var member in members)
        {
            partyStrength += member.CalculatePower(config);
            maintenancePenalty += member.GetMaintenancePenalty(config);
            var classData = config.GetClassData(member.Class);
            if (classData)
            {
                var affinity = classData.GetAffinity(quest.Category);
                if (affinity == CategoryAffinity.Preferred)
                    affinityModifier += config.PreferredClassBonus;
                else if (affinity == CategoryAffinity.Disliked)
                    affinityModifier -= config.DislikedClassPenalty; 
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
        
        var finalChance = baseChance + affinityModifier - maintenancePenalty;
        return Mathf.Clamp(finalChance, questConfig.MinSuccessChance, questConfig.MaxSuccessChance);
    }
    
    public float CalculateSuccessChance(QuestData quest, IEnumerable<string> memberIds)
    {
        var members = new List<AdventurerData>();
        foreach (var id in memberIds)
        {
            AdventurerData a = GetAdventurer(id);
            if (a != null) members.Add(a);
        }
        return CalculateSuccessChance(quest, members);
    }

    private int CalculateQuestXp(QuestData quest, AdventurerData adventurer, ClassData classData)
    {
        var baseXp = questConfig.GetRankBaseXp(quest.Rank);
        var threshold = Mathf.Max(1f, questConfig.GetRankPowerThreshold(quest.Rank));
        var efficiency = Mathf.Clamp(adventurer.CalculatePower(config) / threshold, 0.5f, 2.0f);
        var modifier   = Mathf.Lerp(1.5f, 0.5f, (efficiency - 0.5f) / 1.5f);
        if (classData && classData.GetAffinity(quest.Category) == CategoryAffinity.Preferred)
            modifier *= 1.1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseXp * modifier));
    }

    private int CalculateQuestRankPoints(QuestData quest)
        => Mathf.Max(1, questConfig.GetRankBaseXp(quest.Rank) / 10);
    #endregion
    
    #region Public Queries
    public AdventurerData GetAdventurer(string id)
    {
        return _adventurers.FirstOrDefault(adventurer => adventurer.Id == id);
    }
    
    public PartyData GetParty(string id) 
        => _parties.GetValueOrDefault(id);

    public List<AdventurerData> GetPartyMembers(string partyId)
    {
        var members = new List<AdventurerData>();
        if (string.IsNullOrEmpty(partyId))
            return members;
        members.AddRange(_adventurers.Where(adventurer => adventurer.PartyId == partyId));
        return members;
    }
    #endregion
    
    #region Private Helpers
    private List<QuestData> GetAllPostedQuests()
    {
        var posted = new List<QuestData>();
        if (!QuestManager.Instance)
            return posted;
        var slots = questConfig.MaxBoardSlots;
        for (var i = 0; i < slots; i++)
        {
            var quest = QuestManager.Instance.GetBoardSlot(i);
            if (quest is { Status: QuestStatus.Posted })
                posted.Add(quest);
        }
        return posted;
    }

    private PartyData GetPartyFromApplication(QuestApplication application)
    {
        if (application == null)
            return null;
        var leader = GetAdventurer(application.LeaderId);
        return leader != null && !string.IsNullOrEmpty(leader.PartyId)
            ? GetParty(leader.PartyId)
            : null;
    }

    private RankUpApplicationData FindRankUpApplication(string applicationId)
    {
        return _rankUpApplications.FirstOrDefault(application => application.ApplicationId == applicationId);
    }
    #endregion
}