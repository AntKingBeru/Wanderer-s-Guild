using UnityEngine;

namespace WanderersGuild
{
    public sealed class FrameRateApplier
    {
        /// <param name="vSync"></param>
        /// <param name="fpsLimit">0 or less = unlimited.</param>
        public void Apply(bool vSync, int fpsLimit)
        {
            QualitySettings.vSyncCount = vSync ? 1 : 0;
            Application.targetFrameRate = vSync || fpsLimit <= 0 ? -1 : fpsLimit;
        }
    }
}