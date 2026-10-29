using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Credits Data", fileName = "CreditsData")]
    public class CreditsData : ScriptableObject
    {
        [SerializeField] private CreditsSection[] sections = Array.Empty<CreditsSection>();

        public IReadOnlyList<CreditsSection> Sections => sections;
    }
}