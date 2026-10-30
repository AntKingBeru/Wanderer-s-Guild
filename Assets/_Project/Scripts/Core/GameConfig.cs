using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace WanderersGuild
{
    [DefaultExecutionOrder(-101)]
    public class GameConfig : Singleton<GameConfig>
    {
        [SerializeField] private SceneSettings scenes = new();
        [SerializeField] private TransitionSettings transitions = new();
        [SerializeField] private TweeningSettings tweening = new();
        [SerializeField] private InputBindingSettings inputBindings = new();
        [SerializeField] private PlayerSettingsConfig playerSettings = new();
        [SerializeField] private DisplaySettingsConfig displaySettings = new();
        [SerializeField] private SaveSlotSettings saveSlots = new();
        [SerializeField] private UIScaleSettings uiScale = new();
        [SerializeField] private AudioMixerSettings audioMixer = new();
        [SerializeField] private MusicSettings music = new();
        [SerializeField] private UISoundSettings uiSounds = new();
        [SerializeField] private SplashSettings splash = new();
        [SerializeField] private TitleScreenSettings titleScreen = new();
        [SerializeField] private CameraPanSettings cameraPan = new();
        [SerializeField] private ParallaxSettings parallax = new();
        [SerializeField] private PupilFollowSettings pupilFollow = new();
        [SerializeField] private TitleCharacterReactionSettings titleCharacterReactions = new();
        [SerializeField] private MenuButtonSettings menuButtons = new();
        [SerializeField] private MenuEntranceSettings menuEntrance = new();
        [SerializeField] private PanelTransitionSettings panelTransitions = new();
        [SerializeField] private CreditsSettings credits = new();
        [SerializeField] private IntroSettings intro = new();
        [SerializeField] private NewGuildSettings newGuild = new();
        [SerializeField] private TimeSettings time = new();
        [SerializeField] private SaveGameSettings saveGame = new();
        [SerializeField] private GuildRankSettings guildRanks = new();
        [SerializeField] private ReputationSettings reputation = new();
        [SerializeField] private GuildHudSettings guildHud = new();
        [SerializeField] private RequestSettings requests = new();
        [SerializeField] private ApplicationSettings applications = new();
        [SerializeField] private QuestSettings quests = new();
        [SerializeField] private QuestClipboardSettings questClipboard = new();
        [SerializeField] private QuestPanelSettings questPanel = new();
        [SerializeField] private AdventurerSettings adventurers = new();
        [SerializeField] private PromotionSettings promotion = new();
        [SerializeField] private WorldViewSettings worldView = new();
        [SerializeField] private GuildCameraSettings guildCamera = new();
        [SerializeField] private InteractionSettings interaction = new();
        [SerializeField] private FacilitySettings facilities = new();

        public SceneSettings Scenes => scenes;
        public TransitionSettings Transitions => transitions;
        public TweeningSettings Tweening => tweening;
        public InputBindingSettings InputBindings => inputBindings;
        public PlayerSettingsConfig PlayerSettings => playerSettings;
        public DisplaySettingsConfig DisplaySettings => displaySettings;
        public SaveSlotSettings SaveSlots => saveSlots;
        public UIScaleSettings UIScale => uiScale;
        public AudioMixerSettings AudioMixer => audioMixer;
        public MusicSettings Music => music;
        public UISoundSettings UISounds => uiSounds;
        public SplashSettings Splash => splash;
        public TitleScreenSettings TitleScreen => titleScreen;
        public CameraPanSettings CameraPan => cameraPan;
        public ParallaxSettings Parallax => parallax;
        public PupilFollowSettings PupilFollow => pupilFollow;
        public TitleCharacterReactionSettings TitleCharacterReactions => titleCharacterReactions;
        public MenuButtonSettings MenuButtons => menuButtons;
        public MenuEntranceSettings MenuEntrance => menuEntrance;
        public PanelTransitionSettings PanelTransitions => panelTransitions;
        public CreditsSettings Credits => credits;
        public IntroSettings Intro => intro;
        public NewGuildSettings NewGuild => newGuild;
        public TimeSettings Time => time;
        public SaveGameSettings SaveGame => saveGame;
        public GuildRankSettings GuildRanks => guildRanks;
        public ReputationSettings Reputation => reputation;
        public GuildHudSettings GuildHud => guildHud;
        public RequestSettings Requests => requests;
        public ApplicationSettings Applications => applications;
        public QuestSettings Quests => quests;
        public QuestClipboardSettings QuestClipboard => questClipboard;
        public QuestPanelSettings QuestPanel => questPanel;
        public AdventurerSettings Adventurers => adventurers;
        public PromotionSettings Promotion => promotion;
        public WorldViewSettings WorldView => worldView;
        public GuildCameraSettings GuildCamera => guildCamera;
        public InteractionSettings Interaction => interaction;
        public FacilitySettings Facilities => facilities;
    }
    
    #region Scene Management
    [Serializable]
    public class SceneSettings
    {
        [Tooltip("Scene the bootstrapper hands off to once booting finishes.")]
        [SerializeField] private SceneId firstSceneAfterBoot = SceneId.MainMenu;

        [SerializeField] private SceneEntry[] entries =
        {
            new(SceneId.Bootstrap, "Bootstrap"),
            new(SceneId.MainMenu, "MainMenu"),
            new(SceneId.Intro, "Intro"),
            new(SceneId.Game, "Game")
        };

        public SceneId FirstSceneAfterBoot => firstSceneAfterBoot;

        //Returns the Build Profile scene name for an id, or null when it isn't mapped.
        public string GetSceneName(SceneId id)
        {
            return (from entry in entries where entry.Id == id select entry.SceneName).FirstOrDefault();
        }
    }
    #endregion
    
    #region Transitions
    [Serializable]
    public class TransitionSettings
    {
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.75f;
        [SerializeField] private Ease fadeEase = Ease.InOutSine;

        public float FadeOutDuration => fadeOutDuration;
        public float FadeInDuration => fadeInDuration;
        public Ease FadeEase => fadeEase;
    }
    #endregion
    
    #region Tweening
    [Serializable]
    public class TweeningSettings
    {
        [SerializeField] private bool useSafeMode = true;
        [SerializeField] private LogBehaviour logBehaviour = LogBehaviour.ErrorsOnly;
        [SerializeField, Min(1)] private int tweenersCapacity = 200;
        [SerializeField, Min(1)] private int sequencesCapacity = 50;

        public bool UseSafeMode => useSafeMode;
        public LogBehaviour LogBehaviour => logBehaviour;
        public int TweenersCapacity => tweenersCapacity;
        public int SequencesCapacity => sequencesCapacity;
    }
    #endregion
    
    #region Input
    // NOTE: Named InputBindingSettings to avoid clashing with UnityEngine.InputSystem.InputSettings.
    [Serializable]
    public class InputBindingSettings
    {
        [Tooltip("Button — <Keyboard>/escape. Pressed: skips the studio splash. Held: skips cutscenes.")]
        [SerializeField] private InputActionReference skip;

        [Tooltip("Button — <Keyboard>/anyKey + <Mouse>/left, right, middle buttons. Dismisses the title screen, advances cutscenes.")]
        [SerializeField] private InputActionReference anyKey;

        [Tooltip("Value (Vector2) — <Pointer>/position. Feeds PointerTracker.")]
        [SerializeField] private InputActionReference point;
        
        [Tooltip("Button — <Keyboard>/space. Pauses, or resumes at the last speed.")]
        [SerializeField] private InputActionReference togglePause;

        [Tooltip("Button — <Keyboard>/1. Normal speed.")]
        [SerializeField] private InputActionReference speedNormal;

        [Tooltip("Button — <Keyboard>/2. Fast speed.")]
        [SerializeField] private InputActionReference speedFast;

        [Tooltip("Button — <Keyboard>/3. Very fast speed.")]
        [SerializeField] private InputActionReference speedVeryFast;
        
        [Tooltip("Button — <Keyboard>/escape (Game map). Opens the in-game pause menu.")]
        [SerializeField] private InputActionReference pauseMenu;
        
        [Tooltip("Value (Vector2) — WASD + arrow-key composites (Game map). Pans the guild camera.")]
        [SerializeField] private InputActionReference cameraMove;

        [Tooltip("Button — <Mouse>/middleButton (Game map). Held: drag-pans the guild camera (with Point).")]
        [SerializeField] private InputActionReference cameraDrag;

        [Tooltip("Value (Axis) — 1D composite: negative <Keyboard>/q, positive <Keyboard>/e (Game map). Rotates 90°.")]
        [SerializeField] private InputActionReference cameraRotate;

        [Tooltip("Value (Axis) — <Mouse>/scroll/y (Game map). Zooms the guild camera (sign only).")]
        [SerializeField] private InputActionReference cameraZoom;
        
        [Tooltip("Button — <Mouse>/leftButton (Game map). Clicks the hovered guild object (opens its screen).")]
        [SerializeField] private InputActionReference worldClick;

        public InputActionReference Skip => skip;
        public InputActionReference AnyKey => anyKey;
        public InputActionReference Point => point;
        public InputActionReference TogglePause => togglePause;
        public InputActionReference SpeedNormal => speedNormal;
        public InputActionReference SpeedFast => speedFast;
        public InputActionReference SpeedVeryFast => speedVeryFast;
        public InputActionReference PauseMenu => pauseMenu;
        public InputActionReference CameraMove => cameraMove;
        public InputActionReference CameraDrag => cameraDrag;
        public InputActionReference CameraRotate => cameraRotate;
        public InputActionReference CameraZoom => cameraZoom;
        public InputActionReference WorldClick => worldClick;
    }
    #endregion
    
    #region Settings
    // NOTE: "Player settings" = the player's own Options choices (not Unity's PlayerSettings).
    [Serializable]
    public class PlayerSettingsConfig
    {
        [Tooltip("Settings file name inside Application.persistentDataPath.")]
        [SerializeField] private string fileName = "settings.json";

        [Tooltip("Volume sliders move in steps of this many percent.")]
        [SerializeField, Range(1, 25)] private int volumeStep = 5;

        [SerializeField, Range(0, 100)] private int defaultMasterVolume = 100;
        [SerializeField, Range(0, 100)] private int defaultMusicVolume = 80;
        [SerializeField, Range(0, 100)] private int defaultSfxVolume = 80;
        [SerializeField, Range(0, 100)] private int defaultCharacterVolume = 80;

        public string FileName => fileName;
        public int VolumeStep => volumeStep;

        public int DefaultVolume(AudioChannel channel) => channel switch
        {
            AudioChannel.Master => defaultMasterVolume,
            AudioChannel.Music => defaultMusicVolume,
            AudioChannel.Sfx => defaultSfxVolume,
            AudioChannel.Character => defaultCharacterVolume,
            _ => 100
        };
    }
    
    [Serializable]
    public class DisplaySettingsConfig
    {
        [SerializeField] private WindowModeSetting defaultWindowMode = WindowModeSetting.Borderless;
        [SerializeField] private bool defaultVSync = true;

        [Tooltip("FPS limit choices in slider order. 0 = Unlimited.")]
        [SerializeField] private int[] fpsLimits = { 30, 60, 90, 120, 144, 165, 240, 0 };
        [SerializeField, Min(0)] private int defaultFpsLimitIndex = 7;

        [SerializeField, Range(0, 100)] private int defaultBrightness = 50;
        [Tooltip("Brightness slider moves in steps of this much.")]
        [SerializeField, Range(1, 25)] private int brightnessStep = 5;
        [Tooltip("Exposure (EV) at brightness 0 / 100 is minus / plus this. 50 = no change.")]
        [SerializeField, Min(0.1f)] private float maxExposure = 1f;
        [Tooltip("Priority of the global brightness Volume; keep above every scene Volume.")]
        [SerializeField] private float brightnessVolumePriority = 100f;

        [Tooltip("Seconds before a resolution/window/monitor change reverts unless the player keeps it.")]
        [SerializeField, Min(1)] private int keepPromptSeconds = 10;

        [Tooltip("Seconds between FPS counter updates (averaged over that window).")]
        [SerializeField, Min(0.1f)] private float fpsCounterInterval = 0.5f;

        public WindowModeSetting DefaultWindowMode => defaultWindowMode;
        public bool DefaultVSync => defaultVSync;
        public int FpsLimitCount => fpsLimits?.Length ?? 0;
        public int DefaultFpsLimitIndex => Mathf.Clamp(defaultFpsLimitIndex, 0, Mathf.Max(0, FpsLimitCount - 1));
        public int DefaultBrightness => defaultBrightness;
        public int BrightnessStep => brightnessStep;
        public float MaxExposure => maxExposure;
        public float BrightnessVolumePriority => brightnessVolumePriority;
        public int KeepPromptSeconds => keepPromptSeconds;
        public float FpsCounterInterval => fpsCounterInterval;

        // The FPS limit at a slider index; 0 means Unlimited.
        public int FpsLimitAt(int index) =>
            FpsLimitCount == 0 ? 0 : fpsLimits[Mathf.Clamp(index, 0, FpsLimitCount - 1)];
    }
    #endregion
    
    #region Save Slots
    [Serializable]
    public class SaveSlotSettings
    {
        [Tooltip("Number of save slots (one guild per slot).")]
        [SerializeField, Range(1, 10)] private int slotCount = 5;

        [Tooltip("Folder inside Application.persistentDataPath holding one sub-folder per slot.")]
        [SerializeField] private string rootFolder = "saves";
        [SerializeField] private string headerFileName = "header.json";
        [SerializeField] private string thumbnailFileName = "thumbnail.png";

        [Tooltip("The full game state of a slot (time, play time, and later every system's data).")]
        [SerializeField] private string gameFileName = "game.json";

        [Tooltip("Pixel size of slot thumbnails (shown at 32×18 UI pixels).")]
        [SerializeField] private Vector2Int thumbnailSize = new(64, 36);

        [Tooltip("Scene a new guild starts in.")]
        [SerializeField] private SceneId newGameScene = SceneId.Intro;

        [Tooltip("Scene a loaded guild opens in.")]
        [SerializeField] private SceneId loadedGameScene = SceneId.Game;

        [Tooltip("How the real-world save time is shown (.NET date/time format).")]
        [SerializeField] private string savedAtFormat = "yyyy-MM-dd HH:mm";

        public int SlotCount => slotCount;
        public string RootFolder => string.IsNullOrWhiteSpace(rootFolder) ? "saves" : rootFolder;
        public string HeaderFileName => headerFileName;
        public string ThumbnailFileName => thumbnailFileName;
        public string GameFileName => string.IsNullOrWhiteSpace(gameFileName) ? "game.json" : gameFileName;
        public Vector2Int ThumbnailSize => thumbnailSize;
        public SceneId NewGameScene => newGameScene;
        public SceneId LoadedGameScene => loadedGameScene;
        public string SavedAtFormat => savedAtFormat;
    }
    #endregion
    
    #region Saving
    [Serializable]
    public class SaveGameSettings
    {
        [Tooltip("Save automatically at the start of every in-game day.")]
        [SerializeField] private bool autosaveAtDayStart = true;

        [Tooltip("Save when the game closes (also when Play Mode stops in the Editor).")]
        [SerializeField] private bool saveOnQuit = true;

        [Tooltip("The game camera is rendered at this size, then box-averaged down to the slot thumbnail size.")]
        [SerializeField] private Vector2Int thumbnailRenderSize = new(320, 180);

        [Tooltip("Fade in/out time of the save toast.")]
        [SerializeField, Min(0f)] private float toastFadeDuration = 0.25f;

        [Tooltip("How long \"Saved\" stays up before fading.")]
        [SerializeField, Min(0f)] private float savedToastDuration = 1.5f;

        [Tooltip("How long \"Save failed\" stays up before fading.")]
        [SerializeField, Min(0f)] private float failedToastDuration = 3f;

        public bool AutosaveAtDayStart => autosaveAtDayStart;
        public bool SaveOnQuit => saveOnQuit;
        public Vector2Int ThumbnailRenderSize => thumbnailRenderSize;
        public float ToastFadeDuration => toastFadeDuration;
        public float SavedToastDuration => savedToastDuration;
        public float FailedToastDuration => failedToastDuration;
    }
    #endregion
    
    #region UI
    [Serializable]
    public class UIScaleSettings
    {
        [Tooltip("Base pixel resolution of all UI art. Panels scale by the largest whole number that fits the screen.")]
        [SerializeField] private Vector2Int referenceResolution = new(320, 180);

        [Tooltip("Panel Settings that render pixel-art UI. Each must use Scale Mode: Constant Pixel Size.")]
        [SerializeField] private PanelSettings[] pixelPanels = Array.Empty<PanelSettings>();

        public Vector2Int ReferenceResolution => referenceResolution;
        public PanelSettings[] PixelPanels => pixelPanels;
    }
    #endregion
    
    #region Audio
    [Serializable]
    public class AudioMixerSettings
    {
        [Tooltip("MainMixer with Master › Music, SFX, Character groups.")]
        [SerializeField] private AudioMixer mixer;

        [Tooltip("Exposed volume parameter names (must match the mixer exactly).")]
        [SerializeField] private string masterParameter = "MasterVolume";
        [SerializeField] private string musicParameter = "MusicVolume";
        [SerializeField] private string sfxParameter = "SfxVolume";
        [SerializeField] private string characterParameter = "CharacterVolume";

        public AudioMixer Mixer => mixer;

        public string ParameterFor(AudioChannel channel) => channel switch
        {
            AudioChannel.Master => masterParameter,
            AudioChannel.Music => musicParameter,
            AudioChannel.Sfx => sfxParameter,
            AudioChannel.Character => characterParameter,
            _ => masterParameter
        };
    }
    
    // NOTE: Named MusicSettings to avoid clashing with UnityEngine.AudioSettings.
    [Serializable]
    public class MusicSettings
    {
        [Tooltip("Mixer group music plays through (MainMixer › Music).")]
        [SerializeField] private AudioMixerGroup outputGroup;

        [SerializeField, Min(0f)] private float defaultFadeIn = 1.5f;
        [SerializeField, Min(0f)] private float defaultFadeOut = 1f;

        [SerializeField] private MusicTrackEntry[] tracks =
        {
            new(MusicTrackId.MainMenu, 1f)
        };

        public AudioMixerGroup OutputGroup => outputGroup;
        public float DefaultFadeIn => defaultFadeIn;
        public float DefaultFadeOut => defaultFadeOut;

        //Finds the entry for a track id. False when the id isn't listed.
        public bool TryGetTrack(MusicTrackId id, out MusicTrackEntry entry)
        {
            foreach (var candidate in tracks)
            {
                if (candidate.Id == id)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
    
    [Serializable]
    public class UISoundSettings
    {
        [Tooltip("Mixer group UI sounds play through (MainMixer › SFX).")]
        [SerializeField] private AudioMixerGroup outputGroup;

        [SerializeField] private UISoundClip hover = new(1f);
        [SerializeField] private UISoundClip click = new(1f);
        [SerializeField] private UISoundClip panelOpen = new(1f);
        [SerializeField] private UISoundClip panelClose = new(1f);

        [Tooltip("Minimum seconds between hover sounds, so sweeping the mouse doesn't machine-gun them.")]
        [SerializeField, Min(0f)] private float hoverCooldown = 0.04f;

        public AudioMixerGroup OutputGroup => outputGroup;
        public UISoundClip Hover => hover;
        public UISoundClip Click => click;
        public UISoundClip PanelOpen => panelOpen;
        public UISoundClip PanelClose => panelClose;
        public float HoverCooldown => hoverCooldown;
    }
    #endregion
    
    #region Bootstrap
    [Serializable]
    public class SplashSettings
    {
        [Tooltip("Match this to Player Settings > Splash Image background for a seamless hand-off.")]
        [SerializeField] private Color backgroundColor = Color.black;

        [SerializeField] private SplashSlide[] slides =
        {
            new("Kobold Brigade", 0.5f, 1f, 1.5f, 1f)
        };

        public Color BackgroundColor => backgroundColor;
        public SplashSlide[] Slides => slides;
    }
    #endregion
    
    #region Main Menu
    [Serializable]
    public class TitleScreenSettings
    {
        [SerializeField, Min(0f)] private float promptFadeOutDuration = 0.4f;
        
        public float PromptFadeOutDuration => promptFadeOutDuration;
    }
    
    [Serializable]
    public class CameraPanSettings
    {
        [SerializeField, Min(0f)] private float duration = 2.5f;
        [SerializeField] private Ease ease = Ease.InOutSine;

        public float Duration => duration;
        public Ease Ease => ease;
    }
    
    [Serializable]
    public class ParallaxSettings
    {
        [Tooltip("World-unit shift of a depth-1 (back-most) layer at the screen edge. Layers get this much overhang.")]
        [SerializeField] private Vector2 maxShift = new(0.4f, 0.25f);

        [Tooltip("Higher = snappier follow. Frame-rate independent.")]
        [SerializeField, Min(0.01f)] private float smoothing = 6f;

        [Tooltip("On: layers move opposite the pointer (looking-around feel). Off: layers follow the pointer.")]
        [SerializeField] private bool moveAgainstPointer = true;

        [Tooltip("On: every layer in a group shares the largest required scale (uniform pixel size, more crop on ultrawide).\n" +
                 "Off: only layers that need it scale up further (less crop, slightly larger pixels on those layers).")]
        [SerializeField] private bool uniformLayerScale = true;

        [Tooltip("How much layers follow the camera during pans (× depth). 0 = flat pan; higher = stronger depth.")]
        [SerializeField, Range(0f, 0.9f)] private float panStrength = 0.3f;

        [Tooltip("Character groups: pointer shift relative to the background (0.5 = half as far).")]
        [SerializeField, Range(0f, 1f)] private float characterPointerStrength = 0.5f;

        [Tooltip("Character groups: follow speed (lower than Smoothing = lazier).")]
        [SerializeField, Min(0.01f)] private float characterSmoothing = 3f;
        
        [Tooltip("Pan joints: how many sprite pixels the upper layer's edge sinks into the lower one (hides hairline seams).")]
        [SerializeField, Range(0f, 4f)] private float panJointOverlapPixels = 1f;

        public Vector2 MaxShift => maxShift;
        public float Smoothing => smoothing;
        public bool MoveAgainstPointer => moveAgainstPointer;
        public bool UniformLayerScale => uniformLayerScale;
        public float PanStrength => panStrength;
        public float PanJointOverlapPixels => panJointOverlapPixels;

        public float PointerStrength(ParallaxMotion motion) => motion == ParallaxMotion.Character ? characterPointerStrength : 1f;
        public float SmoothingFor(ParallaxMotion motion) => motion == ParallaxMotion.Character ? characterSmoothing : smoothing;
    }

    [Serializable]
    public class PupilFollowSettings
    {
        [Tooltip("Furthest a pupil moves from its rest centre, in sprite pixels (x = sideways, y = up/down).")]
        [SerializeField] private Vector2 maxOffsetPixels = new(2f, 1f);

        [Tooltip("Cursor distance (sprite pixels) from an eye's centre at which its pupil reaches the max offset. " +
                 "Keep at or below half the distance between the pupils, so a cursor between the eyes crosses them inward.")]
        [SerializeField, Min(1f)] private float reachPixels = 8f;

        [Tooltip("Higher = snappier eye movement. Frame-rate independent.")]
        [SerializeField, Min(0.01f)] private float smoothing = 12f;

        public Vector2 MaxOffsetPixels => maxOffsetPixels;
        public float ReachPixels => reachPixels;
        public float Smoothing => smoothing;
    }
    
    [Serializable]
    public class TitleCharacterReactionSettings
    {
        [Tooltip("Physics layers the character's click shapes are on.")]
        [SerializeField] private LayerMask clickLayers = ~0;

        [Tooltip("How far a clicked eyebrow rises, in sprite pixels (one pixel per step).")]
        [SerializeField, Range(1, 8)] private int eyebrowRaisePixels = 2;
        [SerializeField, Min(0.01f)] private float eyebrowStepDuration = 0.04f;
        [Tooltip("Time the eyebrow stays fully raised.")]
        [SerializeField, Min(0f)] private float eyebrowHoldDuration = 0.35f;

        [Tooltip("Moustache twitch: whole-pixel offsets shown in order, each for its duration; it returns to rest afterwards.")]
        [SerializeField] private PixelStep[] moustacheTwitch =
        {
            new(new Vector2Int(0, 1), 0.05f),
            new(new Vector2Int(0, 0), 0.05f),
            new(new Vector2Int(1, 0), 0.05f),
            new(new Vector2Int(-1, 0), 0.05f),
            new(new Vector2Int(0, 1), 0.05f)
        };

        public LayerMask ClickLayers => clickLayers;
        public int EyebrowRaisePixels => eyebrowRaisePixels;
        public float EyebrowStepDuration => eyebrowStepDuration;
        public float EyebrowHoldDuration => eyebrowHoldDuration;
        public System.Collections.Generic.IReadOnlyList<PixelStep> MoustacheTwitch =>
            moustacheTwitch ?? Array.Empty<PixelStep>();
    }
    
    [Serializable]
    public class MenuButtonSettings
    {
        [Tooltip("Scale while selected (hover or keyboard focus). 1.08 = 8% larger.")]
        [SerializeField, Min(1f)] private float popScale = 1.08f;
        [SerializeField, Min(0f)] private float popDuration = 0.12f;
        [SerializeField] private Ease popEase = Ease.OutBack;

        [Tooltip("Return-to-normal timing when the button loses selection.")]
        [SerializeField, Min(0f)] private float settleDuration = 0.1f;
        [SerializeField] private Ease settleEase = Ease.OutQuad;

        public float PopScale => popScale;
        public float PopDuration => popDuration;
        public Ease PopEase => popEase;
        public float SettleDuration => settleDuration;
        public Ease SettleEase => settleEase;
    }
    
    [Serializable]
    public class MenuEntranceSettings
    {
        [SerializeField, Min(0f)] private float logoDuration = 0.6f;

        [Tooltip("UI pixels the logo settles down from.")]
        [SerializeField, Min(0f)] private float logoDrop = 6f;
        [SerializeField] private Ease logoEase = Ease.OutCubic;

        [Tooltip("Pause after the logo before the first column item starts.")]
        [SerializeField, Min(0f)] private float delayBeforeItems = 0.1f;

        [Tooltip("UI pixels each column item slides in from (from the left).")]
        [SerializeField, Min(0f)] private float itemSlide = 24f;
        [SerializeField, Min(0f)] private float itemDuration = 0.35f;
        [SerializeField, Min(0f)] private float itemStagger = 0.06f;
        [SerializeField] private Ease itemEase = Ease.OutCubic;

        public float LogoDuration => logoDuration;
        public float LogoDrop => logoDrop;
        public Ease LogoEase => logoEase;
        public float DelayBeforeItems => delayBeforeItems;
        public float ItemSlide => itemSlide;
        public float ItemDuration => itemDuration;
        public float ItemStagger => itemStagger;
        public Ease ItemEase => itemEase;
    }
    
    [Serializable]
    public class PanelTransitionSettings
    {
        [SerializeField, Min(0f)] private float openDuration = 0.25f;
        [SerializeField, Min(0f)] private float closeDuration = 0.18f;

        [Tooltip("UI pixels the panel window rises from while opening (and sinks by while closing).")]
        [SerializeField, Min(0f)] private float windowSlide = 8f;
        [SerializeField] private Ease openEase = Ease.OutCubic;
        [SerializeField] private Ease closeEase = Ease.InCubic;

        [Tooltip("How long the menu column and logo take to fade away when a menu-hiding panel opens.")]
        [SerializeField, Min(0f)] private float menuHideDuration = 0.2f;
        [SerializeField] private Ease menuHideEase = Ease.InCubic;

        [Tooltip("Speed of the menu's return animation after a panel closes (1 = the original entrance speed).")]
        [SerializeField, Min(0.1f)] private float menuReturnSpeed = 2f;

        public float OpenDuration => openDuration;
        public float CloseDuration => closeDuration;
        public float WindowSlide => windowSlide;
        public Ease OpenEase => openEase;
        public Ease CloseEase => closeEase;
        public float MenuHideDuration => menuHideDuration;
        public Ease MenuHideEase => menuHideEase;
        public float MenuReturnSpeed => menuReturnSpeed;
    }

    [Serializable]
    public class CreditsSettings
    {
        [Tooltip("UI pixels scrolled per Up/Down press.")]
        [SerializeField, Min(1f)] private float keyScrollStep = 12f;

        [Tooltip("UI pixels scrolled per mouse-wheel notch.")]
        [SerializeField, Min(1f)] private float wheelScrollStep = 12f;

        public float KeyScrollStep => keyScrollStep;
        public float WheelScrollStep => wheelScrollStep;
    }
    #endregion
    
    #region Intro
    [Serializable]
    public class IntroSettings
    {
        [Tooltip("Narration typing speed.")]
        [SerializeField, Min(1f)] private float charactersPerSecond = 40f;

        [SerializeField, Min(0f)] private float slideCrossfadeDuration = 0.6f;

        [Tooltip("Fade of the whole cutscene to black once it ends or is skipped.")]
        [SerializeField, Min(0f)] private float endFadeDuration = 0.6f;

        [Tooltip("Seconds the skip key must be held to skip.")]
        [SerializeField, Min(0.1f)] private float skipHoldDuration = 1f;

        [Tooltip("How much faster the skip bar drains than it fills after releasing.")]
        [SerializeField, Min(1f)] private float skipDrainMultiplier = 2f;

        [Tooltip("On/off time of the blinking continue arrow.")]
        [SerializeField, Min(0.05f)] private float promptBlinkInterval = 0.5f;

        [Tooltip("UI pixels the narration box scrolls per mouse-wheel notch.")]
        [SerializeField, Min(1f)] private float textScrollStep = 10f;
        
        [Tooltip("Fade-in of the guild-naming panel after the cutscene.")]
        [SerializeField, Min(0f)] private float namingFadeDuration = 0.4f;

        public float CharactersPerSecond => charactersPerSecond;
        public float SlideCrossfadeDuration => slideCrossfadeDuration;
        public float EndFadeDuration => endFadeDuration;
        public float SkipHoldDuration => skipHoldDuration;
        public float SkipDrainMultiplier => skipDrainMultiplier;
        public float PromptBlinkInterval => promptBlinkInterval;
        public float TextScrollStep => textScrollStep;
        public float NamingFadeDuration => namingFadeDuration;
    }
    #endregion
    
    #region Guild
    [Serializable]
    public class NewGuildSettings
    {
        [SerializeField, Range(1, 40)] private int nameMinLength = 1;
        [SerializeField, Range(1, 40)] private int nameMaxLength = 20;

        [Tooltip("Rank, in-game date and gold a new guild starts with.")]
        [SerializeField] private GuildRank startingRank = GuildRank.F;
        [SerializeField] private GameDateStamp startingDate = new(1, 1, 1);
        [SerializeField, Min(0)] private long startingGold = 500;

        public int NameMinLength => Mathf.Clamp(nameMinLength, 1, NameMaxLength);
        public int NameMaxLength => Mathf.Max(1, nameMaxLength);
        public GuildRank StartingRank => startingRank;
        public GameDateStamp StartingDate => startingDate;
        public long StartingGold => startingGold;
    }

    [Serializable]
    public class GuildRankSettings
    {
        // NOTE: Built-in fallback, used if the configured list is invalid.
        private static readonly RankDefinition[] Defaults =
        {
            new(GuildRank.F, 0, new Color32(150, 150, 150, 255)),
            new(GuildRank.E, 100, new Color32(96, 184, 88, 255)),
            new(GuildRank.D, 300, new Color32(64, 176, 168, 255)),
            new(GuildRank.C, 700, new Color32(72, 120, 216, 255)),
            new(GuildRank.B, 1500, new Color32(152, 88, 200, 255)),
            new(GuildRank.A, 3100, new Color32(232, 136, 56, 255)),
            new(GuildRank.S, 6300, new Color32(232, 200, 72, 255)),
            new(GuildRank.National, 12700, new Color32(200, 40, 56, 255))
        };

        [Tooltip("One entry per rank, F → National, with cumulative points required (F must be 0, strictly ascending) and its colour.")]
        [SerializeField] private RankDefinition[] ranks = (RankDefinition[])Defaults.Clone();

        private RankDefinition[] _validated;

        // The validated rank table (falls back to the defaults, with a warning, if the configured one is invalid).
        public System.Collections.Generic.IReadOnlyList<RankDefinition> Ranks => _validated ??= Validate();

        public GuildRank Highest => Ranks[^1].Rank;

        public long PointsFor(GuildRank rank) => Ranks[(int)rank].PointsRequired;
        public Color ColorOf(GuildRank rank) => Ranks[(int)rank].Color;

        public bool TryGetNext(GuildRank rank, out GuildRank next)
        {
            var index = (int)rank + 1;
            next = index < Ranks.Count ? Ranks[index].Rank : rank;
            return index < Ranks.Count;
        }

        // NOTE: Entry "i" must be rank "i", F must need 0 points and requirements must strictly rise — the lookup relies on it.
        private RankDefinition[] Validate()
        {
            var count = Enum.GetValues(typeof(GuildRank)).Length;
            var valid = ranks != null && ranks.Length == count && ranks[0].PointsRequired == 0;

            for (var i = 0; valid && i < count; i++)
            {
                valid = (int)ranks[i].Rank == i && (i == 0 || ranks[i].PointsRequired > ranks[i - 1].PointsRequired);
            }

            if (valid)
                return ranks;

            Debug.LogWarning("[GuildRankSettings] The rank table must list every rank F → National in order, F at 0 points, " +
                             "strictly ascending; using the defaults.");
            return Defaults;
        }
    }

    [Serializable]
    public class ReputationSettings
    {
        [SerializeField] private int minimum = -100;
        [SerializeField] private int maximum = 100;
        [SerializeField] private int starting;

        [Tooltip("Each level's lowest value (inclusive), worst → best. The worst level should start at Minimum.")]
        [SerializeField] private ReputationLevelDefinition[] levels =
        {
            new(ReputationLevel.Despised, -100, new Color32(176, 40, 40, 255)),
            new(ReputationLevel.Distrusted, -59, new Color32(224, 128, 56, 255)),
            new(ReputationLevel.Neutral, -19, new Color32(200, 196, 184, 255)),
            new(ReputationLevel.Respected, 20, new Color32(96, 184, 88, 255)),
            new(ReputationLevel.Renowned, 60, new Color32(232, 200, 72, 255))
        };

        public int Minimum => Mathf.Min(minimum, maximum);
        public int Maximum => Mathf.Max(minimum, maximum);
        public int Starting => Mathf.Clamp(starting, Minimum, Maximum);

        // The best level whose minimum the value reaches (the worst level if none).
        public ReputationLevel LevelOf(int value)
        {
            if (levels == null || levels.Length == 0)
                return ReputationLevel.Neutral;

            var best = levels[0];
            foreach (var definition in levels)
            {
                if (value >= definition.MinimumValue && definition.MinimumValue >= best.MinimumValue)
                    best = definition;
            }
            return best.Level;
        }

        public Color ColorOf(ReputationLevel level)
        {
            if (levels != null)
            {
                foreach (var definition in levels)
                {
                    if (definition.Level == level)
                        return definition.Color;
                }
            }
            return Color.white;
        }
    }
    
    [Serializable]
    public class GuildHudSettings
    {
        [Tooltip("Gold number colour for a moment after a gain / a loss.")]
        [SerializeField] private Color goldGainColor = new Color32(96, 184, 88, 255);
        [SerializeField] private Color goldLossColor = new Color32(224, 112, 96, 255);
        [SerializeField, Min(0f)] private float goldFlashDuration = 0.4f;

        [Tooltip("Counter shake on every change (gold and adventurers): whole UI pixels, alternating left/right, one step per interval.")]
        [SerializeField, Range(1, 4)] private int goldShakePixels = 1;
        [SerializeField, Range(1, 12)] private int goldShakeSteps = 6;
        [SerializeField, Min(0.01f)] private float goldShakeStepDuration = 0.04f;

        [Tooltip("Adventurer count colour while the guild is full.")]
        [SerializeField] private Color adventurersFullColor = new Color32(232, 176, 64, 255);

        [Header("Floating numbers")]
        [Tooltip("Colour of a gain / a loss.")]
        [SerializeField] private Color floatGainColor = new Color32(96, 184, 88, 255);
        [SerializeField] private Color floatLossColor = new Color32(224, 112, 96, 255);
        [Tooltip("Life time, and how far it drifts (x = left, y = down) in UI pixels.")]
        [SerializeField, Min(0.1f)] private float floatDuration = 0.9f;
        [SerializeField] private Vector2 floatDrift = new(10f, 8f);
        [Tooltip("Gap between a float and its counter's left edge; rows for changes that arrive together.")]
        [SerializeField, Min(0f)] private float floatGapPixels = 2f;
        [SerializeField, Min(0f)] private float floatStackPixels = 8f;
        [SerializeField, Min(0f)] private float floatStackWindow = 0.35f;
        [SerializeField, Range(1, 5)] private int floatStackRows = 3;
        [SerializeField, Range(2, 16)] private int floatPoolSize = 6;

        [Tooltip("Time for the rank bar to fill to a new value.")]
        [SerializeField, Min(0f)] private float rankFillDuration = 0.4f;

        [Tooltip("Reputation bar: fade in/out time, fill animation time, and how long it stays after a change.")]
        [SerializeField, Min(0f)] private float reputationFadeDuration = 0.2f;
        [SerializeField, Min(0f)] private float reputationFillDuration = 0.3f;
        [SerializeField, Min(0f)] private float reputationShowDuration = 2f;

        [Tooltip("Rank-up banner: on-screen time, fade time, and UI pixels it slides down from.")]
        [SerializeField, Min(0f)] private float bannerDuration = 3f;
        [SerializeField, Min(0f)] private float bannerFadeDuration = 0.3f;
        [SerializeField, Min(0f)] private float bannerSlidePixels = 6f;

        public Color GoldGainColor => goldGainColor;
        public Color GoldLossColor => goldLossColor;
        public float GoldFlashDuration => goldFlashDuration;
        public int GoldShakePixels => goldShakePixels;
        public int GoldShakeSteps => goldShakeSteps;
        public float GoldShakeStepDuration => goldShakeStepDuration;
        public Color AdventurersFullColor => adventurersFullColor;
        public Color FloatGainColor => floatGainColor;
        public Color FloatLossColor => floatLossColor;
        public float FloatDuration => floatDuration;
        public Vector2 FloatDrift => floatDrift;
        public float FloatGapPixels => floatGapPixels;
        public float FloatStackPixels => floatStackPixels;
        public float FloatStackWindow => floatStackWindow;
        public int FloatStackRows => floatStackRows;
        public int FloatPoolSize => floatPoolSize;
        public float RankFillDuration => rankFillDuration;
        public float ReputationFadeDuration => reputationFadeDuration;
        public float ReputationFillDuration => reputationFillDuration;
        public float ReputationShowDuration => reputationShowDuration;
        public float BannerDuration => bannerDuration;
        public float BannerFadeDuration => bannerFadeDuration;
        public float BannerSlidePixels => bannerSlidePixels;
    }
    #endregion
    
    #region Time
    [Serializable]
    public class TimeSettings
    {
        [Tooltip("Months in calendar order; a year has this many months. Each month belongs to one season.")]
        [SerializeField] private MonthDefinition[] months =
        {
            new("Spring", Season.Spring),
            new("Summer", Season.Summer),
            new("Autumn", Season.Autumn),
            new("Winter", Season.Winter)
        };

        [SerializeField, Min(1)] private int daysPerMonth = 28;

        [Tooltip("Real seconds one in-game day takes at Normal speed (1440 = 24 minutes = 1 real second per in-game minute).")]
        [SerializeField, Min(1f)] private float realSecondsPerDay = 1440f;

        [Tooltip("In-game minutes per simulation tick. Speed changes how often ticks happen, never their size.")]
        [SerializeField, Range(1, 60)] private int minutesPerTick = 1;

        [SerializeField, Min(1f)] private float fastMultiplier = 2f;
        [SerializeField, Min(1f)] private float veryFastMultiplier = 4f;

        [Tooltip("At most this many ticks run in one frame; any larger backlog (after a hitch) is dropped.")]
        [SerializeField, Min(1)] private int maxTicksPerFrame = 8;

        [Tooltip("Start hours of each day phase. Must be ascending: Dawn < Day < Dusk < Night.")]
        [SerializeField, Range(0, 23)] private int dawnHour = 5;
        [SerializeField, Range(0, 23)] private int dayHour = 7;
        [SerializeField, Range(0, 23)] private int duskHour = 18;
        [SerializeField, Range(0, 23)] private int nightHour = 20;

        [Tooltip("Hour a new game (and, until the full game save exists, a loaded game) starts at.")]
        [SerializeField, Range(0, 23)] private int startHour = 8;

        public int MonthsPerYear => Mathf.Max(1, months?.Length ?? 0);
        public int DaysPerMonth => daysPerMonth;
        public int DaysPerYear => MonthsPerYear * daysPerMonth;
        public float RealSecondsPerDay => realSecondsPerDay;
        public int MinutesPerTick => minutesPerTick;
        public int MaxTicksPerFrame => maxTicksPerFrame;
        public int DawnHour => dawnHour;
        public int DayHour => dayHour;
        public int DuskHour => duskHour;
        public int NightHour => nightHour;
        public int StartHour => startHour;
        public bool HasValidPhaseHours => dawnHour < dayHour && dayHour < duskHour && duskHour < nightHour;

        // 1-based month → its name ("M5" when the calendar has no such month).
        public string MonthName(int month) =>
            months != null && month >= 1 && month <= months.Length && !string.IsNullOrEmpty(months[month - 1].Name)
                ? months[month - 1].Name
                : $"M{month}";

        // 1-based month → its season (clamped into the calendar).
        public Season SeasonOf(int month) =>
            months == null || months.Length == 0 ? Season.Spring : months[Mathf.Clamp(month, 1, months.Length) - 1].Season;

        public float Multiplier(GameSpeed speed) => speed switch
        {
            GameSpeed.Normal => 1f,
            GameSpeed.Fast => fastMultiplier,
            GameSpeed.VeryFast => veryFastMultiplier,
            _ => 0f
        };
    }
    #endregion
    
    #region Quests
    [Serializable]
    public class RequestSettings
    {
        [Tooltip("The imported RequestDatabase (Database › Load builds it at Assets/_Project/Data/Databases).")]
        [SerializeField] private RequestDatabase database;

        [Tooltip("Most open requests on the board at once.")]
        [SerializeField, Range(1, 30)] private int boardCapacity = 10;

        [Tooltip("New requests each dawn, by reputation level: Despised, Distrusted, Neutral, Respected, Renowned.")]
        [SerializeField] private int[] dawnBatchByReputation = { 1, 2, 2, 3, 3 };

        [Tooltip("Requests up to this many ranks above the guild's rank can appear.")]
        [SerializeField, Range(0, 3)] private int rankAllowance = 1;

        [Tooltip("Sources that only offer requests at or above the minimum reputation level below.")]
        [SerializeField] private RequestSource[] gatedSources = { RequestSource.Kingdom, RequestSource.Nobility };
        [SerializeField] private ReputationLevel gatedSourceMinimumLevel = ReputationLevel.Respected;

        [Tooltip("Reputation change when a request expires unclaimed / when the player dismisses one.")]
        [SerializeField] private int expiryReputation = -2;
        [SerializeField] private int dismissReputation = -1;

        public RequestDatabase Database => database;
        public int BoardCapacity => boardCapacity;
        public int RankAllowance => rankAllowance;
        public int ExpiryReputation => expiryReputation;
        public int DismissReputation => dismissReputation;

        public int BatchSize(ReputationLevel level)
        {
            if (dawnBatchByReputation == null || dawnBatchByReputation.Length == 0)
                return 1;
            return Mathf.Max(0, dawnBatchByReputation[Mathf.Clamp((int)level, 0, dawnBatchByReputation.Length - 1)]);
        }

        public bool IsSourceAllowed(RequestSource source, ReputationLevel level)
        {
            if (gatedSources == null || level >= gatedSourceMinimumLevel)
                return true;
            return Array.IndexOf(gatedSources, source) < 0;
        }
    }
    
    [Serializable]
    public class ApplicationSettings
    {
        [Tooltip("In-game hours a declined adventurer must wait before applying to the same quest again (other quests: next sign-up).")]
        [SerializeField, Range(0, 168)] private int reapplyCooldownHours = 24;

        public long ReapplyCooldownMinutes => (long)reapplyCooldownHours * GameCalendar.MinutesPerHour;
    }
    
    [Serializable]
    public class QuestSettings
    {
        [Header("Quest board")]
        [Tooltip("Slots on the quest board, how many are open at rank F, and how many each rank above F opens.")]
        [SerializeField, Range(1, 20)] private int boardSlots = 10;
        [SerializeField, Range(1, 20)] private int baseOpenSlots = 3;
        [SerializeField, Range(0, 5)] private int slotsPerRank = 1;

        [Header("Posting terms")]
        [Tooltip("Reward share offered to the party (% of the request's gold): min, max, step, default. The guild keeps the rest.")]
        [SerializeField, Range(0, 100)] private int minRewardShare = 50;
        [SerializeField, Range(0, 100)] private int maxRewardShare = 100;
        [SerializeField, Range(1, 50)] private int rewardShareStep = 10;
        [SerializeField, Range(0, 100)] private int defaultRewardShare = 70;

        [Tooltip("The quest's minimum adventurer rank can be set this many ranks below / above the request's rank.")]
        [SerializeField, Range(0, 7)] private int ranksBelowRequest = 1;
        [SerializeField, Range(0, 7)] private int ranksAboveRequest = 1;

        [Tooltip("Party size limits: lower-rank quests, and quests from this rank up. 1 allows solo quests.")]
        [SerializeField] private Vector2Int lowerRankPartySize = new(1, 5);
        [SerializeField] private Vector2Int higherRankPartySize = new(1, 7);
        [SerializeField] private GuildRank higherRankFrom = GuildRank.B;

        [Header("Sign-up")]
        [Tooltip("Gold per member an adventurer expects, per level.")]
        [SerializeField, Min(1f)] private float expectedGoldPerLevel = 10f;
        [Tooltip("Affinity for the category makes an offer look this much bigger (1.25 = accepts 20% less).")]
        [SerializeField, Min(1f)] private float affinityAcceptFactor = 1.25f;

        [Header("Duration")]
        [Tooltip("Base days by category: Combat, Gathering, Escort, Delivery, Subjugation, Exploration, Investigation, Dungeon.")]
        [SerializeField] private int[] baseDaysByCategory = { 2, 1, 3, 1, 2, 3, 2, 4 };
        [Tooltip("One extra day per this many ranks above F.")]
        [SerializeField, Min(1)] private int ranksPerExtraDay = 2;

        [Header("Resolution")]
        [Tooltip("Difficulty per recommended member, by request rank F → National.")]
        [SerializeField] private float[] difficultyPerMemberByRank = { 6f, 10f, 16f, 24f, 34f, 46f, 60f, 80f };
        [Tooltip("A member with affinity for the category counts this much more (0.2 = +20%).")]
        [SerializeField, Min(0f)] private float affinityPowerBonus = 0.2f;
        [Tooltip("Success chance when party power equals difficulty, and how much it moves per 1.0 of power ÷ difficulty.")]
        [SerializeField, Range(0f, 1f)] private float chanceAtEven = 0.7f;
        [SerializeField, Min(0f)] private float chancePerRatio = 1f;
        [SerializeField, Range(0f, 1f)] private float minChance = 0.05f;
        [SerializeField, Range(0f, 1f)] private float maxChance = 0.95f;
        [SerializeField] private StatWeights[] statWeights =
        {
            new(QuestCategory.Combat, 0.5f, 0.2f, 0f, 0.3f),
            new(QuestCategory.Gathering, 0f, 0.4f, 0.4f, 0.2f),
            new(QuestCategory.Escort, 0.3f, 0.3f, 0f, 0.4f),
            new(QuestCategory.Delivery, 0f, 0.6f, 0f, 0.4f),
            new(QuestCategory.Subjugation, 0.4f, 0f, 0.2f, 0.4f),
            new(QuestCategory.Exploration, 0f, 0.4f, 0.3f, 0.3f),
            new(QuestCategory.Investigation, 0f, 0.3f, 0.7f, 0f),
            new(QuestCategory.Dungeon, 0.3f, 0.2f, 0.2f, 0.3f)
        };

        [Header("Outcome")]
        [Tooltip("XP per member and guild rank points on success, by quest rank F → National.")]
        [SerializeField] private long[] experienceByRank = { 150, 300, 600, 1200, 2400, 4800, 9600, 19200 };
        [SerializeField] private long[] rankPointsByRank = { 10, 20, 40, 80, 160, 320, 640, 1280 };
        [Tooltip("Reputation on success / failure. (Expiry and cancel use GameConfig › Requests: −2 / −1.)")]
        [SerializeField] private int successReputation = 2;
        [SerializeField] private int failureReputation = -3;

        public int BoardSlots => boardSlots;
        public int MinRewardShare => Mathf.Min(minRewardShare, maxRewardShare);
        public int MaxRewardShare => Mathf.Max(minRewardShare, maxRewardShare);
        public int RewardShareStep => rewardShareStep;
        public int DefaultRewardShare => SnapShare(defaultRewardShare);
        public float ExpectedGoldPerLevel => expectedGoldPerLevel;
        public float AffinityAcceptFactor => affinityAcceptFactor;
        public int RanksPerExtraDay => ranksPerExtraDay;
        public float AffinityPowerBonus => affinityPowerBonus;
        public float ChanceAtEven => chanceAtEven;
        public float ChancePerRatio => chancePerRatio;
        public float MinChance => Mathf.Min(minChance, maxChance);
        public float MaxChance => Mathf.Max(minChance, maxChance);
        public int SuccessReputation => successReputation;
        public int FailureReputation => failureReputation;

        // Open board slots at this guild rank: base + per rank above F, at most BoardSlots.
        public int OpenSlots(GuildRank guildRank) =>
            Mathf.Clamp(baseOpenSlots + (int)guildRank * slotsPerRank, 1, Mathf.Max(1, boardSlots));

        // The lowest guild rank at which this slot (0-based) is open. False if no rank opens it.
        public bool TryGetUnlockRank(int slotIndex, out GuildRank rank)
        {
            for (rank = GuildRank.F; rank <= GuildRank.National; rank++)
            {
                if (OpenSlots(rank) > slotIndex)
                    return true;
            }
            rank = GuildRank.National;
            return false;
        }

        // The share clamped to the range and snapped to the step.
        public int SnapShare(int percent)
        {
            var step = Mathf.Max(1, rewardShareStep);
            var snapped = MinRewardShare + Mathf.RoundToInt((percent - MinRewardShare) / (float)step) * step;
            return Mathf.Clamp(snapped, MinRewardShare, MaxRewardShare);
        }

        // Allowed minimum ranks for a request's quest: from ranksBelowRequest below to ranksAboveRequest above
        // the request's rank, clamped to F…National.
        public (GuildRank lowest, GuildRank highest) MinRankLimits(GuildRank requestRank)
        {
            const int top = (int)GuildRank.National;
            var lowest = Mathf.Clamp((int)requestRank - ranksBelowRequest, 0, top);
            var highest = Mathf.Clamp((int)requestRank + ranksAboveRequest, lowest, top);
            return ((GuildRank)lowest, (GuildRank)highest);
        }

        // Allowed party sizes (x = min, y = max) for a quest of this rank.
        public Vector2Int PartySizeLimits(GuildRank questRank)
        {
            var limits = questRank >= higherRankFrom ? higherRankPartySize : lowerRankPartySize;
            return new Vector2Int(Mathf.Max(1, limits.x), Mathf.Max(Mathf.Max(1, limits.x), limits.y));
        }

        public int BaseDays(QuestCategory category) => Mathf.Max(1, ByIndex(baseDaysByCategory, (int)category, 2));
        public float DifficultyPerMember(GuildRank rank) => ByIndex(difficultyPerMemberByRank, (int)rank, 10f);
        public long Experience(GuildRank rank) => ByIndex(experienceByRank, (int)rank, 150L);
        public long RankPoints(GuildRank rank) => ByIndex(rankPointsByRank, (int)rank, 10L);

        public StatWeights Weights(QuestCategory category)
        {
            if (statWeights != null)
            {
                foreach (var weights in statWeights)
                {
                    if (weights.Category == category)
                        return weights;
                }
            }
            return new StatWeights(category, 0.25f, 0.25f, 0.25f, 0.25f);
        }

        private static T ByIndex<T>(T[] values, int index, T fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Clamp(index, 0, values.Length - 1)];
    }
    
    [Serializable]
    public class QuestClipboardSettings
    {
        [Tooltip("Slide up/down time and distance (UI pixels it starts below its resting place).")]
        [SerializeField, Min(0f)] private float slideDuration = 0.3f;
        [SerializeField, Min(0f)] private float slideDistance = 180f;

        [Tooltip("POSTED stamp: land time, starting scale, and how long it stays before the clipboard slides away.")]
        [SerializeField, Min(0f)] private float stampDuration = 0.15f;
        [SerializeField, Min(1f)] private float stampStartScale = 1.6f;
        [SerializeField, Min(0f)] private float stampHold = 0.45f;

        public float SlideDuration => slideDuration;
        public float SlideDistance => slideDistance;
        public float StampDuration => stampDuration;
        public float StampStartScale => stampStartScale;
        public float StampHold => stampHold;
    }
    
    [Serializable]
    public class QuestPanelSettings
    {
        [Tooltip("12×12 icon per quest category, shown on pinned notes (2× size). Missing icons show the category's first two letters.")]
        [SerializeField] private QuestCategoryIcons categoryIcons;

        [Tooltip("Lifting a note: time, and the scale it grows from.")]
        [SerializeField, Min(0f)] private float liftDuration = 0.15f;
        [SerializeField, Range(0.3f, 1f)] private float liftStartScale = 0.7f;

        [Tooltip("How long a quest-result toast stays fully visible (fades use GameConfig › Save Game › Toast Fade Duration).")]
        [SerializeField, Min(0.5f)] private float resultToastHold = 2.5f;

        public QuestCategoryIcons CategoryIcons => categoryIcons;
        public float LiftDuration => liftDuration;
        public float LiftStartScale => liftStartScale;
        public float ResultToastHold => resultToastHold;
    }
    #endregion
    
    #region Adventurers
    // Cumulative XP needed for each level (index 0 = level 1 = 0 XP). Default: the D&D 5e table, cap 20.
    [Serializable]
    public class ExperienceTable
    {
        private static readonly long[] Defaults =
        {
            0, 300, 900, 2700, 6500, 14000, 23000, 34000, 48000, 64000,
            85000, 100000, 120000, 140000, 165000, 195000, 225000, 265000, 305000, 355000
        };

        [Tooltip("Total XP to reach each level, starting at level 1 (must be 0) and strictly ascending. Its length is the level cap.")]
        [SerializeField] private long[] thresholds = (long[])Defaults.Clone();

        private long[] _validated;

        private long[] Table => _validated ??= Validate();

        public int MaxLevel => Table.Length;

        // Total XP at which the level starts (clamped to 1…MaxLevel).
        public long ThresholdFor(int level) => Table[Mathf.Clamp(level, 1, MaxLevel) - 1];

        // The highest level whose threshold the XP reaches.
        public int LevelFor(long experience)
        {
            var table = Table;
            var level = 1;
            while (level < table.Length && experience >= table[level])
                level++;
            return level;
        }

        // Progress from this level's threshold to the next (0–1); 1 at the cap.
        public float Progress(long experience)
        {
            var level = LevelFor(experience);
            if (level >= MaxLevel)
                return 1f;

            var from = ThresholdFor(level);
            var to = ThresholdFor(level + 1);
            return Mathf.Clamp01((float)(experience - from) / (to - from));
        }

        private long[] Validate()
        {
            var valid = thresholds is { Length: >= 1 } && thresholds[0] == 0;
            for (var i = 1; valid && i < thresholds.Length; i++)
                valid = thresholds[i] > thresholds[i - 1];

            if (valid)
                return thresholds;

            Debug.LogWarning("[ExperienceTable] Thresholds must start at 0 and strictly rise; using the D&D defaults.");
            return Defaults;
        }
    }

    [Serializable]
    public class AdventurerSettings
    {
        [Tooltip("Imported databases (Database › Load builds them at Assets/_Project/Data/Databases).")]
        [SerializeField] private ClassDatabase classDatabase;
        [SerializeField] private SpeciesDatabase speciesDatabase;

        [Tooltip("First names and epithets for new adventurers.")]
        [SerializeField] private AdventurerNameList nameList;
        [Tooltip("Chance a new adventurer also gets an epithet (\"Bran the Bold\").")]
        [SerializeField, Range(0f, 1f)] private float epithetChance = 0.25f;

        [Tooltip("Most adventurers the guild can hold (Bedrooms will raise it later).")]
        [SerializeField, Range(1, 50)] private int capacity = 6;

        [Tooltip("Adventurers a new guild starts with.")]
        [SerializeField, Range(0, 6)] private int startingAdventurers = 2;

        [Tooltip("Chance of one arrival each dawn, by reputation level: Despised, Distrusted, Neutral, Respected, Renowned.")]
        [SerializeField] private float[] arrivalChanceByReputation = { 0.15f, 0.30f, 0.50f, 0.70f, 0.90f };

        [Tooltip("Highest level an arrival can have, by reputation level (arrivals are level 1 to this).")]
        [SerializeField] private int[] maxArrivalLevelByReputation = { 1, 2, 3, 5, 8 };

        [Tooltip("Starting wealth = a random amount in this range × the adventurer's level.")]
        [SerializeField] private Vector2Int wealthPerLevel = new(20, 60);

        [SerializeField] private ExperienceTable experience = new();

        public ClassDatabase ClassDatabase => classDatabase;
        public SpeciesDatabase SpeciesDatabase => speciesDatabase;
        public AdventurerNameList NameList => nameList;
        public float EpithetChance => epithetChance;
        public int Capacity => capacity;
        public int StartingAdventurers => startingAdventurers;
        public Vector2Int WealthPerLevel => new(Mathf.Max(0, wealthPerLevel.x), Mathf.Max(wealthPerLevel.x, wealthPerLevel.y));
        public ExperienceTable Experience => experience;

        public float ArrivalChance(ReputationLevel level) => ByLevel(arrivalChanceByReputation, level, 0.5f);

        public int MaxArrivalLevel(ReputationLevel level) =>
            Mathf.Clamp(ByLevel(maxArrivalLevelByReputation, level, 1), 1, experience.MaxLevel);

        private static T ByLevel<T>(T[] values, ReputationLevel level, T fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Clamp((int)level, 0, values.Length - 1)];
    }
    
    [Serializable]
    public class PromotionSettings
    {
        [Header("Rank progress")]
        [Tooltip("Progress needed before an adventurer can take the exam for the next rank.")]
        [SerializeField, Min(0.5f)] private float progressRequired = 5f;
        [Tooltip("Progress per successful quest: quest one or more ranks above the adventurer's, same rank, one rank below. Two or more below gives 0.")]
        [SerializeField, Min(0f)] private float aboveRankProgress = 2f;
        [SerializeField, Min(0f)] private float sameRankProgress = 1f;
        [SerializeField, Min(0f)] private float oneBelowProgress = 0.5f;

        [Header("Eligibility")]
        [Tooltip("Minimum level to take the exam for each rank, F → National (F is never a target).")]
        [SerializeField] private int[] minLevelByRank = { 1, 3, 5, 8, 11, 14, 17, 20 };

        [Header("Exam")]
        [Tooltip("Fee the guild pays for an exam, by target rank F → National.")]
        [SerializeField] private long[] feeByRank = { 0, 50, 100, 200, 400, 800, 1600, 3200 };
        [Tooltip("In-game days the adventurer is away.")]
        [SerializeField, Range(1, 10)] private int examDays = 2;
        [Tooltip("Reputation when an adventurer passes (an adventurer achievement).")]
        [SerializeField] private int passReputation = 1;

        public float ProgressRequired => progressRequired;
        public int ExamDays => examDays;
        public int PassReputation => passReputation;

        // Progress a successful quest of this rank gives an adventurer of that rank.
        public float ProgressFor(GuildRank questRank, GuildRank adventurerRank)
        {
            var difference = (int)questRank - (int)adventurerRank;
            return difference switch
            {
                >= 1 => aboveRankProgress,
                0 => sameRankProgress,
                _ => difference == -1 ? oneBelowProgress : 0f
            };
        }

        public int MinLevel(GuildRank target) => Mathf.Max(1, ByIndex(minLevelByRank, (int)target, 1));
        public long Fee(GuildRank target) => Math.Max(0L, ByIndex(feeByRank, (int)target, 0L));

        private static T ByIndex<T>(T[] values, int index, T fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Clamp(index, 0, values.Length - 1)];
    }
    #endregion
    
    #region World
    [Serializable]
    public class WorldViewSettings
    {
        [Tooltip("Render-texture size the world is drawn at (point-upscaled with the UI). 320×180 with 16 px/unit textures " +
                 "shows ~20 units across at 1 texel ≈ 1 pixel at the far zoom.")]
        [SerializeField] private Vector2Int renderSize = new(320, 180);

        public Vector2Int RenderSize => new(Mathf.Max(16, renderSize.x), Mathf.Max(16, renderSize.y));
    }

    [Serializable]
    public class GuildCameraSettings
    {
        [Tooltip("Vertical field of view (degrees).")]
        [SerializeField, Range(10f, 60f)] private float fieldOfView = 30f;

        [Tooltip("Distance from the focus point: zoomed out (far) and zoomed in (near). With 320×180 and 16 px/unit, ~21 units " +
                 "is 1 texel ≈ 1 pixel; farther than that, texels get smaller than a pixel (raise Render Size if it shimmers).")]
        [SerializeField, Min(1f)] private float farDistance = 30f;
        [SerializeField, Min(1f)] private float nearDistance = 21f;

        [Tooltip("Downward tilt (degrees): zoomed out and zoomed in (a few degrees flatter when close).")]
        [SerializeField, Range(10f, 89f)] private float farPitch = 55f;
        [SerializeField, Range(10f, 89f)] private float nearPitch = 45f;

        [Tooltip("Starting zoom (0 = far, 1 = near) and yaw (degrees). 45° = isometric; rotation steps keep the 45° offset.")]
        [SerializeField, Range(0f, 1f)] private float startZoom;
        [SerializeField] private float startYaw = 45f;

        [Tooltip("Zoom per wheel notch (0–1 range) and smoothing time.")]
        [SerializeField, Range(0.05f, 1f)] private float zoomStep = 0.2f;
        [SerializeField, Min(0f)] private float zoomSmoothTime = 0.12f;

        [Tooltip("Degrees per Q/E press and smoothing time.")]
        [SerializeField] private float rotationStep = 90f;
        [SerializeField, Min(0f)] private float rotationSmoothTime = 0.15f;

        [Tooltip("Keyboard pan speed (units/second at the far zoom; slower when zoomed in) and smoothing time.")]
        [SerializeField, Min(0f)] private float panSpeed = 12f;
        [SerializeField, Min(0f)] private float panSmoothTime = 0.08f;

        [Tooltip("How far inside the bounds box the focus point must stay.")]
        [SerializeField, Min(0f)] private float boundsPadding = 1f;

        [Tooltip("Snap the camera to the render-pixel grid (at the focus distance) so textures don't crawl while panning.")]
        [SerializeField] private bool pixelSnap = true;

        public float FieldOfView => fieldOfView;
        public float FarDistance => Mathf.Max(farDistance, nearDistance);
        public float NearDistance => Mathf.Min(farDistance, nearDistance);
        public float FarPitch => farPitch;
        public float NearPitch => nearPitch;
        public float StartZoom => startZoom;
        public float StartYaw => startYaw;
        public float ZoomStep => zoomStep;
        public float ZoomSmoothTime => zoomSmoothTime;
        public float RotationStep => rotationStep;
        public float RotationSmoothTime => rotationSmoothTime;
        public float PanSpeed => panSpeed;
        public float PanSmoothTime => panSmoothTime;
        public float BoundsPadding => boundsPadding;
        public bool PixelSnap => pixelSnap;
    }
    
    [Serializable]
    public class InteractionSettings
    {
        [Tooltip("Physics layer the interactables' colliders are put on (hover rays only hit this layer).")]
        [SerializeField, Range(0, 31)] private int interactableLayer = 8;

        [Tooltip("Rendering-layer bit switched on while hovered. MUST match the PixelOutline renderer feature's bit.")]
        [SerializeField, Range(0, 31)] private int outlineRenderingLayer = 7;

        [Tooltip("Longest hover ray (units).")]
        [SerializeField, Min(1f)] private float maxRayDistance = 200f;

        [Tooltip("Gap between the object's top and its name label (render pixels).")]
        [SerializeField, Min(0f)] private float labelGap = 2f;

        public int InteractableLayer => interactableLayer;
        public int InteractableMask => 1 << interactableLayer;
        public uint OutlineBit => 1u << outlineRenderingLayer;
        public float MaxRayDistance => maxRayDistance;
        public float LabelGap => labelGap;
    }
    #endregion
    
    #region Facilities
    [Serializable]
    public class FacilitySettings
    {
        [Tooltip("The imported FacilityDatabase (Database › Load builds it).")]
        [SerializeField] private FacilityDatabase database;

        [Tooltip("Sheet ID of the Guild Hall — placed for free, already complete, on a new guild; can't be demolished.")]
        [SerializeField] private string guildHallId = "fac_guild_hall";
        [Tooltip("The Guild Hall's origin cell and rotation (0–3). Default centres a 20×28 hall on the world origin.")]
        [SerializeField] private Vector2Int guildHallOrigin = new(-10, -14);
        [SerializeField, Range(0, 3)] private int guildHallRotation;

        [Tooltip("Cells rooms may occupy (x, y = min corner; width, height).")]
        [SerializeField] private RectInt buildArea = new(-60, -60, 120, 120);

        [Tooltip("Share of a room's total cost returned when it's demolished.")]
        [SerializeField, Range(0f, 1f)] private float demolishRefund = 0.5f;

        [Tooltip("Reputation when a new room finishes construction (not for upgrades).")]
        [SerializeField] private int builtReputation = 1;

        [Tooltip("Shown over a footprint while it's being built or upgraded (scaled to the footprint). Empty = a plain box.")]
        [SerializeField] private GameObject scaffoldPrefab;

        [Tooltip("Extra space (units) around all rooms for the camera's bounds.")]
        [SerializeField, Min(0f)] private float cameraBoundsPadding = 1f;

        public FacilityDatabase Database => database;
        public string GuildHallId => guildHallId;
        public Vector2Int GuildHallOrigin => guildHallOrigin;
        public int GuildHallRotation => guildHallRotation & 3;
        public RectInt BuildArea => buildArea;
        public float DemolishRefund => demolishRefund;
        public int BuiltReputation => builtReputation;
        public GameObject ScaffoldPrefab => scaffoldPrefab;
        public float CameraBoundsPadding => cameraBoundsPadding;
    }
    #endregion
}