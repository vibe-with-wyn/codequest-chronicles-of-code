using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TapToStartController : MonoBehaviour
{
    [SerializeField] private AudioSource tapSound;

    public void OnTap()
    {
        StartCoroutine(HandleTap());
    }

    private IEnumerator HandleTap()
    {
        if (tapSound != null)
        {
            tapSound.Play();
            yield return new WaitForSeconds(tapSound.clip.length);
            Debug.Log($"Sound played for {tapSound.clip.length} seconds");
        }
        SceneManager.LoadScene("Main Menu");
    }
}