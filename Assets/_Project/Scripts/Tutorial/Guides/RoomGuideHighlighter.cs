// Makes the active room guide's target blink: the HUD hammer outside build mode, its catalog card inside — never blocking.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: On HudUI. Catalog cards are pooled, so the card class is moved to whichever card shows the room (and cleared from
    // the last one) every frame. Stops once the room is picked or placed.
    [RequireComponent(typeof(UIDocument))]
    public class RoomGuideHighlighter : MonoBehaviour
    {
        private const string HammerClass = "hud-build--guided";
        private const string CardClass = "build-card--guided";

        private VisualElement _hammer;
        private VisualElement _card;

        private void Start() => _hammer = GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>("hud-build");

        private void OnDisable()
        {
            _hammer?.RemoveFromClassList(HammerClass);
            SetCard(null);
        }

        private void LateUpdate()
        {
            var facility = RoomGuideDirector.HasInstance ? RoomGuideDirector.Instance.HighlightFacility : null;
            var build = BuildModeController.HasInstance ? BuildModeController.Instance : null;
            var building = build != null && build.IsActive;
            var blinkOn = facility != null && IsBlinkOn(GameConfig.Instance.Tutorial.BlinkSeconds);

            _hammer?.EnableInClassList(HammerClass, blinkOn && !building);

            var wantsCard = facility != null && building && build.Selected != facility;
            SetCard(wantsCard ? build.CatalogCard(facility) : null);
            _card?.EnableInClassList(CardClass, blinkOn);
        }

        private void SetCard(VisualElement card)
        {
            if (card == _card)
                return;

            _card?.RemoveFromClassList(CardClass);
            _card = card;
        }

        private static bool IsBlinkOn(float period) => Mathf.Repeat(Time.unscaledTime, period) < period * 0.5f;
    }
}