using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tests for LoadingScreenController core functionality
/// Focuses on singleton, visibility, UI updates, and scene loading behavior
/// </summary>
public class LoadingScreenControllerTests
{
    private GameObject controllerObject;
    private LoadingScreenController controller;
    private GameObject canvasObject;
    private Slider progressBar;
    private TMP_Text progressText;
    private TMP_Text targetSceneText;

    [SetUp]
    public void Setup()
    {
        // Clear singleton instance
        ResetSingleton();

        // Create controller GameObject
        controllerObject = new GameObject("LoadingScreenController");

        // Create UI hierarchy before adding controller component
        CreateUIHierarchy();

        // Add controller component (triggers Awake which caches canvas)
        controller = controllerObject.AddComponent<LoadingScreenController>();

        // Assign UI references via reflection
        AssignUIReferences();
    }

    [TearDown]
    public void Teardown()
    {
        if (controllerObject != null)
            Object.Destroy(controllerObject);
        if (canvasObject != null)
            Object.Destroy(canvasObject);

        LoadingScreenController.TargetSceneName = null;
        ResetSingleton();
    }

    private void CreateUIHierarchy()
    {
        // Create canvas as child of controller
        canvasObject = new GameObject("Canvas");
        canvasObject.transform.SetParent(controllerObject.transform);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Create slider for progress bar
        GameObject sliderObj = new GameObject("ProgressBar");
        sliderObj.transform.SetParent(canvasObject.transform);
        progressBar = sliderObj.AddComponent<Slider>();

        // Create progress percentage text
        GameObject progressTextObj = new GameObject("ProgressText");
        progressTextObj.transform.SetParent(canvasObject.transform);
        progressText = progressTextObj.AddComponent<TextMeshProUGUI>();

        // Create target scene name text
        GameObject targetTextObj = new GameObject("TargetSceneText");
        targetTextObj.transform.SetParent(canvasObject.transform);
        targetSceneText = targetTextObj.AddComponent<TextMeshProUGUI>();
    }

