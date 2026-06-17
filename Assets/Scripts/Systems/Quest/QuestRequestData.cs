// ScriptableObject representing one request template in the guild's request pool.
// Create instances at Assets/Data/Quest/Requests/ via Create → Guild Manager → Quest Request.
// QuestManager holds the full pool and draws RequestsPerDay entries each in-game day, making them available at the reception desk.

using System;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestRequest_New", menuName = "Guild Manager/Quest Request")]
public class QuestRequestData : ScriptableObject
{
    #region Variables
    [Header("Identity")]
    [Tooltip("Unique identifier for this request. Auto-generated as a GUID on creation. " +
             "Can be replaced with a readable slug (e.g. 'goblin_cave_01'). " +
             "Must remain unique across all requests — used as a lookup key when " +
             "external file-reading is added later.")]
    [SerializeField] private string requestId = "";
    
    [Tooltip("Short title shown in the reception desk request list and on the quest card.")]
    [SerializeField] private string requestName = "New Request";

    [Tooltip("Flavour description displayed when the guild manager reviews this request. " +
             "No gameplay effect.")]
    [TextArea(3, 6)]
    [SerializeField] private string description = "";

    [Tooltip("Location name for flavour only. Shown on the quest card; never used in calculations.")]
    [SerializeField] private string location = "";
    
    [Header("Classification")]
    [Tooltip("The type of work this quest involves. Determines which adventurer classes " +
             "receive affinity bonuses or penalties during success chance calculation.")]
    [SerializeField] private QuestCategory category = QuestCategory.Combat;

    [Tooltip("Base difficulty rank of this request. The player may set the final quest rank " +
             "one step above or one step below this value when creating the quest " +
             "(clamped to F at the low end, Special at the high end).")]
    [SerializeField] private QuestRank baseRank = QuestRank.F;
    
    [Header("Parameters")]
    [Tooltip("Maximum gold the client is offering. The player chooses the adventurer reward " +
             "anywhere between 0 and this amount via slider when creating the quest. " +
             "Any remainder above the chosen reward goes to the guild's funds on success.")]
    [SerializeField, Min(0)] private int maxReward = 100;

    [Tooltip("Minimum number of adventurers permitted on this quest. " +
             "1 = solo only. 2–5 = up to this many adventurers (solo or party applicants).")]
    [SerializeField, Range(1, 5)] private int partyMin = 1;
    
    [Tooltip("Maximum number of adventurers permitted on this quest. " +
             "1 = solo only. 2–5 = up to this many adventurers (solo or party applicants).")]
    [SerializeField, Range(1, 5)] private int partyLimit = 3;

    [Tooltip("Duration in in-game hours. Serves two roles once the quest is posted: " +
             "(1) expiry timer — if no application is approved before it hits zero the quest is removed; " +
             "(2) the absolute deadline a dispatched party must return by.")]
    [SerializeField, Min(1)] private int timeLimitHours = 24;
    
    [Header("Hidden Tags")]
    [Tooltip("Arbitrary string tags reserved for future systems such as lore triggers, " +
             "adventurer trait interactions, or world events. Safe to leave empty for now.")]
    [SerializeField] private string[] hiddenTags = Array.Empty<string>();
    #endregion
    
    #region Public Accessors
    public string RequestId => requestId;
    public string RequestName => requestName;
    public string Description => description;
    public string Location => location;
    public QuestCategory Category => category;
    public QuestRank BaseRank => baseRank;
    public int MaxReward => maxReward;
    public int PartyMin => partyMin;
    public int PartyLimit => partyLimit;
    public int TimeLimitHours => timeLimitHours;
    #endregion
    
    #region Rank Range Helpers
    // The lowest rank the player may assign when creating a quest from this request.
    // Clamps at F(0) so the result is always a valid QuestRank value.
    public QuestRank GetMinAllowedRank()
        => (QuestRank)Mathf.Max(0, (int)baseRank - 1);
    
    // The highest rank the player may assign when creating a quest from this request.
    // Clamps at Special(7) so the result is always a valid QuestRank value.
    public QuestRank GetMaxAllowedRank() 
        => (QuestRank)Mathf.Min(7, (int)baseRank + 1);
    
    // Returns true if the given rank falls within the ±1 window of this request's base rank.
    public bool IsRankAllowed(QuestRank rank)
    {
        var r = (int)rank;
        return r >= (int)GetMinAllowedRank() && r <= (int)GetMaxAllowedRank();
    }
    
    // Returns a shallow copy of the hidden tags array so external code cannot mutate the source asset data.
    public string[] GetHiddenTagsCopy() 
        => (string[])hiddenTags.Clone();
    #endregion
    
#if UNITY_EDITOR
    // Called by Unity when the asset is first created via the Create menu, or when Reset is selected in the inspector context menu.
    // Generates a unique ID automatically so no two assets start with a blank ID.
    private void Reset()
    {
        requestId = Guid.NewGuid().ToString();
    }
    
    // Warns in the editor if a request has been saves without an ID, which would cause silent lookup failures when file-reading is added.
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(requestId))
            Debug.LogWarning($"[QuestRequestData] '{name}' has no Request ID set. " +
                             $"Use the Inspector Reset button to auto-generate one.");

        // Guard against an impossible party-size window (e.g. min 4, max 2) -
        // such a quest could never receive a valid application.
        if (partyMin > partyLimit)
        {
            Debug.LogWarning($"[QuestRequestData] '{name}' has PartyMin ({partyMin}) greater than " +
                             $"PartyLimit ({partyLimit}). Clamping PartyMin down to match.");
            partyMin = partyLimit;
        }
    }
#endif
}