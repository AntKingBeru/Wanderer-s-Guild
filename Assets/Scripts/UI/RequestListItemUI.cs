// A single clickable entry in the Reception Desk's request list.
// Fires OnClicked with the associated QuestRequest when pressed.
// Instantiated at runtime by ReceptionDeskUI.

using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class RequestListItemUI : MonoBehaviour
{
    [Header("Labels")]
    [Tooltip("Displays the request name.")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    
    [Tooltip("Displays the rank letter.")]
    [SerializeField] private TextMeshProUGUI rankLabel;
    
    [Tooltip("Desplays the quest category.")]
    [SerializeField] private TextMeshProUGUI categoryLabel;
    
    [Header("Visuals")]
    [Tooltip("Thin color bar on the side of the item, tinted to the request's rank color.")]
    [SerializeField] private Image rankColorBar;
    
    [Tooltip("The button component on this item's root or a child.")]
    [SerializeField] private Button button;
    
    // Fires when the player click this item.
    public event Action<QuestRequest> OnClicked;
    
    private QuestRequest _request;
    
    #region Lifecycle
    private void Awake()
    {
        if (button)
            button.onClick.AddListener(HandleClick);
        else
            Debug.LogWarning($"[RequestListItemUI] '{name}' has no Button reference assigned.");
    }

    private void OnDestroy()
    {
        if (button)
            button.onClick.RemoveListener(HandleClick);
    }
    #endregion
    
    #region Public API

    public void Populate(QuestRequest request)
    {
        _request = request;
        nameLabel.text = request.RequestName;
        categoryLabel.text = request.Category.ToString();

        if (QuestManager.Instance?.Config)
        {
            var config = QuestManager.Instance.Config.GetRankConfig(request.BaseRank);
            rankLabel.text = config.DisplayName;
            if (rankColorBar)
                rankColorBar.color = config.CardColor;
        }
        else
        {
            rankLabel.text = request.BaseRank.ToString();
        }
    }
    #endregion
    
    #region Private
    private void HandleClick()
    {
        if (_request is { IsAvailable: true })
            OnClicked?.Invoke(_request);
    }
    #endregion
}