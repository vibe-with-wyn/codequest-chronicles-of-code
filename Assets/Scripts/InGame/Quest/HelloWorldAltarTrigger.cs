using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles player detection at the Hello World Altar and manages the "Start Quiz" button visibility
/// UPDATED: Uses world-space canvas prefab button that stays near the altar
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HelloWorldAltarTrigger : MonoBehaviour
{
    [Header("Quest Settings")]
    [SerializeField] private string requiredQuestId = "Q2_HelloJava";
    [SerializeField] private string requiredObjectiveTitle = "Complete the Hello World Altar Quiz";

    [Header("Mini Quest Data")]
    [SerializeField] private MiniQuestData miniQuestData;

    [Header("Start Quiz Button Prefab")]
    [Tooltip("Prefab with world-space Canvas containing the Start Quiz Button")]
    [SerializeField] private GameObject startQuizButtonPrefab;

    [Header("Button Position")]
    [Tooltip("Offset from the altar where the button should appear (world space)")]
    [SerializeField] private Vector3 buttonOffset = new Vector3(0, 2f, 0); // Above the altar

    [Header("Button Settings")]
    [SerializeField] private float buttonFadeSpeed = 5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool isPlayerInRange = false;
    private bool isQuizActive = false;
    private bool hasCompletedQuiz = false;
    private Transform playerTransform;
    private MiniQuestUIController miniQuestUI;

    // Runtime instantiated button
    private GameObject startQuizButtonInstance;
    private Button startQuizButton;
    private CanvasGroup buttonCanvasGroup;

    void Awake()
    {
        // Ensure collider is set to trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"HelloWorldAltarTrigger on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }
    }

    void Start()
    {
        CacheMiniQuestUI();
        ValidateMiniQuestData();
        InitializeStartQuizButton();
    }

    void Update()
    {
        UpdateButtonVisibility();
    }

    private void InitializeStartQuizButton()
    {
        if (startQuizButtonPrefab == null)
        {
            Debug.LogError("[HelloWorldAltar] Start Quiz Button Prefab not assigned! Please assign it in the Inspector.");
            return;
        }

        // Instantiate the button prefab at the altar's position
        Vector3 buttonPosition = transform.position + buttonOffset;
        startQuizButtonInstance = Instantiate(startQuizButtonPrefab, buttonPosition, Quaternion.identity);

        // Parent it to the altar so it moves with the altar (if altar moves)
        startQuizButtonInstance.transform.SetParent(transform, true); // worldPositionStays = true

        // Get references to button components
        startQuizButton = startQuizButtonInstance.GetComponentInChildren<Button>();
        buttonCanvasGroup = startQuizButtonInstance.GetComponent<CanvasGroup>();

        if (startQuizButton == null)
        {
            Debug.LogError("[HelloWorldAltar] Button component not found in Start Quiz Button Prefab!");
            return;
        }

        if (buttonCanvasGroup == null)
        {
            // Add CanvasGroup if missing
            buttonCanvasGroup = startQuizButtonInstance.AddComponent<CanvasGroup>();
        }

        // Setup button listener
        startQuizButton.onClick.AddListener(OnStartQuizButtonClicked);

        // Initially hide button
        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;
        startQuizButtonInstance.SetActive(false);

        if (debugMode)
        {
            Debug.Log($"[HelloWorldAltar] Start Quiz Button instantiated at position: {buttonPosition}");
        }
    }

    private void CacheMiniQuestUI()
    {
        if (miniQuestUI == null)
        {
            miniQuestUI = Object.FindFirstObjectByType<MiniQuestUIController>();

            if (miniQuestUI == null)
            {
                Debug.LogError("[HelloWorldAltar] MiniQuestUIController not found in scene! Please add it to Mini Quest Controller GameObject.");
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log("[HelloWorldAltar] MiniQuestUIController found successfully");
                }
            }
        }
    }

    private void ValidateMiniQuestData()
    {
        if (miniQuestData == null)
        {
            Debug.LogError("[HelloWorldAltar] MiniQuestData not assigned! Please create and assign a MiniQuestData ScriptableObject.");
            return;
        }

        if (miniQuestData.questions.Count == 0)
        {
            Debug.LogError($"[HelloWorldAltar] MiniQuestData '{miniQuestData.name}' has no questions!");
        }

        if (debugMode)
        {
            Debug.Log($"[HelloWorldAltar] Mini Quest Data loaded: {miniQuestData.miniQuestTitle}");
            Debug.Log($"[HelloWorldAltar] Total questions: {miniQuestData.GetTotalQuestions()}");
        }
    }

    private void UpdateButtonVisibility()
    {
        if (buttonCanvasGroup == null || startQuizButtonInstance == null) return;

        // Check if quest objective is active
        bool shouldShowButton = isPlayerInRange && !isQuizActive && !hasCompletedQuiz && IsObjectiveActive();

        if (shouldShowButton)
        {
            // Fade in
            if (!startQuizButtonInstance.activeSelf)
            {
                startQuizButtonInstance.SetActive(true);
            }

            buttonCanvasGroup.alpha = Mathf.Lerp(buttonCanvasGroup.alpha, 1f, Time.deltaTime * buttonFadeSpeed);
            buttonCanvasGroup.interactable = buttonCanvasGroup.alpha > 0.9f;
            buttonCanvasGroup.blocksRaycasts = buttonCanvasGroup.interactable;
        }
        else
        {
            // Fade out
            buttonCanvasGroup.alpha = Mathf.Lerp(buttonCanvasGroup.alpha, 0f, Time.deltaTime * buttonFadeSpeed);
            buttonCanvasGroup.interactable = false;
            buttonCanvasGroup.blocksRaycasts = false;

            if (buttonCanvasGroup.alpha < 0.01f && startQuizButtonInstance.activeSelf)
            {
                startQuizButtonInstance.SetActive(false);
            }
        }
    }

    private bool IsObjectiveActive()
    {
        if (QuestManager.Instance == null) return false;

        QuestData currentQuest = QuestManager.Instance.GetCurrentQuest();
        if (currentQuest == null || currentQuest.questId != requiredQuestId) return false;

        // Check if the specific objective is active
        QuestObjective objective = currentQuest.objectives.Find(o => o.objectiveTitle == requiredObjectiveTitle);
        if (objective != null && objective.isActive && !objective.isCompleted)
        {
            return true;
        }

        return false;
    }

    private void OnStartQuizButtonClicked()
    {
        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] Start Quiz button clicked!");
        }

        if (miniQuestData == null)
        {
            Debug.LogError("[HelloWorldAltar] Cannot start quiz - MiniQuestData not assigned!");
            return;
        }

        if (miniQuestUI == null)
        {
            Debug.LogError("[HelloWorldAltar] Cannot start quiz - MiniQuestUIController not found!");
            return;
        }

        // Start the quiz
        isQuizActive = true;
        miniQuestUI.StartMiniQuest(miniQuestData, OnQuizCompleted);

        if (debugMode)
        {
            Debug.Log("[HelloWorldAltar] Quiz started!");
        }
    }

    private void OnQuizCompleted(bool success)
    {
        isQuizActive = false;

        if (success)
        {
            hasCompletedQuiz = true;

            // Complete the quest objective
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CompleteObjectiveByTitle(requiredObjectiveTitle);
                if (debugMode)
                {
                    Debug.Log($"[HelloWorldAltar] ✓ Quest objective '{requiredObjectiveTitle}' completed!");
                }
            }
        }
        else
        {
            if (debugMode)
            {
                Debug.Log("[HelloWorldAltar] Quiz was cancelled or failed");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;

            if (debugMode)
            {
                Debug.Log("[HelloWorldAltar] Player entered altar range");
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerTransform = null;

            if (debugMode)
            {
                Debug.Log("[HelloWorldAltar] Player left altar range");
            }
        }
    }

    void OnDestroy()
    {
        // Clean up instantiated button when altar is destroyed
        if (startQuizButtonInstance != null)
        {
            Destroy(startQuizButtonInstance);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw trigger zone
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = hasCompletedQuiz ? Color.green : Color.yellow;

            if (col is BoxCollider2D boxCol)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            }
            else if (col is CircleCollider2D circleCol)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
            }
        }

        // Draw button spawn position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + buttonOffset, 0.3f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
            $"Hello World Altar\n{requiredObjectiveTitle}\nButton Offset: {buttonOffset}",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan } });
#endif
    }
}