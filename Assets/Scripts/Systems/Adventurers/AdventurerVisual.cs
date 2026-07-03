// Facade for one adventurer's in-world visual: binds id and forwards content/movement calls.

using UnityEngine;

public class AdventurerVisual : MonoBehaviour
{
    [SerializeField] private AdventurerMovement movement;
    [SerializeField] private BillboardContent content;
    [SerializeField] private BillboardHealthBar healthBar;
    
    public int AdventurerId { get; private set; }
    
    public void Bind(Adventurer adventurer, Sprite classSprite)
    {
        AdventurerId = adventurer.Id;
        movement.Initialize(adventurer.Id);
        content.Render(new BillboardInfo(adventurer.Level, adventurer.Name, adventurer.Class, adventurer.Rank), classSprite);
        healthBar.SetRatio(1f);
    }

    public void SetGoal(MovementGoal goal)
        => movement.SetGoal(goal);
    
    public void SetHealthRatio(float ratio)
        => healthBar.SetRatio(ratio);
}