using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    public sealed class ScopedActionListener : IDisposable
    {
        private readonly InputAction _action;
        private readonly Action<InputAction.CallbackContext> _callback;
        
        public ScopedActionListener(InputActionReference reference, Action<InputAction.CallbackContext> callback)
        {
            _action = reference ? reference.action : null;
            _callback = callback;

            if (_action == null)
            {
                Debug.LogWarning("[ScopedActionListener] Missing InputActionReference; listener inactive.");
                return;
            }

            // NOTE: Enables if needed but never disables. The action may be shared (e.g. by the UI input module).
            if (!_action.enabled)
                _action.Enable();

            _action.performed += _callback;
        }
        
        // Safe to call more than once, and safe to call from inside the callback itself.
        public void Dispose()
        {
            if (_action != null)
                _action.performed -= _callback;
        }
    }
}