// The quest clipboard (inside the request board's document): binds its fields and plays the slide and POSTED-stamp animations.
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class QuestClipboardView
    {
        private readonly VisualElement _layer;
        private readonly VisualElement _board;
        private readonly OutlinedText _title;
        private readonly OutlinedText _meta;
        private readonly OutlinedText _split;
        private readonly OutlinedText _duration;
        private readonly OutlinedText _stamp;
        private readonly GameObject _linkTarget;
        private readonly List<VisualElement> _order = new();
        private Sequence _sequence;
        private float _shown; // 0 = hidden below, 1 = in place.

        public OptionCycler Share { get; }
        public OptionCycler Rank { get; }
        public OptionCycler Size { get; }
        public Button Back { get; }
        public Button Post { get; }

        public bool IsOpen { get; private set; }

        // True while sliding or stamping — input is ignored then.
        public bool IsBusy { get; private set; }

        // Up/Down order: the three cyclers, then Back and Post.
        public IReadOnlyList<VisualElement> NavigationOrder => _order;

        private QuestClipboardView(VisualElement root, GameObject linkTarget)
        {
            _linkTarget = linkTarget;
            _layer = root.Q<VisualElement>("clipboard-layer");
            _board = root.Q<VisualElement>("clipboard");
            _title = root.Q<OutlinedText>("clip-title");
            _meta = root.Q<OutlinedText>("clip-meta");
            _split = root.Q<OutlinedText>("clip-split");
            _duration = root.Q<OutlinedText>("clip-duration");
            _stamp = root.Q<OutlinedText>("clip-stamp");
            Share = root.Q<OptionCycler>("clip-share");
            Rank = root.Q<OptionCycler>("clip-rank");
            Size = root.Q<OptionCycler>("clip-size");
            Back = root.Q<Button>("clip-back");
            Post = root.Q<Button>("clip-post");
        }

        private bool IsComplete =>
            _layer != null && _board != null && _title != null && _meta != null && _split != null && _duration != null &&
            _stamp != null && Share != null && Rank != null && Size != null && Back != null && Post != null;

        public static QuestClipboardView Bind(VisualElement root, GameObject linkTarget)
        {
            QuestClipboardView view = new(root, linkTarget);
            if (!view.IsComplete)
                return null;

            view._order.AddRange(new VisualElement[] { view.Share, view.Rank, view.Size, view.Back, view.Post });
            view.HideImmediate();
            return view;
        }

        public void SetRequest(RequestData request)
        {
            _title.Text = request.DisplayName;
            _meta.Text = $"{request.Category} · {request.Source} · Rank {request.RecommendedRank}";
        }

        public void SetSummary(string goldSplit, string duration)
        {
            _split.Text = goldSplit;
            _duration.Text = duration;
        }

        public void HideImmediate()
        {
            Kill();
            IsOpen = false;
            IsBusy = false;
            ApplySlide(0f);
            SetStamp(0f, 1f, visible: false);
            _layer.style.display = DisplayStyle.None;
        }

        public void Open(QuestClipboardSettings settings, Action onOpened)
        {
            Kill();
            IsOpen = true;
            IsBusy = true;
            SetStamp(0f, 1f, visible: false);
            _layer.style.display = DisplayStyle.Flex;

            _sequence = DOTween.Sequence()
                .Append(DOVirtual.Float(_shown, 1f, settings.SlideDuration, ApplySlide).SetEase(Ease.OutCubic))
                .OnComplete(() => { IsBusy = false; onOpened?.Invoke(); })
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        public void Close(QuestClipboardSettings settings, Action onClosed)
        {
            Kill();
            IsBusy = true;
            _sequence = DOTween.Sequence();
            AppendSlideDown(settings, onClosed);
        }

        // The POSTED stamp lands on the paper, holds, then the clipboard slides away.
        public void StampAndClose(QuestClipboardSettings settings, Action onClosed)
        {
            Kill();
            IsBusy = true;
            var startScale = settings.StampStartScale;

            _sequence = DOTween.Sequence()
                .Append(DOVirtual.Float(0f, 1f, settings.StampDuration, t => SetStamp(t, Mathf.Lerp(startScale, 1f, t), visible: true))
                    .SetEase(Ease.InQuad))
                .AppendInterval(settings.StampHold);
            AppendSlideDown(settings, onClosed);
        }

        private void AppendSlideDown(QuestClipboardSettings settings, Action onClosed)
        {
            _sequence
                .Append(DOVirtual.Float(_shown, 0f, settings.SlideDuration, ApplySlide).SetEase(Ease.InCubic))
                .OnComplete(() =>
                {
                    _layer.style.display = DisplayStyle.None;
                    IsOpen = false;
                    IsBusy = false;
                    onClosed?.Invoke();
                })
                .SetUpdate(true)
                .SetLink(_linkTarget);
        }

        // NOTE: The dim fades with the slide; the clipboard's offset is rounded to whole UI pixels.
        private void ApplySlide(float shown)
        {
            _shown = Mathf.Clamp01(shown);
            var distance = GameConfig.HasInstance ? GameConfig.Instance.QuestClipboard.SlideDistance : 180f;

            _layer.style.opacity = _shown;
            _board.style.translate = new Translate(0f, Mathf.Round((1f - _shown) * distance));
        }

        private void SetStamp(float opacity, float scale, bool visible)
        {
            _stamp.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _stamp.style.opacity = opacity;
            _stamp.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        private void Kill()
        {
            if (_sequence.IsActive())
                _sequence.Kill();
        }
    }
}