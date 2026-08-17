// IDataProvider backed by the authored ScriptableObject databases.
using System.Collections.Generic;

namespace WanderersGuild
{
    public class DatabaseDataProvider : IDataProvider
    {
        private readonly SpeciesDatabase _species;
        private readonly ClassDatabase _classes;
        private readonly RequestDatabase _requests;

        public DatabaseDataProvider(SpeciesDatabase species, ClassDatabase classes, RequestDatabase requests)
        {
            _species = species;
            _classes = classes;
            _requests = requests;
        }

        public IReadOnlyList<SpeciesData> AllSpecies => _species.Entries;
        public IReadOnlyList<ClassData> AllClasses => _classes.Entries;
        public IReadOnlyList<RequestData> AllRequests => _requests.Entries;

        public SpeciesData GetSpecies(string id) => _species.GetById(id);
        public ClassData GetClass(string id) => _classes.GetById(id);
        public RequestData GetRequest(string id) => _requests.GetById(id);
    }
}