// A name pool: given names, plus a family name or an epithet sometimes added ("Bran Ashford", "Bran the Bold"). One per
// species (SpeciesData › Name List); GameConfig › Adventurers › Name List is the fallback.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Adventurer Name List", fileName = "AdventurerNameList")]
    public class AdventurerNameList : ScriptableObject
    {
        // NOTE: Renamed from firstNames — FormerlySerializedAs keeps existing assets' names.
        [Tooltip("One word each — the generator treats a full name's first word as its given name.")]
        [FormerlySerializedAs("firstNames")]
        [SerializeField] private string[] givenNames = { "Bran", "Mira", "Tobin", "Elsa", "Garrick", "Lysa", "Corwin", "Nell" };

        [Tooltip("Added after the given name (\"Bran Ashford\").")]
        [SerializeField] private string[] familyNames = Array.Empty<string>();
        [Tooltip("Chance a name gets a family name.")]
        [SerializeField, Range(0f, 1f)] private float familyNameChance = 0.4f;

        [Tooltip("Added after the given name instead of a family name (\"Bran the Bold\").")]
        [SerializeField] private string[] epithets = { "the Bold", "the Quiet", "Ironhand", "the Lucky", "Swiftfoot" };
        [Tooltip("Chance a name gets an epithet instead — never both (capped so the two chances add up to at most 1).")]
        [SerializeField, Range(0f, 1f)] private float epithetChance = 0.2f;

        public IReadOnlyList<string> GivenNames => givenNames ?? Array.Empty<string>();
        public IReadOnlyList<string> FamilyNames => familyNames ?? Array.Empty<string>();
        public IReadOnlyList<string> Epithets => epithets ?? Array.Empty<string>();
        public float FamilyNameChance => familyNameChance;
        public float EpithetChance => Mathf.Min(epithetChance, 1f - familyNameChance);

#if UNITY_EDITOR
        // Editor-only: fills the whole list (the species name-list tool). Arrays are copied.
        public void Fill(string[] given, string[] family, float familyChance, string[] epithetList, float epithetRoll)
        {
            givenNames = (string[])given.Clone();
            familyNames = (string[])family.Clone();
            familyNameChance = Mathf.Clamp01(familyChance);
            epithets = (string[])epithetList.Clone();
            epithetChance = Mathf.Clamp01(epithetRoll);
        }
#endif
    }
}