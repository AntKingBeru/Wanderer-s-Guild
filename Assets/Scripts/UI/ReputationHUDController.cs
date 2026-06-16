// Listens to reputation events through GameEventRelay and updates the HUD bar and label.
// Replaces direct ReputationSystem event subscriptions.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReputationHUDController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The fill image of the reputation bar.")]
    [SerializeField] private Image reputationBar;

    [Tooltip("Optional label showing the numeric reputation value.")]
    [SerializeField] private TextMeshProUGUI reputationLabel;

    [Tooltip("Optional label showing the reputation tier name.")]
    [SerializeField] private TextMeshProUGUI reputationLevelLabel;
    
    private void OnEnable()
    {
        if (!GameEventRelay.Instance)
            return;
        GameEventRelay.Instance.onReputationChanged.AddListener(HandleReputationChanged);
        GameEventRelay.Instance.onReputationLevelChanged.AddListener(HandleReputationLevelChanged);

        // Populate immediately from current state.
        if (ReputationSystem.Instance)
        {
            HandleReputationChanged(ReputationSystem.Instance.CurrentReputation);
            HandleReputationLevelChanged(ReputationSystem.Instance.CurrentLevel);
        }
    }

    private void OnDisable()
    {
        if (!GameEventRelay.Instance)
            return;
        GameEventRelay.Instance.onReputationChanged.RemoveListener(HandleReputationChanged);
        GameEventRelay.Instance.onReputationLevelChanged.RemoveListener(HandleReputationLevelChanged);
    }

    // Updates bar fill as a 0–1 normalized value across the full -100 to +100 range.
    private void HandleReputationChanged(int value)
    {
        if (reputationBar)
        {
            var normalised = Mathf.InverseLerp(ReputationSystem.MinReputation, ReputationSystem.MaxReputation, value);
            reputationBar.fillAmount = normalised;
        }
        if (reputationLabel)
            reputationLabel.text = value.ToString();
    }

    private void HandleReputationLevelChanged(ReputationLevel level)
    {
        if (reputationLevelLabel)
            reputationLevelLabel.text = level.ToString();
    }
}