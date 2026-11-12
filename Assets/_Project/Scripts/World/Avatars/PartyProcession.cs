// One quest party's walk-out: its members in gathering order, the phase and its timer.
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public sealed class PartyProcession
    {
        // A member's progress. NOTE: A class — arrival callbacks write to it after the fact.
        public sealed class Member
        {
            public AdventurerAvatar Avatar;
            public bool Gathered;
            public bool Sent;
        }

        private readonly List<Member> _members = new();

        public IReadOnlyList<Member> Members => _members;
        public int Count => _members.Count;
        public ProcessionPhase Phase { get; set; } = ProcessionPhase.Starting;
        public float Timer { get; set; }

        public void Add(AdventurerAvatar avatar) => _members.Add(new Member { Avatar = avatar });

        // NOTE: Unity null check — despawned avatars go; so do members called back before they were sent (no longer leaving).
        public void DropInvalid() =>
            _members.RemoveAll(member => member.Avatar == null || (!member.Sent && !member.Avatar.IsLeaving));

        public bool AllGathered()
        {
            foreach (var member in _members)
            {
                if (!member.Gathered)
                    return false;
            }
            return true;
        }

        // The first member not sent out yet (gathering order = nearest the door first), or null.
        public Member NextToSend()
        {
            return _members.FirstOrDefault(member => !member.Sent);
        }
    }
}