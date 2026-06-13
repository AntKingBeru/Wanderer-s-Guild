// ScriptableObject that defines a single buildable room template.
// Holds identity, cost, build time, icon, and the capability flags this room grants the guild.
// Create via: Guild Manager / Room Definition

using UnityEngine;

[CreateAssetMenu(fileName = "RoomDef_New", menuName = "Guild Manager/Room Definition")]
public class RoomDefinition : ScriptableObject
{
    #region Identity
    [Header("Identity")]
    [Tooltip("Internal unique key used by the build system. Must be unique across all RoomDefinitions.")]
    [SerializeField] private string roomId = "";

    [Tooltip("Display name shown in the radial menu and confirmation popup.")]
    [SerializeField] private string roomName = "New Room";

    [Tooltip("Flavour description shown in the tooltip popup in the radial menu.")]
    [TextArea(2, 5)]
    [SerializeField] private string description = "";

    [Tooltip("Icon displayed on the radial menu slice and in the tooltip.")]
    [SerializeField] private Sprite icon;
    #endregion
    
    #region Economy
    [Header("Economy")]
    [Tooltip("Gold cost deducted from guild funds when the player confirms construction.")]
    [SerializeField, Min(0)] private int goldCost = 100;

    [Tooltip("Construction time in in-game hours from confirmation to completion.")]
    [SerializeField, Min(0.5f)] private float buildTimeHours = 8f;
    #endregion
    
    #region Capabilities
    [Header("Capabilities")]
    [Tooltip("What this room provides to the guild once fully built.")]
    [SerializeField] private RoomCapabilities capabilities;
    #endregion
    
    #region Public Accessors
    public string RoomId       => roomId;
    public string RoomName     => roomName;
    public string Description  => description;
    public Sprite Icon         => icon;
    public int GoldCost        => goldCost;
    public float BuildTimeHours => buildTimeHours;
    public RoomCapabilities Capabilities => capabilities;
    #endregion
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(roomId))
            Debug.LogWarning($"[RoomDefinition] '{name}' has no roomId set. " +
                             "Assign a unique slug (e.g. 'barracks_01') to avoid lookup collisions.");
    }
#endif
}