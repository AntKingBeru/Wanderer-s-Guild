// ScriptableObject mapping each AdventurerRank to a display color, ordered F…National.

using UnityEngine;

[CreateAssetMenu(fileName = "RankPalette", menuName = "Wanderer's Guild/Rank Palette")]
public class RankPalette : ScriptableObject
{
    [Tooltip("One colour per AdventurerRank, ordered F, E, D, C, B, A, S, National.")]
    [SerializeField] private Color[] colors = new Color[8];
    
    public Color GetColor(GuildRank rank)
    {
        var i = (int)rank;
        return (colors != null && i >= 0 && i < colors.Length)
            ? colors[i]
            : Color.white;
    }
    
    private void Reset()
    {
        colors = new[]
        {
            new Color(0.60f, 0.60f, 0.62f), // F
            new Color(0.42f, 0.74f, 0.45f), // E
            new Color(0.30f, 0.70f, 0.74f), // D
            new Color(0.35f, 0.56f, 0.90f), // C
            new Color(0.64f, 0.45f, 0.86f), // B
            new Color(0.94f, 0.60f, 0.26f), // A
            new Color(0.95f, 0.82f, 0.32f), // S
            new Color(1.0f, 0.0f, 0.0f)     // National
        };
    }
}