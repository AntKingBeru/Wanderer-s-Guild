// Reception Desk applications panel: lists pending quest applications with approve/reject buttons.

using System;
using UnityEngine.UIElements;

public class ApplicationPanelView
{
    private readonly ScrollView _list;
    private readonly Action<int> _onSelect;
    
    public ApplicationPanelView(VisualElement root, Action<int> onSelect)
    {
        _list = root.Q<ScrollView>("application-list");
        _onSelect = onSelect;
    }
    
    public void Refresh()
    {
        if (_list == null || !ApplicationBoard.Exists) return;
        _list.Clear();

        foreach (var appId in ApplicationBoard.Instance.GetPendingIds())
            _list.Add(BuildItem(appId));
    }
    
    private VisualElement BuildItem(int appId)
    {
        var item = new VisualElement();
        item.AddToClassList("application-item");
        item.userData = appId;

        // Kind label so the player sees quest vs rank-up at a glance.
        var kind = ApplicationBoard.Instance.GetType(appId);
        var kindText = kind == ApplicationType.RankUp ? "Rank-Up" : "Quest";
        var kindLabel = new Label(kindText);
        kindLabel.AddToClassList("application-item__kind");
        item.Add(kindLabel);

        var title = new Label(ApplicationDetailAssembler.BuildHeader(appId));
        title.AddToClassList("application-item__title");
        item.Add(title);

        item.RegisterCallback<ClickEvent>(_ => _onSelect?.Invoke(appId));
        return item;
    }
}