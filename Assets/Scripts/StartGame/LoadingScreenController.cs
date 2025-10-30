using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance;
    public static string TargetSceneName { get; set; }

    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text targetSceneText;

    private Canvas loadingCanvas;

    private void Awake()
    {
        // Cache canvas for toggling visibility
        loadingCanvas = GetComponentInChildren<Canvas>(true);

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("LoadingScreenController set as singleton and persisted");
        }
        else if (Instance != this)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name == "Loading Screen")
            {
                Instance = this;
                Debug.Log("LoadingScreenController updated for scene reload");
                return;
            }
            Destroy(gameObject);
            Debug.LogWarning("Duplicate LoadingScreenController destroyed (not LoadingScreen scene)");
        }
    }

    private void Start()
    {
        Debug.Log("LoadingScreenController Started");
        if (progressBar == null || targetSceneText == null)
        {
            Debug.LogError("ProgressBar or Target Scene Text is not assigned!");
            return;
        }

        // If a target scene is set, start loading it
        if (!string.IsNullOrEmpty(TargetSceneName))
        {
            LoadScene(TargetSceneName);
        }
        else
        {
            SetVisible(false); // hide if no scene to load
        }
    }

    public void LoadScene(string sceneName)
    {
        string environmentName = GetEnvironmentName(sceneName);

        // Make sure UI is visible when starting a load
        SetVisible(true);

        targetSceneText.text = $"Loading {environmentName}";
        Debug.Log($"LoadScene called with sceneName: {sceneName}, displaying: {environmentName}");
        StartCoroutine(LoadAsyncOperation(sceneName));
    }

    private IEnumerator LoadAsyncOperation(string sceneName)
    {
        Debug.Log($"Starting LoadAsyncOperation for {sceneName}");
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"Failed to start LoadSceneAsync for {sceneName}");
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            float progressValue = Mathf.Clamp01(operation.progress / 0.9f);
            progressBar.value = progressValue;

            if (progressText != null)
            {
                int percent = Mathf.RoundToInt(progressValue * 100f);
                progressText.text = $"{percent}%";
            }

            Debug.Log($"Progress: {progressBar.value * 100}% for {sceneName}");
            yield return null;
        }

        progressBar.value = 1f;
        if (progressText != null)
        {
            progressText.text = "100%";
        }
        operation.allowSceneActivation = true;

        Debug.Log($"Progress: {progressBar.value * 100}% for {sceneName}");
        Debug.Log($"Loading Complete, activating {sceneName}");

        while (!operation.isDone)
        {
            yield return null;
        }

        Debug.Log($"Fully loaded and switched to {sceneName}");

        // Hide UI after loading into gameplay/world
        SetVisible(false);

        // Reset TargetSceneName to avoid accidental reuse
        TargetSceneName = null;
    }

    private string GetEnvironmentName(string sceneName)
    {
        switch (sceneName.ToLower())
        {
            case "game world context":
                return "World";
            case "oak woods of syntax":
                return "Oak Woods of Syntax";
            default:
                return sceneName; // fallback
        }
    }

    /// <summary>
    /// Toggle the visibility of the loading screen UI
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.enabled = visible;
            Debug.Log($"Loading screen visibility set to {visible}");
        }
    }
}
