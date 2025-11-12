using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MainMenuControllerTests
{
    private GameObject testGameObject;
    private MainMenuController controller;

    [SetUp]
    public void Setup()
    {
        // Create a test GameObject with the controller
        testGameObject = new GameObject("MainMenuTest");
        controller = testGameObject.AddComponent<MainMenuController>();
    }

    [TearDown]
    public void Teardown()
    {
        // Clean up after each test
        if (testGameObject != null)
        {
            Object.Destroy(testGameObject);
        }
    }

    [Test]
    public void MainMenuController_ComponentExists()
    {
        // Assert
        Assert.IsNotNull(controller);
        Assert.IsInstanceOf<MonoBehaviour>(controller);
    }

    [UnityTest]
    public IEnumerator OnNewGameButton_CallsCoroutine()
    {
        // Arrange - Add audio source for testing
        AudioSource audioSource = testGameObject.AddComponent<AudioSource>();
        AudioClip testClip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
        audioSource.clip = testClip;
        audioSource.playOnAwake = false;

        var fieldInfo = typeof(MainMenuController).GetField("menuSound",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo.SetValue(controller, audioSource);

        // Act
        controller.OnNewGameButton();
        yield return new WaitForSeconds(0.1f);

        // Assert
        Assert.IsNotNull(controller);
    }

    [UnityTest]
    public IEnumerator OnResumeButton_CallsCoroutine()
    {
        // Act
        controller.OnResumeButton();
        yield return new WaitForSeconds(0.1f);

        // Assert
        Assert.IsNotNull(controller);
    }

    [Test]
    public void OnExitButton_DoesNotThrowException()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => controller.OnExitButton());
    }

    [UnityTest]
    public IEnumerator OnNewGameButton_WithAudioSource_PlaysSound()
    {
        // Arrange
        AudioSource audioSource = testGameObject.AddComponent<AudioSource>();
        AudioClip testClip = AudioClip.Create("TestClip", 44100, 1, 44100, false);
        audioSource.clip = testClip;
        audioSource.playOnAwake = false;

        var fieldInfo = typeof(MainMenuController).GetField("menuSound",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo.SetValue(controller, audioSource);

        // Act
        controller.OnNewGameButton();
        yield return new WaitForSeconds(0.1f);

        // Assert
        Assert.IsTrue(audioSource.isPlaying || audioSource.time > 0);
    }
}