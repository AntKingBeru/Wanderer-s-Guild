// Keeps a bobbing "!" above each guild object that needs the player (AttentionRules): recalculated on events, placed every frame.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: Observer — the game events only mark the state dirty; LateUpdate recalculates once, then places. After the camera
    // and the speech bubbles (135), so the mark follows this frame's view. Put it on the World View object with the speech
    // bubbles' director.
    [DefaultExecutionOrder(136)]
    public class AttentionMarkerDirector : MonoBehaviour
    {
        private const string LayerName = "world-markers";

        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldViewPresenter view;

        // NOTE: The objects that can ask for attention, by the screen they open; one mark each (index-matched below).
        private static readonly MenuPanelId[] Tracked = { MenuPanelId.ReceptionDesk, MenuPanelId.Recruits };

        private readonly bool[] _needed = new bool[Tracked.Length];
        private AttentionMarkerView[] _markers;
        private AttentionMarkerSettings _settings;
        private bool _dirty = true;
        private bool _building;

        private void Awake() => _settings = GameConfig.Instance.AttentionMarkers;

        private void OnEnable()
        {
            GameEventsRelay.OnRequestAdded.AddListener(OnRequestChanged);
            GameEventsRelay.OnRequestSeen.AddListener(OnRequestChanged);
            GameEventsRelay.OnRequestRemoved.AddListener(OnRequestRemoved);
            GameEventsRelay.OnQuestPartyReady.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.AddListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.AddListener(OnQuestEnded);
            GameEventsRelay.OnQuestBoardChanged.AddListener(MarkDirty);
            GameEventsRelay.OnApplicantArrived.AddListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.AddListener(OnApplicantLeft);
            GameEventsRelay.OnBuildModeChanged.AddListener(OnBuildModeChanged);
            _dirty = true;
        }

        private void OnDisable()
        {
            GameEventsRelay.OnRequestAdded.RemoveListener(OnRequestChanged);
            GameEventsRelay.OnRequestSeen.RemoveListener(OnRequestChanged);
            GameEventsRelay.OnRequestRemoved.RemoveListener(OnRequestRemoved);
            GameEventsRelay.OnQuestPartyReady.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestUpdated.RemoveListener(OnQuestChanged);
            GameEventsRelay.OnQuestEnded.RemoveListener(OnQuestEnded);
            GameEventsRelay.OnQuestBoardChanged.RemoveListener(MarkDirty);
            GameEventsRelay.OnApplicantArrived.RemoveListener(OnApplicantArrived);
            GameEventsRelay.OnApplicantLeft.RemoveListener(OnApplicantLeft);
            GameEventsRelay.OnBuildModeChanged.RemoveListener(OnBuildModeChanged);
            HideAll();
        }

        // NOTE: In Start — the world view's document root is set in its OnEnable.
        private void Start()
        {
            var layer = view != null && view.DocumentRoot != null ? view.DocumentRoot.Q<VisualElement>(LayerName) : null;
            if (layer == null)
            {
                Debug.LogError($"[AttentionMarkerDirector] Missing '{LayerName}' in WorldView.uxml (or no view assigned).", this);
                enabled = false;
                return;
            }

            _markers = new AttentionMarkerView[Tracked.Length];
            for (var i = 0; i < Tracked.Length; i++)
                _markers[i] = new AttentionMarkerView(layer, _settings.Icon);
        }

        private void LateUpdate()
        {
            if (_markers == null)
                return;

            if (_dirty)
                Recalculate();

            var lift = _settings.GapPixels + BobOffset();
            for (var i = 0; i < _markers.Length; i++)
                Place(i, lift);
        }

        // NOTE: Hidden while building, while its object is hovered (the name label sits there), when the object doesn't
        // exist (no office yet) or is off-screen.
        private void Place(int index, float lift)
        {
            var marker = _markers[index];
            var target = _needed[index] && !_building ? GuildInteractables.Find(Tracked[index]) : null;
            if (target == null || target.IsHovered ||
                !WorldViewProjection.TryToLocal(worldCamera, view, target.LabelAnchor, lift, out var local))
            {
                marker.Hide();
                return;
            }

            marker.Place(local);
        }

        private void Recalculate()
        {
            _dirty = false;
            for (var i = 0; i < Tracked.Length; i++)
                _needed[i] = AttentionRules.NeedsAttention(Tracked[i]);
        }

        // NOTE: A smooth up-and-down in whole UI pixels, on real time (keeps bobbing at any game speed).
        private float BobOffset()
        {
            if (_settings.BobPixels <= 0)
                return 0f;

            var phase = Mathf.Repeat(Time.unscaledTime / _settings.BobPeriod, 1f);
            var height = (1f - Mathf.Cos(phase * 2f * Mathf.PI)) * 0.5f;
            return Mathf.Round(height * _settings.BobPixels);
        }

        private void HideAll()
        {
            if (_markers == null)
                return;
            foreach (var marker in _markers)
                marker.Hide();
        }

        private void MarkDirty() => _dirty = true;
        private void OnRequestChanged(ActiveRequest _) => _dirty = true;
        private void OnRequestRemoved(ActiveRequest _, RequestEndReason __) => _dirty = true;
        private void OnQuestChanged(Quest _) => _dirty = true;
        private void OnQuestEnded(Quest _, QuestEndReason __) => _dirty = true;
        private void OnApplicantArrived(Applicant _) => _dirty = true;
        private void OnApplicantLeft(Applicant _, ApplicantEndReason __) => _dirty = true;
        private void OnBuildModeChanged(bool building) => _building = building;
    }
}