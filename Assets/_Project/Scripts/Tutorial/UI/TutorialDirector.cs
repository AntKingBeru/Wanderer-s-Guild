// Runs the guided start tutorial: one step at a time (State pattern), driving the overlay, the input rules and the time pause.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Starts on OnTutorialStarted (Play Tutorial). Only the script's last
    // step completes the tutorial; running out of steps just stops guiding (requests, arrivals and saving stay held). Stops
    // fully on OnTutorialCompleted (its own end, or Skip / Complete from the debug menu). LateUpdate at 140 — after the camera,
    // the world UI and the screens have moved this frame.
    [DefaultExecutionOrder(140)]
    public class TutorialDirector : Singleton<TutorialDirector>
    {
        private const int MissingFramesBeforeSkip = 3;
        private static readonly TutorialRules Locked = new(); // Between steps: nothing in the world, build mode closed.

        [SerializeField] private GamePanelHost host;
        [SerializeField] private TutorialOverlay overlay;
        [Tooltip("The HUD UIDocument (HudUI).")]
        [SerializeField] private UIDocument hud;
        [Tooltip("The build-mode UIDocument (BuildModeUI).")]
        [SerializeField] private UIDocument buildMode;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldViewPresenter view;
        [Tooltip("The facility grid (FacilityGrid) — frames build spots.")]
        [SerializeField] private Grid grid;

        private readonly List<VisualElement> _holes = new();
        private readonly List<ITutorialTarget> _alwaysAllowed = new();
        private List<TutorialStep> _steps;
        private TutorialContext _context;
        private int _index;
        private bool _entered;
        private bool _nextPressed;
        private bool _clicked;
        private bool _ownsPause;
        private int _missingFrames;
        private string _text;
        private Button _clickButton;
        private Focusable _pendingFocus;
        private ITutorialTarget _pauseButton;

        // True while steps are being guided.
        public bool IsRunning => _steps != null;
        public int StepIndex => _index;
        public int StepCount => _steps?.Count ?? 0;
        public string CurrentText => _text;

        // NOTE: The pause menu and its screens, and scene loads, are the player's own — the tutorial steps aside meanwhile.
        private bool IsSuspended =>
            host.IsActive(MenuPanelId.Pause) || host.IsActive(MenuPanelId.Options) || host.IsActive(MenuPanelId.QuitConfirmation) ||
            (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy);

        private void OnEnable()
        {
            GameEventsRelay.OnTutorialStarted.AddListener(Begin);
            GameEventsRelay.OnTutorialCompleted.AddListener(OnCompleted);
            GameEventsRelay.OnQuestPosted.AddListener(OnQuestPosted);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnTutorialStarted.RemoveListener(Begin);
            GameEventsRelay.OnTutorialCompleted.RemoveListener(OnCompleted);
            GameEventsRelay.OnQuestPosted.RemoveListener(OnQuestPosted);
            Stop();
        }

        // ---------- Flow ----------

        // NOTE: A scene missing its pieces can't run the tutorial — it's completed instead, so the game isn't left blocked. Guild
        // objects come with the spawned Guild Hall and are found when needed; a missing one only gets a warning (its step waits).
        private void Begin()
        {
            if (IsRunning)
                return;

            if (host == null || overlay == null || !overlay.IsBound)
            {
                Debug.LogError("[TutorialDirector] Missing the panel host or the overlay — completing the tutorial.", this);
                if (TutorialManager.HasInstance)
                    TutorialManager.Instance.Complete();
                return;
            }

            if (grid == null)
                Debug.LogWarning("[TutorialDirector] No grid assigned — build spots won't be framed (placing still works).", this);

            var targets = new TutorialTargets(host, hud, buildMode, grid, new TutorialWorldProjector(worldCamera, view));
            _context = new TutorialContext(host, targets);
            foreach (var target in new[] { _context.Desk, _context.QuestBoard, _context.Lounge })
            {
                if (!target.IsAvailable)
                    Debug.LogWarning($"[TutorialDirector] No guild object opens {target.Opens} yet — its step will wait for one.", this);
            }

            _alwaysAllowed.Clear();
            _alwaysAllowed.Add(targets.HudFirst("hud-speeds"));
            _pauseButton = targets.Hud("hud-pause");

            _steps = TutorialScript.Build(_context);
            _index = 0;
            _entered = false;
            overlay.SetNextHandler(OnNext);
            TutorialGate.Apply(Locked);
        }

        // NOTE: After entering a step, IsRunning is checked again — a step that completes the tutorial (its OnEnter) has
        // already stopped everything through OnTutorialCompleted.
        private void LateUpdate()
        {
            if (!IsRunning)
                return;

            if (IsSuspended)
            {
                overlay.Hide();
                return;
            }

            if (_index >= _steps.Count)
            {
                EndOfScript();
                return;
            }

            var step = _steps[_index];
            if (!_entered && !TryEnter(step))
                return;
            if (!IsRunning)
                return;

            if (step.IsInstant || IsDone(step))
            {
                Exit(step);
                _index++;
                return;
            }

            Render(step);
        }

        // Debug: finishes the current step as if done (its OnEnter still runs).
        public void SkipStep()
        {
            if (!IsRunning || _index >= _steps.Count)
                return;

            var step = _steps[_index];
            if (!_entered)
                Enter(step);
            if (!IsRunning)
                return;

            Exit(step);
            _index++;
        }

        // NOTE: Running out of steps never completes the tutorial — only the script's own last step does (TutorialContext.
        // CompleteTutorial). Until then guiding stops but the tutorial stays running: no requests, arrivals or saves, and its
        // quest stays held.
        private void EndOfScript()
        {
            Debug.Log("[Tutorial] The script ended without completing the tutorial — requests, arrivals and saving stay held. " +
                      "Debug › Tutorial › Complete Now finishes it.", this);
            Release();
        }

        // ---------- Steps ----------

        // NOTE: Until the step can show, the UI stays blocked with nothing spotlit. A SkipIfMissing step whose highlight stays
        // missing for a few frames once everything has settled is passed over.
        private bool TryEnter(TutorialStep step)
        {
            if (IsReady(step))
            {
                Enter(step);
                return true;
            }

            if (step.SkipIfMissing && IsSettled(step) && ++_missingFrames >= MissingFramesBeforeSkip)
            {
                _missingFrames = 0;
                _index++;
            }

            overlay.Show(TutorialFrame.BlockOnly, CollectHoles(null));
            return false;
        }

        private bool IsSettled(TutorialStep step) => !host.IsBusy && (step.Ready == null || step.Ready());

        private bool IsReady(TutorialStep step) =>
            IsSettled(step) &&
            (step.Highlight == null || step.Highlight.IsAvailable) &&
            (step.ClickTarget == null || step.ClickTarget.IsAvailable);

        // NOTE: The text is fixed as the step starts. Focus goes to Next (Say) or the click target, so Enter works too.
        private void Enter(TutorialStep step)
        {
            _entered = true;
            _nextPressed = false;
            _clicked = false;
            _missingFrames = 0;
            _text = step.Text?.Invoke();

            TutorialGate.Apply(step.Rules);
            SetPause(step.PausesTime);
            step.Highlight?.SetEmphasis(true);
            SubscribeClick(step);

            var clickElement = step.ClickTarget?.Element;
            _pendingFocus = step.NextLabel != null ? overlay.NextButton
                          : clickElement is { focusable: true } ? clickElement : null;

            // NOTE: Last — it may complete the tutorial, which stops this director (see LateUpdate).
            step.OnEnter?.Invoke();
        }

        private void Exit(TutorialStep step)
        {
            UnsubscribeClick();
            step.Highlight?.SetEmphasis(false);
            _entered = false;
            _pendingFocus = null;
            TutorialGate.Apply(Locked);
        }

        private bool IsDone(TutorialStep step)
        {
            if (step.Until != null)
                return step.Until();
            if (step.ClickTarget != null)
                return _clicked;
            return _nextPressed;
        }

        private void Render(TutorialStep step)
        {
            var rect = default(Rect);
            var hasHighlight = step.Highlight != null && step.Highlight.TryGetRect(out rect);
            overlay.Show(new TutorialFrame(_text, step.NextLabel, hasHighlight, rect), CollectHoles(step));
            TryFocusPending();
        }

        // NOTE: The speed buttons are always usable; the pause button isn't while build mode is kept open — the pause menu
        // closes build mode, which would strand a building step.
        private List<VisualElement> CollectHoles(TutorialStep step)
        {
            _holes.Clear();
            if (step != null)
            {
                foreach (var target in step.Holes)
                    AddHole(target);
            }

            foreach (var target in _alwaysAllowed)
                AddHole(target);
            if (!TutorialGate.KeepsBuildMode)
                AddHole(_pauseButton);
            return _holes;
        }

        private void AddHole(ITutorialTarget target)
        {
            var element = target?.Element;
            if (element != null)
                _holes.Add(element);
        }

        // NOTE: Retried each frame until it sticks — the box is invisible on its first frame (not yet measured).
        private void TryFocusPending()
        {
            if (_pendingFocus == null)
                return;

            _pendingFocus.Focus();
            if (_pendingFocus.focusController != null && _pendingFocus.focusController.focusedElement == _pendingFocus)
                _pendingFocus = null;
        }

        // ---------- Clicks, Next ----------

        // NOTE: Subscribed after the screen's own handler, so the screen acts on the click first.
        private void SubscribeClick(TutorialStep step)
        {
            if (step.ClickTarget?.Element is not Button button)
                return;

            _clickButton = button;
            _clickButton.clicked += OnTargetClicked;
        }

        private void UnsubscribeClick()
        {
            if (_clickButton != null)
                _clickButton.clicked -= OnTargetClicked;
            _clickButton = null;
        }

        private void OnTargetClicked() => _clicked = true;

        private void OnNext()
        {
            if (_entered)
                _nextPressed = true;
        }

        // ---------- Pause ----------

        // NOTE: Its own owner in the counted menu pause — the pause menu opening over a step can't release it.
        private void SetPause(bool pause)
        {
            if (pause == _ownsPause)
                return;

            _ownsPause = pause;
            if (!TimeManager.HasInstance)
                return;

            if (pause)
                TimeManager.Instance.EnterMenuPause(this);
            else
                TimeManager.Instance.ExitMenuPause(this);
        }

        // ---------- End ----------

        // NOTE: Stops guiding — the current step, the input rules, the pause and the overlay. The tutorial quest stays held
        // (the context is kept). Safe to call twice.
        private void Release()
        {
            if (!IsRunning)
                return;

            if (_entered && _index < _steps.Count)
                Exit(_steps[_index]);

            _steps = null;
            _text = null;
            TutorialGate.Clear();
            SetPause(false);
            if (overlay != null)
                overlay.Hide();
        }

        // NOTE: The tutorial is over (or the scene is going): stop guiding and let go of its quest, so it plays out normally
        // instead of waiting forever. Safe to call twice.
        private void Stop()
        {
            Release();
            _context?.ReleaseQuest();
            _context = null;
        }

        private void OnCompleted(bool _) => Stop();

        private void OnQuestPosted(Quest quest)
        {
            if (IsRunning)
                _context.Track(quest);
        }
    }
}