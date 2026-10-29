using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    // NOTE: Runs before default scripts, so parallax (and the future eye-follow) read this frame's value.
    [DefaultExecutionOrder(-50)]
    public class PointerTracker : Singleton<PointerTracker>
    {
        private InputAction _pointAction;

        public Vector2 ScreenPosition { get; private set; }
        
        // (-1,-1) = bottom-left, (0,0) = center, (1,1) = top-right. Clamped when the pointer leaves the window.
        public Vector2 NormalizedPosition { get; private set; }
        
        protected override void OnSingletonAwake()
        {
            var reference = GameConfig.Instance.InputBindings.Point;
            _pointAction = reference ? reference.action : null;

            if (_pointAction == null)
            {
                Debug.LogWarning("[PointerTracker] No Point action assigned in GameConfig.", this);
                return;
            }

            if (!_pointAction.enabled)
                _pointAction.Enable();
        }
        
        private void Update()
        {
            if (_pointAction == null)
                return;

            ScreenPosition = _pointAction.ReadValue<Vector2>();
            if (Screen.width <= 0 || Screen.height <= 0)
                return; // Minimized window.

            var x = ScreenPosition.x / Screen.width * 2f - 1f;
            var y = ScreenPosition.y / Screen.height * 2f - 1f;
            NormalizedPosition = new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        }
    }
}