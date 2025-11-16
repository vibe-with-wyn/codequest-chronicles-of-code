using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameWorldContextController : MonoBehaviour
{
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

        if (narrationAudio != null)
        {
            narrationAudio.Play();
            Debug.Log("Narration audio started");
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
            yield return new WaitForSeconds(0.7f);
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
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        // Immediately hide the GameWorldContext UI to prevent flashing
        if (gameWorldCanvas != null)
        {
            gameWorldCanvas.enabled = false;
        }

        if (LoadingScreenController.Instance != null)
        {
            LoadingScreenController.Instance.LoadScene("Oak Woods Of Syntax");
        }
        else
        {
            // fallback if somehow missing
            LoadingScreenController.TargetSceneName = "Oak Woods Of Syntax";
            SceneManager.LoadScene("Loading Screen");
        }
    }

}