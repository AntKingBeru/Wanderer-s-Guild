// Abstraction over static game data so the backing store can be swapped (placeholder vs databases).
using System.Collections.Generic;

namespace WanderersGuild
{
    public interface IDataProvider
    {
        IReadOnlyList<SpeciesData> AllSpecies { get; }
        IReadOnlyList<ClassData> AllClasses { get; }
        IReadOnlyList<RequestData> AllRequests { get; }

        SpeciesData GetSpecies(string id);
        ClassData GetClass(string id);
        RequestData GetRequest(string id);
    }
}