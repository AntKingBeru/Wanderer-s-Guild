// HUD rank bar (bottom-center): rank-letter badge + a horizontal bar whose fill blends from this rank's color to the next's.
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RankBarView
    {
        private const string BadgeName = "rank-badge";
        private const string BadgeLabelName = "rank-badge-label";
        private const string TrackName = "rank-track";
        private const string FillName = "rank-fill";

        private readonly VisualElement _badge;
        private readonly OutlinedText _badgeLabel;
        private readonly VisualElement _track;
        private readonly VisualElement _fill;
        private readonly GameObject _linkTarget;
        private Tween _fillTween;
        private Color _fromColor = Color.white;
        private Color _toColor = Color.white;
        private float _progress;

        public bool HasRank { get; private set; }
        public GuildRank Rank { get; private set; }

        private RankBarView(VisualElement badge, OutlinedText badgeLabel, VisualElement track, VisualElement fill, GameObject linkTarget)
        {
            _badge = badge;
            _badgeLabel = badgeLabel;
            _track = track;
            _fill = fill;
            _linkTarget = linkTarget;

            // NOTE: The track has no size until layout runs; re-apply the stored progress once it does.
            _track.RegisterCallback<GeometryChangedEvent>(_ => ApplyProgress(_progress));
        }

        public static RankBarView Bind(VisualElement root, GameObject linkTarget)
        {
            var badge = root.Q<VisualElement>(BadgeName);
            var badgeLabel = root.Q<OutlinedText>(BadgeLabelName);
            var track = root.Q<VisualElement>(TrackName);
            var fill = root.Q<VisualElement>(FillName);

            return badge != null && badgeLabel != null && track != null && fill != null
                ? new RankBarView(badge, badgeLabel, track, fill, linkTarget)
                : null;
        }

        // Switches badge and colors to the rank and empties the bar (a new rank always fills from 0).
        public void SetRank(GuildRank rank, GuildRankSettings ranks)
        {
            Rank = rank;
            HasRank = true;

            _fromColor = ranks.ColorOf(rank);
            _toColor = ranks.TryGetNext(rank, out var next) ? ranks.ColorOf(next) : _fromColor;
            _badge.style.backgroundColor = _fromColor;
            _badgeLabel.Text = BadgeLabel(rank);

            KillTween();
            ApplyProgress(0f);
        }

        public void SetProgress(float progress, bool animate, float duration)
        {
            KillTween();
            var target = Mathf.Clamp01(progress);

            if (!animate || duration <= 0f)
            {
                ApplyProgress(target);
                return;
            }

            _fillTween = DOVirtual.Float(_progress, target, duration, ApplyProgress)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        // NOTE: Whole UI pixels only; the color blends with the fill amount.
        private void ApplyProgress(float progress)
        {
            _progress = progress;

            var width = _track.contentRect.width;
            if (float.IsNaN(width) || width <= 0f)
                return;

            _fill.style.width = Mathf.Round(progress * width);
            _fill.style.backgroundColor = Color.Lerp(_fromColor, _toColor, progress);
        }

        private void KillTween()
        {
            if (_fillTween.IsActive())
                _fillTween.Kill();
        }

        // NOTE: One letter per badge; National shows "N".
        private static string BadgeLabel(GuildRank rank) => rank == GuildRank.National ? "N" : rank.ToString();
    }
}