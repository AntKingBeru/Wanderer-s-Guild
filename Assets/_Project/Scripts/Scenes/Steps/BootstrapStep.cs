using System.Threading;
using UnityEngine;

namespace WanderersGuild
{
    public abstract class BootstrapStep : MonoBehaviour
    {
        public string StepName => GetType().Name;

        // Performs the step. The bootstrapper waits for it before running the next one.
        public abstract Awaitable ExecuteAsync(CancellationToken token);
    }
}