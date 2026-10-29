using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace WanderersGuild
{
    public class GameBootstrapper : MonoBehaviour
    {
        [Tooltip("Executed top to bottom. Each step finishes before the next one starts.")]
        [SerializeField] private List<BootstrapStep> steps = new();

        private async void Start()
        {
            var token = destroyCancellationToken;

            foreach (var step in steps.Where(step => step))
            {
                if (!await TryRunStepAsync(step, token))
                    return; // Canceled: the application is quitting.
            }

            GameEventsRelay.OnBootstrapCompleted.Invoke();
            HandOff();
        }

        // NOTE: A failing step is logged and skipped, so one broken system can't brick the boot.
        private static async Awaitable<bool> TryRunStepAsync(BootstrapStep step, CancellationToken token)
        {
            GameEventsRelay.OnBootstrapStepStarted.Invoke(step.StepName);

            try
            {
                await step.ExecuteAsync(token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, step);
                return true;
            }
        }

        // NOTE: The transition is handed to the persistent SceneLoader because this object dies when Bootstrap unloads.
        private static void HandOff()
        {
            if (!SceneLoader.HasInstance)
            {
                Debug.LogError("[GameBootstrapper] No SceneLoader in the scene; can't leave Bootstrap.");
                return;
            }

            if (SceneLoader.Instance.HasPreloadedScene)
                SceneLoader.Instance.TransitionToPreloaded();
            else
                SceneLoader.Instance.TransitionTo(GameConfig.Instance.Scenes.FirstSceneAfterBoot);
        }
    }
}