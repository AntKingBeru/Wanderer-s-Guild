using System.Threading;
using UnityEngine;

namespace WanderersGuild
{
    public class LoadSettingsStep : BootstrapStep
    {
        public override async Awaitable ExecuteAsync(CancellationToken token)
        {
            if (SettingsManager.HasInstance)
                SettingsManager.Instance.LoadAndApply();
            else
                Debug.LogWarning("[LoadSettingsStep] No SettingsManager under __PersistentSystems; using mixer defaults.", this);

            await Awaitable.NextFrameAsync(token);
        }
    }
}