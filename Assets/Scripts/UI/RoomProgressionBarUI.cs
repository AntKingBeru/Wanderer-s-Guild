// Progress bar displayed above a room that is currently under construction.
// Subscribes to BuildManager.OnRoomProgressUpdated and OnRoomCompleted (Observer).
// Shows an image fill (0–1) and a percentage label.
// One instance per under-construction room; destroyed automatically when that room completes.
// Attach to a World Space Canvas child that is placed above the room in the scene.
// Assign the targetRoomInstanceId in the inspector after the room is queued,
// OR call Initialize(instanceId) from the spawning code.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomProgressionBarUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Image set to Filled mode; its fillAmount is driven by build progress (0–1).")]
    [SerializeField] private Image progressFill;

    [Tooltip("Label showing the percentage, e.g. '42%'.")]
    [SerializeField] private TMP_Text percentageLabel;

    [Header("Target Room")]
    [Tooltip("Instance ID of the room this bar tracks. Set via Initialize() at runtime.")]
    [SerializeField] private string targetRoomInstanceId;
    
    private void OnEnable()
    {
        GameEventRelay.Instance.onRoomProgressUpdated.AddListener(HandleProgressUpdated);
        GameEventRelay.Instance.onRoomCompleted.AddListener(HandleRoomCompleted);
    }

    private void OnDisable()
    {
        GameEventRelay.Instance.onRoomProgressUpdated.RemoveListener(HandleProgressUpdated);
        GameEventRelay.Instance.onRoomCompleted.RemoveListener(HandleRoomCompleted);
    }
    
    // Call this immediately after the room is queued to bind this bar to its instance.
    public void Initialize(string roomInstanceId)
        => targetRoomInstanceId = roomInstanceId;

    private void HandleProgressUpdated(RoomInstance room)
    {
        if (room.InstanceId != targetRoomInstanceId)
            return;
        SetProgress(room.Progress);
    }

    private void HandleRoomCompleted(RoomInstance room)
    {
        if (room.InstanceId != targetRoomInstanceId)
            return;
        SetProgress(1f);
        // Hide the bar once building is done.
        gameObject.SetActive(false);
    }

    private void SetProgress(float value)
    {
        if (progressFill)
            progressFill.fillAmount = value;
        if (percentageLabel)
            percentageLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}