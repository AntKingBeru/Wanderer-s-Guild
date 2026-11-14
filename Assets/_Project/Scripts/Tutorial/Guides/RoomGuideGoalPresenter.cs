// Shows the room guides' goal line under the HUD bar ("Build a Tavern"), below the lodging goal.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: On WorldViewUI (its document holds the line). Reads the current line in Start, then follows the event.
    [RequireComponent(typeof(UIDocument))]
    public class RoomGuideGoalPresenter : MonoBehaviour
    {
        private OutlinedText _label;

        private void OnEnable() => GameEventsRelay.OnRoomGuideGoalChanged.AddListener(Show);
        private void OnDisable() => GameEventsRelay.OnRoomGuideGoalChanged.RemoveListener(Show);

        private void Start()
        {
            _label = GetComponent<UIDocument>().rootVisualElement?.Q<OutlinedText>("world-guide");
            if (_label == null)
            {
                Debug.LogError("[RoomGuideGoalPresenter] Missing 'world-guide' in WorldView.uxml.", this);
                return;
            }

            Show(RoomGuideDirector.HasInstance ? RoomGuideDirector.Instance.GoalLine : null);
        }

        private void Show(string line)
        {
            if (_label == null)
                return;

            _label.Text = line ?? string.Empty;
            _label.style.display = string.IsNullOrEmpty(line) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}