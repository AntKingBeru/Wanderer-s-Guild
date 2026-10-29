// A posted quest: its request, terms, state (waiting → ready → in progress), applicants by id, decline cooldowns, timing.
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class Quest
    {
        private readonly List<int> _memberIds;
        private readonly Dictionary<int, long> _declinedUntil = new();

        public Quest(int id, RequestData request, long postedAtMinutes, long expiresAtMinutes, QuestTerms terms,
                     QuestState state = QuestState.Waiting, IEnumerable<int> memberIds = null,
                     long departedAtMinutes = 0, long returnsAtMinutes = 0,
                     int[] declinedIds = null, long[] declinedUntilMinutes = null)
        {
            Id = id;
            Request = request;
            PostedAtMinutes = postedAtMinutes;
            ExpiresAtMinutes = expiresAtMinutes;
            Terms = terms;
            State = state;
            _memberIds = memberIds != null ? new List<int>(memberIds) : new List<int>();
            DepartedAtMinutes = departedAtMinutes;
            ReturnsAtMinutes = returnsAtMinutes;

            // NOTE: Cooldown arrays are paired by index; a length mismatch (edited save) keeps only the matched pairs.
            if (declinedIds != null && declinedUntilMinutes != null)
            {
                for (var i = 0; i < declinedIds.Length && i < declinedUntilMinutes.Length; i++)
                    _declinedUntil[declinedIds[i]] = declinedUntilMinutes[i];
            }
        }

        // Unique per guild (saved).
        public int Id { get; }
        public RequestData Request { get; }
        public long PostedAtMinutes { get; }

        // The request's original expiry: a quest that hasn't departed by then expires.
        public long ExpiresAtMinutes { get; }
        public QuestTerms Terms { get; }
        public QuestState State { get; private set; }
        public long DepartedAtMinutes { get; private set; }
        public long ReturnsAtMinutes { get; private set; }

        // Applicant / member ids — resolved through AdventurerManager when needed (never held as objects).
        public IReadOnlyList<int> MemberIds => _memberIds;
        public bool IsFull => _memberIds.Count >= Terms.PartySize;

        // True until the party leaves (waiting or ready) — declines, cancels and expiry apply.
        public bool IsBeforeDeparture => State != QuestState.InProgress;

        // The party's total share of the request's gold.
        public int PartyGold => (int)((long)Request.Reward.Gold * Terms.RewardSharePercent / 100);

        // What each member of a full party receives (the rounding remainder goes to the guild).
        public int GoldPerMember => PartyGold / System.Math.Max(1, Terms.PartySize);

        // Open (waiting, not full), not already a member, and not on a decline cooldown for this quest.
        public bool CanApply(int adventurerId, long nowMinutes) =>
            State == QuestState.Waiting && !IsFull && !_memberIds.Contains(adventurerId) &&
            !(_declinedUntil.TryGetValue(adventurerId, out var until) && nowMinutes < until);

        // False when not open, full, or already a member (cooldowns are checked by CanApply).
        public bool TryAddMember(int adventurerId)
        {
            if (State != QuestState.Waiting || IsFull || _memberIds.Contains(adventurerId))
                return false;

            _memberIds.Add(adventurerId);
            return true;
        }

        // A full waiting quest becomes ready (waits for approval). True if it changed.
        public bool MarkReadyIfFull()
        {
            if (State != QuestState.Waiting || !IsFull)
                return false;

            State = QuestState.Ready;
            return true;
        }

        // Declines an applicant: removed, can't re-apply here until the time given; a ready quest reopens.
        public bool Decline(int adventurerId, long blockedUntilMinutes)
        {
            if (!IsBeforeDeparture || !_memberIds.Remove(adventurerId))
                return false;

            _declinedUntil[adventurerId] = blockedUntilMinutes;
            if (State == QuestState.Ready) State = QuestState.Waiting;
            return true;
        }

        // Drops member ids that no longer exist (load repair); a ready quest that's no longer full reopens.
        public void PruneMembers(System.Predicate<int> exists)
        {
            _memberIds.RemoveAll(id => !exists(id));
            if (State == QuestState.Ready && !IsFull)
                State = QuestState.Waiting;
        }

        public void Depart(long nowMinutes, long returnsAtMinutes)
        {
            State = QuestState.InProgress;
            DepartedAtMinutes = nowMinutes;
            ReturnsAtMinutes = returnsAtMinutes;
        }

        // NOTE: Expired cooldowns are dropped when saving.
        public SavedQuest ToSaved(long nowMinutes)
        {
            List<int> ids = new();
            List<long> until = new();
            foreach (var pair in _declinedUntil.Where(pair => pair.Value > nowMinutes))
            {
                ids.Add(pair.Key);
                until.Add(pair.Value);
            }

            return new SavedQuest(Id, Request.Id, PostedAtMinutes, ExpiresAtMinutes, Terms, State, _memberIds.ToArray(),
                                  DepartedAtMinutes, ReturnsAtMinutes, ids.ToArray(), until.ToArray());
        }
    }
}