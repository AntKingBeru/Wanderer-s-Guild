// HUD rank-up banner (top-center): "Guild Rank D!" in the rank's color; several rank-ups play one after another.
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class RankUpBannerView
    {
        private const string BannerName = "rank-banner";
        private const string LabelName = "rank-banner-label";

        private readonly VisualElement _banner;
        private readonly OutlinedText _label;
        private readonly GameObject _linkTarget;
        private readonly Queue<(GuildRank rank, Color color)> _queue = new();
        private Sequence _sequence;
        private bool _isPlaying;

        private RankUpBannerView(VisualElement banner, OutlinedText label, GameObject linkTarget)
        {
            _banner = banner;
            _label = label;
            _linkTarget = linkTarget;
            _banner.style.opacity = 0f;
        }

        public static RankUpBannerView Bind(VisualElement root, GameObject linkTarget)
        {
            var banner = root.Q<VisualElement>(BannerName);
            var label = root.Q<OutlinedText>(LabelName);
            return banner != null && label != null ? new RankUpBannerView(banner, label, linkTarget) : null;
        }

        public void Enqueue(GuildRank rank, Color color, GuildHudSettings settings)
        {
            _queue.Enqueue((rank, color));
            if (!_isPlaying)
                PlayNext(settings);
        }

        // NOTE: Each banner slides down and fades in, stays, fades out, then the next queued rank plays.
        private void PlayNext(GuildHudSettings settings)
        {
            if (_queue.Count == 0)
            {
                _isPlaying = false;
                return;
            }

            _isPlaying = true;
            var (rank, color) = _queue.Dequeue();

            _label.Text = $"Guild Rank {rank}!";
            _label.SetFillColor(color);
            _banner.style.borderTopColor = color;
            _banner.style.borderBottomColor = color;
            _banner.style.borderLeftColor = color;
            _banner.style.borderRightColor = color;

            if (_sequence.IsActive()) _sequence.Kill();
            _sequence = DOTween.Sequence()
                .Append(DOVirtual.Float(0f, 1f, settings.BannerFadeDuration, t => Apply(t, settings.BannerSlidePixels)))
                .AppendInterval(settings.BannerDuration)
                .Append(DOVirtual.Float(1f, 0f, settings.BannerFadeDuration, t => Apply(t, settings.BannerSlidePixels)))
                .OnComplete(() => PlayNext(settings))
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        // NOTE: t = 1 is fully shown at rest; the slide offset is rounded to whole UI pixels.
        private void Apply(float t, float slidePixels)
        {
            _banner.style.opacity = t;
            _banner.style.translate = new Translate(0f, Mathf.Round(-slidePixels * (1f - t)));
        }
    }
}