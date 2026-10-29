using System.Threading;
using UnityEngine;

namespace WanderersGuild
{
    public class PreloadSceneStep : BootstrapStep
    {
        public override async Awaitable ExecuteAsync(CancellationToken token)
        {
            var target = GameConfig.Instance.Scenes.FirstSceneAfterBoot;

            if (!SceneLoader.Instance.Preload(target))
                Debug.LogWarning($"[PreloadSceneStep] Preload of {target} failed; the bootstrapper will load it normally.", this);

            await Awaitable.NextFrameAsync(token);
        }
    }
}