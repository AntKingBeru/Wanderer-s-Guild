// Shows the opening goal under the HUD bar — "Build lodgings: 1/3" — until every starter bedroom is finished.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: On WorldViewUI (its document holds the goal label). Reads the current progress in Start, then follows the event.
    [RequireComponent(typeof(UIDocument))]
    public class LodgingGoalPresenter : MonoBehaviour
    {
        private OutlinedText _label;

        private void OnEnable() => GameEventsRelay.OnStarterLodgingsChanged.AddListener(Show);
        private void OnDisable() => GameEventsRelay.OnStarterLodgingsChanged.RemoveListener(Show);

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
            if (_label == null)
                return;

            _label.Text = $"Build lodgings: {built}/{total}";
            _label.style.display = total > 0 && built < total ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}