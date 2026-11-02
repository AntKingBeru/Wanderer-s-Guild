// Base for what a facility does (Strategy pattern): effect assets are assigned on FacilityData in Unity, never in the sheet.
using UnityEngine;

namespace WanderersGuild
{
    public abstract class FacilityEffect : ScriptableObject
    {
        // Short text for the room card at a level (1-based), e.g. "Capacity +2".
        public abstract string Describe(int level);
    }
}