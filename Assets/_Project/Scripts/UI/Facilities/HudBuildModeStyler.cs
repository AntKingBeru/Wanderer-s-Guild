// Hides the HUD's bottom elements (rank bar, reputation) while build mode is open — the build bar takes that space.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class HudBuildModeStyler : MonoBehaviour
    {
        private const string BuildingClass = "game-hud--building";

        private VisualElement _hud;

        private void OnEnable()
        {
            _hud = GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>("game-hud");
            if (_hud == null)
            {
                Debug.LogError("[HudBuildModeStyler] Missing 'game-hud' in the HUD UXML.", this);
                return;
            }

            _hud.EnableInClassList(BuildingClass, BuildModeController.HasInstance && BuildModeController.Instance.IsActive);
            GameEventsRelay.OnBuildModeChanged.AddListener(OnBuildModeChanged);
        }

        private void OnDisable() => GameEventsRelay.OnBuildModeChanged.RemoveListener(OnBuildModeChanged);

        private void OnBuildModeChanged(bool active) => _hud?.EnableInClassList(BuildingClass, active);
    }
}