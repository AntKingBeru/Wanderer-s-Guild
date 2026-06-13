using UnityEngine;

[System.Serializable]
public class RoomCapabilities
{
    [Header("Lodging")]
    [Tooltip("Number of adventurer bed slots this room adds to the guild.")]
    public int bedSlots;

    [Header("Tavern")]
    [Tooltip("If true, adventurers can spend their own gold here to buy food and drink, " +
             "resetting their DaysWithoutFood counter each day.")]
    public bool providesFoodAndDrink;

    [Header("Training")]
    [Tooltip("If true, this room offers training services (concrete type defined later per-room).")]
    public bool providesTraining;

    [Header("Stat Boosts — applied globally to all adventurers while the room exists")]
    [Tooltip("Flat bonus added to every adventurer's MaxHp once construction completes.")]
    public float hpBonus;

    [Tooltip("Flat bonus added to every adventurer's Damage once construction completes.")]
    public float damageBonus;

    [Tooltip("Flat bonus added to every adventurer's Speed once construction completes.")]
    public float speedBonus;
}