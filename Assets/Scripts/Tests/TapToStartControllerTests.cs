using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TapToStartControllerTests
{
    private GameObject testGameObject;
    private TapToStartController controller;
    private bool sceneLoaded;
    private string loadedSceneName;

    [SetUp]
    public void Setup()
    {
        // Create a test GameObject with the controller
        testGameObject = new GameObject("TapToStartTest");
        controller = testGameObject.AddComponent<TapToStartController>();
        
        // Reset scene tracking
        sceneLoaded = false;
        loadedSceneName = string.Empty;
        
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [TearDown]
    public void Teardown()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Clean up after each test
        if (testGameObject != null)
        {
            Object.Destroy(testGameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneLoaded = true;
        loadedSceneName = scene.name;
        Debug.Log($"Scene loaded in test: {scene.name}");
    }

    [UnityTest]
    public IEnumerator OnTap_WithoutAudioSource_LoadsMainMenuScene()
    {
        // Arrange
        string expectedScene = "Main Menu";
        string initialScene = SceneManager.GetActiveScene().name;

        // Act
        controller.OnTap();
        
        // Wait for scene loading to complete (max 5 seconds)
        float timeout = 5f;
        float elapsed = 0f;
        
        while (!sceneLoaded && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Assert
        Assert.IsTrue(sceneLoaded, 
            $"Scene should have loaded within {timeout} seconds. Make sure '{expectedScene}' is added to Build Settings.");
        
        Assert.AreEqual(expectedScene, loadedSceneName, 
            $"Expected scene '{expectedScene}' but loaded '{loadedSceneName}'");
        
        Debug.Log($"✓ Test passed: Successfully loaded '{loadedSceneName}' from '{initialScene}'");
    }

    [UnityTest]
    public IEnumerator OnTap_WithAudioSource_PlaysAudioBeforeSceneLoad()
    {
        // Arrange
        AudioSource audioSource = testGameObject.AddComponent<AudioSource>();
        AudioClip testClip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
        audioSource.clip = testClip;
        audioSource.playOnAwake = false;

        // Use reflection to set the private tapSound field
        var fieldInfo = typeof(TapToStartController).GetField("tapSound",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo.SetValue(controller, audioSource);

        // Act
        controller.OnTap();
        yield return new WaitForSeconds(0.1f);

        // Assert
        Assert.IsTrue(audioSource.isPlaying || audioSource.time > 0, 
            "Audio should be playing or have started playing");
    }

    [Test]
    public void TapToStartController_ComponentExists()
    {
        // Assert
        Assert.IsNotNull(controller, "Controller should not be null");
        Assert.IsInstanceOf<MonoBehaviour>(controller, "Controller should be a MonoBehaviour");
    }
}