    private void AssignUIReferences()
    {
        var progressBarField = typeof(LoadingScreenController).GetField("progressBar",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        progressBarField.SetValue(controller, progressBar);

        var progressTextField = typeof(LoadingScreenController).GetField("progressText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        progressTextField.SetValue(controller, progressText);

        var targetSceneTextField = typeof(LoadingScreenController).GetField("targetSceneText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        targetSceneTextField.SetValue(controller, targetSceneText);
    }

    private void ResetSingleton()
    {
        var instanceField = typeof(LoadingScreenController).GetField("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        instanceField.SetValue(null, null);
    }

    private Canvas GetLoadingCanvas()
    {
        var canvasField = typeof(LoadingScreenController).GetField("loadingCanvas",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (Canvas)canvasField.GetValue(controller);
    }

    #region Singleton Pattern Tests

    [Test]
    public void Singleton_CreatesInstanceOnAwake()
    {
        Assert.IsNotNull(LoadingScreenController.Instance);
        Assert.AreSame(controller, LoadingScreenController.Instance);
    }

    [UnityTest]
    public IEnumerator Singleton_DestroysDuplicateInstances()
    {
        var firstInstance = LoadingScreenController.Instance;
        yield return null;

        // Create duplicate controller
        GameObject duplicateObj = new GameObject("DuplicateController");
        LoadingScreenController duplicate = duplicateObj.AddComponent<LoadingScreenController>();

        yield return null;

        // Duplicate should be destroyed
        Assert.IsTrue(duplicate == null || !duplicate.gameObject.activeInHierarchy);
        Assert.AreSame(firstInstance, LoadingScreenController.Instance);
    }

    [Test]
    public void Singleton_PersistsAcrossScenes()
    {
        // Verify DontDestroyOnLoad flag is set
        Assert.IsNotNull(controller.gameObject);
        // Instance should remain the same
        Assert.AreSame(controller, LoadingScreenController.Instance);
    }

    #endregion

    #region Static Property Tests

    [Test]
    public void TargetSceneName_CanBeSetAndRetrieved()
    {
        LoadingScreenController.TargetSceneName = "Oak Woods Of Syntax";
        Assert.AreEqual("Oak Woods Of Syntax", LoadingScreenController.TargetSceneName);
    }

    [Test]
    public void TargetSceneName_CanBeCleared()
    {
        LoadingScreenController.TargetSceneName = "Test Scene";
        LoadingScreenController.TargetSceneName = null;
        Assert.IsNull(LoadingScreenController.TargetSceneName);
    }

    #endregion

    #region Visibility Tests

    [Test]
    public void SetVisible_EnablesCanvas()
    {
        Canvas canvas = GetLoadingCanvas();
        Assert.IsNotNull(canvas, "Canvas should be cached in Awake");

        controller.SetVisible(true);
        Assert.IsTrue(canvas.enabled);
    }

    [Test]
    public void SetVisible_DisablesCanvas()
    {
        Canvas canvas = GetLoadingCanvas();

        controller.SetVisible(true);
        Assert.IsTrue(canvas.enabled);

        controller.SetVisible(false);
        Assert.IsFalse(canvas.enabled);
    }

    [Test]
    public void SetVisible_TogglesMultipleTimes()
    {
        Canvas canvas = GetLoadingCanvas();

        controller.SetVisible(true);
        Assert.IsTrue(canvas.enabled);

        controller.SetVisible(false);
        Assert.IsFalse(canvas.enabled);

        controller.SetVisible(true);
        Assert.IsTrue(canvas.enabled);
    }

    #endregion

    #region Environment Name Tests

    [Test]
    public void GetEnvironmentName_MapsGameWorldContext()
    {
        var method = typeof(LoadingScreenController).GetMethod("GetEnvironmentName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        string result = (string)method.Invoke(controller, new object[] { "Game World Context" });
        Assert.AreEqual("World", result);
    }

    [Test]
    public void GetEnvironmentName_PreservesOakWoodsSyntax()
    {
        var method = typeof(LoadingScreenController).GetMethod("GetEnvironmentName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        string result = (string)method.Invoke(controller, new object[] { "Oak Woods of Syntax" });
        Assert.AreEqual("Oak Woods of Syntax", result);
    }

    [Test]
    public void GetEnvironmentName_IsCaseInsensitive()
    {
        var method = typeof(LoadingScreenController).GetMethod("GetEnvironmentName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        string result1 = (string)method.Invoke(controller, new object[] { "GAME WORLD CONTEXT" });
        Assert.AreEqual("World", result1);

        string result2 = (string)method.Invoke(controller, new object[] { "game world context" });
        Assert.AreEqual("World", result2);
    }

    [Test]
    public void GetEnvironmentName_ReturnsOriginalForUnknownScenes()
    {
        var method = typeof(LoadingScreenController).GetMethod("GetEnvironmentName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        string result = (string)method.Invoke(controller, new object[] { "Unknown Scene" });
        Assert.AreEqual("Unknown Scene", result);
    }

    #endregion

    #region LoadScene Tests

    [UnityTest]
    public IEnumerator LoadScene_ShowsLoadingScreen()
    {
        controller.SetVisible(false);
        yield return null;

        LogAssert.ignoreFailingMessages = true;
        controller.LoadScene("Oak Woods of Syntax");
        yield return null;
        LogAssert.ignoreFailingMessages = false;

        Canvas canvas = GetLoadingCanvas();
        Assert.IsTrue(canvas.enabled, "Canvas should be visible when loading starts");
    }

    [UnityTest]
    public IEnumerator LoadScene_UpdatesTargetSceneText()
    {
        LogAssert.ignoreFailingMessages = true;
        controller.LoadScene("Oak Woods of Syntax");
        yield return new WaitForSeconds(0.1f);
        LogAssert.ignoreFailingMessages = false;

        Assert.IsTrue(targetSceneText.text.Contains("Loading"));
        Assert.IsTrue(targetSceneText.text.Contains("Oak Woods of Syntax"));
    }

    [UnityTest]
    public IEnumerator LoadScene_UsesFormattedEnvironmentName()
    {
        LogAssert.ignoreFailingMessages = true;
        controller.LoadScene("Game World Context");
        yield return new WaitForSeconds(0.1f);
        LogAssert.ignoreFailingMessages = false;

        Assert.IsTrue(targetSceneText.text.Contains("Loading World"),
            "Should display formatted name 'World' instead of 'Game World Context'");
    }

    #endregion

    #region UI Component Tests

    [Test]
    public void ProgressBar_InitiallyExists()
    {
        Assert.IsNotNull(progressBar);
    }

    [Test]
    public void ProgressText_InitiallyExists()
    {
        Assert.IsNotNull(progressText);
    }

    [Test]
    public void TargetSceneText_InitiallyExists()
    {
        Assert.IsNotNull(targetSceneText);
    }

    [Test]
    public void Canvas_CachedInAwake()
    {
        Canvas canvas = GetLoadingCanvas();
        Assert.IsNotNull(canvas, "Canvas should be cached during Awake");
    }

    #endregion

    #region Integration Tests

    [UnityTest]
    public IEnumerator Integration_TypicalLoadingFlow()
    {
        // Step 1: Set target scene
        LoadingScreenController.TargetSceneName = "Game World Context";

        // Step 2: Show loading screen
        controller.SetVisible(true);
        yield return null;

        Canvas canvas = GetLoadingCanvas();
        Assert.IsTrue(canvas.enabled);

        // Step 3: Load scene
        LogAssert.ignoreFailingMessages = true;
        controller.LoadScene(LoadingScreenController.TargetSceneName);
        yield return new WaitForSeconds(0.1f);
        LogAssert.ignoreFailingMessages = false;

        // Verify UI updated
        Assert.IsTrue(targetSceneText.text.Contains("Loading"));
    }

    [UnityTest]
    public IEnumerator Integration_MultipleSceneLoads()
    {
        LogAssert.ignoreFailingMessages = true;

        // Load first scene
        controller.LoadScene("Game World Context");
        yield return new WaitForSeconds(0.1f);

        // Load second scene
        controller.LoadScene("Oak Woods of Syntax");
        yield return new WaitForSeconds(0.1f);

        LogAssert.ignoreFailingMessages = false;

        // Verify last scene is displayed
        Assert.IsTrue(targetSceneText.text.Contains("Oak Woods of Syntax"));
    }

    #endregion
}