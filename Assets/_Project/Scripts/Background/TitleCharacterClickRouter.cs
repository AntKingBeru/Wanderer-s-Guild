// Routes title-screen clicks that land on the character to its parts, so poking the character doesn't dismiss the title.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    [RequireComponent(typeof(ParallaxGroup))]
    public class TitleCharacterClickRouter : MonoBehaviour
    {
        private readonly List<Collider2D> _hits = new();
        private ParallaxGroup _group;
        private ContactFilter2D _filter;

        private void Awake()
        {
            _group = GetComponent<ParallaxGroup>();
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = GameConfig.Instance.TitleCharacterReactions.ClickLayers,
                useTriggers = true
            };
        }

        // True when the press was a pointer click on a character part (it reacted / absorbed the click).
        public bool TryHandleClick(InputAction.CallbackContext context)
        {
            // NOTE: Only pointer presses are hit-tested — keyboard keys always dismiss the title.
            if (context.control?.device is not Pointer)
                return false;

            var camera = _group.TargetCamera;
            if (!camera || !PointerTracker.HasInstance)
                return false;

            var target = FindTopTarget(CursorWorld(camera));
            if (!target)
                return false;

            target.Click();
            return true;
        }

        // NOTE: Parallax moved the parts in LateUpdate; 2D physics hasn't synced them yet, so sync before the query.
        private CharacterClickTarget FindTopTarget(Vector2 worldPoint)
        {
            Physics2D.SyncTransforms();
            _hits.Clear();
            Physics2D.OverlapPoint(worldPoint, _filter, _hits);

            CharacterClickTarget top = null;
            foreach (var hit in _hits)
            {
                if (!hit.TryGetComponent(out CharacterClickTarget candidate) || !candidate.isActiveAndEnabled)
                    continue;
                if (!candidate.transform.IsChildOf(transform))
                    continue;

                if (!top || candidate.IsAbove(top))
                    top = candidate;
            }
            return top;
        }

        private Vector3 CursorWorld(Camera camera)
        {
            var screen = PointerTracker.Instance.ScreenPosition;
            var distance = transform.position.z - camera.transform.position.z;
            return camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, distance));
        }
    }
}