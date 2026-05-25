using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QuestSystem.UI
{
    /// <summary>
    /// Root controller for the Quest Board window.
    /// The 10 BoardSlotUI GameObjects should be placed in the editor in the
    /// BoardSlotsGrid. Their SlotIndex is assigned automatically by this script.
    /// </summary>
    public class QuestBoardUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Canvas rootCanvas;
        
        [Header("Left Panel – Unposted Quests")]
        [SerializeField] private Transform unpostedListContent;
        [SerializeField] private QuestCardUI questCardPrefab;
        
        [Header("Right Panel – Board Slots")]
        [SerializeField] private List<BoardSlotUI> boardSlots = new();
        [SerializeField] private Button closeButton;
 
        private QuestBoardInteractable _ownerInteractable;
        private readonly List<QuestCardUI> _spawnedCards = new();
 
        private void Awake()
        {
            closeButton.onClick.AddListener(OnClickClosed);
 
            // Assign slot indices
            for (var i = 0; i < boardSlots.Count; i++)
                boardSlots[i].SlotIndex = i;
 
            rootPanel.SetActive(false);
        }
 
        private void OnEnable()
        {
            if (QuestManager.Instance)
                QuestManager.Instance.OnQuestCreated += OnQuestCreated;
        }
 
        private void OnDisable()
        {
            if (QuestManager.Instance)
                QuestManager.Instance.OnQuestCreated -= OnQuestCreated;
        }
        
        public void Show(QuestBoardInteractable owner = null)
        {
            _ownerInteractable = owner;
            RefreshUnpostedList();
            RefreshBoardSlots();
            rootPanel.SetActive(true);
        }

        public void Hide() => rootPanel.SetActive(false);
        
        private void OnClickClosed()
        {
            if (_ownerInteractable)
                _ownerInteractable.RequestClose();
            else
                Hide();
        }
        
        private void RefreshUnpostedList()
        {
            foreach (var card in _spawnedCards.Where(card => card)) Destroy(card.gameObject);
            _spawnedCards.Clear();
 
            if (!QuestManager.Instance)
                return;
 
            foreach (var quest in QuestManager.Instance.CreatedQuests)
                SpawnCard(quest);
        }
 
        private void RefreshBoardSlots()
        {
            if (!QuestManager.Instance)
                return;
 
            var slots = QuestManager.Instance.BoardSlots;
            for (var i = 0; i < boardSlots.Count && i < slots.Length; i++)
            {
                if (slots[i] != null)
                    boardSlots[i].Fill(slots[i]);
                else
                    boardSlots[i].SetEmpty();
            }
        }
 
        private void SpawnCard(QuestData quest)
        {
            var card = Instantiate(questCardPrefab, unpostedListContent);
            card.Bind(quest, rootCanvas);
            card.OriginalParent = unpostedListContent;
            card.OnDroppedOnSlot += OnCardDroppedOnSlot;
            _spawnedCards.Add(card);
        }
 
        private void OnCardDroppedOnSlot(QuestCardUI card, BoardSlotUI slot)
        {
            if (!slot.IsEmpty || !QuestManager.Instance)
                return;
 
            var success = QuestManager.Instance.PostQuestToSlot(card.Quest, slot.SlotIndex);
            if (success)
            {
                slot.Fill(card.Quest);
                _spawnedCards.Remove(card);
                Destroy(card.gameObject);
            }
            else
            {
                // Snap card back to list (handled in QuestCardUI.OnEndDrag fallback)
                Debug.LogWarning("[QuestBoardUI] PostQuestToSlot failed.");
            }
        }
 
        private void OnQuestCreated(QuestData quest)
        {
            // If the board window is open, add the new card immediately
            if (rootPanel.activeSelf)
                SpawnCard(quest);
        }
    }
}