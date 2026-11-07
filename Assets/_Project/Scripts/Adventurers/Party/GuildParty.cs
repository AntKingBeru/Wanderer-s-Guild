// A permanent party (pure state): name, member ids, successes together since forming, and the current failure streak.
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class GuildParty
    {
        private readonly List<int> _memberIds;

        public GuildParty(int id, string name, IEnumerable<int> memberIds, int sharedSuccesses = 0, int failStreak = 0)
        {
            Id = id;
            Name = name;
            _memberIds = new List<int>(memberIds);
            SharedSuccesses = System.Math.Max(0, sharedSuccesses);
            FailStreak = System.Math.Max(0, failStreak);
        }

        public int Id { get; }
        public string Name { get; }
        public IReadOnlyList<int> MemberIds => _memberIds;
        public int SharedSuccesses { get; private set; }
        public int FailStreak { get; private set; }

        public bool Contains(int adventurerId) => _memberIds.Contains(adventurerId);

        public void RecordSuccess()
        {
            SharedSuccesses++;
            FailStreak = 0;
        }

        public void RecordFailure() => FailStreak++;

        // Drops a member (load repair). True if they were in it. 
        public bool Remove(int adventurerId) => _memberIds.Remove(adventurerId);

        public SavedParty ToSaved() => new(Id, Name, _memberIds.ToArray(), SharedSuccesses, FailStreak);
    }
}