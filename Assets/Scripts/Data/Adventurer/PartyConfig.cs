// ScriptableObject holding every designer-tunable number for the party system: size limits,
// the temporary-party trial window, and the cooldown after a failed trial disband.

using UnityEngine;

[CreateAssetMenu(fileName = "PartyConfig", menuName = "Guild Manager/Party/Party Config")]
public class PartyConfig : ScriptableObject
{
    [Header("Party Size")]
    [Tooltip("Smallest group that counts as a party. Below this, they're solo adventurers.")]
    [SerializeField, Min(2)] private int minPartySize = 2;

    [Tooltip("Largest group a single party can hold. Applies equally regardless of member rank.")]
    [SerializeField, Min(2)] private int maxPartySize = 4;

    [Header("Temporary Party Trial")]
    [Tooltip("Minimum quests a temporary party must complete together before a decision " +
             "(permanent/disband) can be made.")]
    [SerializeField, Min(1)] private int minTrialQuests = 2;

    [Tooltip("Maximum quests in the trial — if results are still mixed at this point, " +
             "majority rules. A tie disbands (fail-safe default).")]
    [SerializeField, Min(1)] private int maxTrialQuests = 3;

    [Header("Disband Cooldown")]
    [Tooltip("In-game days a disbanded trial party's members must wait before joining ANY new party.")]
    [SerializeField, Min(0f)] private float disbandCooldownDays = 3f;

    public int MinPartySize => minPartySize;
    public int MaxPartySize => maxPartySize;
    public int MinTrialQuests => minTrialQuests;
    public int MaxTrialQuests => maxTrialQuests;
    public float DisbandCooldownDays => disbandCooldownDays;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (minPartySize > maxPartySize)
            Debug.LogWarning("[PartyConfig] MinPartySize should not exceed MaxPartySize.");
        if (minTrialQuests > maxTrialQuests)
            Debug.LogWarning("[PartyConfig] MinTrialQuests should not exceed MaxTrialQuests.");
    }
#endif
}