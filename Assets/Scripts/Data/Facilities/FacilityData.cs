// ScriptableObject prototype: a facility type and its ordered per-level construction definitions.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FacilityData", menuName = "Wanderer's Guild/Facility Data")]
public class FacilityData : ScriptableObject
{
    [SerializeField] private FacilityType type;
    [Tooltip("Level definitions in order; element 0 = level 1, element 1 = level 2, etc.")]
    [SerializeField] private List<FacilityLevelDef> levels = new List<FacilityLevelDef>();

    public FacilityType Type => type;
    public int MaxLevel => levels?.Count ?? 0;
    
    public bool TryGetLevel(int level, out FacilityLevelDef def)
    {
        if (levels != null && level >= 1 && level <= levels.Count)
        {
            def = levels[level - 1];
            return true;
        }
        def = default;
        return false;
    }
}