// Application popup view: renders quest/party (or rank-up) detail and routes approve/reject/close.

using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ApplicationPopupView
{
    private readonly VisualElement _root;
    private readonly Label _header;
    private readonly Label _subheader;
    private readonly ScrollView _memberList;
    private readonly RankPalette _palette;

    public bool IsOpen => _root != null && _root.style.display == DisplayStyle.Flex;
    
    public ApplicationPopupView(VisualElement root, RankPalette palette, Action onApproveInput, Action onRejectInput, Action onClose)
    {
        _root = root.Q<VisualElement>("application-popup");
        _header = root.Q<Label>("popup-header");
        _subheader = root.Q<Label>("popup-subheader");
        _memberList = root.Q<ScrollView>("popup-members");
        _palette = palette;
        var onApprove = onApproveInput;
        var onReject = onRejectInput;

        root.Q<Button>("popup-approve")?.RegisterCallback<ClickEvent>(_ => onApprove?.Invoke());
        root.Q<Button>("popup-reject")?.RegisterCallback<ClickEvent>(_ => onReject?.Invoke());
        root.Q<Button>("popup-close")?.RegisterCallback<ClickEvent>(_ => onClose?.Invoke());

        Hide();
    }
    
    public void Show(int applicationId)
    {
        if (_root == null)
            return;

        if (_header != null)
            _header.text = ApplicationDetailAssembler.BuildHeader(applicationId);
        if (_subheader != null)
            _subheader.text = ApplicationDetailAssembler.BuildSubheader(applicationId);

        if (_memberList != null)
        {
            _memberList.Clear();
            foreach (var line in ApplicationDetailAssembler.BuildMembers(applicationId))
                _memberList.Add(BuildMemberRow(line));
        }

        _root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (_root != null)
            _root.style.display = DisplayStyle.None;
    }
    
    private VisualElement BuildMemberRow(MemberLine line)
    {
        var row = new VisualElement();
        row.AddToClassList("popup-member");

        var name = new Label(line.isLeader ? $"{line.name}  (Leader)" : line.name);
        name.AddToClassList("popup-member__name");

        var detail = new Label($"{line.@class}  •  Lv {line.level}");
        detail.AddToClassList("popup-member__detail");

        var rank = new Label(line.rank.ToString());
        rank.AddToClassList("popup-member__rank");
        rank.style.color = _palette ? _palette.GetColor(line.rank) : Color.white;

        row.Add(name);
        row.Add(detail);
        row.Add(rank);
        return row;
    }
}