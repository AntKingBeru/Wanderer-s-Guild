// Manages the scrollable applications list in the reception desk right panel.
// Subscribes to QuestManager.OnApplicationSubmitted and
// AdventurerManager.OnRankUpApplicationCreated to add items.
// Subscribes to status-change events to remove resolved or expired items.

using System.Collections.Generic;
using UnityEngine;

public class ApplicationListUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab instantiated for each application entry.")]
    [SerializeField] private ApplicationListItemUI itemPrefab;

    [Tooltip("Detail overlay to open when a list item is clicked.")]
    [SerializeField] private ApplicationDetailUI detailUI;

    [Tooltip("Used to resolve board slot counts when looking up a quest by ID.")]
    [SerializeField] private QuestConfig questConfig;

    private readonly List<ApplicationListItemUI> _items = new();
    
    #region Lifecycle
    private void OnEnable()
    {
        GameEventRelay.Instance.onApplicationSubmitted.AddListener(HandleApplicationSubmitted);
        GameEventRelay.Instance.onApplicationRejected.AddListener(HandleApplicationRejected);
        GameEventRelay.Instance.onQuestStatusChanged.AddListener(HandleQuestStatusChanged);
        GameEventRelay.Instance.onRankUpApplicationCreated.AddListener(HandleRankUpCreated);
        GameEventRelay.Instance.onRankUpApplicationResolved.AddListener(HandleRankUpResolved);
    }

    private void OnDisable()
    {
        GameEventRelay.Instance.onApplicationSubmitted.RemoveListener(HandleApplicationSubmitted);
        GameEventRelay.Instance.onApplicationRejected.RemoveListener(HandleApplicationRejected);
        GameEventRelay.Instance.onQuestStatusChanged.RemoveListener(HandleQuestStatusChanged);
        GameEventRelay.Instance.onRankUpApplicationCreated.RemoveListener(HandleRankUpCreated);
        GameEventRelay.Instance.onRankUpApplicationResolved.RemoveListener(HandleRankUpResolved);
    }
    #endregion
    
    #region Event Handlers
    private void HandleApplicationSubmitted(QuestApplication application)
    {
        var quest = FindQuestById(application.QuestId);
        if (quest == null)
            return;
        SpawnItem(item => item.InitializeRegular(application, quest, questConfig));
    }
    
    private void HandleApplicationRejected(QuestApplication application)
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            if (!item.IsRankUp && item.RegularApplication?.ApplicationId == application.ApplicationId)
            {
                DestroyItem(i);
                return;
            }
        }
    }

    private void HandleRankUpCreated(RankUpApplicationData application)
    {
        SpawnItem(item => item.InitializeRankUp(application, questConfig));
    }

    private void HandleQuestStatusChanged(QuestData quest)
    {
        var remove = quest.Status is QuestStatus.InProgress or QuestStatus.Completed or QuestStatus.Failed or QuestStatus.Expired;
        if (remove)
            RemoveItemForQuest(quest.QuestId);
    }
    
    private void HandleRankUpResolved(RankUpApplicationData application)
        => RemoveRankUpItem(application.ApplicationId);
    #endregion
    
    #region Item Click Handling
    private void HandleItemClicked(ApplicationListItemUI item)
    {
        if (!detailUI)
            return;
        if (item.IsRankUp)
        {
            var adventurer = AdventurerManager.Instance?.GetAdventurer(item.RankUpApplication.AdventurerId);
            detailUI.ShowRankUp(item.RankUpApplication, adventurer);
        }
        else
        {
            detailUI.ShowRegular(item.Quest, item.RegularApplication);
        }
    }
    #endregion
    
    #region Helpers
    private void SpawnItem(System.Action<ApplicationListItemUI> initialize)
    {
        if (!itemPrefab)
            return;
        var item = Instantiate(itemPrefab, transform);
        initialize(item);
        item.OnItemClicked += HandleItemClicked;
        _items.Add(item);
    }

    private void RemoveItemForQuest(string questId)
    {

        for (var i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            if (!item.IsRankUp && item.Quest?.QuestId == questId)
                DestroyItem(i);
        }
    }

    private void RemoveRankUpItem(string applicationId)
    {
        for (var i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            if (item.IsRankUp && item.RankUpApplication?.ApplicationId == applicationId)
            {
                DestroyItem(i);
                return;
            }
        }
    }

    private void DestroyItem(int index)
    {
        _items[index].OnItemClicked -= HandleItemClicked;
        Destroy(_items[index].gameObject);
        _items.RemoveAt(index);
    }

    private QuestData FindQuestById(string id)
    {
        if (!QuestManager.Instance)
            return null;
        var slots = QuestManager.Instance.Config?.MaxBoardSlots ?? 10;
        for (var i = 0; i < slots; i++)
        {
            var quest = QuestManager.Instance.GetBoardSlot(i);
            if (quest != null && quest.QuestId == id)
                return quest;
        }
        return null;
    }
    #endregion
}