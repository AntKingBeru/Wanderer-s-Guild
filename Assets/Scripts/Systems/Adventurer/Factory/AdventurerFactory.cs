// Abstract base for all adventurer factories (Factory Method pattern).
// AdventurerManager calls CreateAdventurer() through whichever concrete factory fits the
// situation — RandomAdventurerFactory for spontaneous arrivals, SetAdventurerFactory for
// designer-defined presets. Both funnel through AdventurerBuilder for validated construction.

public abstract class AdventurerFactory
{
    // Returns a fully initialized AdventurerData ready to register, or null on failure.
    public abstract AdventurerData CreateAdventurer(AdventurerCreationContext context);

    // Shared utility: generates a collision-resistant unique ID for new adventurers.
    protected static string GenerateID() => System.Guid.NewGuid().ToString();
}