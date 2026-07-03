// Serializes reputation-change animations into a queue so they play sequentially without retracting mid-batch.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReputationHudSequencer
{
    private readonly ReputationHudView _view;
    private readonly MonoBehaviour _runner;
    private readonly float _slideDuration;
    private readonly float _moveDuration;

    private readonly Queue<(float normalized, ReputationTier tier)> _pending = new Queue<(float, ReputationTier)>();
    private bool _running;
    private float _currentNormalized;
    
    public ReputationHudSequencer(ReputationHudView view, MonoBehaviour runner, float slideDuration, float moveDuration)
    {
        _view = view;
        _runner = runner;
        _slideDuration = slideDuration;
        _moveDuration = moveDuration;
    }

    public void SetInitial(float normalized, ReputationTier tier)
    {
        _currentNormalized = normalized;
        _view.SetEmojiPosition(normalized);
        _view.SetTierColor(tier);
        _view.SetSlide(0f);
    }
    
    public void Enqueue(float normalized, ReputationTier tier)
    {
        _pending.Enqueue((normalized, tier));
        if (!_running)
            _runner.StartCoroutine(RunSequence());
    }
    
    private IEnumerator RunSequence()
    {
        _running = true;

        yield return _runner.StartCoroutine(UiTween.Run(_slideDuration,
            t => _view.SetSlide(UiTween.EaseInOut(t))));
        
        while (_pending.Count > 0)
        {
            var (target, tier) = _pending.Dequeue();
            var from = _currentNormalized;
            _view.SetTierColor(tier);
            yield return _runner.StartCoroutine(UiTween.Run(_moveDuration, t =>
            {
                var e = UiTween.EaseInOut(t);
                _view.SetEmojiPosition(Mathf.Lerp(from, target, e));
            }));
            _currentNormalized = target;
        }

        yield return _runner.StartCoroutine(UiTween.Run(_slideDuration,
            t => _view.SetSlide(UiTween.EaseInOut(1f - t))));

        _running = false;
    }
}