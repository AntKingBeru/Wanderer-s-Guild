using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    public class MenuEntranceAnimator : MonoBehaviour
    {
        [SerializeField] private MainMenuView view;

        private Sequence _sequence;

        private bool IsReady => view && view.IsBound;

        // Puts the logo and column items in their hidden state: invisible and offset.
        public void PrepareHidden()
        {
            KillSequence();
            if (!IsReady)
                return;

            var settings = GameConfig.Instance.MenuEntrance;
            ApplyProgress(view.Logo, 0f, LogoOffset(settings));

            foreach (var item in view.ColumnItems)
                ApplyProgress(item, 0f, ItemOffset(settings));
        }

        // Plays the staggered entrance from the hidden state. Speed above 1 plays it faster (used on return).
        public async Awaitable PlayAsync(CancellationToken token, float speed = 1f)
        {
            if (!IsReady)
                return;

            KillSequence();
            _sequence = BuildEntranceSequence(GameConfig.Instance.MenuEntrance);
            _sequence.timeScale = Mathf.Max(0.01f, speed);
            await _sequence.AwaitCompletionAsync(token);
            ShowImmediate(); // NOTE: Guarantees exact rest values even if the sequence was killed early.
        }

        // Fades and slides every item back to its hidden state at once (before a menu-hiding panel opens).
        public async Awaitable HideAsync(CancellationToken token)
        {
            if (!IsReady)
                return;

            KillSequence();
            _sequence = BuildHideSequence(GameConfig.Instance.MenuEntrance, GameConfig.Instance.PanelTransitions);
            await _sequence.AwaitCompletionAsync(token);
            PrepareHidden(); // NOTE: Exact hidden values, ready for the next PlayAsync.
        }

        public void ShowImmediate()
        {
            KillSequence();
            if (!IsReady)
                return;

            ApplyProgress(view.Logo, 1f, Vector2.zero);
            foreach (var item in view.ColumnItems)
                ApplyProgress(item, 1f, Vector2.zero);
        }

        private Sequence BuildEntranceSequence(MenuEntranceSettings settings)
        {
            var sequence = NewSequence();
            sequence.Insert(0f, ProgressTween(view.Logo, LogoOffset(settings), 0f, 1f, settings.LogoDuration, settings.LogoEase));

            var itemsStart = settings.LogoDuration + settings.DelayBeforeItems;
            for (var i = 0; i < view.ColumnItems.Count; i++)
            {
                var startTime = itemsStart + i * settings.ItemStagger;
                sequence.Insert(startTime, ProgressTween(view.ColumnItems[i], ItemOffset(settings), 0f, 1f, settings.ItemDuration, settings.ItemEase));
            }

            return sequence;
        }

        private Sequence BuildHideSequence(MenuEntranceSettings entrance, PanelTransitionSettings panels)
        {
            var sequence = NewSequence();
            sequence.Insert(0f, ProgressTween(view.Logo, LogoOffset(entrance), 1f, 0f, panels.MenuHideDuration, panels.MenuHideEase));

            foreach (var item in view.ColumnItems)
                sequence.Insert(0f, ProgressTween(item, ItemOffset(entrance), 1f, 0f, panels.MenuHideDuration, panels.MenuHideEase));

            return sequence;
        }

        private Sequence NewSequence() => DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        private static Vector2 LogoOffset(MenuEntranceSettings settings) => new(0f, -settings.LogoDrop);
        private static Vector2 ItemOffset(MenuEntranceSettings settings) => new(-settings.ItemSlide, 0f);

        // NOTE: One tween per element drives both opacity and offset; explicit from/to (DOVirtual) avoids stale getters.
        private static Tween ProgressTween(VisualElement element, Vector2 hiddenOffset, float from, float to, float duration, Ease ease) =>
            DOVirtual.Float(from, to, duration, t => ApplyProgress(element, t, hiddenOffset)).SetEase(ease);

        /// <summary>t = 1 is fully shown at rest; t = 0 is invisible at the hidden offset.</summary>
        private static void ApplyProgress(VisualElement element, float t, Vector2 hiddenOffset)
        {
            element.style.opacity = t;
            var offset = hiddenOffset * (1f - t);
            element.style.translate = new Translate(new Length(offset.x), new Length(offset.y));
        }

        private void KillSequence()
        {
            if (_sequence.IsActive())
                _sequence.Kill();
        }
    }
}