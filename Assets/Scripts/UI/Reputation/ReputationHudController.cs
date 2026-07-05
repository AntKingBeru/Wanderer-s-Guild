// Reputation HUD controller: observes reputation changes and drives the slide/move sequencer.

using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
[DefaultExecutionOrder(10)]
public class ReputationHudController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float moveDuration = 0.5f;

    private ReputationHudView _view;
    private ReputationHudSequencer _sequencer;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
            return;

        _view = new ReputationHudView(root);
        _sequencer = new ReputationHudSequencer(_view, this, slideDuration, moveDuration);

        if (GameEventsRelay.Exists)
        {
            GameEventsRelay.Instance.onReputationChanged.AddListener(HandleReputationChanged);
            GameEventsRelay.Instance.onReputationTierChanged.AddListener(HandleTierChanged);
        }
        SyncInitial();
    }

    private void OnDisable()
    {
        if (!GameEventsRelay.Exists)
            return;
        GameEventsRelay.Instance.onReputationChanged.RemoveListener(HandleReputationChanged);
        GameEventsRelay.Instance.onReputationTierChanged.RemoveListener(HandleTierChanged);
    }

    private void SyncInitial()
    {
        if (!ReputationController.Exists)
        {
            _sequencer.SetInitial(0.5f, ReputationTier.Unknown);
            return;
        }
        _sequencer.SetInitial(Normalize(ReputationController.Instance.Value), ReputationController.Instance.CurrentTier);
    }
    
    private void HandleReputationChanged(int delta, ReputationChangeReason reason)
    {
        if (!ReputationController.Exists)
            return;
        _sequencer.Enqueue(Normalize(ReputationController.Instance.Value), ReputationController.Instance.CurrentTier);
    }
    
    private void HandleTierChanged(ReputationTier tier) { }
    
    private float Normalize(int value)
    {
        var config = GameConfig.Instance.Reputation;
        float range = Mathf.Max(1, config.maxReputation - config.minReputation);
        return Mathf.Clamp01((value - config.minReputation) / range);
    }
}