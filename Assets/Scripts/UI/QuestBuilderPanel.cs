// Reception Desk middle panel: configures a QuestBuilder from the selected request and creates a draft.

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class QuestBuilderPanel
{
    private readonly VisualElement _emptyState;
    private readonly VisualElement _form;
    private readonly Label _header;
    private readonly SliderInt _guildPercent;
    private readonly Label _guildPercentValue;
    private readonly DropdownField _minRank;
    private readonly DropdownField _maxRank;
    private readonly SliderInt _minParty;
    private readonly SliderInt _maxParty;
    private readonly Button _createButton;
    private readonly Label _error;
    private readonly Action _onCreated;
    
    private int _requestId = -1;
    private static readonly string[] RankNames = Enum.GetNames(typeof(GuildRank));

    public QuestBuilderPanel(VisualElement root, Action onCreated)
    {
        _onCreated = onCreated;
        _emptyState = root.Q<VisualElement>("builder-empty");
        _form = root.Q<VisualElement>("builder-form");
        _header = root.Q<Label>("builder-header");
        _guildPercent = root.Q<SliderInt>("guild-percent");
        _guildPercentValue = root.Q<Label>("guild-percent-value");
        _minRank = root.Q<DropdownField>("min-rank");
        _maxRank = root.Q<DropdownField>("max-rank");
        _minParty = root.Q<SliderInt>("min-party");
        _maxParty = root.Q<SliderInt>("max-party");
        _createButton = root.Q<Button>("create-quest");
        _error = root.Q<Label>("builder-error");

        InitControls();
        Clear();
    }
    
    private void InitControls()
    {
        var ranks = new List<string>(RankNames);
        if (_minRank != null)
            _minRank.choices = ranks;
        if (_maxRank != null)
            _maxRank.choices = ranks;

        _guildPercent?.RegisterValueChangedCallback(e =>
        {
            if (_guildPercentValue != null) _guildPercentValue.text = $"{e.newValue}%";
        });
        _createButton?.RegisterCallback<ClickEvent>(_ => Create());
    }
    
    public void Load(int requestId)
    {
        _requestId = requestId;
        var r = RequestBoard.Exists ? RequestBoard.Instance.Get(requestId) : null;
        if (r == null)
        {
            Clear();
            return;
        }

        var cfg = GameConfig.Instance;
        if (_header != null)
            _header.text = r.Objective;
        SetPercent(cfg.Quest.defaultGuildRewardPercent);
        if (_minRank != null)
            _minRank.value = RankNames[(int)r.RecommendedRank];
        if (_maxRank != null)
            _maxRank.value = RankNames[^1];   // S
        SetParty(cfg.Party.lowRankSize.min, cfg.Party.lowRankSize.max);
        SetError(null);

        if (_emptyState != null)
            _emptyState.style.display = DisplayStyle.None;
        if (_form != null)
            _form.style.display = DisplayStyle.Flex;
    }
    
    public void Clear()
    {
        _requestId = -1;
        if (_emptyState != null)
            _emptyState.style.display = DisplayStyle.Flex;
        if (_form != null)
            _form.style.display = DisplayStyle.None;
        SetError(null);
    }
    
    private void Create()
    {
        if (_requestId < 0)
        {
            SetError("Select a request first.");
            return;
        }
        
        var req = RequestBoard.Exists ? RequestBoard.Instance.Get(_requestId) : null;
        if (req == null)
        {
            SetError("Request no longer available."); Clear(); _onCreated?.Invoke();
            return;
        }

        var builder = new QuestBuilder(req)
            .WithGuildRewardPercent(_guildPercent?.value ?? 30)
            .WithRankRange(ParseRank(_minRank), ParseRank(_maxRank))
            .WithPartySize(_minParty?.value ?? 2,
                _maxParty?.value ?? 5);

        if (!QuestBoard.Exists)
        {
            SetError("Quest board unavailable.");
            return;
        }

        var created = QuestBoard.Instance.CreateFromRequest(_requestId, builder, out var error);
        if (created == null)
        {
            SetError(error);
            return;
        }

        Clear();
        _onCreated?.Invoke();
    }
    
    private void SetPercent(int percent)
    {
        if (_guildPercent != null)
            _guildPercent.value = percent;
        if (_guildPercentValue != null)
            _guildPercentValue.text = $"{percent}%";
    }

    private void SetParty(int min, int max)
    {
        if (_minParty != null)
            _minParty.value = min;
        if (_maxParty != null)
            _maxParty.value = max;
    }

    private void SetError(string message)
    {
        if (_error == null)
            return;
        _error.text = message ?? string.Empty;
        _error.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private static GuildRank ParseRank(DropdownField field)
        => field != null && Enum.TryParse(field.value, out GuildRank rank) ? rank : GuildRank.F;
}
