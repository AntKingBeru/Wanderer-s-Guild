// Manages party data: formation, disbanding, member splits, and deterioration checks.
// Owns the _parties dictionary. AdventurerManager holds a reference to this and delegates all party-specific work here.
// Uses the Observer pattern via GameEventRelay for all outbound events.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Adventurer config — used for party size limits and deterioration thresholds.")]
    [SerializeField] private AdventurerConfig config;

    // All active parties keyed by partyId.
    private readonly Dictionary<string, PartyData> _parties = new();

    public IReadOnlyDictionary<string, PartyData> Parties => _parties;

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
    
    #region Public API
    // Creates a new party, assigns leader/members, and fires the Formed event.
    public PartyData CreateParty(string leaderId, IEnumerable<string> memberIds, bool isTemporary)
    {
        var partyId = Guid.NewGuid().ToString();
        var memberList = memberIds.ToList();
        var party = new PartyData(partyId, leaderId, memberList, isTemporary);
        _parties[partyId] = party;

        // Update adventurer data via SoloAdventurerManager's roster lookup.
        var sam = SoloAdventurerManager.Instance;
        sam?.GetAdventurer(leaderId)?.SetParty(partyId, true);
        foreach (var id in memberList.Where(id => id != leaderId))
            sam?.GetAdventurer(id)?.SetParty(partyId, false);

        FirePartyEvent(party, PartyChangeReason.Formed);
        return party;
    }

    // Fully disbands a party and clears party state from all members.
    public void DisbandParty(PartyData party, PartyChangeReason reason)
    {
        if (party == null)
            return;
        var sam = SoloAdventurerManager.Instance;
        foreach (var id in party.MemberIds)
            sam?.GetAdventurer(id)?.ClearParty();

        _parties.Remove(party.PartyId);
        FirePartyEvent(party, reason);
    }

    // Returns the PartyData for the given id, or null if not found.
    public PartyData GetParty(string id)
        => _parties.GetValueOrDefault(id);

    // Returns all adventurers belonging to the given partyId.
    public List<AdventurerData> GetPartyMembers(string partyId)
    {
        var members = new List<AdventurerData>();
        if (string.IsNullOrEmpty(partyId) || SoloAdventurerManager.Instance == null)
            return members;
        members.AddRange(
            SoloAdventurerManager.Instance.Adventurers
                .Where(a => a.PartyId == partyId)
        );
        return members;
    }
    #endregion
    
    #region Deterioration - called by SoloAdventurerManager after quest resolution
    // Evaluates rank-gap splits, consecutive-failure disbands, and temporary→permanent promotions.
    public void CheckPartyDeterioration(PartyData party, bool isSuccess)
    {
        if (party == null)
            return;
        var members = GetPartyMembers(party.PartyId);
        if (members.Count < 2)
            return;

        // Rank gap split check
        var highest = members[0].Rank;
        var lowest  = members[0].Rank;
        foreach (var m in members)
        {
            if ((int)m.Rank > (int)highest) highest = m.Rank;
            if ((int)m.Rank < (int)lowest)  lowest  = m.Rank;
        }

        if ((int)highest - (int)lowest >= config.RankGapSplitThreshold)
        {
            if (UnityEngine.Random.value < config.RankGapSplitChance)
            {
                // Remove the lowest-ranked non-leader member.
                var leaver = members.Find(m => m.Rank == lowest && m.Id != party.LeaderId);
                if (leaver != null)
                {
                    SplitMembersFromParty(party, new List<string> { leaver.Id }, PartyChangeReason.RankDifference);
                    // Party may have been disbanded during the split — bail out if so.
                    if (!_parties.ContainsKey(party.PartyId)) return;
                }
            }
        }

        // Consecutive failure disband check
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

        // Temporary → permanent promotion
        if (party.IsTemporary && party.QuestsCompletedTogether >= config.TemporaryPartyQuestsToMakePermanent)
        {
            party.MakePermanent();
            FirePartyEvent(party, PartyChangeReason.TemporaryMadePermanent);
            Debug.Log($"[PartyManager] Party {party.PartyId} became permanent.");
        }
    }
    #endregion
    
    #region Private Helpers
    // Removes specific members from a party; disbands if headcount drops to 1 or fewer.
    private void SplitMembersFromParty(PartyData party, List<string> idsToRemove, PartyChangeReason reason)
    {
        var sam = SoloAdventurerManager.Instance;
        foreach (var id in idsToRemove)
        {
            sam?.GetAdventurer(id)?.ClearParty();
            party.RemoveMember(id);
        }

        if (party.MemberIds.Count <= 1)
        {
            DisbandParty(party, PartyChangeReason.Disbanded);
            return;
        }
        FirePartyEvent(party, reason);
    }

    // Fires both the specific party event and the generic roster-changed event.
    private static void FirePartyEvent(PartyData party, PartyChangeReason reason)
    {
        if (!GameEventRelay.Instance)
            return;
        GameEventRelay.Instance.onPartyChanged.Invoke(party, reason);
        GameEventRelay.Instance.onRosterChanged.Invoke();
    }
    #endregion
}