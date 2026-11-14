// Shows the opening goal under the HUD bar — "Build lodgings: 1/3" — until every starter bedroom is finished; hidden while
// the start tutorial runs.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: On WorldViewUI (its document holds the goal label). Reads the current progress in Start, then follows the events.
    [RequireComponent(typeof(UIDocument))]
    public class LodgingGoalPresenter : MonoBehaviour
    {
        private OutlinedText _label;
        private int _built;
        private int _total;

        private void OnEnable()
        {
            GameEventsRelay.OnStarterLodgingsChanged.AddListener(Show);
            GameEventsRelay.OnTutorialCompleted.AddListener(OnTutorialCompleted);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnStarterLodgingsChanged.RemoveListener(Show);
            GameEventsRelay.OnTutorialCompleted.RemoveListener(OnTutorialCompleted);
        }

        private void Start()
        {
            _label = GetComponent<UIDocument>().rootVisualElement?.Q<OutlinedText>("world-goal");
            if (_label == null)
            {
                Debug.LogError("[LodgingGoalPresenter] Missing 'world-goal' in WorldView.uxml.", this);
                return;
            }

            if (LodgingManager.HasInstance)
                Show(LodgingManager.Instance.StarterBuilt, LodgingManager.Instance.StarterTotal);
            else
                _label.style.display = DisplayStyle.None;
        }

        private void Show(int built, int total)
        {
            _built = built;
            _total = total;
            Refresh();
        }

        private void OnTutorialCompleted(bool _) => Refresh();

        // NOTE: The tutorial gives its own instructions (and builds the bedrooms with the player).
        private void Refresh()
        {
            if (_label == null)
                return;

            var tutorialRunning = TutorialManager.HasInstance && !TutorialManager.Instance.IsDone;
            _label.Text = $"Build lodgings: {_built}/{_total}";
            _label.style.display = !tutorialRunning && _total > 0 && _built < _total ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}