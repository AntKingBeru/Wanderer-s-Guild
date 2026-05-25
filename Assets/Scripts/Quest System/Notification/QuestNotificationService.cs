using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        public readonly NotificationType Type;
        public readonly string Message;
        public readonly DateTime Timestamp;
 
        public NotificationData(NotificationType type, string message)
        {
            Type      = type;
            Message   = message;
            Timestamp = DateTime.Now;
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
            Debug.Log($"[Notification] [{data.Type}] {data.Message}");
            OnNotification?.Invoke(data);
        }
    }
    
    #region Toast UI

    /// <summary>
    /// Displays brief pop-up toast notifications.
    /// Assign the panel and text fields in the Inspector.
    /// The panel should start inactive.
    /// </summary>
    public class QuestToastUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private TextMeshProUGUI toastText;
        [SerializeField] private Image toastIcon; // optional colored icon
 
        [Header("Settings")]
        [SerializeField] private float displayDuration = 3f;
        
        [Serializable]
        public struct TypeStyle
        {
            public NotificationType type;
            public Color color;
            public Sprite icon;
        }
        [SerializeField] private List<TypeStyle> typeStyles = new();
 
        private readonly Queue<NotificationData> _queue = new();
        private bool _showing;
 
        private void Start()
        {
            if (QuestNotificationService.Instance)
                QuestNotificationService.Instance.OnNotification += Enqueue;
            else
                Debug.LogWarning("[QuestToastUI] QuestNotificationService not found.");
 
            if (toastPanel) toastPanel.SetActive(false);
        }
 
        private void Enqueue(NotificationData data)
        {
            _queue.Enqueue(data);
            if (!_showing) StartCoroutine(ShowNext());
        }

        private IEnumerator ShowNext()
        {
            while (_queue.Count > 0)
            {
                _showing = true;
                var data = _queue.Dequeue();
                
                if (toastText)
                    toastText.text = data.Message;
                ApplyStyle(data.Type);
                if (toastPanel)
                    toastPanel.SetActive(true);
                
                yield return new WaitForSeconds(displayDuration);
                
                if (toastPanel)
                    toastPanel.SetActive(false);
                
                // Add a berief gap between toasts 
                yield return new WaitForSeconds(0.2f);
            }
            
            _showing = false;
        }

        private void ApplyStyle(NotificationType type)
        {
            foreach (var style in typeStyles.Where(style => style.type != type))
            {
                if (toastIcon)
                {
                    toastIcon.color = style.color;
                    if (style.icon)
                        toastIcon.sprite = style.icon;
                }

                break;
            }
        }
    }
    
    #endregion
    
    #region Badge counter

    /// <summary>
    /// Shows a numeric badge on a UI element.
    /// Increment when a new request arrives; decrement when a quest is created.
    /// Attach near the world-space button UI or any HUD icon.
    /// </summary>
    public class QuestNotificationBadge : MonoBehaviour
    {
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TextMeshProUGUI countText;
 
        private int _count;
 
        private void Start()
        {
            var questManager = QuestManager.Instance;
            if (!questManager)
                return;
 
            questManager.OnRequestReceived += _ => SetCount(_count + 1);
            questManager.OnQuestCreated += _ => SetCount(Mathf.Max(0, _count - 1));
 
            SetCount(0);
        }
 
        private void SetCount(int value)
        {
            _count = value;
            
            if (countText)
                countText.text = _count.ToString();
            
            if (badgeRoot)
                badgeRoot.SetActive(_count > 0);
        }
    }
    
    #endregion
}