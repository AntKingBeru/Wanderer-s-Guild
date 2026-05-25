using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace QuestSystem
{
    /// <summary>
    /// Central runtime manager.  Attach to a persistent GameObject in your scene (DontDestroyOnLoad).
    /// Responsibilities:
    ///   - Holds all active QuestRequestSO instances (injected at design time or at runtime).
    ///   - Holds all created (but not yet posted) quests.
    ///   - Holds the board slots (posted quests).
    ///   - Raises C# events that UI and notification systems subscribe to.
    /// Follows the Observer pattern via plain C# events so no scene coupling is needed.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static QuestManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Settings")] [Tooltip("Drag your QuestSettings asset here.")] [SerializeField]
        private QuestSettings settings;

        [Header("Initial Requests (optional)")]
        [Tooltip("Pre-populate with designer-authored requests. More can be added at runtime.")]
        [SerializeField]
        private List<QuestRequest> initialRequests = new();

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>
        /// Fired when a new request is registered (e.g., arrives from a client NPC).
        /// </summary>
        public event Action<QuestRequest> OnRequestReceived;

        /// <summary>
        /// Fired when a new QuestData is created by the quest creator.
        /// </summary>
        public event Action<QuestData> OnQuestCreated;

        /// <summary>
        /// Fired when a quest is posted to the board (dragged into a slot).
        /// </summary>
        public event Action<QuestData> OnQuestPosted;

        // ── Runtime State ─────────────────────────────────────────────────────────

        /// <summary>All pending requests (not yet turned into quests).</summary>
        public IReadOnlyList<QuestRequest> PendingRequests => _pendingRequests;

        private readonly List<QuestRequest> _pendingRequests = new();

        /// <summary>Quests created but not yet posted on the board.</summary>
        public IReadOnlyList<QuestData> CreatedQuests => _createdQuests;

        private readonly List<QuestData> _createdQuests = new();

        /// <summary>
        /// Board slots — fixed-size array.  null = empty slot.
        /// Size comes from QuestSettings.boardSlotCount.
        /// </summary>
        public QuestData[] BoardSlots { get; private set; }

        public int GlobalMaxRank => settings ? settings.globalMaxRank : 10;
        public int BoardSlotCount => settings ? settings.boardSlotCount : 10;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BoardSlots = new QuestData[GlobalMaxRank * GlobalMaxRank];

            // Register designed-authored initial requests silently (no notification on startup)
            foreach (var req in initialRequests.Where(req => req))
                _pendingRequests.Add(req);
        }

        /// <summary>
        /// Register a new request at runtime (e.g., from a client NPC script).
        /// Fires <see cref="OnRequestReceived"/> so the quest creator UI can react.
        /// </summary>
        public void ReceiveRequest(QuestRequest request)
        {
            if (!request)
                return;

            request.ClampToGlobalMax(GlobalMaxRank);
            _pendingRequests.Add(request);
            OnRequestReceived?.Invoke(request);
            Debug.Log($"[QuestManager] New request received: '{request.requestName}'");
        }

        /// <summary>
        /// Creates a quest from a request + player choices via the Factory.
        /// Removes the request from pending, adds the quest to created list,
        /// and fires <see cref="OnQuestCreated"/>.
        /// </summary>
        public QuestData CreateQuest(QuestRequest request, int chosenRank, int chosenReward)
        {
            var quest = QuestFactory.CreateFromRequest(request, chosenRank, chosenReward, GlobalMaxRank);

            _pendingRequests.Remove(request);
            _createdQuests.Add(quest);
            OnQuestCreated?.Invoke(quest);
            Debug.Log($"[QuestManager] Quest created: '{quest.QuestName}' (Rank {quest.Rank}, Points {quest.Points})");
            return quest;
        }

        /// <summary>
        /// Posts a quest into the given board slot index.
        /// Validates the slot is empty and the quest is in Created state.
        /// Fires <see cref="OnQuestPosted"/>.
        /// </summary>
        public bool PostQuestToSlot(QuestData quest, int slotIndex)
        {
            if (quest == null)
            {
                Debug.LogWarning("[QuestManager] PostQuestToSlot: quest is null.");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= BoardSlots.Length)
            {
                Debug.LogWarning($"[QuestManager] Slot index {slotIndex} is out of range.");
                return false;
            }

            if (BoardSlots[slotIndex] != null)
            {
                Debug.LogWarning($"[QuestManager] Slot {slotIndex} is already occupied.");
                return false;
            }

            if (quest.State != QuestState.Created)
            {
                Debug.LogWarning($"[QuestManager] Quest '{quest.QuestName}' is not in Created state.");
                return false;
            }
            
            quest.Post();
            BoardSlots[slotIndex] = quest;
            _createdQuests.Remove(quest);
            OnQuestPosted?.Invoke(quest);
            Debug.Log($"[QuestManager] Quest '{quest.QuestName}' posted to slot {slotIndex}.");
            return true;
        }
        
        /// <summary>
        /// Returns true if at least one board slot is empty.
        /// </summary>
        public bool HasEmptyBoardSlot()
        {
            return BoardSlots.Any(slot => slot == null);
        }
 
        /// <summary>
        /// Returns the first empty board slot index, or -1 if full.
        /// </summary>
        public int FirstEmptySlotIndex()
        {
            for (var i = 0; i < BoardSlots.Length; i++)
                if (BoardSlots[i] == null) return i;
            return -1;
        }
    }
}