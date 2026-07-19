// Runtime state of a single facility: its data, current built level (0 = unbuilt), and lifecycle state.

public class Facility
{
    private readonly FacilityData _data;

    public FacilityType Type => _data.Type;
    public FacilityData Data => _data;
    public int Level { get; private set; }
    public FacilityState State { get; private set; }
    
    public int NextLevel => Level + 1;
    public bool HasNextLevel => NextLevel <= _data.MaxLevel;
    public bool IsOperational => Level >= 1 && State == FacilityState.Operational;
    
    public Facility(FacilityData data)
    {
        _data = data;
        Level = 0;
        State = FacilityState.Available;
    }
    
    public void SetState(FacilityState state)
        => State = state;
    
    public void CompleteConstruction()
    {
        Level = NextLevel;
        State = FacilityState.Operational;
    }
}