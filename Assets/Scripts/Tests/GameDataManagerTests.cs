using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// Complete & corrected tests for GameDataManager
/// Covers: singleton, data defaults, progress, checkpoint, persistence,
/// intro flags, scene management, reset workflow, and full game flow.
/// </summary>
public class GameDataManagerTests
{
    private GameObject managerObject;
    private GameDataManager manager;

    // ============================
    // Test Setup / Teardown
    // ============================

    [SetUp]
    public void Setup()
    {
        // Clear PlayerPrefs to ensure a clean environment
        PlayerPrefs.DeleteAll();

        // Reset static singleton before each test
        ResetSingleton();

        // Instantiate new GameDataManager
        managerObject = new GameObject("GameDataManager");
        manager = managerObject.AddComponent<GameDataManager>();
    }

    [TearDown]
    public void Teardown()
    {
        if (managerObject != null)
            Object.DestroyImmediate(managerObject);

        PlayerPrefs.DeleteAll();
        ResetSingleton();
    }

    private void ResetSingleton()
    {
        var instanceField = typeof(GameDataManager)
            .GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        instanceField?.SetValue(null, null);
    }

    // ============================
    // Singleton Tests
    // ============================

    [Test]
    public void Singleton_CreatesInstance()
    {
        Assert.IsNotNull(GameDataManager.Instance);
        Assert.AreSame(manager, GameDataManager.Instance);
    }

    // ============================
    // Constants Tests
    // ============================

    [Test]
    public void GameConstants_ReturnJavaAndRoran()
    {
        Assert.AreEqual("Java", manager.SelectedLanguage);
        Assert.AreEqual("Roran", manager.SelectedCharacter);
    }

    // ============================
    // Progress Tests
    // ============================

    [Test]
    public void ProgressLevel_StartsAtOne()
    {
        Assert.AreEqual(1, manager.ProgressLevel);
    }

    [Test]
    public void UpdateProgress_UpdatesCorrectly()
    {
        manager.UpdateProgress(5);
        Assert.AreEqual(5, manager.ProgressLevel);
    }

    [Test]
    public void UpdateProgress_RejectsInvalidValues()
    {
        int initial = manager.ProgressLevel;

        manager.UpdateProgress(0);
        Assert.AreEqual(initial, manager.ProgressLevel);

        manager.UpdateProgress(-10);
        Assert.AreEqual(initial, manager.ProgressLevel);
    }

    // ============================
    // Scene Management Tests
    // ============================

    [Test]
    public void LastScene_DefaultsCorrectly()
    {
        Assert.AreEqual("Oak Woods Of Syntax", manager.LastScene);
    }

    [Test]
    public void SetLastScene_UpdatesCorrectly()
    {
        manager.SetLastScene("BattleScene");
        Assert.AreEqual("BattleScene", manager.LastScene);
    }

    [Test]
    public void SetLastScene_RejectsInvalid()
    {
        string initial = manager.LastScene;

        manager.SetLastScene(null);
        Assert.AreEqual(initial, manager.LastScene);

        manager.SetLastScene("");
        Assert.AreEqual(initial, manager.LastScene);
    }

    // ============================
    // Starting Position Tests
    // ============================

    [Test]
    public void GetStartingPosition_ReturnsDefault()
    {
        Vector3 pos = manager.GetStartingPosition();

        Assert.AreEqual(-77.98f, pos.x, 0.01f);
        Assert.AreEqual(-18.31f, pos.y, 0.01f);
        Assert.AreEqual(0f, pos.z, 0.01f);
    }

    [Test]
    public void SetStartingPosition_Vector2_Works()
    {
        manager.SetStartingPosition(new Vector2(10f, 20f));
        Vector3 pos = manager.GetStartingPosition();

        Assert.AreEqual(10f, pos.x, 0.01f);
        Assert.AreEqual(20f, pos.y, 0.01f);
    }

    [Test]
    public void SetStartingPosition_Vector3_Works()
    {
        manager.SetStartingPosition(new Vector3(30f, 40f, 999f));
        Vector3 pos = manager.GetStartingPosition();

        Assert.AreEqual(30f, pos.x, 0.01f);
        Assert.AreEqual(40f, pos.y, 0.01f);
        Assert.AreEqual(0f, pos.z, 0.01f);
    }

    // ============================
    // Checkpoint Tests
    // ============================

    [Test]
    public void CurrentRespawn_UsesStartingPositionInitially()
    {
        Vector3 respawn = manager.CurrentRespawnPoint;
        Vector3 start = manager.GetStartingPosition();

        Assert.AreEqual(start.x, respawn.x, 0.01f);
        Assert.AreEqual(start.y, respawn.y, 0.01f);
    }

