// ScriptableObject name source: combines random first/last names for new adventurers.

using UnityEngine;

[CreateAssetMenu(fileName = "NamePool", menuName = "Wanderer's Guild/Name Pool")]
public class NamePool : ScriptableObject
{
    [SerializeField] private string[] firstNames;
    [SerializeField] private string[] lastNames;
    
    public string GenerateName(System.Random rng)
    {
        var first = Pick(firstNames, rng, "Adventurer");
        var last = Pick(lastNames, rng, "");
        return string.IsNullOrEmpty(last) ? first : $"{first} {last}";
    }

    private static string Pick(string[] pool, System.Random rng, string fallback)
        => pool is { Length: > 0 } ? pool[rng.Next(pool.Length)] : fallback;
}