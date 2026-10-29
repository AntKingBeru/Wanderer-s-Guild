// Name pool for new adventurers: first names, plus epithets that are sometimes added ("Bran the Bold").
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Adventurer Name List", fileName = "AdventurerNameList")]
    public class AdventurerNameList : ScriptableObject
    {
        [SerializeField] private string[] firstNames = { "Bran", "Mira", "Tobin", "Elsa", "Garrick", "Lysa", "Corwin", "Nell" };

        [Tooltip("Appended after the name, e.g. \"the Bold\" → \"Bran the Bold\".")]
        [SerializeField] private string[] epithets = { "the Bold", "the Quiet", "Ironhand", "the Lucky", "Swiftfoot" };

        public IReadOnlyList<string> FirstNames => firstNames ?? Array.Empty<string>();
        public IReadOnlyList<string> Epithets => epithets ?? Array.Empty<string>();
    }
}