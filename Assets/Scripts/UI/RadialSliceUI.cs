// One interactive slice inside the radial build menu.
// Displays a room's icon on a button. Fires hover and click callbacks
// provided by BuildRadialMenuUI so this component stays data-agnostic.
// Attach to the slice prefab root alongside a Button component.

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadialSliceUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Image component that displays the room's icon.")]
    [SerializeField] private Image iconImage;

    [Tooltip("Button component on this slice. Selected visuals handled by Unity's Button.")]
    [SerializeField] private Button button;
    
    private RoomDefinition _room;
    private Action<RoomDefinition, Vector2> _onHover;
    private Action _onUnhover;
    private Action<RoomDefinition> _onClick;
    
    // Called by BuildRadialMenuUI after instantiation.
    public void Initialize(RoomDefinition room,
        Action<RoomDefinition, Vector2> onHover,
        Action onUnhover,
        Action<RoomDefinition> onClick)
    {
        _room = room;
        _onHover = onHover;
        _onUnhover = onUnhover;
        _onClick = onClick;

        if (iconImage && room.Icon)
            iconImage.sprite = room.Icon;

        if (button)
            button.onClick.AddListener(HandleClick);
    }
    
    private void OnDestroy()
    {
        if (button)
            button.onClick.RemoveListener(HandleClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
        => _onHover?.Invoke(_room, eventData.position);

    public void OnPointerExit(PointerEventData eventData)
        => _onUnhover?.Invoke();

    private void HandleClick()
        => _onClick?.Invoke(_room);
}