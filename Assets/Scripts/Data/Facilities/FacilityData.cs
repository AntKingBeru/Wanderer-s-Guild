// ScriptableObject prototype: a facility type and its ordered per-level construction definitions.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FacilityData", menuName = "Wanderer's Guild/Facility Data")]
public class FacilityData : ScriptableObject
{
    [SerializeField] private FacilityType type;
    [Tooltip("Level definitions in order; element 0 = level 1, element 1 = level 2, etc.")]
    [SerializeField] private List<FacilityLevelDef> levels = new List<FacilityLevelDef>();
    
    [Header("Construction Stage Prefabs")]
    [SerializeField] private GameObject emptyPrefab;
    [SerializeField] private GameObject earlyScaffoldPrefab;
    [SerializeField] private GameObject lateScaffoldPrefab;
    [SerializeField] private GameObject finishedPrefab;
    
    [Header("Per-Level Add-On Pieces (index 0 = level 2's add-on, etc.)")]
    [SerializeField] private GameObject[] levelAddOns;
    
    [Header("Placement")]
    [SerializeField] private RoomFootprint footprint;

    public FacilityType Type => type;
    public int MaxLevel => levels?.Count ?? 0;
    public RoomFootprint Footprint => footprint;
    
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
    
    public GameObject StagePrefab(ConstructionStage stage) => stage switch
    {
        ConstructionStage.Empty => emptyPrefab,
        ConstructionStage.EarlyScaffolding => earlyScaffoldPrefab,
        ConstructionStage.LateScaffolding => lateScaffoldPrefab,
        ConstructionStage.Finished => finishedPrefab,
        _ => null
    };
    
    public GameObject AddOnForLevel(int level)
    {
        var i = level - 2;
        return levelAddOns != null && i >= 0 && i < levelAddOns.Length ? levelAddOns[i] : null;
    }
}