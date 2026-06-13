// ScriptableObject that holds all designer-tunable build system constants.
// Holds the full room pool available in the radial menu and the B key binding label.
// Create via: Guild Manager / Build Config

using UnityEngine;

[CreateAssetMenu(fileName = "BuildConfig", menuName = "Guild Manager/Build Config")]
public class BuildConfig : ScriptableObject
{
    [Header("Room Pool")]
    [Tooltip("All rooms available in the radial build menu. " +
             "Order determines radial slice order (clockwise from top).")]
    [SerializeField] private RoomDefinition[] availableRooms;

    [Header("Stat Boost Cap")]
    [Tooltip("Maximum cumulative HP bonus any single adventurer can receive from all built rooms. " +
             "Prevents stacking from multiple room types from becoming degenerate.")]
    [SerializeField, Min(0f)] private float maxHpBonus = 500f;

    [Tooltip("Maximum cumulative Damage bonus.")]
    [SerializeField, Min(0f)] private float maxDamageBonus = 100f;

    [Tooltip("Maximum cumulative Speed bonus.")]
    [SerializeField, Min(0f)] private float maxSpeedBonus = 20f;

    public RoomDefinition[] AvailableRooms => availableRooms;
    public float MaxHpBonus => maxHpBonus;
    public float MaxDamageBonus => maxDamageBonus;
    public float MaxSpeedBonus => maxSpeedBonus;
}