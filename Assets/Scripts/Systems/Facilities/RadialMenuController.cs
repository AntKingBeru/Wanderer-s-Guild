// Singleton radial controller: resolves options, projects the door to screen each frame, executes builds.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(11)]
public class RadialMenuController : MonoSingleton<RadialMenuController>
{
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject roomInstancePrefab;
    [SerializeField] private FacilityData[] buildable;

    public bool IsOpen { get; private set; }

    private RadialMenuView _view;
    private UIDocument _document;
    private BuildOptionProvider _provider;
    private RoomInstanceFactory _factory;

    private List<BuildOption> _options = new List<BuildOption>();
    private int _page;
    private Vector3 _worldPos;
    
    protected override void OnSingletonAwake()
    {
        _document = GetComponent<UIDocument>();
        _provider = new BuildOptionProvider(buildable);
        _factory = new RoomInstanceFactory(roomInstancePrefab);
    }

    private void OnEnable()
    {
        var root = _document ? _document.rootVisualElement : null;
        if (root == null)
            return;
        _view = new RadialMenuView(root, OnSelect, PrevPage, NextPage, Close);
        _view.SetVisible(false);
    }
    
    public void Open(DoorKey door, Vector3 worldPos)
    {
        _worldPos = worldPos;
        _options = _provider.OptionsFor(door);
        _page = 0;
        IsOpen = true;
        _view.SetVisible(true);
        RenderCurrentPage();
    }

    public void Close()
    {
        IsOpen = false;
        _view?.SetVisible(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        var sp = cam.WorldToScreenPoint(_worldPos);
        if (sp.z <= 0f)
        {
            _view.SetVisible(false);
            return;
        }
        _view.SetVisible(true);

        var panel = _document.rootVisualElement.panel;
        var panelPos = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(sp.x, sp.y));
        _view.SetCenter(panelPos);
    }

    private void RenderCurrentPage()
    {
        var max = Mathf.Max(1, GameConfig.Instance.Build.maxRadialOptions);
        var start = _page * max;
        var end = Mathf.Min(start + max, _options.Count);

        var page = new List<BuildOption>();
        for (var i = start; i < end; i++)
            page.Add(_options[i]);

        var hasPrev = _page > 0;
        var hasNext = end < _options.Count;
        _view.RenderPage(page, hasPrev, hasNext, GameConfig.Instance.Build.radialRadius);
    }

    private void PrevPage()
    {
        if (_page > 0)
        {
            _page--;
            RenderCurrentPage();
        }
    }
    private void NextPage()
    {
        var max = Mathf.Max(1, GameConfig.Instance.Build.maxRadialOptions);
        if ((_page + 1) * max < _options.Count)
        {
            _page++;
            RenderCurrentPage();
        }
    }
    
    private void OnSelect(BuildOption opt)
    {
        if (opt is not { Enabled: true })
            return;
        
        new PlaceRoomCommand(opt.Type, opt.Data.Footprint, opt.Origin, _factory).Execute();

        if (FacilityController.Exists && !FacilityController.Instance.StartConstruction(opt.Type, out var error))
            Debug.LogWarning($"[Build] StartConstruction failed: {error}");

        Close();
    }
}