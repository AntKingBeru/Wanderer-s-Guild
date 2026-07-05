// Guild-rank HUD controller: animates the EXP fill, faster for larger gains.

using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(10)]
public class GuildRankHudController : MonoBehaviour
{
    [SerializeField] private RankPalette rankPalette;
    [Header("Fill Animation")]
    [Tooltip("Seconds to fill when a small amount is gained; scaled down for larger gains.")]
    [SerializeField] private float baseFillDuration = 0.6f;

    private GuildRankHudView _view;
    private Coroutine _fillAnim;
    private float _displayedRatio;
    
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
            return;

        _view = new GuildRankHudView(root, rankPalette);
        if (GameEventsRelay.Exists)
        {
            GameEventsRelay.Instance.onGuildRankProgress.AddListener(HandleProgress);
            GameEventsRelay.Instance.onGuildRankChanged.AddListener(HandleRankChanged);
        }
        SyncInitial();
    }

    private void OnDisable()
    {
        if (!GameEventsRelay.Exists)
            return;
        GameEventsRelay.Instance.onGuildRankProgress.RemoveListener(HandleProgress);
        GameEventsRelay.Instance.onGuildRankChanged.RemoveListener(HandleRankChanged);
    }

    private void SyncInitial()
    {
        if (!GuildController.Exists)
            return;
        var atMax = GuildController.Instance.CurrentRank >= GuildRank.National;
        _view.SetRanks(GuildController.Instance.CurrentRank, atMax);
        _displayedRatio = CurrentRatio();
        _view.SetFill(_displayedRatio);
    }
    
    private void HandleProgress(int progress)
    {
        var target = CurrentRatio();
        var delta = Mathf.Abs(target - _displayedRatio);
        if (delta <= 0.0001f)
            return;
        
        var duration = baseFillDuration / (1f + delta * 3f);

        if (_fillAnim != null) StopCoroutine(_fillAnim);
        var from = _displayedRatio;
        _fillAnim = StartCoroutine(UiTween.Run(duration, t =>
        {
            _displayedRatio = Mathf.Lerp(from, target, UiTween.EaseInOut(t));
            _view.SetFill(_displayedRatio);
        }, () => _fillAnim = null));
    }
    
    private void HandleRankChanged(GuildRank rank)
    {
        var atMax = rank >= GuildRank.National;
        _view.SetRanks(rank, atMax);
        _displayedRatio = 0f;
        _view.SetFill(0f);
    }

    private float CurrentRatio()
    {
        if (!GuildController.Exists)
            return 0f;
        if (GuildController.Instance.CurrentRank >= GuildRank.National)
            return 1f;
        return Mathf.Clamp01((float)GuildController.Instance.RankProgress / GameConfig.Instance.Guild.rankExpPerRank);
    }
}