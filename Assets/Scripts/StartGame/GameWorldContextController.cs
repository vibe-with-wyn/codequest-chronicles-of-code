using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameWorldContextController : MonoBehaviour
{
#if UNITY_INCLUDE_TESTS
    // When true, scene loading is bypassed so tests won't destroy objects
    public static bool DisableSceneLoadingForTests = false;
#endif

    [SerializeField] private AudioSource narrationAudio;
    [SerializeField] private Button skipButton;
    [SerializeField] private Canvas gameWorldCanvas;

    private bool isSkipping = false;

    void Start()
    {
        // If canvas is not assigned, try to find it
        if (gameWorldCanvas == null)
        {
            gameWorldCanvas = GetComponentInChildren<Canvas>();
        }

#if UNITY_INCLUDE_TESTS
        // Prevent scene loading during PlayMode tests
        if (DisableSceneLoadingForTests)
        {
            // Only start narration and button binding — no scene transitions
            if (narrationAudio != null)
            {
                narrationAudio.Play();
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipButton);
            }

            return; // Skip all scene-loading logic
        }
#endif

        if (narrationAudio != null)
        {
            narrationAudio.Play();
            StartCoroutine(CheckNarrationCompletion());
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButton);
        }
    }

    private IEnumerator CheckNarrationCompletion()
    {
        while (narrationAudio != null && narrationAudio.isPlaying && !isSkipping)
        {
            yield return null;
        }

        if (!isSkipping)
        {
#if UNITY_INCLUDE_TESTS
            if (DisableSceneLoadingForTests) yield break;
#endif
            yield return new WaitForSeconds(0.1f);
            LoadNextScene();
        }
    }

    public void OnSkipButton()
    {
        isSkipping = true;

        if (narrationAudio != null && narrationAudio.isPlaying)
        {
            narrationAudio.Stop();
        }

#if UNITY_INCLUDE_TESTS
        if (DisableSceneLoadingForTests)
        {
            // Still disable canvas during tests
            if (gameWorldCanvas != null)
                gameWorldCanvas.enabled = false;

            return; // Do not load scenes during tests
        }
#endif

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        // Immediately hide the GameWorldContext UI to prevent flashing
        if (gameWorldCanvas != null)
        {
            gameWorldCanvas.enabled = false;
        }

#if UNITY_INCLUDE_TESTS
        if (DisableSceneLoadingForTests)
            return;
#endif

        if (LoadingScreenController.Instance != null)
        {
            LoadingScreenController.Instance.LoadScene("Oak Woods Of Syntax");
        }
        else
        {
            LoadingScreenController.TargetSceneName = "Oak Woods Of Syntax";
            SceneManager.LoadScene("Loading Screen");
        }
    }
}
