// Drives the reputation bar and level label in the HUD.
// Attach to ReputationBarPanel (child of HUD_Root).
// Subscribes to ReputationSystem events — no polling, no Update loop.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReputationHUDController : MonoBehaviour
{
    #region Inspector
    [Header("Bar")]
    [Tooltip("Filled Image (Fill Method: Vertical, Fill Origin: Bottom). " +
             "fillAmount 0 = MinReputation, 1 = MaxReputation.")]
    [SerializeField] private Image barFill;

    [Header("Label")]
    [Tooltip("Displays the current ReputationLevel tier name.")]
    [SerializeField] private TMP_Text levelLabel;

    [Header("Bar Colours by Tier")]
    [SerializeField] private Color colorExtremelyLow = new(0.55f, 0.08f, 0.08f);
    [SerializeField] private Color colorLow = new(0.80f, 0.35f, 0.08f);
    [SerializeField] private Color colorAverage = new(0.85f, 0.76f, 0.10f);
    [SerializeField] private Color colorHigh = new(0.18f, 0.75f, 0.32f);
    #endregion
    
    #region Display Names
    // Matches the enum names the user requested, formatted for display.
    // Replace with a localization lookup when i18n is added.
    private static readonly System.Collections.Generic.Dictionary<ReputationLevel, string>
        DisplayNames = new()
        {
            { ReputationLevel.ExtremelyLow, "Extremely Low" },
            { ReputationLevel.Low, "Low" },
            { ReputationLevel.Average, "Average" },
            { ReputationLevel.High, "High" }
        };
    #endregion
    
    #region Lifecycle
    private void OnEnable()
    {
        ReputationSystem.OnReputationChanged += HandleValueChanged;
        ReputationSystem.OnReputationLevelChanged += HandleLevelChanged;
    }

    private void OnDisable()
    {
        ReputationSystem.OnReputationChanged -= HandleValueChanged;
        ReputationSystem.OnReputationLevelChanged -= HandleLevelChanged;
    }

    private void Start()
    {
        // If ReputationSystem already initialized before this controller awoke, pull its current state so the HUD is never blank on first frame.
        if (!ReputationSystem.Instance)
            return;
        HandleValueChanged(ReputationSystem.Instance.CurrentReputation);
        HandleLevelChanged(ReputationSystem.Instance.CurrentLevel);
    }
    #endregion
    
    #region Event Handlers
    private void HandleValueChanged(int reputation)
    {
        if (!barFill)
            return;
        // Map [-100, 100] → [0, 1] for fillAmount;
        const float range = ReputationSystem.MaxReputation - ReputationSystem.MinReputation;
        barFill.fillAmount = (reputation - ReputationSystem.MinReputation) / range;
    }

    private void HandleLevelChanged(ReputationLevel level)
    {
        if (levelLabel)
            levelLabel.text = DisplayNames.TryGetValue(level, out var levelName)
                ? levelName
                : level.ToString();
        if (barFill)
            barFill.color = TierToColor(level);
    }
    #endregion
    
    #region Helpers
    private Color TierToColor(ReputationLevel level) => level switch
    {
        ReputationLevel.ExtremelyLow => colorExtremelyLow,
        ReputationLevel.Low => colorLow,
        ReputationLevel.Average => colorAverage,
        ReputationLevel.High => colorHigh,
        _ => colorAverage
    };
    #endregion
}