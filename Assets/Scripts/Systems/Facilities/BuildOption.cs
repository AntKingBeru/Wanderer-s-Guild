// One radial build option: a room type resolved at a door, with cost/time and enabled state.

public class BuildOption
{
    public FacilityType Type;
    public FacilityData Data;
    public TileCoord Origin;
    public int Cost;
    public int ConstructionHours;
    public bool Fits;
    public bool Affordable;
    public bool Enabled => Fits && Affordable;
    public string DisabledReason;
}