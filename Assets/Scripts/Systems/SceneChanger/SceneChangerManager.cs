// Singleton Scene change manager, works asynchronously
// UI is handles in SceneChangerUIController


using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangerManager : MonoBehaviour
{
    // Making this class singleton
    public static SceneChangerManager Instance { get; private set; }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Cross-Scene singleton
        DontDestroyOnLoad(gameObject);
    }
    
    
    
    public void LoadScene(int sceneId)
    {
        var operation = SceneManager.LoadSceneAsync(sceneId);
        StartCoroutine(LoadSceneAsync(operation));
    }
    
    public void LoadScene(string sceneName)
    {
        var operation = SceneManager.LoadSceneAsync(sceneName);
        StartCoroutine(LoadSceneAsync(operation));
    }

    private IEnumerator LoadSceneAsync(AsyncOperation operation)
    {
        // Prevents automatic scene change, we want to wait for the player to press something
        // turn to true to change scene automatically
        operation.allowSceneActivation = false;
        
        // Fires to let everyone know we started the scene change
        GameEventRelay.Instance.onSceneProgressChanged?.Invoke(0f);

        while (!operation.isDone)
        {
            // Unity async progress ranges from 0 to 0.9. 
            // Dividing by 0.9 normalizes this value to a clean 0.0 - 1.0 range.
            float progressValue = Mathf.Clamp01(operation.progress / 0.9f);
            
            GameEventRelay.Instance.onSceneProgressChanged?.Invoke(progressValue);
            
            yield return null;
        }
    }
}
