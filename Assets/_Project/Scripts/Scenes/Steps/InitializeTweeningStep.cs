using System.Threading;
using DG.Tweening;
using UnityEngine;

namespace WanderersGuild
{
    public class InitializeTweeningStep : BootstrapStep
    {
        public override async Awaitable ExecuteAsync(CancellationToken token)
        {
            var settings = GameConfig.Instance.Tweening;

            // NOTE: Must run before any tween is created; otherwise DOTween auto-inits with default settings.
            DOTween.Init(recycleAllByDefault: false, useSafeMode: settings.UseSafeMode, logBehaviour: settings.LogBehaviour)
                .SetCapacity(settings.TweenersCapacity, settings.SequencesCapacity);

            await Awaitable.NextFrameAsync(token); // Spreads boot work across frames.
        }
    }
}