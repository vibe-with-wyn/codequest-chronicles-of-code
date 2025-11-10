using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// QUEST 4 OBJECTIVE 3: Key Collection
/// Handles key pickup interaction and objective completion
/// Similar to rune collection pattern but without quiz
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KeyCollectionTrigger : MonoBehaviour
{
    [Header("Quest Settings")]
    [SerializeField] private string requiredQuestId = "Q4_StringsPrinting";
    [SerializeField] private string requiredObjectiveTitle = "Complete the String Exercises";

    [Header("Collection Button Prefab")]
    [SerializeField] private GameObject collectionButtonPrefab;

    [Header("Button Settings")]
    [SerializeField] private Vector3 buttonOffset = new Vector3(0, 1f, 0);
    [SerializeField] private float buttonFadeSpeed = 5f;

    [Header("Collection Animation")]
    [Tooltip("Duration key takes to fly to player")]
    [SerializeField] private float collectionDuration = 0.8f;
    
    [Tooltip("Arc height during collection flight")]
    [SerializeField] private float collectionArcHeight = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // State
    private bool isPlayerInRange = false;
    private bool isCollected = false;
    private bool isCollecting = false;
    private Transform playerTransform;

    // Button
    private GameObject buttonInstance;
    private Button collectionButton;
    private CanvasGroup buttonCanvasGroup;

    // Visuals
    private SpriteRenderer keySprite;
    private Collider2D keyCollider;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"KeyCollectionTrigger: Collider set to trigger.");
        }

        keyCollider = col;
        keySprite = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        InitializeButton();
    }

    void Update()
    {
        UpdateButtonVisibility();
    }

    #region Button

    private void InitializeButton()
    {
        if (collectionButtonPrefab == null)
        {
            Debug.LogError("[Key] Collection Button Prefab not assigned!");
            return;
        }

        Vector3 buttonPos = transform.position + buttonOffset;
        buttonInstance = Instantiate(collectionButtonPrefab, buttonPos, Quaternion.identity);
        buttonInstance.transform.SetParent(transform, true);

        collectionButton = buttonInstance.GetComponentInChildren<Button>();
        buttonCanvasGroup = buttonInstance.GetComponent<CanvasGroup>();

        if (collectionButton == null)
        {
            Debug.LogError("[Key] Button not found in prefab!");
            return;
        }

        if (buttonCanvasGroup == null)
            buttonCanvasGroup = buttonInstance.AddComponent<CanvasGroup>();

        TextMeshProUGUI btnText = buttonInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
            btnText.text = "Collect Key";

        collectionButton.onClick.AddListener(OnCollectKey);

        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        buttonCanvasGroup.blocksRaycasts = false;
        buttonInstance.SetActive(false);

        if (debugMode)
            Debug.Log($"[Key] Button created at {buttonPos}");
    }

    private void UpdateButtonVisibility()
    {
        if (buttonCanvasGroup == null || buttonInstance == null) return;

        bool shouldShow = isPlayerInRange && !isCollected && !isCollecting && IsObjectiveActive();

        if (shouldShow)
        {
            if (!buttonInstance.activeSelf)
                buttonInstance.SetActive(true);

            buttonCanvasGroup.alpha = Mathf.Lerp(buttonCanvasGroup.alpha, 1f, Time.deltaTime * buttonFadeSpeed);
            buttonCanvasGroup.interactable = buttonCanvasGroup.alpha > 0.9f;
            buttonCanvasGroup.blocksRaycasts = buttonCanvasGroup.interactable;
        }
        else
        {
            buttonCanvasGroup.alpha = Mathf.Lerp(buttonCanvasGroup.alpha, 0f, Time.deltaTime * buttonFadeSpeed);
            buttonCanvasGroup.interactable = false;
            buttonCanvasGroup.blocksRaycasts = false;

            if (buttonCanvasGroup.alpha < 0.01f && buttonInstance.activeSelf)
                buttonInstance.SetActive(false);
        }
    }

    private bool IsObjectiveActive()
    {
        if (QuestManager.Instance == null) return false;

        QuestData quest = QuestManager.Instance.GetCurrentQuest();
        if (quest == null || quest.questId != requiredQuestId) return false;

        QuestObjective obj = quest.objectives.Find(o => o.objectiveTitle == requiredObjectiveTitle);
        return obj != null && obj.isActive && !obj.isCompleted;
    }

    #endregion

    #region Collection

    private void OnCollectKey()
    {
        if (debugMode)
            Debug.Log("[Key] Collect Key button clicked!");

        if (playerTransform == null)
        {
            Debug.LogError("[Key] Player reference lost!");
            return;
        }

        StartCoroutine(CollectKeySequence());
    }

    private IEnumerator CollectKeySequence()
    {
        isCollecting = true;

        // Disable collider
        if (keyCollider != null)
            keyCollider.enabled = false;

        if (debugMode)
            Debug.Log("[Key] Starting collection animation...");

        // Animate key flying to player
        Vector3 startPos = transform.position;
        Vector3 endPos = playerTransform.position + Vector3.up; // Slightly above player

        float elapsed = 0f;

        while (elapsed < collectionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectionDuration;

            // Horizontal lerp
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            // Vertical arc
            pos.y += Mathf.Sin(t * Mathf.PI) * collectionArcHeight;

            transform.position = pos;

            // Optional: rotate key during flight
            transform.Rotate(Vector3.forward, 360f * Time.deltaTime * 2f);

            yield return null;
        }

        // Key collected
        isCollected = true;

        if (debugMode)
            Debug.Log("[Key] Key collected!");

        // Complete objective
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteObjectiveByTitle(requiredObjectiveTitle);

            if (debugMode)
                Debug.Log($"[Key] ✓ Quest objective '{requiredObjectiveTitle}' completed!");
        }

        // Destroy key
        Destroy(gameObject);
    }

    #endregion

    #region Trigger

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;

            if (debugMode)
                Debug.Log("[Key] Player entered key range");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            playerTransform = null;

            if (debugMode)
                Debug.Log("[Key] Player left key range");
        }
    }

    #endregion

    #region Cleanup

    void OnDestroy()
    {
        if (buttonInstance != null)
            Destroy(buttonInstance);
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = isCollected ? Color.green : Color.yellow;

            if (col is BoxCollider2D box)
                Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
            else if (col is CircleCollider2D circle)
                Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + buttonOffset, 0.2f);

#if UNITY_EDITOR
        string status = isCollected ? "COLLECTED" : (isCollecting ? "COLLECTING..." : "READY");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"Eternal Key [{status}]\nCollection Duration: {collectionDuration}s",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
#endif
    }

    #endregion
}