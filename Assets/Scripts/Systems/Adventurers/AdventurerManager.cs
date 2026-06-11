// Facade that unifies SoloAdventurerManager and PartyManager behind a single access point.
// Existing code that previously called AdventurerManager.Instance.GetAdventurer() or
// .Parties etc. continues to work without changes to call sites.
// New code should prefer calling SoloAdventurerManager / PartyManager directly.

using System.Collections.Generic;
using UnityEngine;

public class AdventurerManager : MonoBehaviour
{
    public static AdventurerManager Instance { get; private set; }

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
    
    #region Roster pass-through (delegates to SoloAdventurerManager)
    // All registered adventurers. Read-only snapshot.
    public IReadOnlyList<AdventurerData> Adventurers
        => SoloAdventurerManager.Instance?.Adventurers
           ?? new List<AdventurerData>();

    // Looks up a single adventurer by their unique ID.
    public AdventurerData GetAdventurer(string id)
        => SoloAdventurerManager.Instance?.GetAdventurer(id);

    // Pending rank-up applications awaiting player review.
    public IReadOnlyList<RankUpApplicationData> PendingRankUpApplications
        => SoloAdventurerManager.Instance?.PendingRankUpApplications
           ?? new List<RankUpApplicationData>();

    // Approves a rank-up application by ID.
    public bool ApproveRankUpApplication(string applicationId)
        => SoloAdventurerManager.Instance?.ApproveRankUpApplication(applicationId) ?? false;

    // Rejects a rank-up application by ID.
    public bool RejectRankUpApplication(string applicationId)
        => SoloAdventurerManager.Instance?.RejectRankUpApplication(applicationId) ?? false;

    // Approves a regular quest application.
    public bool ApproveQuestApplication(QuestApplication application)
        => SoloAdventurerManager.Instance?.ApproveQuestApplication(application) ?? false;

    // Rejects a regular quest application and cancels adventurer states.
    public bool RejectQuestApplication(QuestApplication application)
        => SoloAdventurerManager.Instance?.RejectQuestApplication(application) ?? false;

    // Calculates success chance for a quest given a list of adventurer data objects.
    public float CalculateSuccessChance(QuestData quest, IEnumerable<AdventurerData> members)
        => SoloAdventurerManager.Instance?.CalculateSuccessChance(quest, members) ?? 0f;

    // Calculates success chance for a quest given member IDs.
    public float CalculateSuccessChance(QuestData quest, IEnumerable<string> memberIds)
        => SoloAdventurerManager.Instance?.CalculateSuccessChance(quest, memberIds) ?? 0f;
    #endregion
    
    #region Party pass-through (delegates to PartyManager)
    // All active parties.
    public IReadOnlyDictionary<string, PartyData> Parties
        => PartyManager.Instance?.Parties
           ?? new Dictionary<string, PartyData>();

    // Creates a new party and returns it.
    public PartyData CreateParty(string leaderId, IEnumerable<string> memberIds, bool isTemporary)
        => PartyManager.Instance?.CreateParty(leaderId, memberIds, isTemporary);

    // Looks up a party by ID.
    public PartyData GetParty(string id)
        => PartyManager.Instance?.GetParty(id);

    // Returns all adventurers in the given party.
    public List<AdventurerData> GetPartyMembers(string partyId)
        => PartyManager.Instance?.GetPartyMembers(partyId) ?? new List<AdventurerData>();
    #endregion
}