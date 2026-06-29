// Reception Desk left panel: renders the request list with rank-colored borders; reports selection.

using System;
using UnityEngine;
using UnityEngine.UIElements;

public class RequestListView
{
    private readonly ScrollView _list;
    private readonly RankPalette _palette;
    private readonly Action<int> _onSelect;
    
    public int SelectedId { get; private set; } = -1;
    
    public RequestListView(VisualElement root, RankPalette palette, Action<int> onSelect)
    {
        _list = root.Q<ScrollView>("request-list");
        _palette = palette;
        _onSelect = onSelect;
    }
    
    public void Refresh()
    {
        if (_list == null)
            return;
        _list.Clear();

        if (!RequestBoard.Exists)
            return;
        foreach (var r in RequestBoard.Instance.GetAll())
            _list.Add(BuildItem(r));

        if (SelectedId != -1 && RequestBoard.Instance.Get(SelectedId) == null)
            SelectedId = -1;
        ApplyHighlight();
    }
    
    private VisualElement BuildItem(Request r)
    {
        var item = new VisualElement { name = $"req-{r.Id}" };
        item.AddToClassList("list-item");
        item.userData = r.Id;
        item.style.borderLeftColor = _palette ? _palette.GetColor(r.RecommendedRank) : Color.white;

        var title = new Label($"{r.Source} · {r.Category}");
        title.AddToClassList("list-item__title");
        var objective = new Label(r.Objective);
        objective.AddToClassList("list-item__sub");
        var meta = new Label($"{r.Difficulty}  •  Rec {r.RecommendedRank}  •  {r.RewardGold}g");
        meta.AddToClassList("list-item__meta");

        item.Add(title);
        item.Add(objective);
        item.Add(meta);
        item.RegisterCallback<ClickEvent>(_ => Select(r.Id));
        return item;
    }
    
    private void Select(int id)
    {
        SelectedId = id;
        ApplyHighlight();
        _onSelect?.Invoke(id);
    }

    private void ApplyHighlight()
    {
        foreach (var child in _list.Children())
            child.EnableInClassList("list-item--selected", child.userData is int id && id == SelectedId);
    }
}
