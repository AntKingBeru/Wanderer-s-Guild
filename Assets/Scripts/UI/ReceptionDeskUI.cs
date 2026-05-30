// Root controller for the Reception Desk
// Coordinates the request list (left), quest creation form (middle), and request popup.
// Subscribes to InteractionManager for screen open/close and QuestManager for data changes.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReceptionDeskUI : MonoBehaviour
{
    [Header("Screen Root")]
    [Tooltip("Root GameObject of the entire Reception Desk screen. Shown/hidden on open/close.")]
    [SerializeField] private GameObject screenRoot;

    [Header("Left Panel")]
    [Tooltip("Vertical layout container inside the request list scroll view's content.")]
    [SerializeField] private Transform requestListContent;

    [Tooltip("Prefab for a single request item in the left list.")]
    [SerializeField] private RequestListItemUI requestItemPrefab;

    [Header("Middle Panel")]
    [Tooltip("The quest creation form component.")]
    [SerializeField] private QuestFormUI questForm;

    [Tooltip("The request details popup component.")]
    [SerializeField] private RequestPopupUI requestPopup;

    [Header("Buttons")]
    [Tooltip("Bottom-right of the screen. Closes the Reception Desk.")]
    [SerializeField] private Button closeButton;

    private readonly List<RequestListItemUI> _listItems = new List<RequestListItemUI>();

    #region Lifecycle
    private void Awake()
    {
        closeButton?.onClick.AddListener(HandleClose);
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

        if (QuestManager.Instance)
            QuestManager.Instance.OnAvailableRequestsChanged += RefreshRequestList;

        if (requestPopup)
        {
            requestPopup.OnCloseClicked += HandlePopupClose;
            requestPopup.OnCreateClicked += HandlePopupCreate;
        }

        if (questForm)
            questForm.OnCreateQuestClicked += HandleFormCreate;
        
        // Always start hidden; InteractionManager reveals it on demand.
        screenRoot.SetActive(false);
    }

    private void OnDisable()
    {
        if (InteractionManager.Instance)
        {
            InteractionManager.Instance.OnScreenOpened -= HandleScreenOpened;
            InteractionManager.Instance.OnScreenClosed -= HandleScreenClosed;
        }

        if (QuestManager.Instance)
            QuestManager.Instance.OnAvailableRequestsChanged -= RefreshRequestList;

        if (requestPopup)
        {
            requestPopup.OnCloseClicked -= HandlePopupClose;
            requestPopup.OnCreateClicked -= HandlePopupCreate;
        }

        if (questForm)
            questForm.OnCreateQuestClicked -= HandleFormCreate;
    }
    #endregion

    #region Screen Visibility
    private void HandleScreenOpened(ScreenType type)
    {
        if (type != ScreenType.ReceptionDesk)
            return;
        screenRoot.SetActive(true);
        RefreshRequestList();
        questForm?.Clear();
        requestPopup?.Hide();
    }

    private void HandleScreenClosed(ScreenType type)
    {
        if (type != ScreenType.ReceptionDesk)
            return;
        screenRoot.SetActive(false);
    }

    private void HandleClose()
        => InteractionManager.Instance?.CloseScreen();
    #endregion
    
    #region Request List
    private void RefreshRequestList()
    {
        // Unsubscribe and destroy all current items.
        foreach (var item in _listItems)
        {
            item.OnClicked -= HandleRequestItemClicked;
            Destroy(item.gameObject);
        }
        _listItems.Clear();

        if (!QuestManager.Instance || !requestItemPrefab)
            return;
        foreach (var request in QuestManager.Instance.AvailableRequests)
        {
            if (!request.IsAvailable)
                return;
            var item = Instantiate(requestItemPrefab, requestListContent);
            item.Populate(request);
            item.OnClicked += HandleRequestItemClicked;
            _listItems.Add(item);
        }
    }

    private void HandleRequestItemClicked(QuestRequest request)
    {
        requestPopup?.Show(request);
    }
    #endregion
    
    #region Popup
    private void HandlePopupClose() 
        => requestPopup?.Hide();

    private void HandlePopupCreate(QuestRequest request)
    {
        requestPopup?.Hide();
        questForm?.LoadRequest(request);
    }
    #endregion
    
    #region Form
    private void HandleFormCreate(QuestRequest request, QuestRank rank, int reward)
    {
        if (!QuestManager.Instance)
            return;
        var created = QuestManager.Instance.CreateQuest(request, rank, reward);
        if (created != null)
            questForm.Clear();
        // The request list refresh automatically via OnAvailableRequestsChanged.
    }
    #endregion
}