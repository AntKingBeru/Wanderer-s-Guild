// Reusable component that visually represents a QuestData on a quest card.
// Used by QuestBoardSlotUI when a slot is filled with a posted quest.
// Sets background color from the quest's rank via QuestConfig.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestCardUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Image whose color is set to the quest's rank color.")]
    [SerializeField] private Image backgroundImage;
    
    [Tooltip("Displays the quest name.")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    
    [Tooltip("Displays the rank letter (e.g. 'S', 'Special').")]
    [SerializeField] private TextMeshProUGUI rankLabel;
    
    [Tooltip("Displays the quest category.")]
    [SerializeField] private TextMeshProUGUI categoryLabel;
    
    [Tooltip("Displays the adventurer reward in gold.")]
    [SerializeField] private TextMeshProUGUI rewardLabel;
    
    [Tooltip("Displays the party size limit.")]
    [SerializeField] private TextMeshProUGUI partySizeLabel;
    
    [Tooltip("Displays the current quest status.")]
    [SerializeField] private TextMeshProUGUI statusLabel;
    
    #region Public API
    // Populates all labels and applies the rank color. Activates the GameObject.
    public void Populate(QuestData quest)
    {
        if (quest == null)
        {
            Clear();
            return;
        }
        
        gameObject.SetActive(true);

        nameLabel.text = quest.QuestName;
        categoryLabel.text = quest.Category.ToString();
        rewardLabel.text = $"{quest.PartyReward}g";
        partySizeLabel.text = $"Party: 1-{quest.PartyLimit}";
        statusLabel.text = quest.Status.ToString();

        if (QuestManager.Instance?.Config)
        {
            var config =  QuestManager.Instance.Config.GetRankConfig(quest.Rank);
            rankLabel.text = config.DisplayName;
            if (backgroundImage)
                backgroundImage.color = config.CardColor;
        }
        else
        {
            rankLabel.text = quest.Rank.ToString();
        }
    }
    
    // Hides the card. Called when its board slot is cleared.
    public void Clear() 
        => gameObject.SetActive(false);
    #endregion
}