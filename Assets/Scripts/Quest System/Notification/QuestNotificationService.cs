using System;
using UnityEngine;

namespace QuestSystem
{
    public enum NotificationType
    {
        NewRequest,
        QuestCreated,
        QuestPosted
    }

    public readonly struct NotificationData
    {
        public readonly NotificationType type;
        public readonly string message;
        public readonly DateTime timestamp;
 
        public NotificationData(NotificationType type, string message)
        {
            this.type = type;
            this.message = message;
            timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Thin event bus that decouples notification producers from consumers.
    /// The QuestManager fires events; this service forwards them as typed notifications.
    /// Any UI component (toast, badge, log panel) subscribes here.
    /// </summary>
    public class QuestNotificationService : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static QuestNotificationService Instance { get; private set; }
        
        /// <summary>
        /// Subscribe to receive all quest-system notifications.
        /// </summary>
        public event Action<NotificationData> OnNotification;

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

        private void Start()
        {
            var questManager = QuestManager.Instance;
            if (!questManager)
            {
                Debug.LogError("[QuestNotificationService] QuestManager not found.");
                return;
            }
            
            questManager.OnRequestReceived += r => Dispatch(new NotificationData(NotificationType.NewRequest,
                $"New request received: \"{r.requestName}\". Head to the Quest Creator!"));
            
            questManager.OnQuestCreated += q => Dispatch(new NotificationData(NotificationType.QuestCreated,
                $"Quest \"{q.QuestName}\" has been created. Post it on the board!"));
            
            questManager.OnQuestPosted += q => Dispatch(new NotificationData(NotificationType.QuestPosted,
                $"Quest \"{q.QuestName}\" is now posted on the board."));
        }

        private void Dispatch(NotificationData data)
        {
            Debug.Log($"[Notification] [{data.type}] {data.message}");
            OnNotification?.Invoke(data);
        }
    }
}