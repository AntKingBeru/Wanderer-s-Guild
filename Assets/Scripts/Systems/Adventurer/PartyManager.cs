// Singleton that owns party formation, membership, and the temporary→permanent trial.
// Adventurers can party up regardless of rank — PartyManager only enforces party size and
// a per-adventurer cooldown after a failed trial disband. Trial-outcome logic lives on
// PartyData itself; this class only orchestrates and raises events.
// Membership is tracked here (not on AdventurerData) so there's a single source of truth.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    private PartyConfig _config;
    private readonly Dictionary<string, PartyData> _parties = new();
    private readonly Dictionary<string, string> _memberToParty = new(); // adventurerId -> partyId
    private readonly Dictionary<string, float> _cooldowns = new(); // adventurerId -> game-hour cooldown ends

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

        _config = GameManager.Instance ? GameManager.Instance.PartyConfig : null;
        if (!_config)
            Debug.LogError("[PartyManager] No PartyConfig found on GameManager.");
    }
    
    private void OnEnable()
    {
        if (GameEventRelay.Instance)
            GameEventRelay.Instance.OnAdventurerRemoved.AddListener(OnAdventurerRemoved);
    }

    private void OnDisable()
    {
        if (GameEventRelay.Instance)
            GameEventRelay.Instance.OnAdventurerRemoved.RemoveListener(OnAdventurerRemoved);
    }
    
    #region Formation
    // Forms a new temporary party. Fails (returns null) if size is out of range, or any member
    // doesn't exist, is dead, is already partied, or is on a post-disband cooldown.
    public PartyData CreateTemporaryParty(IEnumerable<string> memberIds, string leaderId = null)
    {
        if (!_config)
            return null;

        var members = memberIds.Distinct().ToList();
        if (members.Count < _config.MinPartySize || members.Count > _config.MaxPartySize)
        {
            Debug.LogWarning($"[PartyManager] Party size {members.Count} is outside the allowed range " +
                              $"({_config.MinPartySize}-{_config.MaxPartySize}).");
            return null;
        }

        var currentHour = GetCurrentGameHour();
        foreach (var id in members)
        {
            if (!IsAvailableForParty(id, currentHour, out var reason))
            {
                Debug.LogWarning($"[PartyManager] Cannot form party — '{id}' is unavailable: {reason}");
                return null;
            }
        }

        var partyId = Guid.NewGuid().ToString();
        var leader = !string.IsNullOrEmpty(leaderId) && members.Contains(leaderId) ? leaderId : members[0];
        var party = new PartyData(partyId, leader, members, isTemporary: true);

        _parties[partyId] = party;
        foreach (var id in members)
            _memberToParty[id] = partyId;

        FirePartyEvent(party, PartyChangeReason.Formed);
        return party;
    }

    // Adds a single adventurer to an existing party (temporary or permanent).
    public bool AddMember(string partyId, string adventurerId)
    {
        if (!_config || !_parties.TryGetValue(partyId, out var party))
            return false;
        if (party.MemberIds.Count >= _config.MaxPartySize)
        {
            Debug.LogWarning($"[PartyManager] Party {partyId} is already at max size.");
            return false;
        }
        if (!IsAvailableForParty(adventurerId, GetCurrentGameHour(), out var reason))
        {
            Debug.LogWarning($"[PartyManager] Cannot add '{adventurerId}' — {reason}");
            return false;
        }

        party.AddMember(adventurerId);
        _memberToParty[adventurerId] = partyId;
        FirePartyEvent(party, PartyChangeReason.MemberJoined);
        return true;
    }

    // Voluntary departure (or death, via OnAdventurerRemoved). Auto-disbands the party with
    // NO cooldown if headcount drops below the minimum — this isn't a trial failure.
    public bool RemoveMember(string adventurerId, PartyChangeReason reason = PartyChangeReason.MemberLeft)
    {
        if (!_memberToParty.TryGetValue(adventurerId, out var partyId) || !_parties.TryGetValue(partyId, out var party))
            return false;

        party.RemoveMember(adventurerId);
        _memberToParty.Remove(adventurerId);

        var minSize = _config ? _config.MinPartySize : 2;
        if (party.MemberIds.Count < minSize)
        {
            DisbandParty(partyId, PartyChangeReason.Disbanded, applyCooldown: false);
            return true;
        }

        FirePartyEvent(party, reason);
        return true;
    }
    #endregion
    
    #region Trial Outcome — call this once the Quest system is rebuilt and a quest resolves
    // Feeds one quest's outcome into the party's trial. Promotes to permanent, disbands with
    // a cooldown, or just records progress, depending on PartyData's own evaluation.
    public void RecordQuestResult(string partyId, bool success)
    {
        if (!_config || !_parties.TryGetValue(partyId, out var party))
            return;

        switch (party.RecordQuestResult(success, _config))
        {
            case PartyTrialResult.Promote:
                party.MakePermanent();
                FirePartyEvent(party, PartyChangeReason.TemporaryMadePermanent);
                break;
            case PartyTrialResult.Disband:
                DisbandParty(partyId, PartyChangeReason.Disbanded, applyCooldown: true);
                break;
            case PartyTrialResult.Continue:
                GameEventRelay.Instance?.OnPartyTrialProgress.Invoke(party);
                break;
        }
    }
    #endregion
    
    #region Disbanding
    // Fully disbands a party. applyCooldown puts every member on a party-formation cooldown —
    // use this for failed trials, but not for voluntary leaves or death-driven dissolution.
    public bool DisbandParty(string partyId, PartyChangeReason reason, bool applyCooldown = false)
    {
        if (!_parties.TryGetValue(partyId, out var party))
            return false;

        float? cooldownEnd = null;
        if (applyCooldown && _config)
            cooldownEnd = GetCurrentGameHour() + _config.DisbandCooldownDays * 24f;

        foreach (var memberId in party.MemberIds)
        {
            _memberToParty.Remove(memberId);
            if (cooldownEnd.HasValue)
                _cooldowns[memberId] = cooldownEnd.Value;
        }

        _parties.Remove(partyId);
        FirePartyEvent(party, reason);
        return true;
    }
    #endregion
    
    #region Queries
    public PartyData GetParty(string partyId)
        => _parties.GetValueOrDefault(partyId);

    public PartyData GetPartyForAdventurer(string adventurerId)
        => _memberToParty.TryGetValue(adventurerId, out var partyId) ? GetParty(partyId) : null;

    public List<AdventurerData> GetPartyMembers(string partyId)
    {
        var result = new List<AdventurerData>();
        var party = GetParty(partyId);
        if (party == null || !AdventurerManager.Instance)
            return result;

        foreach (var id in party.MemberIds)
        {
            var adventurer = AdventurerManager.Instance.GetAdventurer(id);
            if (adventurer != null)
                result.Add(adventurer);
        }
        return result;
    }

    // True if the adventurer exists, is alive, isn't already at a party, and isn't on cooldown.
    public bool IsAvailableForParty(string adventurerId, out string reason)
        => IsAvailableForParty(adventurerId, GetCurrentGameHour(), out reason);

    private bool IsAvailableForParty(string adventurerId, float currentHour, out string reason)
    {
        var adventurer = AdventurerManager.Instance?.GetAdventurer(adventurerId);
        if (adventurer == null) { reason = "unknown adventurer"; return false; }
        if (!adventurer.IsAlive) { reason = "dead"; return false; }
        if (_memberToParty.ContainsKey(adventurerId)) { reason = "already in a party"; return false; }
        if (_cooldowns.TryGetValue(adventurerId, out var endHour) && currentHour < endHour)
        {
            reason = $"on a party cooldown until game-hour {endHour:F1}";
            return false;
        }
        reason = null;
        return true;
    }
    #endregion
    
    #region Private Helpers
    private void OnAdventurerRemoved(AdventurerData adventurer)
        => RemoveMember(adventurer.Id, PartyChangeReason.MemberDied);

    private static float GetCurrentGameHour()
        => TimeManager.Instance ? TimeManager.Instance.GetTotalGameHours() : 0f;

    private static void FirePartyEvent(PartyData party, PartyChangeReason reason)
    {
        if (!GameEventRelay.Instance)
            return;
        GameEventRelay.Instance.OnPartyChanged.Invoke(party, reason);
        GameEventRelay.Instance.OnRosterChanged.Invoke();
    }
    #endregion
}