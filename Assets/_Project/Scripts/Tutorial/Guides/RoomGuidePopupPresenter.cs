// Shows room-guide messages one at a time under the HUD bar; they wait while a game screen is open and never block play.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Queue — messages arriving together (two guides at rank E) come one after another; OK moves to the next.
    [RequireComponent(typeof(UIDocument))]
    public class RoomGuidePopupPresenter : MonoBehaviour
    {
        [SerializeField] private GamePanelHost host;

        private readonly Queue<RoomGuideMessage> _queue = new();
        private RoomGuidePopupView _view;
        private bool _hasCurrent;

        // NOTE: Not over a game screen (or one opening/closing), nor during a scene load.
        private bool CanShow =>
            (host == null || (!host.HasOpenPanel && !host.IsBusy)) &&
            !(SceneLoader.HasInstance && SceneLoader.Instance.IsBusy);

        private void OnEnable()
        {
            GameEventsRelay.OnRoomGuideMessage.AddListener(Enqueue);
            _view = RoomGuidePopupView.Bind(GetComponent<UIDocument>().rootVisualElement, gameObject, Dismiss);
            if (_view == null)
            {
                Debug.LogError("[RoomGuidePopupPresenter] RoomGuidePopup.uxml is missing guide-card, guide-title, guide-body or guide-ok.", this);
                return;
            }

            _hasCurrent = false;
            _view.SetVisible(false);
        }

        private void OnDisable() => GameEventsRelay.OnRoomGuideMessage.RemoveListener(Enqueue);

        private void Update()
        {
            if (_view == null)
                return;

            if (!_hasCurrent && _queue.Count > 0)
            {
                _view.Show(_queue.Dequeue());
                _hasCurrent = true;
            }

            _view.SetVisible(_hasCurrent && CanShow);
        }

        private void Enqueue(string title, string body)
        {
            if (!string.IsNullOrWhiteSpace(body))
                _queue.Enqueue(new RoomGuideMessage(title, body));
        }

        private void Dismiss() => _hasCurrent = false;
    }
}