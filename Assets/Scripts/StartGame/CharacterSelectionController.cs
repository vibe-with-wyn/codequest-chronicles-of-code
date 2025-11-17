using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CharacterSelectionController : MonoBehaviour
{
    [Header("Audio")]
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
        Debug.Log("[CharacterSelection] GameDataManager is ready");
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

        Debug.Log($"[CharacterSelection] Received character parameter: '{character}'");

        if (!IsCharacterImplemented(character))
        {
            yield break;
        }

        if (!isDataManagerReady)
        {
            yield return CheckDataManager();
        }

        if (GameDataManager.Instance != null)
        {
            Debug.Log($"[CharacterSelection] Character confirmed as: {GameDataManager.Instance.SelectedCharacter}");
            
            GameDataManager.Instance.UpdateProgress(1);

            if (!string.IsNullOrEmpty(GameDataManager.Instance.SelectedLanguage) && 
                !string.IsNullOrEmpty(GameDataManager.Instance.SelectedCharacter))
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
            case "roran":
                return swordsmanSound;
            case "mage":
                return mageSound;
            case "archer":
                return archerSound;
            default:
                return null;
        }
    }

    private bool IsCharacterImplemented(string character)
    {
        switch (character.ToLower())
        {
            case "swordsman":
            case "roran":
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
        Debug.Log($"[CharacterSelection] Loading scene: Loading Screen -> {targetScene}");
        LoadingScreenController.TargetSceneName = targetScene;

        AsyncOperation loadOp = SceneManager.LoadSceneAsync("Loading Screen");

        while (!loadOp.isDone)
        {
            yield return null;
        }
    }
}