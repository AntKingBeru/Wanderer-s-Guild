// Command that starts construction/upgrade of a facility; usable by prop interactions or UI.

public class BuildFacilityCommand : IInteractionCommand
{
    private readonly FacilityType _type;
    public BuildFacilityCommand(FacilityType type) => _type = type;
    
    public void Execute()
    {
        if (FacilityController.Exists)
            FacilityController.Instance.StartConstruction(_type, out _);
    }
}