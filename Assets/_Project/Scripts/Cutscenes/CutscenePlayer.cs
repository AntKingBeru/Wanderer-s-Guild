// Plays a slide sequence: cross-fade each slide, type its text, wait for advance; skip ends it early. Ends faded out.
using System.Threading;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace WanderersGuild
{
    public sealed class CutscenePlayer
    {
        private readonly CutsceneView _view;
        private readonly IntroSettings _settings;
        private readonly GameObject _linkTarget;
        private readonly SlideCrossfader _slides;
        private readonly TypewriterText _typewriter;
        private readonly BlinkingPrompt _prompt;
        private Tween _fadeTween;
        private bool _isPlaying;
        private bool _advanceRequested;
        private bool _skipRequested;

        public CutscenePlayer(CutsceneView view, IntroSettings settings, GameObject linkTarget)
        {
            _view = view;
            _settings = settings;
            _linkTarget = linkTarget;
            _slides = new SlideCrossfader(view.ImageA, view.ImageB, linkTarget);

            ScrollingTextBox textBox = new(view.TextScroll, view.TextClip, view.Text, settings.TextScrollStep);
            _typewriter = new TypewriterText(textBox, linkTarget);
            _prompt = new BlinkingPrompt(view.Prompt, linkTarget);
        }

        // Finishes the typing, or moves to the next slide when the text is complete.
        public void RequestAdvance()
        {
            if (_isPlaying)
                _advanceRequested = true;
        }

        public void RequestSkip()
        {
            if (_isPlaying)
                _skipRequested = true;
        }

        // Plays every slide, then fades the cutscene out. Returns true when it was skipped.
        public async Awaitable<bool> PlayAsync(IReadOnlyList<CutsceneSlide> slides, CancellationToken token)
        {
            _isPlaying = true;
            _advanceRequested = false;
            _skipRequested = false;

            try
            {
                foreach (var slide in slides)
                {
                    if (_skipRequested)
                        break;

                    _prompt.SetVisible(false, _settings.PromptBlinkInterval);
                    _typewriter.Clear();
                    await _slides.ShowAsync(slide, _settings.SlideCrossfadeDuration, token);
                    if (_skipRequested)
                        break;

                    // NOTE: Presses during the cross-fade don't count — they would complete the text instantly.
                    _advanceRequested = false;
                    _typewriter.Play(slide.Text, _settings.CharactersPerSecond);
                    await WaitForAdvanceAsync(token);
                }

                var skipped = _skipRequested;
                _prompt.SetVisible(false, _settings.PromptBlinkInterval);
                _typewriter.Stop();
                await FadeOutAsync(token);
                return skipped;
            }
            finally
            {
                _isPlaying = false;
            }
        }

        // NOTE: First press finishes the typing; the next press (with the text complete) moves on.
        private async Awaitable WaitForAdvanceAsync(CancellationToken token)
        {
            while (!_skipRequested)
            {
                if (_advanceRequested)
                {
                    _advanceRequested = false;
                    if (!_typewriter.IsTyping)
                        return;
                    _typewriter.Complete();
                }

                _prompt.SetVisible(!_typewriter.IsTyping, _settings.PromptBlinkInterval);
                await Awaitable.NextFrameAsync(token);
            }
        }

        private async Awaitable FadeOutAsync(CancellationToken token)
        {
            if (_fadeTween.IsActive())
                _fadeTween.Kill();

            var root = _view.Root;
            _fadeTween = DOVirtual.Float(1f, 0f, _settings.EndFadeDuration, value => root.style.opacity = value)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(_linkTarget);

            await _fadeTween.AwaitCompletionAsync(token);
            root.style.opacity = 0f;
        }
    }
}