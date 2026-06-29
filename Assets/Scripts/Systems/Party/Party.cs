// Runtime party: an ordered set of adventurer ids with a lifecycle state.

using System.Collections.Generic;

public class Party
{
    public int Id { get; }
    public PartyState State { get; private set; } = PartyState.Forming;

    private readonly List<int> _memberIds;
    public IReadOnlyList<int> MemberIds => _memberIds;
    public int Size => _memberIds.Count;

    public Party(int id, IEnumerable<int> memberIds)
    {
        Id = id;
        _memberIds = new List<int>(memberIds);
    }

    public void SetState(PartyState next)
        => State = next;
    
    public bool Contains(int adventurerId)
        => _memberIds.Contains(adventurerId);
    
    public bool RemoveMember(int adventurerId)
        => _memberIds.Remove(adventurerId);
}