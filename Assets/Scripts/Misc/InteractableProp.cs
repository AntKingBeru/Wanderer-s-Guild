// World prop: glows on hover and runs its interaction command (opens a screen) on click.

using UnityEngine;

public class InteractableProp : MonoBehaviour, IInteractable
{
    [Header("Behaviour")]
    [Tooltip("Screen this prop opens when clicked.")]
    [SerializeField] private ScreenId opensScreen = ScreenId.None;

    [Header("Glow")]
    [Tooltip("Outline on this prop; auto-found on this object if left empty.")]
    [SerializeField] private Outline outline;

    private IInteractionCommand _command;

    private void Awake()
    {
        if (!outline)
            outline = GetComponent<Outline>();
        _command = new OpenScreenCommand(opensScreen);
    }
    
    public void OnHoverEnter()
        => outline?.Show();
    
    public void OnHoverExit()
        => outline?.Hide();
    
    public void OnInteract()
        => _command?.Execute();
}