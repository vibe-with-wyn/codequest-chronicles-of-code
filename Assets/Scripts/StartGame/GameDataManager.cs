using UnityEngine;

/// <summary>
/// Simplified Game Data Manager for CodeQuest: Oak Woods of Syntax
/// Manages persistent player data for Roran (Swordsman) learning Java
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    
    [Header("Game Configuration - Oak Woods of Syntax")]
    [Tooltip("Starting position in Oak Woods when player begins new game (X, Y coordinates)")]
    [SerializeField] private Vector2 startingPosition = new Vector2(-77.98f, -18.31f);
    
    private PlayerData playerData;
    private const string DATA_VERSION = "1.0";
    private const string PLAYER_DATA_KEY = "PlayerData";
    private const string INTRO_PLAYED_KEY = "HasPlayedIntro";

    // === GAME CONSTANTS - SIMPLIFIED ===
    // Only Java language and Roran character are implemented
    public string SelectedLanguage => "Java";
    public string SelectedCharacter => "Roran";
    
    // === PLAYER PROGRESS ===
    public int ProgressLevel => playerData?.ProgressLevel ?? 1;
    public string LastScene => playerData?.LastScene ?? "Oak Woods Of Syntax";
    
    // === SPAWN/CHECKPOINT SYSTEM ===
    /// <summary>
    /// Current respawn point (last checkpoint reached)
    /// Falls back to starting position if no checkpoint saved
    /// </summary>
    public Vector3 CurrentRespawnPoint => playerData?.RespawnPoint ?? GetStartingPosition();
    
    /// <summary>
    /// Gets the starting position as Vector3 (for new games)
    /// </summary>
    public Vector3 GetStartingPosition() => new Vector3(startingPosition.x, startingPosition.y, 0f);
    
    /// <summary>
    /// Gets the starting position as Vector2 (for configuration)
    /// </summary>
    public Vector2 StartingPosition => startingPosition;

    // === INTRO TRACKING ===
    /// <summary>
    /// Track if intro sequence has been played
    /// Persists independently from player data
    /// </summary>
    public bool HasPlayedIntro
    {
        get => PlayerPrefs.GetInt(INTRO_PLAYED_KEY, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(INTRO_PLAYED_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[GameDataManager] HasPlayedIntro set to: {value}");
        }
    }

    private void Awake()
    {
        // Singleton pattern - persist across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPlayerData();
            Debug.Log($"[GameDataManager] Initialized - Language: Java, Character: Roran, Start Position: {GetStartingPosition()}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========== CHECKPOINT MANAGEMENT ==========
    
    /// <summary>
    /// Updates the player's checkpoint/respawn point
    /// Called by CheckpointController when player reaches a checkpoint
    /// Syncs with PlayerPrefs for redundancy
    /// </summary>
    public void UpdateCheckpoint(Vector3 checkpointPosition, string sceneName = null)
    {
        if (playerData == null)
        {
            Debug.LogError("[GameDataManager] PlayerData is null! Cannot update checkpoint.");
            return;
        }

        playerData.RespawnPoint = checkpointPosition;
        
        if (!string.IsNullOrEmpty(sceneName))
        {
            playerData.LastScene = sceneName;
        }
        
        SavePlayerData();
        
        // Also save to PlayerPrefs as backup (your CheckpointController does this too)
        PlayerPrefs.SetFloat("RespawnX", checkpointPosition.x);
        PlayerPrefs.SetFloat("RespawnY", checkpointPosition.y);
        PlayerPrefs.SetFloat("RespawnZ", checkpointPosition.z);
        PlayerPrefs.Save();
        
        Debug.Log($"[GameDataManager] Checkpoint updated to: ({checkpointPosition.x:F2}, {checkpointPosition.y:F2}, {checkpointPosition.z:F2}) in scene: {playerData.LastScene}");
    }

    // ========== PROGRESS MANAGEMENT ==========
    
    /// <summary>
    /// Updates the player's progress level (e.g., quest stage, chapter)
    /// </summary>
    public void UpdateProgress(int level)
    {
        if (level < 1)
        {
            Debug.LogWarning($"[GameDataManager] Invalid progress level: {level}. Must be >= 1.");
            return;
        }

        if (playerData != null)
        {
            playerData.ProgressLevel = level;
            SavePlayerData();
            Debug.Log($"[GameDataManager] Progress updated to level: {level}");
        }
    }

    /// <summary>
    /// Sets the last scene the player was in
    /// </summary>
    public void SetLastScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GameDataManager] Attempted to set last scene to null or empty");
            return;
        }

        if (playerData != null)
        {
            playerData.LastScene = sceneName;
            SavePlayerData();
            Debug.Log($"[GameDataManager] Last scene set to: {sceneName}");
        }
    }

    // ========== DATA ACCESS ==========
    
    /// <summary>
    /// Gets the complete player data (use for direct access if needed)
    /// </summary>
    public PlayerData GetPlayerData()
    {
        return playerData;
    }

    // ========== NEW GAME / RESET ==========
    
    /// <summary>
    /// Resets all player data to defaults (new game)
    /// Sets respawn to starting position
    /// Clears intro flag
    /// </summary>
    public void ResetPlayerData()
    {
        Vector3 startPos = GetStartingPosition();
        playerData = new PlayerData("Java", "Roran", 1, "Oak Woods Of Syntax", startPos);
        HasPlayedIntro = false;
        
        // Clear PlayerPrefs checkpoint backup
        PlayerPrefs.DeleteKey("RespawnX");
        PlayerPrefs.DeleteKey("RespawnY");
        PlayerPrefs.DeleteKey("RespawnZ");
        
        SavePlayerData();
        
        Debug.Log($"[GameDataManager] Player data reset. Starting position: {startPos}");
    }

    /// <summary>
    /// Sets the starting position for new games (can be called from Inspector or code)
    /// </summary>
    public void SetStartingPosition(Vector2 position)
    {
        startingPosition = position;
        Debug.Log($"[GameDataManager] Starting position set to: ({position.x:F2}, {position.y:F2})");
    }
    
    /// <summary>
    /// Sets the starting position using Vector3 (ignores Z)
    /// </summary>
    public void SetStartingPosition(Vector3 position)
    {
        startingPosition = new Vector2(position.x, position.y);
        Debug.Log($"[GameDataManager] Starting position set to: ({position.x:F2}, {position.y:F2})");
    }

    // ========== SAVE/LOAD SYSTEM ==========
    
    /// <summary>
    /// Saves current player data to PlayerPrefs as JSON
    /// </summary>
    private void SavePlayerData()
    {
        if (playerData == null)
        {
            Debug.LogError("[GameDataManager] Cannot save null playerData!");
            return;
        }

        string json = JsonUtility.ToJson(playerData);
        PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
        PlayerPrefs.SetString("DataVersion", DATA_VERSION);
        PlayerPrefs.Save();
        
        Debug.Log("[GameDataManager] Player data saved successfully");
    }

    /// <summary>
    /// Loads player data from PlayerPrefs or creates new data if none exists
    /// </summary>
    private void LoadPlayerData()
    {
        string json = PlayerPrefs.GetString(PLAYER_DATA_KEY, "");
        
        if (string.IsNullOrEmpty(json))
        {
            // No existing data - create new with starting position
            Vector3 startPos = GetStartingPosition();
            playerData = new PlayerData("Java", "Roran", 1, "Oak Woods Of Syntax", startPos);
            SavePlayerData();
            
            Debug.Log($"[GameDataManager] Created new player data with starting position: {startPos}");
        }
        else
        {
            // Load existing data
            playerData = JsonUtility.FromJson<PlayerData>(json);
            
            // Validate and fix any potential issues
            if (string.IsNullOrEmpty(playerData.SelectedLanguage))
                playerData.SelectedLanguage = "Java";
            
            if (string.IsNullOrEmpty(playerData.SelectedCharacter))
                playerData.SelectedCharacter = "Roran";
            
            Debug.Log($"[GameDataManager] Loaded player data - Language: {playerData.SelectedLanguage}, " +
                      $"Character: {playerData.SelectedCharacter}, Progress: {playerData.ProgressLevel}, " +
                      $"Scene: {playerData.LastScene}, Respawn: {playerData.RespawnPoint}");
        }
    }

    // ========== DEBUG TOOLS (Right-click component in Inspector) ==========
    
    [ContextMenu("Debug - Print Current Data")]
    private void DebugPrintData()
    {
        if (playerData != null)
        {
            Debug.Log("╔═══════════════════════════════════════╗");
            Debug.Log("║     CURRENT PLAYER DATA (DEBUG)       ║");
            Debug.Log("╠═══════════════════════════════════════╣");
            Debug.Log($"║ Language:       {playerData.SelectedLanguage}");
            Debug.Log($"║ Character:      {playerData.SelectedCharacter}");
            Debug.Log($"║ Progress Level: {playerData.ProgressLevel}");
            Debug.Log($"║ Last Scene:     {playerData.LastScene}");
            Debug.Log($"║ Respawn Point:  {playerData.RespawnPoint}");
            Debug.Log($"║ Has Intro:      {HasPlayedIntro}");
            Debug.Log($"║ Start Position: {GetStartingPosition()}");
            Debug.Log("╚═══════════════════════════════════════╝");
        }
        else
        {
            Debug.LogWarning("PlayerData is null!");
        }
    }

    [ContextMenu("Debug - Reset All Data")]
    private void DebugResetData()
    {
        ResetPlayerData();
        Debug.Log("[GameDataManager] ⚠️ All data reset via debug menu");
    }
    
    [ContextMenu("Debug - Clear All PlayerPrefs")]
    private void DebugClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.LogWarning("[GameDataManager] ⚠️ ALL PlayerPrefs cleared! Reload scene to reinitialize.");
    }
    
    [ContextMenu("Debug - Force Save Current State")]
    private void DebugForceSave()
    {
        SavePlayerData();
        Debug.Log("[GameDataManager] Force saved current state");
    }
}