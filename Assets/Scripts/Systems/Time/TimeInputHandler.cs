// Routes new-Input-System actions (InputActionReferences) to TimeController speed controls.

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TimeInputHandler : MonoBehaviour
{
    [Header("Input Action References")]
    [SerializeField] private InputActionReference pauseToggle;
    [SerializeField] private InputActionReference cycleSpeed;
    
    private void OnEnable()
    {
        Bind(pauseToggle, OnPauseToggle);
        Bind(cycleSpeed, OnCycleSpeed);
    }
    
    private void OnDisable()
    {
        Unbind(pauseToggle, OnPauseToggle);
        Unbind(cycleSpeed, OnCycleSpeed);
    }
    
    private static void Bind(InputActionReference reference, Action<InputAction.CallbackContext> callback)
    {
        if (!reference || reference.action == null)
            return;
        reference.action.performed += callback;
        reference.action.Enable();
    }
    
    private static void Unbind(InputActionReference reference, Action<InputAction.CallbackContext> callback)
    {
        if (!reference || reference.action == null)
            return;
        reference.action.performed -= callback;
    }

    private void OnPauseToggle(InputAction.CallbackContext _)
    {
        if (TimeController.Exists)
            TimeController.Instance.TogglePause();
    }

    private void OnCycleSpeed(InputAction.CallbackContext _)
    {
        if (TimeController.Exists)
            TimeController.Instance.CycleSpeed();
    }
}