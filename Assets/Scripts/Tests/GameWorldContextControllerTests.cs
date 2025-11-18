using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Simplified tests for GameWorldContextController
/// Focuses on core narration, skip, and canvas functionality only
/// </summary>
public class GameWorldContextControllerTests
{
    private GameObject root;
    private GameWorldContextController controller;

    private AudioSource narration;
    private Canvas canvas;
    private Button skipButton;

    [SetUp]
    public void Setup()
    {
#if UNITY_INCLUDE_TESTS
        GameWorldContextController.DisableSceneLoadingForTests = true;
#endif

        // Root object for the controller
        root = new GameObject("Root");
        root.SetActive(false); // Prevent Start from auto-running early

        // Child: Audio
        var audioObj = new GameObject("Audio");
        audioObj.transform.SetParent(root.transform);
        narration = audioObj.AddComponent<AudioSource>();
        narration.clip = AudioClip.Create("TestClip", 44100, 1, 44100, false);

        // Child: Canvas
        var canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(root.transform);
        canvas = canvasObj.AddComponent<Canvas>();

        // Child: Button
        var btnObj = new GameObject("SkipButton");
        btnObj.transform.SetParent(canvasObj.transform);
        skipButton = btnObj.AddComponent<Button>();

        // Add controller
        controller = root.AddComponent<GameWorldContextController>();

        // Assign serialized fields
        SetPrivate(controller, "narrationAudio", narration);
        SetPrivate(controller, "skipButton", skipButton);
        SetPrivate(controller, "gameWorldCanvas", canvas);

        root.SetActive(true); // Now Start() will run
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(root);
    }

    // Helper for private field injection
    private void SetPrivate(object target, string name, object value)
    {
        var field = target.GetType().GetField(
            name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(target, value);
    }

    private object GetPrivate(object target, string name)
    {
        var field = target.GetType().GetField(
            name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field.GetValue(target);
    }

    // --------------------------------------------------------------------
    // Narration Tests
    // --------------------------------------------------------------------

    [UnityTest]
    public IEnumerator Narration_StartsOnStart()
    {
        yield return null;
        Assert.IsTrue(narration.isPlaying, "Narration should play on Start()");
    }

    [UnityTest]
    public IEnumerator Narration_StopsOnSkip()
    {
        yield return null;

        controller.OnSkipButton();
        yield return null;

        Assert.IsFalse(narration.isPlaying, "Narration should stop when skipped");
    }

    // --------------------------------------------------------------------
    // Skip Tests
    // --------------------------------------------------------------------

    [UnityTest]
    public IEnumerator SkipButton_SetsIsSkippingFlag()
    {
        yield return null;

        controller.OnSkipButton();
        yield return null;

        bool isSkipping = (bool)GetPrivate(controller, "isSkipping");

        Assert.IsTrue(isSkipping, "isSkipping should become true");
    }

    // --------------------------------------------------------------------
    // Canvas Tests
    // --------------------------------------------------------------------

    [UnityTest]
    public IEnumerator Canvas_Disabled_WhenSkipping()
    {
        yield return null;
        Assert.IsTrue(canvas.enabled, "Canvas should start enabled");

        controller.OnSkipButton();
        yield return null;

        Assert.IsFalse(canvas.enabled, "Canvas should be disabled after skipping");
    }

    [UnityTest]
    public IEnumerator Canvas_IsAutoDetectedIfNotAssigned()
    {
        // Remove the assigned canvas
        SetPrivate(controller, "gameWorldCanvas", null);

        // Recreate controller cleanly
        Object.DestroyImmediate(controller);
        controller = root.AddComponent<GameWorldContextController>();
        yield return null;

        var foundCanvas = GetPrivate(controller, "gameWorldCanvas");

        Assert.IsNotNull(foundCanvas, "Controller should find canvas automatically");
        Assert.AreSame(canvas, foundCanvas, "It should detect the correct canvas");
    }
}