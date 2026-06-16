// Drives the progression bar and level label in the HUD.
// Attach to ProgressionBarPanel (child of HUD_Root).
// Subscribes to ProgressionSystem events — no polling, no Update loop.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressionHUDController : MonoBehaviour
{
    private const int MaxProgressionRank = 7;
    
    #region Inspector
    [Header("Bar")]
    [Tooltip("Filled Image (Fill Method: Vertical, Fill Origin: Bottom). " +
             "fillAmount 0 = MinReputation, 1 = MaxReputation.")]
    [SerializeField] private Image barFill;
    
    [Header("Label")]
    [Tooltip("Displays the current ProgressRank")]
    [SerializeField] private TMP_Text currentRankLabel;
    [Tooltip("Displays the next ProgressRank")]
    [SerializeField] private TMP_Text nextRankLabel;
    #endregion
    
    #region Display Names
    // Matches with the number of ranks (7)
    // Replace with localization lookup when i18n is added
    private static readonly System.Collections.Generic.Dictionary<int, string> 
        DisplayNames = new()
    {
        {0, "F"},
        {1, "E"},
        {2, "D"},
        {3, "C"},
        {4, "B"},
        {5, "A"},
        {6, "S"},
        {7, "National"},
    };
    #endregion

    #region Lifecycle
    private void OnEnable()
    {
        GameEventRelay.Instance.onProgressionXpChanged.AddListener(HandleXpChanged);
        GameEventRelay.Instance.onProgressionRankChanged.AddListener(HandleRankChanged);
    }

    private void OnDisable()
    {
        GameEventRelay.Instance.onProgressionXpChanged.RemoveListener(HandleXpChanged);
        GameEventRelay.Instance.onProgressionRankChanged.RemoveListener(HandleRankChanged);
    }
    
    private void Start()
    {
        HandleXpChanged(ProgressionSystem.Instance.CurrentXp, ProgressionSystem.Instance.CurrentThreshold);
        HandleRankChanged(ProgressionSystem.Instance.CurrentRank);
    }
    #endregion

    #region Event Handlers
    private void HandleXpChanged(int xp, int threshold)
    {
        if (!barFill)
            return;
        barFill.fillAmount = xp / (float)threshold;
    }

    private void HandleRankChanged(int rank)
    {
        if (!currentRankLabel)
            return;
        currentRankLabel.text = DisplayNames[rank];
        nextRankLabel.text = rank < MaxProgressionRank ? DisplayNames[rank + 1] : DisplayNames[MaxProgressionRank];
    }
    #endregion
}