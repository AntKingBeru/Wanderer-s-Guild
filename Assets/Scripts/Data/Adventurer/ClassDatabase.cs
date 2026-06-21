// ScriptableObject holding the full pool of ClassData assets that exist in the game,
// regardless of whether they're currently unlocked. ClassRegistry reads from this pool
// at runtime to determine what's actually available.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ClassDatabase", menuName = "Guild Manager/Adventurer/Class Database")]
public class ClassDatabase : ScriptableObject
{
    [Tooltip("Every ClassData asset that exists in the game. Add new classes here as they're created.")]
    [SerializeField] private ClassData[] allClasses;

    public IReadOnlyList<ClassData> AllClasses => allClasses;

    public ClassData GetClassData(AdventurerClass adventurerClass)
        => allClasses?.FirstOrDefault(c => c && c.AdventurerClass == adventurerClass);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (allClasses == null) return;
        var duplicates = allClasses.Where(c => c)
            .GroupBy(c => c.AdventurerClass)
            .Where(g => g.Count() > 1);
        foreach (var group in duplicates)
            Debug.LogWarning($"[ClassDatabase] Multiple ClassData assets share the class '{group.Key}'.");
    }
#endif
}