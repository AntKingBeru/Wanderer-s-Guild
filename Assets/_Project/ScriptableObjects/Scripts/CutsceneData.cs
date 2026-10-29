using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Cutscene Data", fileName = "CutsceneData")]
    public class CutsceneData : ScriptableObject
    {
        [SerializeField] private CutsceneSlide[] slides = Array.Empty<CutsceneSlide>();

        public IReadOnlyList<CutsceneSlide> Slides => slides;
    }
}