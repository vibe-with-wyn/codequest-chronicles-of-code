using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image characterPortrait;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button questLogButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button inventoryButton;
    
    [Header("Movement Controls")]
    [SerializeField] private Button moveLeftButton;
    [SerializeField] private Button moveRightButton;
    [SerializeField] private Button jumpButton;
    
    [Header("Attack Controls")]
    [SerializeField] private Button[] attackButtons;
    [SerializeField] private TextMeshProUGUI[] attackCooldownTexts;
    
    [Header("Character Portraits")]
    [SerializeField] private Sprite swordsmanPortrait;
    [SerializeField] private Sprite magePortrait;
    [SerializeField] private Sprite archerPortrait;

    // Character Stats
    private float characterSpeedMultiplier = 1f;
    public float CharacterSpeedMultiplier => characterSpeedMultiplier;

    // Input States
    private bool isMovingLeft, isMovingRight, isJumping;
    private bool isPlayerDead = false;

    // Attack System
    private float[] attackCooldowns = { 0f, 3f, 2f, 5f, 8f };
    private float[] attackCooldownTimers = new float[5];
    private int pendingSkillAttack = -1;
    private bool basicAttackEnabled = true;
    private float basicAttackDisableTimer = 0f;
    private const float BASIC_ATTACK_DISABLE_DURATION = 0.5f;

    // Health
    private float playerHealth = 100f;
    
    // NEW: Quest UI reference
    private QuestUIController questUIController;

    void Start()
    {
        SetupCharacterPortraitAndStats();
        SetupHealthBar();
        SetupButtonListeners();
        InitializeCooldownSystem();
        InitializeQuestSystem(); // NEW: Initialize quest system
        Debug.Log("UIController initialized successfully");
    }

    void Update()
    {
        UpdateCooldownTimers();
        HandleBasicAttackDisableTimer();
        ProcessPendingSkillAttacks();
    }

    #region Initialization
    private void SetupCharacterPortraitAndStats()
    {
        if (GameDataManager.Instance != null && characterPortrait != null)
        {
            string character = GameDataManager.Instance.SelectedCharacter.ToLower();
            switch (character)
            {
                case "swordsman":
                    characterPortrait.sprite = swordsmanPortrait;
                    characterSpeedMultiplier = 1.2f;
                    break;
                case "mage":
                    characterPortrait.sprite = magePortrait;
                    characterSpeedMultiplier = 0.8f;
                    break;
                case "archer":
                    characterPortrait.sprite = archerPortrait;
                    characterSpeedMultiplier = 1f;
                    break;
                default:
                    Debug.LogWarning($"Unknown character: {character}");
                    characterSpeedMultiplier = 1f;
                    break;
            }
            Debug.Log($"Character setup: {character}, Speed Multiplier: {characterSpeedMultiplier}");
        }
    }

    private void SetupHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = playerHealth / 100f;
        }
    }

    private void SetupButtonListeners()
    {
        // Menu buttons
        SetupSimpleButton(settingsButton, OnSettings);
        SetupSimpleButton(questLogButton, OnQuestLog);
        SetupSimpleButton(helpButton, OnHelp);
        SetupSimpleButton(inventoryButton, OnInventory);
        
        // Movement buttons
        SetupHoldButton(moveLeftButton, () => isMovingLeft = true, () => isMovingLeft = false);
        SetupHoldButton(moveRightButton, () => isMovingRight = true, () => isMovingRight = false);
        SetupSimpleButton(jumpButton, () => isJumping = true);
        
        // Attack buttons
        if (attackButtons != null)
        {
            for (int i = 0; i < attackButtons.Length; i++)
            {
                int attackIndex = i + 1;
                SetupSimpleButton(attackButtons[i], () => OnAttack(attackIndex));
            }
        }
    }

    private void InitializeCooldownSystem()
    {
        for (int i = 0; i < attackCooldownTimers.Length; i++)
        {
            attackCooldownTimers[i] = 0f;
        }
        
        if (attackCooldownTexts != null)
        {
            for (int i = 0; i < attackCooldownTexts.Length; i++)
            {
                if (attackCooldownTexts[i] != null)
                    attackCooldownTexts[i].text = "";
            }
        }
    }
    
    // NEW: Initialize quest system
    private void InitializeQuestSystem()
    {
        // Find QuestUIController in the scene
        questUIController = Object.FindFirstObjectByType<QuestUIController>();
        
        if (questUIController == null)
        {
            Debug.LogWarning("QuestUIController not found in scene! Quest log button will not work.");
        }
        else
        {
            Debug.Log("QuestUIController found and connected to UIController");
        }
    }
    #endregion

    #region Button Setup
    private void SetupSimpleButton(Button button, System.Action onClick)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    private void SetupHoldButton(Button button, System.Action onPress, System.Action onRelease)
    {
        if (button != null)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.Clear();
            }
            else
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener((data) => onPress?.Invoke());
            trigger.triggers.Add(downEntry);

            EventTrigger.Entry upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener((data) => onRelease?.Invoke());
            trigger.triggers.Add(upEntry);
        }
    }

    public void ReinitializeButtons()
    {
        Debug.Log("Reinitializing button listeners");
        SetupButtonListeners();
        InitializeQuestSystem(); // NEW: Also reinitialize quest system
        Debug.Log("Button listeners reinitialized successfully");
    }
    #endregion

    #region Update Methods
    private void UpdateCooldownTimers()
    {
        for (int i = 1; i < attackCooldownTimers.Length; i++) // Skip Attack1
        {
            if (attackCooldownTimers[i] > 0f)
            {
                attackCooldownTimers[i] -= Time.deltaTime;
                if (attackCooldownTexts[i - 1] != null)
                {
                    attackCooldownTexts[i - 1].text = attackCooldownTimers[i].ToString("F1") + "s";
                }
            }
            else if (attackCooldownTexts[i - 1] != null)
            {
                attackCooldownTexts[i - 1].text = "";
            }
        }
    }

    private void HandleBasicAttackDisableTimer()
    {
        if (!basicAttackEnabled)
        {
            basicAttackDisableTimer -= Time.deltaTime;
            if (basicAttackDisableTimer <= 0f)
            {
                basicAttackEnabled = true;
            }
        }
    }

    private void ProcessPendingSkillAttacks()
    {
        if (pendingSkillAttack != -1)
        {
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            if (player != null && player.CanPerformAttack())
            {
                if (CanUseAttack(pendingSkillAttack))
                {
                    player.TriggerAttack(pendingSkillAttack);
                    StartAttackCooldown(pendingSkillAttack);
                    pendingSkillAttack = -1;
                    
                    basicAttackEnabled = true;
                    basicAttackDisableTimer = 0f;
                }
            }
        }
    }
    #endregion

    #region Input Properties (Blocked when dead)
    public bool IsMovingLeft => !isPlayerDead && isMovingLeft;
    public bool IsMovingRight => !isPlayerDead && isMovingRight;
    public bool IsJumping => !isPlayerDead && isJumping;
    #endregion

    #region Attack System
    public bool CanUseAttack(int attackIndex)
    {
        if (attackIndex < 1 || attackIndex > attackCooldownTimers.Length)
        {
            Debug.LogWarning($"Invalid attack index: {attackIndex}");
            return false;
        }
        return attackCooldownTimers[attackIndex - 1] <= 0f;
    }

    public void StartAttackCooldown(int attackIndex)
    {
        if (attackIndex > 1 && attackIndex <= attackCooldowns.Length)
        {
            attackCooldownTimers[attackIndex - 1] = attackCooldowns[attackIndex - 1] / characterSpeedMultiplier;
            Debug.Log($"Started cooldown for Attack{attackIndex}: {attackCooldownTimers[attackIndex - 1]}s");
        }
    }

    private void OnAttack(int attackIndex)
    {
        if (isPlayerDead) 
        {
            Debug.Log("Attack input BLOCKED - player is dead");
            return;
        }
        
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;
        
        if (attackIndex == 1) // Basic Attack
        {
            if (basicAttackEnabled && pendingSkillAttack == -1 && CanUseAttack(attackIndex) && player.CanPerformAttack())
            {
                player.TriggerAttack(attackIndex);
                StartAttackCooldown(attackIndex);
            }
        }
        else // Skill Attacks (Attack2-5)
        {
            if (CanUseAttack(attackIndex))
            {
                pendingSkillAttack = attackIndex;
                
                basicAttackEnabled = false;
                basicAttackDisableTimer = BASIC_ATTACK_DISABLE_DURATION;
                
                if (player.CanPerformAttack())
                {
                    player.TriggerAttack(attackIndex);
                    StartAttackCooldown(attackIndex);
                    pendingSkillAttack = -1;
                    basicAttackEnabled = true;
                    basicAttackDisableTimer = 0f;
                }
            }
            else
            {
                Debug.Log($"Skill Attack{attackIndex} on cooldown: {attackCooldownTimers[attackIndex - 1]}s remaining");
            }
        }
    }
    #endregion

    #region Button Actions
    private void OnSettings() => Debug.Log("Settings clicked - TODO: Open settings menu");
    
    // UPDATED: Quest Log button now shows current quest
    private void OnQuestLog() 
    {
        Debug.Log("Quest Log clicked - Showing current quest");
        
        if (questUIController != null)
        {
            questUIController.ShowCurrentQuest();
        }
        else
        {
            Debug.LogError("QuestUIController not found! Cannot show quest.");
        }
    }
    
    private void OnHelp() => Debug.Log("Help clicked - TODO: Show help UI");
    private void OnInventory() => Debug.Log("Inventory clicked - TODO: Open inventory");
    #endregion

    #region Public Methods
    // FIXED: Health bar now properly reflects max HP changes
    public void UpdateHealth(float newHealth)
    {
        if (healthBar != null)
        {
            // Get the actual max HP from PlayerHealth to calculate correct percentage
            PlayerHealth playerHealthComponent = Object.FindFirstObjectByType<PlayerHealth>();
            if (playerHealthComponent != null)
            {
                int maxHP = playerHealthComponent.GetMaxHealth();
                int currentHP = Mathf.Clamp((int)newHealth, 0, maxHP);
                
                // Calculate percentage based on ACTUAL max HP, not hardcoded 100
                float healthPercentage = (float)currentHP / maxHP;
                healthBar.value = healthPercentage;
                
                playerHealth = newHealth;
                
                Debug.Log($"Health updated: {currentHP}/{maxHP} = {healthPercentage * 100:F1}%");
            }
            else
            {
                // Fallback if PlayerHealth not found
                playerHealth = Mathf.Clamp(newHealth, 0f, 200f);
                healthBar.value = playerHealth / 200f;
                Debug.LogWarning("PlayerHealth component not found, using fallback calculation");
            }
        }
    }

    public void ResetJump()
    {
        isJumping = false;
    }

    public void SetPlayerDeadState(bool isDead)
    {
        isPlayerDead = isDead;
        
        if (isDead)
        {
            Debug.Log("UIController: Player died - ALL inputs BLOCKED");
            // Clear any pending inputs
            isMovingLeft = false;
            isMovingRight = false;
            isJumping = false;
            pendingSkillAttack = -1;
            basicAttackEnabled = true;
            basicAttackDisableTimer = 0f;
        }
        else
        {
            Debug.Log("UIController: Player respawned - inputs RE-ENABLED");
        }
    }

    // Add this method to UIController.cs
    public void SetQuestButtonInteractable(bool interactable)
    {
        if (questLogButton != null)
        {
            questLogButton.interactable = interactable;
        }
    }
    #endregion
}