using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QuestSystem.UI
{
    /// <summary>
    /// Root controller for the Quest Creator window.
    /// /// </summary>
    public class QuestCreatorUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject rootPanel;
 
        [Header("Left Panel – Request List")]
        [SerializeField] private Transform requestListContent;
        [SerializeField] private RequestListItemUI requestItemPrefab;
        
        [Header("Middle Panel")]
        [SerializeField] private QuestCreationPanelUI creationPanel;
 
        [Header("Right Panel")]
        [SerializeField] private Button closeButton;
 
        [Header("Popup")]
        [SerializeField] private RequestPopupUI requestPopup;
 
        // Track spawned list items so we can refresh the list
        private readonly List<RequestListItemUI> _spawnedItems = new();
 
        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            requestPopup.OnCreateQuestClicked += OnCreateQuestFromPopup;
            rootPanel.SetActive(false);
        }
 
        private void OnEnable()
        {
            if (QuestManager.Instance)
                QuestManager.Instance.OnRequestReceived += OnNewRequestArrived;
        }
 
        private void OnDisable()
        {
            if (QuestManager.Instance)
                QuestManager.Instance.OnRequestReceived -= OnNewRequestArrived;
        }
        
        public void Show()
        {
            RefreshRequestList();
            rootPanel.SetActive(true);
        }
 
        public void Hide()
        {
            requestPopup.Hide();
            rootPanel.SetActive(false);
        }
        
        private void RefreshRequestList()
        {
            // Clear old items
            foreach (var item in _spawnedItems.Where(item => item)) Destroy(item.gameObject);
            _spawnedItems.Clear();
 
            if (!QuestManager.Instance)
                return;
 
            foreach (var req in QuestManager.Instance.PendingRequests)
            {
                var item = Instantiate(requestItemPrefab, requestListContent);
                item.Bind(req);
                item.OnSelected += OnRequestSelected;
                _spawnedItems.Add(item);
            }
        }
 
        private void OnRequestSelected(QuestRequest request)
        {
            requestPopup.Show(request);
        }
 
        private void OnCreateQuestFromPopup(QuestRequest request)
        {
            creationPanel.PopulateFromRequest(request);
            // Remove the corresponding list item immediately
            var toRemove = _spawnedItems.Find(i => i.Request == request);
            if (toRemove)
            {
                _spawnedItems.Remove(toRemove);
                Destroy(toRemove.gameObject);
            }
        }
 
        private void OnNewRequestArrived(QuestRequest _)
        {
            // Refresh list if the window is open so new requests appear in real-time
            if (rootPanel.activeSelf)
                RefreshRequestList();
        }
    }
}