// Facade for one adventurer's in-world visual: binds id and forwards content/movement calls.

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class AdventurerVisual : MonoBehaviour
{
    [SerializeField] private AdventurerMovement movement;
    [SerializeField] private BillboardContent content;
    [SerializeField] private BillboardHealthBar healthBar;
    [SerializeField] private UIDocument billboardDocument;
    [SerializeField] private NavMeshAgent agent;
    
    private VisualElement _billboardRoot;
    
    public int AdventurerId { get; private set; }
    
    public void Bind(Adventurer adventurer, Sprite classSprite)
    {
        AdventurerId = adventurer.Id;
        movement.Initialize(adventurer.Id);
        content.Render(new BillboardInfo(adventurer.Level, adventurer.Name, adventurer.Class, adventurer.Rank), classSprite);
        healthBar.SetRatio(1f);
        _billboardRoot = billboardDocument ? billboardDocument.rootVisualElement : null;
    }

    public void SetVisible(bool visible)
    {
        foreach (var render in GetComponentsInChildren<Renderer>(true))
            render.enabled = visible;
        if (_billboardRoot != null)
            _billboardRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (agent)
            agent.enabled = visible;
    }

    public void SetGoal(MovementGoal goal)
        => movement.SetGoal(goal);
    
    public void SetHealthRatio(float ratio)
        => healthBar.SetRatio(ratio);
    
    public void Render(BillboardInfo info, Sprite classSprite)
        => content.Render(info, classSprite);
}