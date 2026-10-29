using System.Threading;
using DG.Tweening;
using UnityEngine;

namespace WanderersGuild
{
    public static class TweenAwaitExtensions
    {
        //Completes when the tween finishes or is killed. Throws OperationCanceledException on cancel.
        public static async Awaitable AwaitCompletionAsync(this Tween tween, CancellationToken token)
        {
            // NOTE: IsActive() comes first, so IsComplete() is never called on a killed tween (which would log a warning).
            while (tween.IsActive() && !tween.IsComplete())
                await Awaitable.NextFrameAsync(token);
        }
    }
}