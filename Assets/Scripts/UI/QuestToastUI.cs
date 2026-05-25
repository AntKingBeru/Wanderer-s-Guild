using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuestSystem.UI
{
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
                    toastText.text = data.message;
                ApplyStyle(data.type);
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
}