// ScriptableObject holding the first/last name pools used by RandomNameGenerator.
// Designers populate these lists; the generator just picks randomly from each.

using UnityEngine;

[CreateAssetMenu(fileName = "NameDatabase", menuName = "Guild Manager/Adventurer/Name Database")]
public class NameDatabase : ScriptableObject
{
    [Tooltip("Pool of first names to draw from.")]
    [SerializeField] private string[] firstNames;

    [Tooltip("Pool of last names to draw from.")]
    [SerializeField] private string[] lastNames;

    public string[] FirstNames => firstNames;
    public string[] LastNames => lastNames;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (firstNames == null || firstNames.Length == 0)
            Debug.LogWarning("[NameDatabase] FirstNames is empty.");
        if (lastNames == null || lastNames.Length == 0)
            Debug.LogWarning("[NameDatabase] LastNames is empty.");
    }
#endif
}