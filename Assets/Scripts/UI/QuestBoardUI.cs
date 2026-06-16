// Controls the Quest Board screen.
// Uses a CanvasGroup for visibility so the GameObject stays active at all times.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestBoardUI : MonoBehaviour
{
    [Header("Screen Visibility")]
    [Tooltip("CanvasGroup on the QuestBoardScreen root.")]
    [SerializeField] private CanvasGroup screenCanvasGroup;
    
    [Header("Left Panel - Unposted Quests")]
    [Tooltip("VerticalLayoutGroup content object inside the unposted quests scroll view.")]
    [SerializeField] private Transform unpostedListContent;
    
    [Tooltip("Prefab for a single draggable unposted quest card.")]
    [SerializeField] private UnpostedQuestItemUI unpostedItemPrefab;
    
    [Tooltip("The root Canvas of the HUD. Drag ghosts are parented here so they render above all other panels during a drag.")]
    [SerializeField] private Canvas rootCanvas;
    
    [Header("Right Panel - Board Slots")]
    [Tooltip("All 10 QuestBoardSlotUI components in order (slots 0-9). " +
             "Assign in the inspector matching the visual layout left-to-right, top-to-bottom.")]
    [SerializeField] private QuestBoardUISlot[] boardSlots;
    
    [Header("Buttons")]
    [Tooltip("Bottom-Right of the screen. Closes the Quest Board")]
    [SerializeField] private Button closeButton;
    
    private readonly List<UnpostedQuestItemUI> _unpostedItems = new List<UnpostedQuestItemUI>();
    
    #region Lifecycle
    private void Awake()
    {
        closeButton?.onClick.AddListener(HandleClose);

        // Assign each slot its index so drop events know which slot was targeted.
        if (boardSlots != null)
            for (var i = 0; i < boardSlots.Length; i++)
                boardSlots[i]?.Initialize(i);
    }

    private void Start()
    {
        // Apply initial slot visibility based on the guild's starting rank.
        RefreshSlotVisibility();
    }

    private void OnDestroy()
    {
        closeButton?.onClick.RemoveListener(HandleClose);
    }

    private void OnEnable()
    {
        if (InteractionManager.Instance)
        {
            InteractionManager.Instance.OnScreenOpened += HandleScreenOpened;
            InteractionManager.Instance.OnScreenClosed += HandleScreenClosed;
        }

        GameEventRelay.Instance.onUnpostedQuestsChanged.AddListener(RefreshUnpostedList);
        GameEventRelay.Instance.onBoardChanged.AddListener(RefreshAllSlots);
        GameEventRelay.Instance.onQuestStatusChanged.AddListener(HandleQuestStatusChanged);
        // Reveal newly unlocked slots whenever the guild ranks up.
        GameEventRelay.Instance.onProgressionRankChanged.AddListener(HandleRankChanged);
        HideScreen();
    }

    private void OnDisable()
    {
        if (InteractionManager.Instance)
        {
            InteractionManager.Instance.OnScreenOpened -= HandleScreenOpened;
            InteractionManager.Instance.OnScreenClosed -= HandleScreenClosed;
        }

        GameEventRelay.Instance.onUnpostedQuestsChanged.RemoveListener(RefreshUnpostedList);
        GameEventRelay.Instance.onBoardChanged.RemoveListener(RefreshAllSlots);
        GameEventRelay.Instance.onQuestStatusChanged.RemoveListener(HandleQuestStatusChanged);
        GameEventRelay.Instance.onProgressionRankChanged.RemoveListener(HandleRankChanged);
    }
    #endregion
    
    #region Screen Visibility
    private void ShowScreen()
    {
        if (!screenCanvasGroup)
            return;
        screenCanvasGroup.alpha = 1f;
        screenCanvasGroup.interactable = true;
        screenCanvasGroup.blocksRaycasts = true;
    }

    private void HideScreen()
    {
        if (!screenCanvasGroup)
            return;
        screenCanvasGroup.alpha = 0f;
        screenCanvasGroup.interactable = false;
        screenCanvasGroup.blocksRaycasts = false;
    }

    private void HandleScreenOpened(ScreenType type)
    {
        if (type != ScreenType.QuestBoard)
            return;
        ShowScreen();
        RefreshUnpostedList();
        RefreshAllSlots();
    }

    private void HandleScreenClosed(ScreenType type)
    {
        if (type != ScreenType.QuestBoard)
            return;
        HideScreen();
    }

    private void HandleClose()
        => InteractionManager.Instance?.CloseScreen();
    #endregion
    
    #region Rank Change
    // Called when the guild ranks up. Shows any newly unlocked slots then refreshes their content.
    private void HandleRankChanged(int newRank)
    {
        RefreshSlotVisibility();
        RefreshAllSlots();
    }

    // Shows slots up to ActiveBoardSlots and hides the rest entirely.
    private void RefreshSlotVisibility()
    {
        if (boardSlots == null)
            return;

        var activeSlots = ProgressionSystem.Instance
            ? ProgressionSystem.Instance.ActiveBoardSlots
            : boardSlots.Length;

        for (var i = 0; i < boardSlots.Length; i++)
        {
            if (!boardSlots[i])
                continue;
            // Slots at index < activeSlots are visible; the rest are hidden.
            boardSlots[i].SetVisible(i < activeSlots);
        }
    }
    #endregion
    
    #region Left Panel
    private void RefreshUnpostedList()
    {
        foreach (var item in _unpostedItems)
            Destroy(item.gameObject);
        _unpostedItems.Clear();
        if (!QuestManager.Instance || !unpostedItemPrefab)
            return;
        foreach (var quest in QuestManager.Instance.UnpostedQuests)
        {
            var item = Instantiate(unpostedItemPrefab, unpostedListContent);
            item.Populate(quest, rootCanvas);
            _unpostedItems.Add(item);
        }
    }
    #endregion
    
    #region Right Panel
    // Full sync of all visible slots against QuestManager's board state.
    private void RefreshAllSlots()
    {
        if (!QuestManager.Instance || boardSlots == null)
            return;
        for (var i = 0; i < boardSlots.Length; i++)
        {
            // Skip hidden slots — they have no content to sync.
            if (!boardSlots[i] || !boardSlots[i].IsVisible)
                continue;
            var slotQuest = QuestManager.Instance.GetBoardSlot(i);
            if (slotQuest != null)
                boardSlots[i].SetOccupied(slotQuest);
            else
                boardSlots[i].SetEmpty();
        }
    }

    // Targeted refresh: find the slot displaying this quest and update it.
    // Falls back to a full refresh if the quest was removed from the board.
    private void HandleQuestStatusChanged(QuestData quest)
    {
        if (boardSlots == null)
            return;
        foreach (var slot in boardSlots)
        {
            if (!slot || !slot.IsVisible || slot.OccupiedQuest != quest)
                continue;
            slot.Refresh();
            return;
        }

        // Quest not found in any visible slot — it may have been removed. Full refresh.
        RefreshAllSlots();
    }
    #endregion
}