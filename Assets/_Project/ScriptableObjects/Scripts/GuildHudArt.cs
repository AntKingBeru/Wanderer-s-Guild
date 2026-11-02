// HUD art slots: gold coin, adventurer icon, pause icon, and one emoji per reputation level (empty = runtime placeholder).
using System;
using UnityEngine;

namespace WanderersGuild
{
    [CreateAssetMenu(menuName = "Wanderer's Guild/Guild HUD Art", fileName = "GuildHudArt")]
    public class GuildHudArt : ScriptableObject
    {
        [Tooltip("Coin shown next to the gold amount (12×12, Point filtering).")]
        [SerializeField] private Sprite coin;

        [Tooltip("Person icon next to the adventurer count (12×12, Point filtering).")]
        [SerializeField] private Sprite adventurer;

        [Tooltip("Pause-menu button icon (12×12, Point filtering).")]
        [SerializeField] private Sprite pause;
        
        [Tooltip("Build-mode button icon (12×12, Point filtering).")]
        [SerializeField] private Sprite build;

        [Tooltip("One emoji per level, worst → best (Despised, Distrusted, Neutral, Respected, Renowned). " +
                 "Author them white/greyscale — they're tinted in the level's colour.")]
        [SerializeField] private Sprite[] reputationIcons = new Sprite[Enum.GetValues(typeof(ReputationLevel)).Length];

        public Sprite Coin => coin;
        public Sprite Adventurer => adventurer;
        public Sprite Pause => pause;
        public Sprite Build => build;

        public Sprite ReputationIcon(ReputationLevel level)
        {
            var index = (int)level;
            return reputationIcons != null && index >= 0 && index < reputationIcons.Length ? reputationIcons[index] : null;
        }
    }
}