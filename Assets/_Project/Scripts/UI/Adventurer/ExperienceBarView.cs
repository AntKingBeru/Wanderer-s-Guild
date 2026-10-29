// A horizontal XP bar: fill width in whole UI pixels; re-applies once layout gives the track a size.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public sealed class ExperienceBarView
    {
        private readonly VisualElement _track;
        private readonly VisualElement _fill;
        private float _progress;

        public ExperienceBarView(VisualElement track, VisualElement fill)
        {
            _track = track;
            _fill = fill;
            _track.RegisterCallback<GeometryChangedEvent>(_ => Apply());
        }

        public void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            Apply();
        }

        private void Apply()
        {
            var width = _track.contentRect.width;
            if (float.IsNaN(width) || width <= 0f)
                return;

            _fill.style.width = Mathf.Round(_progress * width);
        }
    }
}