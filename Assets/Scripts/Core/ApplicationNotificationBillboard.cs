// World-space popup that appears above the reception desk prop to flag a new quest
// or rank-up application. Pure view: it only knows how to show, refresh, and hide
// itself, and always faces the camera (same billboard technique as AdventurerBillboard).
// ReceptionDeskNotifier owns all the "when" logic; this component owns "how it looks".

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ApplicationNotificationBillboard : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("The icon shown above the reception desk when a new application arrives.")]
    [SerializeField] private Image popupIcon;

    [Header("Pop-In Animation")]
    [Tooltip("How long the popup takes to scale up from zero when it appears.")]
    [SerializeField, Min(0.01f)] private float popInDuration = 0.25f;

    private Camera _cam;
    private Coroutine _popCoroutine;

    #region Lifecycle
    private void Awake() => _cam = Camera.main;

    private void Start()
    {
        // Hidden by default. The object must start ACTIVE in the hierarchy/prefab so
        // Awake/Start actually run once at scene load - after this, Show()/Hide() just
        // toggle activeSelf directly and Awake/Start are never re-invoked.
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!_cam)
        {
            _cam = Camera.main;
            return;
        }
        transform.LookAt(
            transform.position + _cam.transform.rotation * Vector3.forward,
            _cam.transform.rotation * Vector3.up
        );
    }
    #endregion

    #region Public API
    // Activates the popup and (re)plays the pop-in animation.
    // pendingCount is no longer displayed (icon-only popup), but it's kept in the
    // signature so ReceptionDeskNotifier doesn't need to change at all.
    public void Show(int pendingCount)
    {
        gameObject.SetActive(true);

        if (_popCoroutine != null)
            StopCoroutine(_popCoroutine);
        _popCoroutine = StartCoroutine(PlayPopIn());
    }

    // Hides the popup immediately.
    public void Hide()
    {
        if (_popCoroutine != null)
        {
            StopCoroutine(_popCoroutine);
            _popCoroutine = null;
        }
        gameObject.SetActive(false);
    }
    #endregion

    #region Animation
    private IEnumerator PlayPopIn()
    {
        transform.localScale = Vector3.zero;
        var elapsed = 0f;
        while (elapsed < popInDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / popInDuration);
            var eased = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.one * eased;
            yield return null;
        }
        transform.localScale = Vector3.one;
        _popCoroutine = null;
    }
    #endregion
}