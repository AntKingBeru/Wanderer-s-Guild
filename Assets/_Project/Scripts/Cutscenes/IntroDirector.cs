// Runs the Intro scene: waits for the fade-in, plays the premise cutscene, then the guild-naming panel.
using UnityEngine;

namespace WanderersGuild
{
    public class IntroDirector : MonoBehaviour
    {
        [SerializeField] private CutsceneView cutsceneView;
        [SerializeField] private HoldToSkip holdToSkip;
        [SerializeField] private CutsceneData cutscene;
        [SerializeField] private GuildNamingView namingView;

        private readonly StateMachine _machine = new();
        private CutscenePlayer _player;
        private GuildNameRules _nameRules;
        private NewGuildSaver _saver;

        private void Start()
        {
            if (!cutsceneView || !cutsceneView.IsBound || !holdToSkip || !namingView || !namingView.IsBound)
            {
                Debug.LogError("[IntroDirector] Missing or unbound cutscene view, hold-to-skip or naming view.", this);
                return;
            }

            if (!cutscene)
                Debug.LogWarning("[IntroDirector] No CutsceneData assigned; the cutscene will end at once.", this);

            if (!GameSession.HasActiveSlot)
                Debug.LogWarning("[IntroDirector] No active save slot — the Intro was entered without New Game.", this);

            _player = new CutscenePlayer(cutsceneView, GameConfig.Instance.Intro, gameObject);
            _nameRules = new GuildNameRules(GameConfig.Instance.NewGuild);
            _saver = new NewGuildSaver(GameConfig.Instance.NewGuild);
            namingView.Initialize(gameObject);

            // NOTE: Starts only once the screen has faded in, so the first line isn't typed under black.
            if (SceneLoader.HasInstance && SceneLoader.Instance.IsBusy)
                GameEventsRelay.OnSceneTransitionCompleted.AddListener(OnTransitionCompleted);
            else
                BeginCutscene();
        }

        private void OnDestroy()
        {
            GameEventsRelay.OnSceneTransitionCompleted.RemoveListener(OnTransitionCompleted);
            _machine.Shutdown();
        }

        private void OnTransitionCompleted(SceneId scene)
        {
            if (scene != SceneId.Intro)
                return;

            GameEventsRelay.OnSceneTransitionCompleted.RemoveListener(OnTransitionCompleted);
            BeginCutscene();
        }

        private void BeginCutscene() =>
            _machine.ChangeState(new IntroCutsceneState(_player, holdToSkip, cutscene,
                                                        GameConfig.Instance.InputBindings.AnyKey,
                                                        OnCutsceneFinished, destroyCancellationToken));

        // NOTE: Played through or skipped, the guild still needs a name.
        private void OnCutsceneFinished(bool skipped) =>
            _machine.ChangeState(new IntroNamingState(namingView, _nameRules, _saver, gameObject, destroyCancellationToken));
    }
}