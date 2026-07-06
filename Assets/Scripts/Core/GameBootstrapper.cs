// Seeds the initial game state: a fixed set of starting requests and prebuilt adventurers.

using UnityEngine;

[DefaultExecutionOrder(-50)]
public class GameBootstrapper : MonoBehaviour
{
    [Header("Guild Hall (pre-placed in scene)")]
    [Tooltip("The Guild Hall already in the scene — its RoomInstance component.")]
    [SerializeField] private RoomInstance sceneGuildHall;
    [SerializeField] private RoomFootprint guildHallFootprint;
    [SerializeField] private TileCoord guildHallOrigin;
    
    [Header("Starting Requests")]
    [SerializeField] private RequestTemplate[] startingRequestTemplates;
    [SerializeField] private int startingRequestCount = 3;
    [SerializeField] private int seed = 12345;
    
    [Header("Starting Adventurers")]
    [SerializeField] private AdventurerClassTemplate archerTemplate;
    [SerializeField] private AdventurerClassTemplate fighterTemplate;
    
    private bool _seeded;
    
    private void Start()
    {
        BuildGuildHall();
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onHourAdvanced.AddListener(HandleFirstHour);
    }

    private void OnDisable()
    {
        if (GameEventsRelay.Exists)
            GameEventsRelay.Instance.onHourAdvanced.RemoveListener(HandleFirstHour);
    }
    
    private void BuildGuildHall()
    {
        if (!guildHallFootprint || !GuildGrid.Exists || !PlacedRoomRegistry.Exists)
            return;
        
        if (GuildGrid.Instance.TryGetOccupant(guildHallOrigin, out _))
            return;

        GuildGrid.Instance.Occupy(guildHallFootprint, guildHallOrigin, FacilityType.GuildHall);
        PlacedRoomRegistry.Instance.Register(FacilityType.GuildHall, guildHallFootprint, guildHallOrigin);
        
        if (sceneGuildHall && FacilityController.Exists)
        {
            var data = FacilityController.Instance.Get(FacilityType.GuildHall)?.Data;
            if (data)
                sceneGuildHall.InitializeAsFinished(FacilityType.GuildHall, data);
        }
        if (FacilityController.Exists)
            FacilityController.Instance.MarkBuilt(FacilityType.GuildHall);
    }
    
    private void HandleFirstHour(int hour)
    {
        if (_seeded)
            return;
        _seeded = true;
        GameEventsRelay.Instance.onHourAdvanced.RemoveListener(HandleFirstHour);

        SeedAdventurers();
        SeedRequests();
    }
    
    private void SeedAdventurers()
    {
        if (!AdventurerRoster.Exists || !IdService.Exists)
            return;

        var rng = new System.Random(seed);
        var factory = new StandardAdventurerFactory(rng, null, GameConfig.Instance.Adventurer.baseExperiencePerLevel);

        if (archerTemplate)
            AdventurerRoster.Instance.Add(factory.
                CreatePrebuilt(IdService.Instance.Next(IdService.Adventurer), archerTemplate, "Jonathan Ashford"));
        if (fighterTemplate)
            AdventurerRoster.Instance.Add(factory.
                CreatePrebuilt(IdService.Instance.Next(IdService.Adventurer), fighterTemplate, "Joshua Blackwood"));
    }
    
    private void SeedRequests()
    {
        if (!RequestBoard.Exists || !IdService.Exists)
            return;
        if (startingRequestTemplates == null || startingRequestTemplates.Length == 0)
            return;

        var rng = new System.Random(seed + 1);
        var config = GameConfig.Instance;
        var factory = new StandardRequestFactory(rng, config.Quest.baseRequestExpirationDays, config.Time.daysPerSeason);
        var now = TimeController.Exists ? TimeController.Instance.CurrentDate : new GameDate(1, Season.Spring, 1);

        for (var i = 0; i < startingRequestCount; i++)
        {
            var template = startingRequestTemplates[i % startingRequestTemplates.Length];
            if (!template)
                continue;
            RequestBoard.Instance.Add(factory.Create(IdService.Instance.Next(IdService.Request), template, now));
        }
    }
}