// Central repository for all value-type structs across every system.
// Mirrors Enums.cs: add new structs here, separated by region, rather than scattering them.

using UnityEngine;

#region Adventurer
// Snapshot of an adventurer's stat block at their current level.
// Recomputed in full on level-up (CurrentHp resets to the new MaxHp).
[System.Serializable]
public struct AdventurerStats
{
    public float maxHp;
    public float currentHp;
    public float strength;
    public float dexterity;
}

// Defines an XP cost band over a range of levels.
// XP required to level up = xpPerLevelMultiplier × the adventurer's current level.
[System.Serializable]
public struct XpBracket
{
    [Tooltip("First level in this bracket (inclusive).")]
    [Min(1)] public int minLevel;

    [Tooltip("Last level in this bracket (inclusive).")]
    [Min(1)] public int maxLevel;

    [Tooltip("XP to level up = this value × the adventurer's current level.")]
    [Min(1)] public int xpPerLevelMultiplier;
}
#endregion