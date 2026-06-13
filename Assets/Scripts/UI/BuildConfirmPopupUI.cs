// Confirmation popup shown after the player clicks a room in the radial menu.
// Displays room name, cost, and build time; offers Confirm and Cancel buttons.
// On Confirm → invokes the onConfirmed callback (BuildRadialMenuUI wires this to BuildManager.TryBuildRoom).
// On Cancel  → invokes the onCancelled callback (returns to radial menu).
// Uses the Command pattern via Action callbacks so this popup stays fully decoupled.

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildConfirmPopupUI : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private TMP_Text buildTimeLabel;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action _onConfirmed;
    private Action _onCancelled;
    
    private void Awake()
    {
        gameObject.SetActive(false);

        if (confirmButton)
            confirmButton.onClick.AddListener(HandleConfirm);
        if (cancelButton)
            cancelButton.onClick.AddListener(HandleCancel);
    }

    private void OnDestroy()
    {
        if (confirmButton)
            confirmButton.onClick.RemoveListener(HandleConfirm);
        if (cancelButton)
            cancelButton.onClick.RemoveListener(HandleCancel);
    }
    
    // Show the popup with the given room info and result callbacks.
    public void Show(RoomDefinition room, Action onConfirmed, Action onCancelled)
    {
        _onConfirmed = onConfirmed;
        _onCancelled = onCancelled;

        if (titleLabel)
            titleLabel.text = $"Build {room.RoomName}?";
        if (costLabel)
            costLabel.text = $"Cost: {room.GoldCost} Gold";
        if (buildTimeLabel)
            buildTimeLabel.text = $"Build Time: {room.BuildTimeHours:0.#}h";

        gameObject.SetActive(true);
    }

    private void HandleConfirm()
    {
        gameObject.SetActive(false);
        _onConfirmed?.Invoke();
    }

    private void HandleCancel()
    {
        gameObject.SetActive(false);
        _onCancelled?.Invoke();
    }
}