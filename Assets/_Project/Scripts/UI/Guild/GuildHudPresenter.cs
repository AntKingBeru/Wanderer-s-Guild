// Presenter for the guild part of the HUD (MVP): gold, adventurer count, reputation, rank and floating numbers.
using UnityEngine;
using UnityEngine.UIElements;

namespace WanderersGuild
{
    [RequireComponent(typeof(UIDocument))]
    public class GuildHudPresenter : MonoBehaviour
    {
        [Tooltip("Coin, adventurer and reputation sprites. Empty slots (or no asset) use runtime placeholders.")]
        [SerializeField] private GuildHudArt art;

        private GoldView _gold;
        private AdventurerCountView _adventurers;
        private FloatingNumberView _floats;
        private RankBarView _rank;
        private ReputationMeterView _reputation;
        private RankUpBannerView _banner;
        private int _quietUntilFrame;

        private static GameConfig Config => GameConfig.Instance;

        // NOTE: Changes in the first frames after enabling (scene start: initial publish, starting adventurers) are applied
        // silently — no floats, no shakes.
        private bool CanAnimate => Time.frameCount > _quietUntilFrame;

        // NOTE: Views bind and listeners attach in OnEnable — before GuildManager.Start publishes the initial values.
        private void OnEnable()
        {
            _quietUntilFrame = Time.frameCount + 1;

            var root = GetComponent<UIDocument>().rootVisualElement;
            _gold = root != null ? GoldView.Bind(root, gameObject) : null;
            _adventurers = root != null ? AdventurerCountView.Bind(root, gameObject) : null;
            _floats = root != null ? FloatingNumberView.Bind(root, gameObject, Config.GuildHud.FloatPoolSize) : null;
            _rank = root != null ? RankBarView.Bind(root, gameObject) : null;
            _reputation = root != null ? ReputationMeterView.Bind(root, gameObject) : null;
            _banner = root != null ? RankUpBannerView.Bind(root, gameObject) : null;

            if (_gold == null || _adventurers == null || _floats == null || _rank == null || _reputation == null || _banner == null)
            {
                Debug.LogError("[GuildHudPresenter] Missing one of the guild HUD elements in the UXML.", this);
                return;
            }

            _gold.SetIcon(Icon(art ? art.Coin : null, PlaceholderIcons.Coin));
            _adventurers.SetIcon(Icon(art ? art.Adventurer : null, PlaceholderIcons.Person));

            GameEventsRelay.OnGoldChanged.AddListener(OnGoldChanged);
            GameEventsRelay.OnAdventurerJoined.AddListener(OnAdventurerJoined);
            GameEventsRelay.OnAdventurerLeft.AddListener(OnAdventurerLeft);
            GameEventsRelay.OnReputationChanged.AddListener(OnReputationChanged);
            GameEventsRelay.OnReputationLevelChanged.AddListener(OnReputationLevelChanged);
            GameEventsRelay.OnRankProgressChanged.AddListener(OnRankProgressChanged);
            GameEventsRelay.OnGuildRankUp.AddListener(OnGuildRankUp);
        }

        private void OnDisable()
        {
            GameEventsRelay.OnGoldChanged.RemoveListener(OnGoldChanged);
            GameEventsRelay.OnAdventurerJoined.RemoveListener(OnAdventurerJoined);
            GameEventsRelay.OnAdventurerLeft.RemoveListener(OnAdventurerLeft);
            GameEventsRelay.OnReputationChanged.RemoveListener(OnReputationChanged);
            GameEventsRelay.OnReputationLevelChanged.RemoveListener(OnReputationLevelChanged);
            GameEventsRelay.OnRankProgressChanged.RemoveListener(OnRankProgressChanged);
            GameEventsRelay.OnGuildRankUp.RemoveListener(OnGuildRankUp);
        }

        // NOTE: Covers a HUD enabled after the managers already published; also shows the starting adventurer count.
        private void Start()
        {
            RefreshAdventurers(0);

            if (_rank == null || !GuildManager.HasInstance || _rank.HasRank)
                return;

            var guild = GuildManager.Instance;
            OnGoldChanged(guild.Wallet.Gold, 0);
            OnReputationLevelChanged(guild.Reputation.Level);
            OnReputationChanged(guild.Reputation.Value, 0);
            OnRankProgressChanged(guild.Rank.Rank, guild.Rank.Progress);
        }

        private void OnGoldChanged(long gold, long delta)
        {
            if (_gold == null)
                return;

            var animate = CanAnimate;
            _gold.SetGold(gold, delta, animate, Config.GuildHud);

            if (delta != 0 && animate)
                SpawnFloat(delta > 0 ? $"+{delta:N0}g" : $"-{-delta:N0}g", delta > 0, _gold.Anchor);
        }

        private void OnAdventurerJoined(Adventurer _) => RefreshAdventurers(+1);
        private void OnAdventurerLeft(Adventurer _) => RefreshAdventurers(-1);

        // NOTE: The count always comes from the roster (never kept here), so it can't drift from the truth.
        private void RefreshAdventurers(int delta)
        {
            if (_adventurers == null || !AdventurerManager.HasInstance)
                return;

            var animate = CanAnimate;
            var roster = AdventurerManager.Instance;
            _adventurers.SetCount(roster.Adventurers.Count, roster.Capacity, delta != 0, animate, Config.GuildHud);

            if (delta != 0 && animate)
                SpawnFloat(delta > 0 ? $"+{delta}" : $"{delta}", delta > 0, _adventurers.Anchor);
        }

        private void SpawnFloat(string text, bool isGain, VisualElement anchor)
        {
            var settings = Config.GuildHud;
            _floats?.Spawn(text, isGain ? settings.FloatGainColor : settings.FloatLossColor, anchor, settings);
        }

        private void OnReputationChanged(int value, int delta)
        {
            if (_reputation == null)
                return;

            var settings = Config.Reputation;
            var normalized = Mathf.InverseLerp(settings.Minimum, settings.Maximum, value);

            if (delta == 0)
                _reputation.SetValueImmediate(normalized);
            else
                _reputation.ShowChange(normalized, Config.GuildHud);
        }

        private void OnReputationLevelChanged(ReputationLevel level)
        {
            if (_reputation == null)
                return;

            var sprite = art ? art.ReputationIcon(level) : null;
            _reputation.SetLevel(Icon(sprite, PlaceholderIcons.Face(level)), Config.Reputation.ColorOf(level));
        }

        // NOTE: The first update (scene start) snaps; a new rank switches the badge and fills from 0; otherwise animate.
        private void OnRankProgressChanged(GuildRank rank, float progress)
        {
            if (_rank == null)
                return;

            var isFirst = !_rank.HasRank;
            if (isFirst || rank != _rank.Rank)
                _rank.SetRank(rank, Config.GuildRanks);

            _rank.SetProgress(progress, animate: !isFirst, Config.GuildHud.RankFillDuration);
        }

        private void OnGuildRankUp(GuildRank rank) =>
            _banner?.Enqueue(rank, Config.GuildRanks.ColorOf(rank), Config.GuildHud);

        private static Background Icon(Sprite sprite, Texture2D placeholder) =>
            sprite ? Background.FromSprite(sprite) : Background.FromTexture2D(placeholder);
    }
}