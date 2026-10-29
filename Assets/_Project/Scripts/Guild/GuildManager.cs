// Game-scene facade for the guild's core state (gold, reputation, rank): loads it from the save and writes it back.
namespace WanderersGuild
{
    // NOTE: Scene-scoped — untick "Persist Across Scenes". Its Awake runs after SaveGameService (−50) has loaded.
    public class GuildManager : Singleton<GuildManager>, ISaveContributor
    {
        public GuildWallet Wallet { get; private set; }
        public GuildReputation Reputation { get; private set; }
        public GuildRankProgression Rank { get; private set; }

        protected override void OnSingletonAwake()
        {
            var config = GameConfig.Instance;
            var save = SaveGameService.HasInstance ? SaveGameService.Instance : null;
            var loaded = save ? save.LoadedData : null;

            // NOTE: v2+ saves carry the guild state; a new guild or a v1 save starts from the new-guild defaults,
            // keeping the rank from its header.
            if (loaded is { HasGuildState: true })
            {
                Wallet = new GuildWallet(loaded.Gold);
                Reputation = new GuildReputation(config.Reputation, loaded.Reputation);
                Rank = new GuildRankProgression(config.GuildRanks, loaded.Rank, loaded.RankPoints);
            }
            else
            {
                var rank = save ? save.Rank : config.NewGuild.StartingRank;
                Wallet = new GuildWallet(config.NewGuild.StartingGold);
                Reputation = new GuildReputation(config.Reputation, config.Reputation.Starting);
                Rank = new GuildRankProgression(config.GuildRanks, rank, config.GuildRanks.PointsFor(rank));
            }
        }

        private void Start()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Register(this);

            // NOTE: Initial values for the HUD — delta 0 means "set, don't animate".
            Wallet.PublishInitial();
            Reputation.PublishInitial();
            Rank.PublishInitial();
        }

        protected override void OnDestroy()
        {
            if (SaveGameService.HasInstance)
                SaveGameService.Instance.Unregister(this);

            base.OnDestroy();
        }

        public void WriteTo(GameSaveData data)
        {
            data.Gold = Wallet.Gold;
            data.Reputation = Reputation.Value;
            data.RankPoints = Rank.Points;
            data.Rank = Rank.Rank;
        }
    }
}