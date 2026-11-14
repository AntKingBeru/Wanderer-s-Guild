// The room-guide popup card: a title, a message and OK.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: OK isn't focusable — Space is the time-pause hotkey and the popup must never steal it. Only the card picks.
    public sealed class RoomGuidePopupView
    {
        private readonly VisualElement _card;
        private readonly OutlinedText _title;
        private readonly OutlinedText _body;

        private RoomGuidePopupView(VisualElement card, OutlinedText title, OutlinedText body)
        {
            _card = card;
            _title = title;
            _body = body;
        }

        public static RoomGuidePopupView Bind(VisualElement documentRoot, GameObject owner, Action onOk)
        {
            var card = documentRoot?.Q<VisualElement>("guide-card");
            var title = documentRoot?.Q<OutlinedText>("guide-title");
            var body = documentRoot?.Q<OutlinedText>("guide-body");
            var ok = documentRoot?.Q<Button>("guide-ok");
            if (card == null || title == null || body == null || ok == null)
                return null;

            ok.RemoveFromClassList(Button.ussClassName);
            ok.AddManipulator(new MenuButtonFeedback(owner, pop: true));
            ok.focusable = false;
            ok.clicked += () => onOk?.Invoke();
            return new RoomGuidePopupView(card, title, body);
        }

        public void Show(RoomGuideMessage message)
        {
            _title.Text = message.Title ?? string.Empty;
            _body.Text = message.Body ?? string.Empty;
        }

        public void SetVisible(bool visible) => _card.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}