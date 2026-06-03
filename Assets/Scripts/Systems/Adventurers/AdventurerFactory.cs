// Abstract base for all adventurer factories using the Factory Method pattern.
// AdventurerManager holds one active factory and calls CreateAdventurer() on the arrival timer.
// Swap factories at runtime to change generation behavior (e.g., random arrivals vs. event-triggered name adventurers).

public abstract class AdventurerFactory
{
    // The single factory method every concrete factory must implement.
    // Returns a fully initialized AdventurerData ready to register, or null on failure.
    public abstract AdventurerData CreateAdventurer(AdventurerCreationContext context);
    
    // Shared utility: generates a collision-resistant unique ID for new adventurers.
    protected static string GenerateID() => System.Guid.NewGuid().ToString();
    
    // Optional lifecycle hooks - override to perform setup or teardown.
    public virtual void OnFactoryActivated() { }
    public virtual void OnFactoryDeactivated() { }
}