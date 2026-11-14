// Makes the tutorial's targets (Factory): elements of a game screen, the HUD or build mode, build spots, and guild objects.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    // NOTE: A screen's elements are searched only while it's the open one (names repeat across screens — confirm-yes, …) and
    // every time a target is used, so re-filled pooled rows are always found.
    public sealed class TutorialTargets
    {
        private readonly GamePanelHost _host;
        private readonly UIDocument _hud;
        private readonly UIDocument _buildMode;
        private readonly Grid _grid;
        private readonly TutorialWorldProjector _projector;

        public TutorialTargets(GamePanelHost host, UIDocument hud, UIDocument buildMode, Grid grid, TutorialWorldProjector projector)
        {
            _host = host;
            _hud = hud;
            _buildMode = buildMode;
            _grid = grid;
            _projector = projector;
        }

        private static BuildModeController Build => BuildModeController.HasInstance ? BuildModeController.Instance : null;

        // ---------- Game screens ----------

        public ITutorialTarget Panel(MenuPanelId id, string name) => new TutorialUiTarget(() => Named(ScreenRoot(id), name));

        // The first shown element with this class (list rows, quest notes).
        public ITutorialTarget PanelFirst(MenuPanelId id, string className) =>
            new TutorialUiTarget(() => FirstShown(ScreenRoot(id), className));

        // The parent of a named element (a clipboard row around its cycler, a bar around its track).
        public ITutorialTarget PanelParent(MenuPanelId id, string name) => new TutorialUiTarget(() => Named(ScreenRoot(id), name)?.parent);

        // True when the named element of this screen isn't showing (or the screen isn't open) — e.g. the clipboard slid away.
        public bool IsHidden(MenuPanelId id, string name) => !TutorialUiTarget.IsShown(Named(ScreenRoot(id), name));

        // ---------- HUD ----------

        public ITutorialTarget Hud(string name) => new TutorialUiTarget(() => Named(DocumentRoot(_hud), name));
        public ITutorialTarget HudFirst(string className) => new TutorialUiTarget(() => FirstShown(DocumentRoot(_hud), className));
        public ITutorialTarget HudParent(string name) => new TutorialUiTarget(() => Named(DocumentRoot(_hud), name)?.parent);

        // ---------- Build mode ----------

        public ITutorialTarget BuildUi(string name) => new TutorialUiTarget(() => Named(DocumentRoot(_buildMode), name));

        // A facility's catalog card — scrolled into view when emphasized.
        public ITutorialTarget BuildCard(FacilityData data) =>
            new TutorialUiTarget(() => Build != null ? Build.CatalogCard(data) : null,
                                 () =>
                                 {
                                     if (Build != null)
                                         Build.RevealCard(data);
                                 });

        // "Build this room here": the hammer → its card → its spot → Yes.
        public TutorialPlacementTarget Placement(TutorialPlacement placement) =>
            new(placement, Hud("hud-build"), BuildCard(placement.Facility),
                new TutorialFootprintTarget(placement, _grid, _projector), BuildUi("confirm-yes"));

        // ---------- World, groups ----------

        // The guild object that opens this screen (desk → ReceptionDesk, quest board → Quests, lounge → Roster).
        public TutorialWorldTarget World(MenuPanelId opens) => new(opens, _projector);
        public ITutorialTarget Union(params ITutorialTarget[] parts) => new TutorialUnionTarget(parts);

        private VisualElement ScreenRoot(MenuPanelId id) => _host != null && _host.IsActive(id) ? _host.RootOf(id) : null;
        private static VisualElement DocumentRoot(UIDocument document) => document != null ? document.rootVisualElement : null;
        private static VisualElement Named(VisualElement root, string name) => root?.Q<VisualElement>(name);

        private static VisualElement FirstShown(VisualElement root, string className) =>
            root?.Query<VisualElement>(className: className).Where(TutorialUiTarget.IsShown).First();
    }
}