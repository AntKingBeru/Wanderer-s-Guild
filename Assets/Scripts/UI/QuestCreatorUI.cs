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
        
        // The world-prop that opened us - needed so Close re-enables camera controls.
        private QuestCreatorInteractable _ownerInteractable;
 
        // Track spawned list items so we can refresh the list
        private readonly List<RequestListItemUI> _spawnedItems = new();
        
        // Tracks which request is loaded into the middle panel (but not yet confirmed).
        // We must NOT remove it from the list until the quest is actually created.
        private QuestRequest _pendingRequest;
 
        private void Awake()
        {
            closeButton.onClick.AddListener(OnCloseClicked);
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
        
        /// <summary>
        /// Open the window. Pass the interactable so the Close button can re-enable
        /// the camera action map via RequestClose().
        /// </summary>
        public void Show(QuestCreatorInteractable owner = null)
        {
            _ownerInteractable = owner;
            _pendingRequest = null;
            RefreshRequestList();
            creationPanel.ShowEmpty();
            rootPanel.SetActive(true);
        }
 
        public void Hide()
        {
            requestPopup.Hide();
            rootPanel.SetActive(false);
        }

        private void OnCloseClicked()
        {
            // Restore any in-progress request back to the list before closing
            // (in case the player had loaded on into the middle panel but didn't confirm)
            _pendingRequest = null;

            if (_ownerInteractable)
                _ownerInteractable.RequestClose();
            else
                Hide();
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
            _pendingRequest = request;
            creationPanel.PopulateFromRequest(request, OnQuestConfirmed);
        }

        /// <summary>
        ///  Callback fired by QuestCreationPanelUI after the player confirms creation.
        /// Only at this point is the request removed from the left-panel list
        /// </summary>
        private void OnQuestConfirmed(QuestRequest confirmedRequest)
        {
            var toRemove = _spawnedItems.Find(item => item.Request == confirmedRequest);
            if (toRemove)
            {
                _spawnedItems.Remove(toRemove);
                Destroy(toRemove.gameObject);
            }
            _pendingRequest = null;
        }
 
        private void OnNewRequestArrived(QuestRequest _)
        {
            // Refresh list if the window is open so new requests appear in real-time
            if (rootPanel.activeSelf)
                RefreshRequestList();
        }
    }
}