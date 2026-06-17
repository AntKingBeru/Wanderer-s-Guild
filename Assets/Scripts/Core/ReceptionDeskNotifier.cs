// Bridges quest/rank-up application events to the reception desk's world-space billboard.
// Tracks how many new applications have arrived since the popup was last dismissed,
// shows the billboard above the desk, and auto-hides it after a delay or as soon as
// the player opens the Reception Desk screen themselves.
// Uses the Observer pattern via GameEventRelay (application events) and InteractionManager
// (screen-opened event) - this component owns the "when", ApplicationNotificationBillboard owns the "how it looks".

using System.Collections;
using UnityEngine;

public class ReceptionDeskNotifier : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The billboard popup positioned above the reception desk prop.")]
    [SerializeField] private ApplicationNotificationBillboard billboard;

    [Header("Behaviour")]
    [Tooltip("Also pop the billboard for rank-up applications, not just regular quest " +
             "applications. They show up in the same reception desk list, so this defaults on.")]
    [SerializeField] private bool includeRankUpApplications = true;

    [Tooltip("Seconds the popup stays visible with no new applications before it auto-hides.")]
    [SerializeField, Min(0.5f)] private float autoHideDelaySeconds = 6f;

    // Count of new applications received since the popup was last dismissed.
    // This is a notification counter, not a live "currently pending" count - it does not
    // decrease if an application is approved or rejected while the popup happens to be showing.
    private int _pendingCount;
    private Coroutine _autoHideCoroutine;

    #region Lifecycle
    private void OnEnable()
    {
        if (GameEventRelay.Instance)
        {
            GameEventRelay.Instance.onApplicationSubmitted.AddListener(HandleApplicationSubmitted);
            if (includeRankUpApplications)
                GameEventRelay.Instance.onRankUpApplicationCreated.AddListener(HandleRankUpApplicationCreated);
        }

        if (InteractionManager.Instance)
            InteractionManager.Instance.OnScreenOpened += HandleScreenOpened;

        if (!billboard)
            Debug.LogWarning($"[ReceptionDeskNotifier] '{name}' has no billboard assigned. " +
                             "Application popups will not be shown.");
    }

    private void OnDisable()
    {
        if (GameEventRelay.Instance)
        {
            GameEventRelay.Instance.onApplicationSubmitted.RemoveListener(HandleApplicationSubmitted);
            GameEventRelay.Instance.onRankUpApplicationCreated.RemoveListener(HandleRankUpApplicationCreated);
        }

        if (InteractionManager.Instance)
            InteractionManager.Instance.OnScreenOpened -= HandleScreenOpened;

        if (_autoHideCoroutine != null)
            StopCoroutine(_autoHideCoroutine);
    }
    #endregion

    #region Event Handlers
    private void HandleApplicationSubmitted(QuestApplication application)
        => NotifyNewApplication();

    private void HandleRankUpApplicationCreated(RankUpApplicationData application)
        => NotifyNewApplication();

    // Dismiss as soon as the player opens the Reception Desk themselves - they're
    // already looking at the application list, so the popup has done its job.
    private void HandleScreenOpened(ScreenType type)
    {
        if (type == ScreenType.ReceptionDesk)
            Dismiss();
    }
    #endregion

    #region Notification Flow
    private void NotifyNewApplication()
    {
        // Don't bother popping a billboard the player can't usefully see while they're
        // already standing at the open Reception Desk screen watching the list live.
        var deskAlreadyOpen = InteractionManager.Instance
            && InteractionManager.Instance.IsScreenOpen
            && InteractionManager.Instance.CurrentScreenType == ScreenType.ReceptionDesk;
        if (deskAlreadyOpen)
            return;

        _pendingCount++;
        billboard?.Show(_pendingCount);

        if (_autoHideCoroutine != null)
            StopCoroutine(_autoHideCoroutine);
        _autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
    }

    private void Dismiss()
    {
        _pendingCount = 0;
        billboard?.Hide();

        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
            _autoHideCoroutine = null;
        }
    }

    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideDelaySeconds);
        Dismiss();
    }
    #endregion
}