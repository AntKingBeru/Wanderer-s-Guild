// Reusable interruptible float-tween coroutine helper for UI animations.

using System;
using System.Collections;
using UnityEngine;

public static class UiTween
{
    public static IEnumerator Run(float duration, Action<float> onStep, Action onDone = null)
    {
        if (duration <= 0f)
        {
            onStep?.Invoke(1f);
            onDone?.Invoke();
            yield break;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            onStep?.Invoke(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        onStep?.Invoke(1f);
        onDone?.Invoke();
    }

    public static float EaseInOut(float t)
        => t * t * (3f - 2f * t);
}