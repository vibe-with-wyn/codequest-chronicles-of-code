using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LanguageSelectionController : MonoBehaviour
{
    [SerializeField] private AudioSource languageSound;

    private bool isDataManagerReady = false;

    private void Start()
    {
        StartCoroutine(CheckDataManager());
    }

    private IEnumerator CheckDataManager()
    {
        // Wait for GameDataManager to be ready
        while (GameDataManager.Instance == null)
        {
            yield return null;
        }
        
        isDataManagerReady = true;
        Debug.Log("[LanguageSelection] GameDataManager is ready");
    }

    /// <summary>
    /// Handles Java language selection
    /// Since only Java is implemented, this simply confirms the selection and proceeds
    /// </summary>
    public void SelectJava()
    {
        StartCoroutine(HandleLanguageSelection());
    }

    private IEnumerator HandleLanguageSelection()
    {
        // Play selection sound
        if (languageSound != null)
        {
            languageSound.Play();
            yield return new WaitForSeconds(languageSound.clip.length);
            Debug.Log($"[LanguageSelection] Sound played for {languageSound.clip.length} seconds");
        }

        // Wait for GameDataManager if not ready
        if (!isDataManagerReady)
        {
            Debug.LogWarning("[LanguageSelection] GameDataManager not ready, waiting...");
            yield return CheckDataManager();
        }

        if (GameDataManager.Instance != null)
        {
            // Language is now hardcoded to "Java" in GameDataManager
            // No need to call SetLanguage() anymore - it's always Java
            Debug.Log($"[LanguageSelection] Language confirmed as: {GameDataManager.Instance.SelectedLanguage}");

            // Proceed to character selection
            StartCoroutine(LoadSceneAsync("Character Selection"));
        }
        else
        {
            Debug.LogError("[LanguageSelection] GameDataManager instance is null!");
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"[LanguageSelection] Loading scene: {sceneName}");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            Debug.Log($"[LanguageSelection] Loading progress: {asyncLoad.progress * 100:F0}%");
            yield return null;
        }
        
        Debug.Log($"[LanguageSelection] Scene '{sceneName}' loaded successfully");
    }
}