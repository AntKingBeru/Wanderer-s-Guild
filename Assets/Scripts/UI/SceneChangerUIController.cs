// Listens to SceneChanger events and shows or hides the loading screen accordingly

using UnityEngine;
using UnityEngine.UI;

public class SceneChangerUIController : MonoBehaviour
{
    
    [Header("UI References")]
    [Tooltip("Loading screen to be shown on every scene change")]
    [SerializeField] private GameObject loadingScreen;

    [Tooltip("Progress bar to be shown on every scene change")]
    [SerializeField] private Slider progressBar;

    // Don't know if we want text
    // [Tooltip("Optional label showing the reputation tier name.")]
    // [SerializeField] private TMP_Text progressText;
    
    private void OnEnable()
    {
        if (!GameEventRelay.Instance)
            return;
        GameEventRelay.Instance.OnSceneProgressChanged.AddListener(HandleSceneProgressChanged);
    }

    private void OnDisable()
    {
        if (!GameEventRelay.Instance)
            return;
        GameEventRelay.Instance.OnSceneProgressChanged.RemoveListener(HandleSceneProgressChanged); 
    }

    private void HandleSceneProgressChanged(float value)
    {   
        // Check if we start the scene change
        if (value == 0f)
        {
            loadingScreen.SetActive(true);
        }
        progressBar.value = value;
        // If we want text
        // progressText.text = Mathf.RoundToInt(value * 100f) + "%";

        // Checking if we finished
        // Change this to wait for use input or something
        if (value >= 1f)
        {
            loadingScreen.SetActive(false);
        }
    }
}
