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
        while (GameDataManager.Instance == null)
        {
            yield return null; // Wait one frame
        }
        isDataManagerReady = true;
        Debug.Log("GameDataManager is ready in LanguageSelection");
    }

    public void SelectLanguage(string language)
    {
        StartCoroutine(HandleLanguageSelection(language));
    }

    public IEnumerator HandleLanguageSelection(string language)
    {
        if (languageSound != null)
        {
            languageSound.Play();
            yield return new WaitForSeconds(languageSound.clip.length); // Wait for sound
            Debug.Log($"Sound played for {languageSound.clip.length} seconds");
        }

        if (isDataManagerReady && GameDataManager.Instance != null && !string.IsNullOrEmpty(language))
        {
            GameDataManager.Instance.SetLanguage(language);

            StartCoroutine(LoadSceneAsync("Character Selection"));
        }
        else
        {
            Debug.LogError("GameDataManager not found, not ready, or invalid language selected");
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            Debug.Log(asyncLoad.progress);
            yield return null;
        }
    }

}