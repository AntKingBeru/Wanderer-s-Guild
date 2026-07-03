// Seeds the initial game state: a fixed set of starting requests and prebuilt adventurers.

using UnityEngine;

[DefaultExecutionOrder(-50)]
public class GameBootstrapper : MonoBehaviour
{
    [Header("Starting Requests")]
    [SerializeField] private RequestTemplate[] startingRequestTemplates;
    [SerializeField] private int startingRequestCount = 3;
    [SerializeField] private int seed = 12345;
    
    [Header("Starting Adventurers")]
    [SerializeField] private AdventurerClassTemplate archerTemplate;
    [SerializeField] private AdventurerClassTemplate fighterTemplate;
    
    private void Start()
    {
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
            AdventurerRoster.Instance.Add(
                factory.CreatePrebuilt(IdService.Instance.Next(IdService.Adventurer), archerTemplate, "Jonathan Ashford"));
        if (fighterTemplate)
            AdventurerRoster.Instance.Add(
                factory.CreatePrebuilt(IdService.Instance.Next(IdService.Adventurer), fighterTemplate, "Joshua Blackwood"));
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