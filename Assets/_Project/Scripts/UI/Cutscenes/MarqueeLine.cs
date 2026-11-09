// Slides a too-long OutlinedText line back and forth inside its box (wait, slide to the end, wait, slide back) in whole pixels.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: State pattern in miniature (rest-start → slide-out → rest-end → slide-back). Runs on the element's own UI scheduler
    // in real time, so it ignores pause/game speed and stops with the element. The line must clip (overflow: hidden) and not
    // wrap — see the "marquee" USS class.
    public sealed class MarqueeLine
    {
        private enum Phase { RestStart, SlideOut, RestEnd, SlideBack }

        private readonly OutlinedText _text;
        private readonly VisualElement _stack;
        private readonly IVisualElementScheduledItem _timer;
        private Phase _phase;
        private float _offset;
        private float _overflow;
        private float _wait;
        private bool _active;
        private string _lastText;

        public MarqueeLine(OutlinedText text)
        {
            _text = text;
            _stack = text.Q(className: OutlinedText.StackUssClassName);

            // NOTE: Either the box or the text block can change size (new text, resized card) — both re-measure.
            _text.RegisterCallback<GeometryChangedEvent>(_ => Measure());
            _stack?.RegisterCallback<GeometryChangedEvent>(_ => Measure());

            _timer = _text.schedule.Execute(Step).Every(16);
            _timer.Pause();
        }

        // Call after setting the text: a new text starts again from the left; the same text keeps sliding where it is
        // (frequent refreshes never snap it back).
        public void Refresh()
        {
            var changed = !_active || _text.Text != _lastText;
            _active = true;
            _lastText = _text.Text;

            if (changed)
                ResetToStart();
            Measure();
        }

        // Stops sliding and shows the start (hidden card); the next Refresh starts over.
        public void Stop()
        {
            _active = false;
            _lastText = null;
            _timer.Pause();
            ResetToStart();
        }

        private void ResetToStart()
        {
            _phase = Phase.RestStart;
            _wait = Settings.Pause;
            _offset = 0f;
            Apply();
        }

        // NOTE: The text's own unwrapped width vs the line's box — independent of whether the inner stack stretches. A resize
        // keeps the phase and clamps the offset (no jump); a line that fits (or is hidden) is parked at the start.
        private void Measure()
        {
            if (_stack == null)
                return;

            var box = _text.contentRect.width;
            var content = _text.MeasureTextWidth();
            if (float.IsNaN(box) || float.IsNaN(content) || box <= 0f)
                return;

            _overflow = Mathf.Max(0f, Mathf.Ceil(content - box));
            if (_active && _overflow > 0f && _text.resolvedStyle.display != DisplayStyle.None)
            {
                _offset = Mathf.Min(_offset, _overflow);
                Apply();
                _timer.Resume();
            }
            else
            {
                _timer.Pause();
                ResetToStart();
            }
        }

        private void Step(TimerState state)
        {
            var dt = state.deltaTime / 1000f;
            var settings = Settings;

            switch (_phase)
            {
                case Phase.RestStart:
                    if ((_wait -= dt) <= 0f) _phase = Phase.SlideOut;
                    break;
                case Phase.SlideOut:
                    _offset = Mathf.Min(_overflow, _offset + settings.Speed * dt);
                    if (_offset >= _overflow)
                    {
                        _phase = Phase.RestEnd;
                        _wait = settings.Pause;
                    }
                    break;
                case Phase.RestEnd:
                    if ((_wait -= dt) <= 0f)
                        _phase = Phase.SlideBack;
                    break;
                case Phase.SlideBack:
                    _offset = Mathf.Max(0f, _offset - settings.Speed * dt);
                    if (_offset <= 0f)
                    {
                        _phase = Phase.RestStart;
                        _wait = settings.Pause;
                    }
                    break;
            }
            Apply();
        }

        private void Apply()
        {
            if (_stack != null)
                _stack.style.translate = new Translate(-Mathf.Round(_offset), 0f);
        }

        private static MarqueeSettings Settings => GameConfig.Instance.Marquee;
    }
}