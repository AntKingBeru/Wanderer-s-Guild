// Reception Desk screen: wires the request list + quest-builder panel and refreshes on request events.

using UnityEngine;
using UnityEngine.UIElements;

[DefaultExecutionOrder(5)]
public class ReceptionDeskScreen : UIScreen
{
    [Header("Display")]
    [SerializeField] private RankPalette rankPalette;

    private RequestListView _listView;
    private QuestBuilderPanel _builderPanel;
    private ApplicationPanelView _applicationPanel;
    private ApplicationPopupView _popup;
    private int _openApplicationId = -1;
    private Button _closeButton;

    protected override void OnBuild(VisualElement root)
    {
        _listView = new RequestListView(root, rankPalette, OnRequestSelected);
        _builderPanel = new QuestBuilderPanel(root, OnQuestCreated);
        _applicationPanel = new ApplicationPanelView(root, OnApplicationSelected);
        _popup = new ApplicationPopupView(root, rankPalette, OnPopupApprove, OnPopupReject, ClosePopup);
        _closeButton = root.Q<Button>("close-screen");
        _closeButton?.RegisterCallback<ClickEvent>(_ => ScreenManager.Instance.Close(Id));
    }
    
    protected override void OnOpened()
    {
        Subscribe();
        _listView.Refresh();
        _applicationPanel.Refresh();
    }
    
    protected override void OnClosed()
        => Unsubscribe();

    private void Subscribe()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onRequestGenerated.AddListener(HandleRequestChanged);
        relay.onRequestExpired.AddListener(HandleRequestChanged);
        relay.onApplicationReceived.AddListener(HandleApplicationChanged);
    }

    private void Unsubscribe()
    {
        if (!GameEventsRelay.Exists)
            return;
        var relay = GameEventsRelay.Instance;
        relay.onRequestGenerated.RemoveListener(HandleRequestChanged);
        relay.onRequestExpired.RemoveListener(HandleRequestChanged);
        relay.onApplicationReceived.RemoveListener(HandleApplicationChanged);
    }

    private void HandleRequestChanged(int _)
    {
        _listView.Refresh();
        if (_builderPanel != null && _listView.SelectedId == -1)
            _builderPanel.Clear();
    }
    
    private void HandleApplicationChanged(int _)
        => _applicationPanel.Refresh();

    private void OnRequestSelected(int requestId)
        => _builderPanel.Load(requestId);
    
    private void OnQuestCreated()
        => _listView.Refresh();

    private void OnApplicationSelected(int applicationId)
    {
        _openApplicationId = applicationId;
        _popup.Show(applicationId);
    }
    
    private void OnPopupApprove()
    {
        if (_openApplicationId >= 0 && ApplicationBoard.Exists)
            ApplicationBoard.Instance.Approve(_openApplicationId);
        ClosePopup();
        _applicationPanel.Refresh();
    }
    
    private void OnPopupReject()
    {
        if (_openApplicationId >= 0 && ApplicationBoard.Exists)
            ApplicationBoard.Instance.Reject(_openApplicationId);
        ClosePopup();
        _applicationPanel.Refresh();
    }

    private void ClosePopup()
    {
        _openApplicationId = -1;
        _popup.Hide();
    }
}