    [Test]
    public void UpdateCheckpoint_UpdatesRespawnPoint()
    {
        manager.UpdateCheckpoint(new Vector3(50f, 75f, 0));

        Assert.AreEqual(50f, manager.CurrentRespawnPoint.x, 0.01f);
        Assert.AreEqual(75f, manager.CurrentRespawnPoint.y, 0.01f);
    }

    [Test]
    public void UpdateCheckpoint_WithScene_UpdatesBoth()
    {
        manager.UpdateCheckpoint(new Vector3(20f, 30f, 0), "TestScene");

        Assert.AreEqual(20f, manager.CurrentRespawnPoint.x, 0.01f);
        Assert.AreEqual("TestScene", manager.LastScene);
    }

    // ============================
    // Intro Flag Tests
    // ============================

    [Test]
    public void IntroFlag_DefaultsToFalse()
    {
        Assert.IsFalse(manager.HasPlayedIntro);
    }

    [Test]
    public void IntroFlag_CanBeSet()
    {
        manager.HasPlayedIntro = true;
        Assert.IsTrue(manager.HasPlayedIntro);

        manager.HasPlayedIntro = false;
        Assert.IsFalse(manager.HasPlayedIntro);
    }

    // ============================
    // Player Data Tests
    // ============================

    [Test]
    public void GetPlayerData_ReturnsCorrectDefaults()
    {
        PlayerData data = manager.GetPlayerData();

        Assert.IsNotNull(data);
        Assert.AreEqual("Java", data.SelectedLanguage);
        Assert.AreEqual("Roran", data.SelectedCharacter);
        Assert.AreEqual(1, data.ProgressLevel);
    }

    [Test]
    public void GetPlayerData_ReflectsChanges()
    {
        manager.UpdateProgress(4);
        manager.SetLastScene("DeepForest");
        manager.UpdateCheckpoint(new Vector3(90f, 120f, 0));

        PlayerData data = manager.GetPlayerData();

        Assert.AreEqual(4, data.ProgressLevel);
        Assert.AreEqual("DeepForest", data.LastScene);
        Assert.AreEqual(90f, data.RespawnPoint.x, 0.01f);
    }

    // ============================
    // Reset Tests
    // ============================

    [Test]
    public void ResetPlayerData_RestoresDefaults()
    {
        manager.UpdateProgress(10);
        manager.SetLastScene("BossRoom");
        manager.UpdateCheckpoint(new Vector3(100f, 200f, 0));
        manager.HasPlayedIntro = true;

        manager.ResetPlayerData();

        Assert.AreEqual(1, manager.ProgressLevel);
        Assert.AreEqual("Oak Woods Of Syntax", manager.LastScene);
        Assert.IsFalse(manager.HasPlayedIntro);
    }

    [Test]
    public void ResetPlayerData_RestoresStartingPosition()
    {
        manager.UpdateCheckpoint(new Vector3(90f, 100f, 0));
        manager.ResetPlayerData();

        Vector3 respawn = manager.CurrentRespawnPoint;
        Vector3 start = manager.GetStartingPosition();

        Assert.AreEqual(start.x, respawn.x, 0.01f);
        Assert.AreEqual(start.y, respawn.y, 0.01f);
    }

    // ============================
    // Persistence Test
    // ============================

    [UnityTest]
    public IEnumerator Persistence_SavesAndLoadsData()
    {
        manager.UpdateProgress(7);
        manager.UpdateCheckpoint(new Vector3(44f, 88f, 0));
        manager.SetLastScene("PersistedLevel");
        manager.HasPlayedIntro = true;

        yield return null;

        Object.DestroyImmediate(managerObject);
        yield return null;

        // Recreate manager
        managerObject = new GameObject("GameDataManager");
        manager = managerObject.AddComponent<GameDataManager>();

        yield return null;

        Assert.AreEqual(7, manager.ProgressLevel);
        Assert.AreEqual("PersistedLevel", manager.LastScene);
        Assert.AreEqual(44f, manager.CurrentRespawnPoint.x, 0.01f);
        Assert.AreEqual(88f, manager.CurrentRespawnPoint.y, 0.01f);
        Assert.IsTrue(manager.HasPlayedIntro);
    }

    // ============================
    // Full Game Flow
    // ============================

    [UnityTest]
    public IEnumerator CompleteFlow_GameProgression()
    {
        manager.ResetPlayerData();
        yield return null;

        manager.UpdateProgress(2);
        manager.UpdateCheckpoint(new Vector3(10f, 20f, 0), "Oak Woods Of Syntax");
        yield return null;

        manager.UpdateProgress(3);
        manager.UpdateCheckpoint(new Vector3(30f, 40f, 0));
        yield return null;

        Assert.AreEqual(3, manager.ProgressLevel);
        Assert.AreEqual(30f, manager.CurrentRespawnPoint.x, 0.01f);
        Assert.AreEqual("Oak Woods Of Syntax", manager.LastScene);
    }
}
