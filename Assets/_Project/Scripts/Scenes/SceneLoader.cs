using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WanderersGuild
{
    public class SceneLoader : Singleton<SceneLoader>
    {
        // NOTE: Unity reports exactly 0.9 when a load with allowSceneActivation = false is ready to activate.
        private const float ActivationThreshold = 0.9f;

        private AsyncOperation _pendingOperation;
        private SceneId _pendingScene;
        private bool _isTransitioning;

        public bool HasPreloadedScene => _pendingOperation != null && !_isTransitioning;
        public bool IsBusy => _pendingOperation != null || _isTransitioning;

        // Starts loading in the background without activating. Only one load may run at a time.
        public bool Preload(SceneId scene)
        {
            if (IsBusy)
            {
                Debug.LogWarning($"[SceneLoader] Can't preload {scene}: another load is in progress.", this);
                return false;
            }

            _pendingOperation = BeginLoad(scene);
            _pendingScene = scene;
            return _pendingOperation != null;
        }

        // Fades out, activates the preloaded scene, then fades in. Fire-and-forget.
        public void TransitionToPreloaded()
        {
            if (!HasPreloadedScene)
            {
                Debug.LogWarning("[SceneLoader] No preloaded scene to transition to.", this);
                return;
            }

            RunTransition(_pendingScene, _pendingOperation);
        }

        // Loads and transitions to a scene in one call. Fire-and-forget.
        public void TransitionTo(SceneId scene)
        {
            if (IsBusy)
            {
                Debug.LogWarning($"[SceneLoader] Can't load {scene}: another load is in progress.", this);
                return;
            }

            var operation = BeginLoad(scene);
            if (operation == null)
                return;

            _pendingOperation = operation;
            _pendingScene = scene;
            RunTransition(scene, operation);
        }

        private AsyncOperation BeginLoad(SceneId scene)
        {
            var sceneName = GameConfig.Instance.Scenes.GetSceneName(scene);
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] '{scene}' is unmapped in GameConfig or missing from the Build Profile scene list.", this);
                return null;
            }

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
                return null;

            operation.allowSceneActivation = false;
            GameEventsRelay.OnSceneLoadStarted.Invoke(scene);
            return operation;
        }

        // NOTE: An async void entry point owned by this persistent object, so the caller's scene can unload mid-transition.
        private async void RunTransition(SceneId scene, AsyncOperation operation)
        {
            _isTransitioning = true;
            var token = destroyCancellationToken;

            try
            {
                if (ScreenFader.HasInstance)
                    await ScreenFader.Instance.FadeOutAsync(token);

                await WaitUntilReadyAsync(operation, token);
                operation.allowSceneActivation = true;

                while (!operation.isDone)
                    await Awaitable.NextFrameAsync(token);

                GameEventsRelay.OnSceneLoadCompleted.Invoke(scene);

                if (ScreenFader.HasInstance)
                    await ScreenFader.Instance.FadeInAsync(token);
                
                // NOTE: Reset BEFORE raising the event, so listeners see an idle loader and may start a new load.
                ResetState();
                GameEventsRelay.OnSceneTransitionCompleted.Invoke(scene);
            }
            catch (OperationCanceledException)
            {
                // The loader was destroyed (application quit). Nothing to recover.
            }
            finally
            {
                ResetState(); // Idempotent; covers the cancel and exception paths.
            }
        }
        
        private void ResetState()
        {
            _pendingOperation = null;
            _isTransitioning = false;
        }

        private static async Awaitable WaitUntilReadyAsync(AsyncOperation operation, CancellationToken token)
        {
            while (operation.progress < ActivationThreshold)
            {
                GameEventsRelay.OnSceneLoadProgress.Invoke(operation.progress / ActivationThreshold);
                await Awaitable.NextFrameAsync(token);
            }

            GameEventsRelay.OnSceneLoadProgress.Invoke(1f);
        }
    }
}