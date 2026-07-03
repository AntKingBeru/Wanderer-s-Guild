// Quest Board screen: draggable draft list + rank-sized slot grid; drops post a quest via QuestBoard.

using UnityEngine;
using UnityEngine.UIElements;

[DefaultExecutionOrder(5)]
public class QuestBoardScreen : UIScreen
{
    [Header("Display")]
    [SerializeField] private RankPalette rankPalette;
    
    [Header("Slot Backgrounds")]
    [Tooltip("Shown on an unlocked but empty slot.")]
    [SerializeField] private Sprite emptyUnlockedSlot;
    [Tooltip("Shown on a locked (not-yet-unlocked) slot.")]
    [SerializeField] private Sprite lockedSlot;
    
    [Header("Grid Layout")]
    [Tooltip("Slots per row in the board grid.")]
    [SerializeField] private int slotsPerRow = 5;

    private ScrollView _draftList;
    private VisualElement _slotGrid;
    private VisualElement _dragLayer;
    private Label _error;
    private Button _closeButton;

    private QuestCardFactory _cardFactory;
    private DragController _drag;
    private VisualElement[] _slotElements;
    private SlotAspectFitter _slotFitter;
    
    protected override void OnBuild(VisualElement root)
    {
        _draftList = root.Q<ScrollView>("draft-list");
        _slotGrid = root.Q<VisualElement>("slot-grid");
        _dragLayer = root.Q<VisualElement>("drag-layer");
        _error = root.Q<Label>("board-error");

        _cardFactory = new QuestCardFactory(rankPalette);
        _drag = new DragController(_dragLayer, BuildGhost, HandleDrop);
        _slotFitter = new SlotAspectFitter(1.4f);
        
        _closeButton = root.Q<Button>("close-screen");
        _closeButton?.RegisterCallback<ClickEvent>(_ => ScreenManager.Instance.Close(Id));
    }

    protected override void OnOpened()
    {
        Subscribe();
        RebuildSlots();
        RefreshDrafts();
    }

    protected override void OnClosed()
        => Unsubscribe();

    private void Subscribe()
    {
        if (!GameEventsRelay.Exists) return;
        var relay = GameEventsRelay.Instance;
        relay.onQuestCreated.AddListener(HandleDraftsChanged);
        relay.onQuestPosted.AddListener(HandlePostedChanged);
        relay.onQuestExpired.AddListener(HandlePostedChanged);
        relay.onGuildRankChanged.AddListener(HandleRankChanged);
    }

    private void Unsubscribe()
    {
        if (!GameEventsRelay.Exists) return;
        var relay = GameEventsRelay.Instance;
        relay.onQuestCreated.RemoveListener(HandleDraftsChanged);
        relay.onQuestPosted.RemoveListener(HandlePostedChanged);
        relay.onQuestExpired.RemoveListener(HandlePostedChanged);
        relay.onGuildRankChanged.RemoveListener(HandleRankChanged);
    }
    
    private void RefreshDrafts()
    {
        if (_draftList == null || !QuestBoard.Exists)
            return;
        _draftList.Clear();

        foreach (var q in QuestBoard.Instance.GetDrafts())
        {
            var card = _cardFactory.Build(q, "quest-card--draft");
            _drag.MakeDraggable(card, q.Id);   // payload = quest id
            _draftList.Add(card);
        }
    }
    
    private void RebuildSlots()
    {
        if (_slotGrid == null || !QuestBoard.Exists)
            return;
        _slotGrid.Clear();
        _slotFitter.Clear();

        var maxSlots = GameConfig.Instance.Guild.MaxBoardSlots;
        var perRow = Mathf.Max(1, slotsPerRow);
        _slotElements = new VisualElement[maxSlots];

        VisualElement currentRow = null;
        for (var i = 0; i < maxSlots; i++)
        {
            if (i % perRow == 0)
            {
                currentRow = new VisualElement();
                currentRow.AddToClassList("slot-row");
                _slotGrid.Add(currentRow);
            }

            var slot = new VisualElement { name = $"slot-{i}" };
            slot.AddToClassList("board-slot");
            slot.userData = i;
            _slotElements[i] = slot;
            currentRow?.Add(slot);
            _slotFitter.Track(slot);
            RenderSlot(i);
        }
    }
    
    private void RenderSlot(int index)
    {
        if (_slotElements == null || index < 0 || index >= _slotElements.Length)
            return;
        var slot = _slotElements[index];
        slot.Clear();
        slot.RemoveFromClassList("board-slot--filled");
        slot.RemoveFromClassList("board-slot--locked");
        slot.style.backgroundImage = StyleKeyword.Null;

        var unlocked = index < QuestBoard.Instance.SlotCount;

        if (!unlocked)
        {
            slot.AddToClassList("board-slot--locked");
            ApplySlotImage(slot, lockedSlot);
            var lockLabel = new Label("Locked");
            lockLabel.AddToClassList("board-slot__hint");
            slot.Add(lockLabel);
            return;
        }

        var q = QuestBoard.Instance.GetSlot(index);
        if (q == null)
        {
            ApplySlotImage(slot, emptyUnlockedSlot);
            var hint = new Label("Empty");
            hint.AddToClassList("board-slot__hint");
            slot.Add(hint);
        }
        else
        {
            slot.AddToClassList("board-slot--filled");
            slot.Add(_cardFactory.Build(q));
        }
    }
    
    private static void ApplySlotImage(VisualElement slot, Sprite sprite)
    {
        if (sprite)
            slot.style.backgroundImage = new StyleBackground(sprite);
    }
    
    private VisualElement BuildGhost(int questId)
    {
        var q = QuestBoard.Exists ? QuestBoard.Instance.GetDraft(questId) : null;
        if (q == null)
            return null;
        var ghost = _cardFactory.Build(q, "quest-card--ghost");
        ghost.style.width = 220;
        return ghost;
    }
    
    private void HandleDrop(int questId, Vector2 screenPos)
    {
        var slotIndex = SlotIndexAt(screenPos);
        Debug.Log($"[Drop] pos={screenPos} slot={slotIndex} | slot0 bound={_slotElements[0].worldBound}");
        if (slotIndex < 0)
            return;

        if (slotIndex >= QuestBoard.Instance.SlotCount)   // dropped on a locked slot
        {
            ShowError("That slot is locked. Raise your guild rank to unlock it.");
            return;
        }

        if (!QuestBoard.Instance.PostToSlot(questId, slotIndex, out var error))
        {
            ShowError(error);
            return;
        }
        ClearError();
        RenderSlot(slotIndex);
    }
    
    private int SlotIndexAt(Vector2 screenPos)
    {
        if (_slotElements == null) return -1;
        for (var i = 0; i < _slotElements.Length; i++)
        {
            var slot = _slotElements[i];
            if (slot == null)
                continue;
            if (slot.worldBound.Contains(screenPos))
                return i;
        }
        return -1;
    }
    
    private void HandleDraftsChanged(int _)
        => RefreshDrafts();

    private void HandlePostedChanged(int _)
    {
        RefreshDrafts();
        RebuildSlots();
    }
    private void HandleRankChanged(GuildRank _)
        => RebuildSlots();

    private void ShowError(string message)
    {
        if (_error == null)
            return;
        _error.text = message ?? string.Empty;
        _error.style.display = DisplayStyle.Flex;
    }

    private void ClearError()
    {
        if (_error == null)
            return;
        _error.style.display = DisplayStyle.None;
    }
}