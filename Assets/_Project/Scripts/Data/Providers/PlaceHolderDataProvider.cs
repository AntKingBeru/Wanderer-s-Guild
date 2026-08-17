// In-memory IDataProvider used until the import tool has authored real data assets.
using System.Linq;
using System.Collections.Generic;

namespace WanderersGuild
{
    public class PlaceholderDataProvider : IDataProvider
    {
        private readonly List<SpeciesData> _species = new()
        {
            SpeciesData.CreatePlaceholder("human", "Human", new StatBlock(1, 1, 1, 1)),
        };
        private readonly List<ClassData>   _classes = new()
        {
            ClassData.CreatePlaceholder("fighter", "Fighter",
                new StatBlock(6, 3, 1, 5), new StatBlock(2, 1, 0, 2),
                QuestCategory.Combat, QuestCategory.Subjugation),
            ClassData.CreatePlaceholder("archer", "Archer",
                new StatBlock(3, 6, 2, 3), new StatBlock(1, 2, 1, 1),
                QuestCategory.Combat, QuestCategory.Investigation),
        };
        private readonly List<RequestData> _requests = new()
        {
            RequestData.CreatePlaceholder("req_wolves", "Cull the wolves threatening the north road.",
                QuestCategory.Combat, Difficulty.Easy, Rank.F, RequestSource.Settlement, 120, 7),
            RequestData.CreatePlaceholder("req_escort", "Escort a merchant caravan to the border.",
                QuestCategory.Escort, Difficulty.Moderate, Rank.E, RequestSource.Merchant, 250, 10),
        };

        public IReadOnlyList<SpeciesData> AllSpecies => _species;
        public IReadOnlyList<ClassData> AllClasses => _classes;
        public IReadOnlyList<RequestData> AllRequests => _requests;

        public SpeciesData GetSpecies(string id) => Find(_species, id);
        public ClassData GetClass(string id) => Find(_classes, id);
        public RequestData GetRequest(string id) => Find(_requests, id);

        private static T Find<T>(List<T> list, string id) where T : class, IIdentifiable
        {
            return list.FirstOrDefault(item => item.Id == id);
        }
    }
}