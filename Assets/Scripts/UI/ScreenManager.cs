// Singleton screen stack: registers screens, opens/closes them, and closes the top one on Escape.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-95)]
public class ScreenManager : MonoSingleton<ScreenManager>
{
    [Header("Input")]
    [Tooltip("CloseScreen action (bound to Escape).")]
    [SerializeField] private InputActionReference closeScreen;
    
    [Header("Layering")]
    [Tooltip("Base sorting order for screens; the HUD panel must sit above this.")]
    [SerializeField] private float baseSortOrder = 10f;
    
    private readonly Dictionary<ScreenId, UIScreen> _registry = new Dictionary<ScreenId, UIScreen>();
    private readonly List<UIScreen> _stack = new List<UIScreen>();
    
    public bool HasOpenScreen => _stack.Count > 0;
    
    public void Register(UIScreen screen)
    {
        if (!screen || screen.Id == ScreenId.None)
            return;
        _registry[screen.Id] = screen;
    }

    private void OnEnable()
    {
        if (closeScreen?.action == null)
            return;
        closeScreen.action.performed += OnCloseScreen;
        closeScreen.action.Enable();
    }

    private void OnDisable()
    {
        if (closeScreen?.action != null)
            closeScreen.action.performed -= OnCloseScreen;
    }
    
    public void Open(ScreenId id)
    {
        if (!_registry.TryGetValue(id, out var screen) || !screen)
            return;
        if (_stack.Contains(screen))
        {
            BringToTop(screen);
            return;
        }

        _stack.Add(screen);
        screen.Open();
        Reorder();
        GameEventsRelay.Instance.RaiseScreenOpened(id);
    }

    public void Close(ScreenId id)
    {
        if (_registry.TryGetValue(id, out var screen) && screen)
            CloseScreen(screen);
    }
    
    public void CloseTop()
    {
        if (_stack.Count > 0) CloseScreen(_stack[^1]);
    }

    private void CloseScreen(UIScreen screen)
    {
        if (!_stack.Remove(screen))
            return;
        screen.Close();
        GameEventsRelay.Instance.RaiseScreenClosed(screen.Id);
        Reorder();
    }

    private void BringToTop(UIScreen screen)
    {
        _stack.Remove(screen);
        _stack.Add(screen);
        Reorder();
    }
    
    private void Reorder()
    {
        for (var i = 0; i < _stack.Count; i++)
            _stack[i].SetSortingOrder(baseSortOrder + i + 1);
    }

    private void OnCloseScreen(InputAction.CallbackContext _)
        => CloseTop();
}