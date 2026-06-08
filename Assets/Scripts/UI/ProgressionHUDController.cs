// Drives the progression bar and level label in the HUD.
// Attach to ProgressionBarPanel (child of HUD_Root).
// Subscribes to ProgressionSystem events — no polling, no Update loop.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressionHUDController : MonoBehaviour
{
    
    #region Inspector
    [Header("Bar")]
    [Tooltip("Filled Image (Fill Method: Vertical, Fill Origin: Bottom). " +
             "fillAmount 0 = MinReputation, 1 = MaxReputation.")]
    [SerializeField] private Image barFill;
    
    [Header("Label")]
    [Tooltip("Displays the current ProgressRank")]
    [SerializeField] private TMP_Text rankLabel;
    #endregion
    
    #region Display Names
    // Matches with the number of ranks (7)
    // Replace with localization lookup when i18n is added
    private static readonly System.Collections.Generic.Dictionary<int, string> 
        DisplayNames = new()
    {
        {0, "0"},
        {1, "1"},
        {2, "2"},
        {3, "3"},
        {4, "4"},
        {5, "5"},
        {6, "6"},
        {7, "7"},
    };
    #endregion

    #region Lifecycle
    private void OnEnable()
    {
        ProgressionSystem.OnProgressionXpChanged += HandleXpChanged;
        ProgressionSystem.OnProgressionRankChanged += HandleRankChanged;
    }

    private void OnDisable()
    {
        ProgressionSystem.OnProgressionXpChanged -= HandleXpChanged;
        ProgressionSystem.OnProgressionRankChanged -= HandleRankChanged;
    }
    
    private void Start()
    {
        // If ProgressionSystem already initialized before this controller awoke, pull its current state so the HUD is never blank on first frame.
        if (!ProgressionSystem.Instance)
            return;
        HandleXpChanged(ProgressionSystem.Instance.CurrentXp, ProgressionSystem.Instance.CurrentThreshold);
        HandleRankChanged(ProgressionSystem.Instance.CurrentRank);
    }
    #endregion

    #region Event Handlers
    private void HandleXpChanged(int xp, int threshold)
    {
        if (!barFill)
            return;
        barFill.fillAmount = (float)xp / (float)threshold;
    }

    private void HandleRankChanged(int rank)
    {
        if (!rankLabel)
            return;
        rankLabel.text = DisplayNames[rank];
    }
    #endregion
}
