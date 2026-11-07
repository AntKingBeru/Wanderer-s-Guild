// Game-scene owner of parties: forms them from groups that succeed together, tracks teamwork and failure streaks,
// breaks them up (death, failures, the player), and saves them.
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Restored in Start (members are adventurer ids). Updates on
    // OnQuestEnded — after the outcome's injuries/deaths and after members are released — using only living members.
    public class PartyManager : Singleton<PartyManager>, ISaveContributor
    {
        private readonly List<GuildParty> _parties = new();
        private readonly Dictionary<string, int> _lineups = new();
        private PartySettings _settings;
        private IReadOnlyList<SavedParty> _pendingParties;
        private IReadOnlyList<SavedLineup> _pendingLineups;
        private int _nextId = 1;

        public IReadOnlyList<GuildParty> Parties => _parties;

        protected override void OnSingletonAwake()
        {
            _settings = GameConfig.Instance.Parties;

            var data = SaveGameService.HasInstance ? SaveGameService.Instance.LoadedData : null;
            if (data == null)
                return;

            _nextId = Mathf.Max(1, data.NextPartyId);
            _pendingParties = data.Parties;
            _pendingLineups = data.PartyLineups;
        }

        private void OnEnable()
        {
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnAdventurerLeft.AddListener(OnAdventurerLeft);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnAdventurerLeft.RemoveListener(OnAdventurerLeft);
        }

        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);
            Restore();
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);
            base.OnDestroy();
        }

        public GuildParty PartyOf(int adventurerId)
        {
            return _parties.FirstOrDefault(party => party.Contains(adventurerId));
        }

        // "Party: The Iron Wolves (3) · Teamwork +4%", or empty when they're not in one.
        public string LineFor(Adventurer adventurer)
        {
            var party = adventurer != null ? PartyOf(adventurer.Id) : null;
            if (party == null)
                return string.Empty;

            var teamwork = Mathf.RoundToInt(_settings.Teamwork(party.SharedSuccesses) * 100f);
            return $"Party: {party.Name} ({party.MemberIds.Count}) · Teamwork +{teamwork}%";
        }

        // The player disbands a party.
        public void Disband(int partyId)
        {
            var party = _parties.Find(p => p.Id == partyId);
            if (party != null)
                Dissolve(party, PartyEndReason.Player);
        }

        // Debug and testing: forms a party from these adventurers now (all must be party-less).
        public bool FormNow(IReadOnlyList<Adventurer> members) => members.Count >= _settings.MinimumSize && Form(members);

        public void WriteTo(GameSaveData data)
        {
            var parties = _parties.Select(party => party.ToSaved()).ToList();

            var lineups = _lineups.Select(pair => new SavedLineup(pair.Key, pair.Value)).ToList();

            data.SetParties(parties, lineups);
            data.NextPartyId = _nextId;
        }

        // ---------- Quest results ----------

        // NOTE: Only parties with ALL their members on this quest are affected (a declined member splits it — no change).
        // A success also bonds the party-less members as a lineup.
        private void OnQuestEnded(Quest quest, QuestEndReason reason)
        {
            if (reason != QuestEndReason.Succeeded && reason != QuestEndReason.Failed)
                return;
            if (!AdventurerManager.HasInstance)
                return;

            var alive = quest.MemberIds.Select(id => AdventurerManager.Instance.Find(id)).Where(adventurer => adventurer != null).ToList();

            var success = reason == QuestEndReason.Succeeded;
            foreach (var party in new List<GuildParty>(_parties).Where(party => AllPresent(party, alive)))
            {
                if (success)
                {
                    party.RecordSuccess();
                    ApplyTeamwork(party);
                    GameEventsRelay.OnPartyChanged.Invoke(party);
                }
                else
                {
                    party.RecordFailure();
                    if (party.FailStreak >= _settings.DisbandAfterFailures)
                        Dissolve(party, PartyEndReason.Failures);
                    else
                        GameEventsRelay.OnPartyChanged.Invoke(party);
                }
            }

            if (success)
                RecordLineup(alive);
        }

        // NOTE: Sorted ids → one key per group whatever the sign-up order. Mixed groups (anyone in a "party") record nothing.
        private void RecordLineup(List<Adventurer> members)
        {
            var loners = members.FindAll(a => PartyOf(a.Id) == null);
            if (loners.Count < _settings.MinimumSize || loners.Count != members.Count)
                return;

            var key = KeyOf(loners);
            var successes = (_lineups.GetValueOrDefault(key, 0)) + 1;
            _lineups[key] = successes;

            if (successes >= _settings.FormationSuccesses)
                Form(loners);
        }

        // ---------- Forming / breaking up ----------

        // NOTE: Every lineup record containing any new member is dropped — no stale record can form an overlapping party.
        private bool Form(IReadOnlyList<Adventurer> members)
        {
            if (members.Any(member => PartyOf(member.Id) != null))
            {
                return false;
            }

            HashSet<string> taken = new();
            foreach (var existing in _parties)
                taken.Add(existing.Name);

            var id = _nextId++;
            var ids = members.Select(member => member.Id).ToList();

            GuildParty party = new(id, PartyNames.Generate(_settings, taken, id), ids);
            _parties.Add(party);
            DropLineupsWith(ids);
            ApplyTeamwork(party);

            GameEventsRelay.OnPartyFormed.Invoke(party);
            return true;
        }

        private void Dissolve(GuildParty party, PartyEndReason reason)
        {
            if (!_parties.Remove(party))
                return;

            foreach (var id in party.MemberIds)
            {
                var member = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(id) : null;
                if (member == null)
                    continue;
                member.ClearParty();
                GameEventsRelay.OnAdventurerChanged.Invoke(member);
            }

            GameEventsRelay.OnPartyDisbanded.Invoke(party, reason);
        }

        // NOTE: A death (or any removal) breaks their party, and their lineup records go with them.
        private void OnAdventurerLeft(Adventurer adventurer)
        {
            var party = PartyOf(adventurer.Id);
            if (party != null)
                Dissolve(party, PartyEndReason.Death);
            DropLineupsWith(new[] { adventurer.Id });
        }

        private void ApplyTeamwork(GuildParty party)
        {
            var bonus = _settings.Teamwork(party.SharedSuccesses);
            foreach (var id in party.MemberIds)
            {
                var member = AdventurerManager.HasInstance ? AdventurerManager.Instance.Find(id) : null;
                member?.SetParty(party.Id, bonus);
            }
        }

        private static bool AllPresent(GuildParty party, List<Adventurer> present)
        {
            return party.MemberIds.All(id => present.Exists(a => a.Id == id));
        }

        private void DropLineupsWith(IReadOnlyCollection<int> ids)
        {
            var drop = _lineups.Keys.Where(key => ids.Any(id => KeyContains(key, id))).ToList();
            foreach (var key in drop)
                _lineups.Remove(key);
        }

        private static string KeyOf(List<Adventurer> members)
        {
            var ids = members.ConvertAll(a => a.Id);
            ids.Sort();
            return string.Join(",", ids);
        }

        private static bool KeyContains(string key, int id) => System.Array.IndexOf(key.Split(','), id.ToString()) >= 0;

        // NOTE: Members that no longer exist (or are already in an earlier party) are pruned; a party left below the
        // minimum is dropped silently. Lineups with missing members are dropped.
        private void Restore()
        {
            if (!AdventurerManager.HasInstance)
                return;

            if (_pendingParties != null)
            {
                HashSet<int> used = new();
                foreach (var saved in _pendingParties)
                {
                    var ids = saved.MemberIds.Where(id => AdventurerManager.Instance.Find(id) != null && used.Add(id)).ToList();
                    if (ids.Count < _settings.MinimumSize)
                        continue;

                    GuildParty party = new(saved.Id, saved.Name, ids, saved.SharedSuccesses, saved.FailStreak);
                    _parties.Add(party);
                    _nextId = Mathf.Max(_nextId, saved.Id + 1);
                    ApplyTeamwork(party);
                }
            }

            if (_pendingLineups != null)
            {
                foreach (var saved in _pendingLineups)
                {
                    if (string.IsNullOrEmpty(saved.Key))
                        continue;

                    var valid = true;
                    foreach (var part in saved.Key.Split(','))
                    {
                        if (!int.TryParse(part, out var id) || AdventurerManager.Instance.Find(id) == null || PartyOf(id) != null)
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (valid)
                        _lineups[saved.Key] = saved.Successes;
                }
            }

            _pendingParties = null;
            _pendingLineups = null;
        }
    }
}