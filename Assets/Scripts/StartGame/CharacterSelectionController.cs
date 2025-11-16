using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CharacterSelectionController : MonoBehaviour
{
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioSource swordsmanSound;
    [SerializeField] private AudioSource mageSound;
    [SerializeField] private AudioSource archerSound;

    private bool isDataManagerReady = false;

    private void Start()
    {
        StartCoroutine(CheckDataManager());

        if (backgroundMusic != null)
        {
            backgroundMusic.Play();
        }
    }

    private IEnumerator CheckDataManager()
    {
        while (GameDataManager.Instance == null)
        {
            yield return null;
        }

        isDataManagerReady = true;

    }

    public void SelectCharacter(string character)
    {
        StartCoroutine(HandleButtonClick(character));
    }

    private IEnumerator HandleButtonClick(string character)
    {
        AudioSource clickSound = GetClickSound(character);

        if (clickSound != null)
        {
            clickSound.Play();

            yield return new WaitForSeconds(clickSound.clip.length);

        }

        Debug.Log($"Received character parameter: '{character}'");

        if (!IsCharacterImplemented(character))
        {
            yield break;
        }

        if (isDataManagerReady && GameDataManager.Instance != null && !string.IsNullOrEmpty(character))
        {
            GameDataManager.Instance.SetCharacter(character);
            GameDataManager.Instance.UpdateProgress(1);

            if (GameDataManager.Instance.SelectedLanguage != null && GameDataManager.Instance.SelectedCharacter != null)
            {
                StartCoroutine(LoadLoadingScreenAndProceed("Game World Context"));
            }
        }
    }

    private AudioSource GetClickSound(string character)
    {
        switch (character.ToLower())
        {
            case "swordsman":
                return swordsmanSound;
            case "mage":
                return mageSound;
            case "archer":
                return archerSound;
            default:
                return null;
        }
    }

    // NEW: Check if character is implemented and playable
    private bool IsCharacterImplemented(string character)
    {
        switch (character.ToLower())
        {
            case "swordsman":
                return true;
            case "mage":
            case "archer":
                return false;
            default:
                return false;
        }
    }

    private IEnumerator LoadLoadingScreenAndProceed(string targetScene)
    {
        Debug.Log("Loading LoadingScreen scene");
        LoadingScreenController.TargetSceneName = targetScene;

        AsyncOperation loadOp = SceneManager.LoadSceneAsync("Loading Screen");

        yield return null;
    }
}