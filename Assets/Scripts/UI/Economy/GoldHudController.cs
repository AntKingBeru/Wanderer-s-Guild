// Gold HUD controller: ticks the displayed total toward the true balance and spawns change floaters.

using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(10)]
public class GoldHudController : MonoBehaviour
{
    [Header("Tick Animation")]
    [SerializeField] private float tickDuration = 0.4f;

    [Header("Floater")]
    [SerializeField] private float floaterDuration = 1.0f;
    [SerializeField] private float floaterRise = 42f;

    private GoldHudView _view;
    private GoldFloater _floater;
    private Coroutine _tick;
    private int _displayed;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
            return;

        _view = new GoldHudView(root);
        _floater = new GoldFloater(_view.FloaterLayer, this, floaterDuration, floaterRise);

        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onGoldChanged.AddListener(HandleGoldChanged);

        SyncInitial();
    }

    private void OnDisable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onGoldChanged.RemoveListener(HandleGoldChanged);
    }

    private void SyncInitial()
    {
        _displayed = TreasuryController.Exists ? TreasuryController.Instance.Gold : 0;
        _view.SetAmount(_displayed);
    }
    
    private void HandleGoldChanged(int newTotal, int delta)
    {
        _floater.Spawn(delta);
        TickTo(newTotal);
    }
    
    private void TickTo(int target)
    {
        if (_tick != null)
            StopCoroutine(_tick);
        var from = _displayed;
        _tick = StartCoroutine(UiTween.Run(tickDuration, t =>
        {
            _displayed = Mathf.RoundToInt(Mathf.Lerp(from, target, UiTween.EaseInOut(t)));
            _view.SetAmount(_displayed);
        }, () =>
        {
            _displayed = target;
            _view.SetAmount(target);
            _tick = null;
        }));
    }
}