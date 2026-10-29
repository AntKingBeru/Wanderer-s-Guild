// Game-scene owner of posted quests: board slots, posting, sign-ups, approval/declines, departure, expiry, cancel, resolution, save.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Quests are restored in Start (after every manager's Awake),
    // because their members are adventurer ids that AdventurerManager must have loaded first.
    public class QuestManager : Singleton<QuestManager>, ISaveContributor
    {
        private readonly List<Quest> _quests = new();
        private readonly List<Adventurer> _members = new();
        private QuestSettings _settings;
        private Dictionary<string, RequestData> _requests = new();
        private IReadOnlyList<SavedQuest> _pendingRestore;
        private int _nextId = 1;

        public IReadOnlyList<Quest> Quests => _quests;

        // Board slots open at the guild's current rank (3 + rank by default). Every posted quest uses one
        // until it ends — waiting, ready or away.
        public int OpenSlots => _settings.OpenSlots(CurrentGuildRank);

        public bool HasFreeSlot => _quests.Count < OpenSlots;

        private static long Now => RequestManager.NowMinutes;

        private static GuildRank CurrentGuildRank =>
            GuildManager.HasInstance ? GuildManager.Instance.Rank.Rank : GameConfig.Instance.NewGuild.StartingRank;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Quests;

            var database = GameConfig.Instance.Requests.Database;
            if (database)
                _requests = database.BuildLookup();

            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            if (data != null)
            {
                _nextId = Mathf.Max(1, data.NextQuestId);
                _pendingRestore = data.Quests;
            }
        }

        private void OnEnable()
        {
            GameEventsRelay.OnHourStarted.AddListener(OnHourStarted);
            GameEventsRelay.OnTimeTick.AddListener(OnTimeTick);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnHourStarted.RemoveListener(OnHourStarted);
            GameEventsRelay.OnTimeTick.RemoveListener(OnTimeTick);
        }

        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);

            Restore();
            ReconcileStatuses();
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);

            base.OnDestroy();
        }

        public QuestTerms DefaultTerms(RequestData request) => QuestCalculator.DefaultTerms(request, _settings);

        // Turns a board request into a posted quest with the given terms (clamped).
        // False if the quest board has no free slot or the request is gone (the request stays on its board then).
        public bool TryPost(int requestInstanceId, QuestTerms terms, out Quest quest)
        {
            quest = null;
            if (!HasFreeSlot)
                return false;
            if (!RequestManager.HasInstance || !RequestManager.Instance.TryTake(requestInstanceId, out var request))
                return false;

            var clamped = QuestCalculator.ClampTerms(terms, request.Data, _settings);
            quest = new Quest(_nextId++, request.Data, Now, request.ExpiresAtMinutes, clamped);
            _quests.Add(quest);

            GameEventsRelay.OnQuestPosted.Invoke(quest);
            return true;
        }

        // Sends a ready (full) party off — the duration starts now. False unless the quest is Ready.
        public bool Approve(int questId)
        {
            var quest = Find(questId);
            if (quest is not { State: QuestState.Ready })
                return false;

            Depart(quest, Now);
            return true;
        }

        // Declines one applicant: they go idle and can't re-apply to this quest for the cooldown; a ready quest
        // reopens. False after departure or if they aren't an applicant.
        public bool Decline(int questId, int adventurerId)
        {
            var quest = Find(questId);
            if (quest == null)
                return false;

            var blockedUntil = Now + GameConfig.Instance.Applications.ReapplyCooldownMinutes;
            if (!quest.Decline(adventurerId, blockedUntil))
                return false;

            var adventurer = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(adventurerId) : null;
            if (adventurer is { Status: AdventurerStatus.SignedUp })
                SetStatus(adventurer, AdventurerStatus.Idle);

            GameEventsRelay.OnQuestUpdated.Invoke(quest);
            return true;
        }

        // Declines every applicant (copy of the ids first — declining edits the list). Returns how many.
        public int DeclineAll(int questId)
        {
            var quest = Find(questId);
            return quest is not { IsBeforeDeparture: true } ? 0 : new List<int>(quest.MemberIds).Count(id => Decline(questId, id));
        }

        // Withdraws a quest before departure (−1 reputation by default); its applicants go back to idle.
        public bool Cancel(int questId)
        {
            var quest = Find(questId);
            if (quest is not { IsBeforeDeparture: true })
                return false;

            ChangeReputation(GameConfig.Instance.Requests.DismissReputation, ReputationSource.RequestDismissed);
            End(quest, QuestEndReason.Cancelled);
            return true;
        }

        public Quest Find(int questId)
        {
            return _quests.FirstOrDefault(quest => quest.Id == questId);
        }

        // Members that still exist in the roster (filled into a reused list — copy it to keep it).
        public IReadOnlyList<Adventurer> MembersOf(Quest quest)
        {
            _members.Clear();
            if (!AdventurerManager.HasInstance)
                return _members;

            foreach (var id in quest.MemberIds)
            {
                var adventurer = AdventurerManager.Instance.Find(id);
                if (adventurer != null)
                    _members.Add(adventurer);
            }
            return _members;
        }

        // The success chance with the current (or would-be) members.
        public float SuccessChance(Quest quest) =>
            QuestCalculator.SuccessChance(QuestCalculator.PartyPower(MembersOf(quest), quest.Request.Category, _settings),
                                          QuestCalculator.Difficulty(quest.Request, _settings), _settings);

        // Debug and testing: run the sign-up pass now / resolve every quest in progress now.
        public void RunSignUpsNow() => RunSignUps(Now);

        public void ResolveAllNow()
        {
            foreach (var quest in new List<Quest>(_quests).Where(quest => quest.State == QuestState.InProgress))
            {
                Resolve(quest);
            }
        }

        public void WriteTo(GameSaveData data)
        {
            var saved = _quests.Select(quest => quest.ToSaved(Now)).ToList();

            data.SetQuests(saved);
            data.NextQuestId = _nextId;
        }

        // NOTE: Sign-ups run on the hour (before the tick's expiry check).
        private void OnHourStarted(GameDateTime now) => RunSignUps(now.TotalMinutes);

        // NOTE: Due quests are collected first, then handled — never modifies the list while iterating it.
        // Anything not yet departed (waiting or ready) expires at the request's expiry.
        private void OnTimeTick(GameDateTime now)
        {
            List<Quest> due = null;
            foreach (var quest in from quest in _quests
                     let expired = quest.IsBeforeDeparture && now.TotalMinutes >= quest.ExpiresAtMinutes
                     let returned = quest.State == QuestState.InProgress && now.TotalMinutes >= quest.ReturnsAtMinutes
                     where expired || returned select quest)
            {
                (due ??= new List<Quest>()).Add(quest);
            }

            if (due == null)
                return;

            foreach (var quest in due)
            {
                if (quest.State == QuestState.InProgress)
                {
                    Resolve(quest);
                }
                else
                {
                    ChangeReputation(GameConfig.Instance.Requests.ExpiryReputation, ReputationSource.RequestExpired);
                    End(quest, QuestEndReason.Expired);
                }
            }
        }

        // NOTE: A quest that fills becomes Ready and waits for the player's approval (Applications); it doesn't depart.
        private void RunSignUps(long nowMinutes)
        {
            if (!AdventurerManager.HasInstance || _quests.Count == 0)
                return;

            var filled = QuestSignUps.Run(AdventurerManager.Instance.Adventurers, _quests, _settings, nowMinutes, OnSignedUp);
            foreach (var quest in filled.Where(quest => quest.MarkReadyIfFull()))
            {
                GameEventsRelay.OnQuestUpdated.Invoke(quest);
                GameEventsRelay.OnQuestPartyReady.Invoke(quest);
            }
        }

        private void OnSignedUp(Quest quest, Adventurer adventurer)
        {
            GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
            GameEventsRelay.OnQuestUpdated.Invoke(quest);
        }

        // NOTE: The trip's length starts at approval, so waiting for approval never shortens it.
        private void Depart(Quest quest, long nowMinutes)
        {
            var returnsAt = nowMinutes + (long)QuestCalculator.DurationDays(quest.Request, _settings) * GameCalendar.MinutesPerDay;
            quest.Depart(nowMinutes, returnsAt);

            foreach (var member in MembersOf(quest))
                SetStatus(member, AdventurerStatus.OnQuest);

            GameEventsRelay.OnQuestUpdated.Invoke(quest);
        }

        // NOTE: A quest whose members are all gone (dropped on load) fails without the reputation penalty.
        private void Resolve(Quest quest)
        {
            List<Adventurer> members = new(MembersOf(quest));
            if (members.Count == 0)
            {
                End(quest, QuestEndReason.Failed);
                return;
            }

            var chance = QuestCalculator.SuccessChance(QuestCalculator.PartyPower(members, quest.Request.Category, _settings),
                                                         QuestCalculator.Difficulty(quest.Request, _settings), _settings);
            var success = Random.value < chance;

            if (success)
                PaySuccess(quest, members);
            else
                ChangeReputation(_settings.FailureReputation, ReputationSource.QuestFailed);

            End(quest, success ? QuestEndReason.Succeeded : QuestEndReason.Failed);
        }

        // NOTE: The party share is split evenly; the guild keeps the rest of the request's gold plus the rounding remainder.
        // Rank progress is granted by PromotionManager when it hears OnQuestEnded (Succeeded).
        private void PaySuccess(Quest quest, List<Adventurer> members)
        {
            var questRank = quest.Request.RecommendedRank;
            var partyGold = quest.PartyGold;
            var perMember = partyGold / members.Count;
            var guildGold = quest.Request.Reward.Gold - (long)perMember * members.Count;

            foreach (var member in members)
            {
                member.AddWealth(perMember);
                if (AdventurerManager.HasInstance)
                    AdventurerManager.Instance.GrantExperience(member.Id, _settings.Experience(questRank));
            }

            if (GuildManager.HasInstance)
            {
                GuildManager.Instance.Wallet.Add(guildGold, GoldReason.QuestReward);
                GuildManager.Instance.Rank.AddPoints(_settings.RankPoints(questRank));
            }

            ChangeReputation(_settings.SuccessReputation, ReputationSource.QuestSucceeded);
        }

        // NOTE: Every way out of the list goes through here — members are always released back to idle.
        private void End(Quest quest, QuestEndReason reason)
        {
            foreach (var member in MembersOf(quest))
                SetStatus(member, AdventurerStatus.Idle);

            _quests.Remove(quest);
            GameEventsRelay.OnQuestEnded.Invoke(quest, reason);
        }

        private static void SetStatus(Adventurer adventurer, AdventurerStatus status)
        {
            if (adventurer.SetStatus(status))
                GameEventsRelay.OnAdventurerChanged.Invoke(adventurer);
        }

        private static void ChangeReputation(int delta, ReputationSource source)
        {
            if (delta != 0 && GuildManager.HasInstance)
                GuildManager.Instance.Reputation.Change(delta, source);
        }

        // NOTE: Quests whose request no longer exists are dropped with a warning. Member ids no longer in the roster are
        // pruned (a ready quest that's no longer full reopens).
        private void Restore()
        {
            if (_pendingRestore == null)
                return;

            foreach (var saved in _pendingRestore)
            {
                if (string.IsNullOrEmpty(saved.RequestId) || !_requests.TryGetValue(saved.RequestId, out RequestData request))
                {
                    Debug.LogWarning($"[QuestManager] Saved quest #{saved.Id} uses request '{saved.RequestId}', which isn't in the database any more; dropped.", this);
                    continue;
                }

                Quest quest = new(saved.Id, request, saved.PostedAtMinutes, saved.ExpiresAtMinutes, saved.Terms, saved.State,
                                  saved.MemberIds, saved.DepartedAtMinutes, saved.ReturnsAtMinutes,
                                  saved.DeclinedIds, saved.DeclinedUntilMinutes);
                if (AdventurerManager.HasInstance)
                    quest.PruneMembers(id => AdventurerManager.Instance.Find(id) != null);

                _quests.Add(quest);
                _nextId = Mathf.Max(_nextId, saved.Id + 1);
            }

            _pendingRestore = null;
        }

        // NOTE: Only the statuses quests own are reconciled: applicants of waiting/ready quests are SignedUp, members of
        // departed quests OnQuest, and anyone else marked SignedUp/OnQuest goes back to idle. InExam is left alone.
        private void ReconcileStatuses()
        {
            if (!AdventurerManager.HasInstance)
                return;

            Dictionary<int, AdventurerStatus> expected = new();
            foreach (var quest in _quests)
            {
                var status = quest.State == QuestState.InProgress ? AdventurerStatus.OnQuest : AdventurerStatus.SignedUp;
                foreach (var id in quest.MemberIds)
                    expected[id] = status;
            }

            foreach (var adventurer in AdventurerManager.Instance.Adventurers)
            {
                if (expected.TryGetValue(adventurer.Id, out var status))
                    adventurer.SetStatus(status);
                else if (adventurer.Status is AdventurerStatus.SignedUp or AdventurerStatus.OnQuest)
                    adventurer.SetStatus(AdventurerStatus.Idle);
            }
        }
    }
}