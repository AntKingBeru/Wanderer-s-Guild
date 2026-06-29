// Minimal facility presence tracker (stub): records which facilities are built. Full system later.

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-85)]
public class FacilityController : MonoSingleton<FacilityController>
{
    private readonly HashSet<FacilityType> _built = new HashSet<FacilityType>();

    public bool IsBuilt(FacilityType type) => _built.Contains(type);
    
    public void MarkBuilt(FacilityType type)
    {
        if (!_built.Add(type))
            return;
        GameEventsRelay.Instance.RaiseFacilityBuilt(type);
    }
}