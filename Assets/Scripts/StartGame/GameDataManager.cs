using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    private PlayerData playerData;
    private const string DATA_VERSION = "1.0";
    private const string PLAYER_DATA_KEY = "PlayerData";
    private const string INTRO_PLAYED_KEY = "HasPlayedIntro"; // Add this

    // Properties for accessing data
    public string SelectedLanguage => playerData?.SelectedLanguage ?? "Python";
    public string SelectedCharacter => playerData?.SelectedCharacter ?? "Swordsman";
    public int progressLevel => playerData?.ProgressLevel ?? 1;
    
    // Make HasPlayedIntro persistent
    public bool HasPlayedIntro 
    { 
        get => PlayerPrefs.GetInt(INTRO_PLAYED_KEY, 0) == 1;
        set 
        {
            PlayerPrefs.SetInt(INTRO_PLAYED_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"HasPlayedIntro set to: {value}");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPlayerData();
            Debug.Log("GameDataManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLanguage(string language)
    {
        if (!string.IsNullOrEmpty(language))
        {
            playerData.SelectedLanguage = language;
            SavePlayerData();
            Debug.Log($"Language set to: {language}");
        }
        else
        {
            Debug.LogWarning("Attempted to set language to null or empty");
        }
    }

    public void SetCharacter(string character)
    {
        if (!string.IsNullOrEmpty(character))
        {
            playerData.SelectedCharacter = character;
            SavePlayerData();
            Debug.Log($"Character set to: {character}");
        }
        else
        {
            Debug.LogWarning("Attempted to set character to null or empty");
        }
    }

    public void UpdateProgress(int level)
    {
        if (level >= 1)
        {
            playerData.ProgressLevel = level;
            SavePlayerData();
            Debug.Log($"Progress updated to level: {level}");
        }
        else
        {
            Debug.LogWarning("Invalid progress level");
        }
    }

    public void SetLastScene(string scene)
    {
        if (!string.IsNullOrEmpty(scene))
        {
            playerData.LastScene = scene;
            SavePlayerData();
            Debug.Log($"Last scene set to: {scene}");
        }
        else
        {
            Debug.LogWarning("Attempted to set last scene to null or empty");
        }
    }

    public void SetRespawnPoint(Vector3 point)
    {
        playerData.RespawnPoint = point;
        SavePlayerData();
        Debug.Log($"Respawn point set to: {point}");
    }

    public bool IsCharacterLoaded()
    {
        return playerData?.SelectedCharacter == "swordsman"; // Only Swordsman for now
    }

    public PlayerData GetPlayerData()
    {
        return playerData;
    }

    public void ResetPlayerData()
    {
        playerData = new PlayerData("", "swordsman", 1, "Game World Context", Vector3.zero);
        HasPlayedIntro = false; // Reset intro flag when resetting data
        SavePlayerData();
        Debug.Log("Player data reset to defaults");
    }

    private void SavePlayerData()
    {
        string json = JsonUtility.ToJson(playerData);
        PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
        PlayerPrefs.SetString("DataVersion", DATA_VERSION);
        PlayerPrefs.Save();
        Debug.Log("Player data saved");
    }

    private void LoadPlayerData()
    {
        string json = PlayerPrefs.GetString(PLAYER_DATA_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            playerData = new PlayerData("", "swordsman", 1, "Game World Context", Vector3.zero);
            SavePlayerData();
            Debug.Log("Created new player data with defaults");
        }
        else
        {
            playerData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log($"Loaded player data - Language: {playerData.SelectedLanguage}, Character: {playerData.SelectedCharacter}, " +
                      $"Progress: {playerData.ProgressLevel}, Scene: {playerData.LastScene}, Respawn: {playerData.RespawnPoint}");
        }
    }
}