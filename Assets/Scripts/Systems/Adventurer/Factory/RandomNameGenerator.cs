// Plain C# utility (not a MonoBehaviour) that generates "First Last" names by combining
// random entries from a NameDatabase. Injected into the factories by AdventurerManager.

using UnityEngine;

public class RandomNameGenerator
{
    private readonly NameDatabase _database;

    public RandomNameGenerator(NameDatabase database)
    {
        _database = database;
        if (!_database)
            Debug.LogError("[RandomNameGenerator] NameDatabase is null.");
    }

    // Returns a random "First Last" name. Falls back to "Adventurer" if the database is missing or empty.
    public string GenerateName()
    {
        if (!_database || _database.FirstNames == null || _database.FirstNames.Length == 0
            || _database.LastNames == null || _database.LastNames.Length == 0)
        {
            Debug.LogWarning("[RandomNameGenerator] NameDatabase missing or empty pools. Using fallback name.");
            return "Adventurer";
        }

        var first = _database.FirstNames[Random.Range(0, _database.FirstNames.Length)];
        var last = _database.LastNames[Random.Range(0, _database.LastNames.Length)];
        return $"{first} {last}";
    }
}