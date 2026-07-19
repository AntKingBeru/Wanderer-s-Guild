// Abstract base for full-screen UI screens: binds its UIDocument once, then toggles visibility.

using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public abstract class UIScreen : MonoBehaviour
{
    [SerializeField] private ScreenId id = ScreenId.None;
    
    public ScreenId Id => id;
    public bool IsOpen { get; private set; }

    private UIDocument _document;
    private VisualElement _root;
    private bool _bound;

    protected VisualElement Root => _root;
    
    protected virtual void Awake()
    {
        _document = GetComponent<UIDocument>();
        if (ScreenManager.Exists)
            ScreenManager.Instance.Register(this);
    }
    
    private void OnEnable()
    {
        if (ScreenManager.Exists)
            ScreenManager.Instance.Register(this);
        EnsureBound();
        ApplyVisibility(IsOpen);
    }
    
    private void EnsureBound()
    {
        if (_bound)
            return;
        _root = _document.rootVisualElement;
        if (_root == null)
            return;
        OnBuild(_root);
        _bound = true;
        ApplyVisibility(false);
    }
    
    public void Open()
    {
        EnsureBound();
        IsOpen = true;
        ApplyVisibility(true);
        OnOpened();
    }

    public void Close()
    {
        IsOpen = false;
        ApplyVisibility(false);
        OnClosed();
    }
    
    public void SetSortingOrder(float order)
    {
        if (_document)
            _document.sortingOrder = order;
    }

    private void ApplyVisibility(bool visible)
    {
        if (_root == null)
            return;
        _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
    protected abstract void OnBuild(VisualElement root);
    protected virtual void OnOpened() { }
    protected virtual void OnClosed() { }
}