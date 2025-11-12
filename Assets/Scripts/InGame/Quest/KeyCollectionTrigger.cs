using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// QUEST 4 OBJECTIVE 3: Key Collection
/// Handles key pickup interaction and objective completion
/// FIXED: Stores player position before disabling collider to prevent OnTriggerExit issues
/// Similar to rune collection pattern
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

    [Header("Key Components")]
    [Tooltip("Animator for the key sprite (auto-found in child if not assigned)")]
    [SerializeField] private Animator keyAnimator;

    [Header("Collection Animation")]
    [Tooltip("Duration key takes to fly to player")]
    [SerializeField] private float collectionDuration = 0.8f;
    
    [Tooltip("Arc height during collection flight")]
    [SerializeField] private float collectionArcHeight = 2f;
    
    [Tooltip("Height above ground where key lands")]
    [SerializeField] private float groundOffset = 0.5f;

    [Header("Fade-Out Settings")]
    [Tooltip("Duration of key fade-out after reaching player")]
    [SerializeField] private float fadeOutDuration = 0.8f;
    
    [Tooltip("Delay after reaching player before starting fade-out")]
    [SerializeField] private float postCollectionDelay = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // State
    private bool isPlayerInRange = false;
    private bool isCollected = false;
    private bool isCollecting = false;
    private Transform playerTransform;
    private Vector3 cachedPlayerPosition; // FIX: Cache player position before disabling collider

    // Button
    private GameObject buttonInstance;
    private Button collectionButton;
    private CanvasGroup buttonCanvasGroup;

    // Visuals (matching rune pattern)
    private SpriteRenderer keySprite;
    private Collider2D keyCollider;
    private Color originalKeyColor;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"KeyCollectionTrigger: Collider set to trigger.");
        }

        keyCollider = col;

        // Auto-find key animator (matching rune pattern)
        if (keyAnimator == null)
        {
            keyAnimator = GetComponentInChildren<Animator>();
            if (keyAnimator != null && debugMode)
                Debug.Log($"[Key] Auto-found Key Animator: {keyAnimator.gameObject.name}");
        }

        // Get key sprite renderer for fade-out (matching rune pattern)
        if (keyAnimator != null)
        {
            keySprite = keyAnimator.GetComponent<SpriteRenderer>();
            if (keySprite != null)
            {
                originalKeyColor = keySprite.color;
                if (debugMode)
                    Debug.Log("[Key] Found Key SpriteRenderer and stored original color");
            }
        }
        else
        {
            // Fallback: try to find sprite renderer directly
            keySprite = GetComponentInChildren<SpriteRenderer>();
            if (keySprite != null)
            {
                originalKeyColor = keySprite.color;
                if (debugMode)
                    Debug.Log("[Key] Found Key SpriteRenderer (fallback)");
            }
        }
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

    #region Collection (Matching Rune Pattern)

    private void OnCollectKey()
    {
        if (debugMode)
            Debug.Log("[Key] Collect Key button clicked!");

        if (playerTransform == null)
        {
            Debug.LogError("[Key] Player reference lost!");
            return;
        }

        // FIX: Cache player position BEFORE starting coroutine
        cachedPlayerPosition = playerTransform.position;

        StartCoroutine(KeyCollectionSequence());
    }

    // MATCHING RUNE PATTERN: Complete collection sequence with fade-out
    private IEnumerator KeyCollectionSequence()
    {
        isCollecting = true;

        // FIX: Disable collider AFTER caching player position
        if (keyCollider != null)
            keyCollider.enabled = false;

        if (debugMode)
            Debug.Log("[Key] ========== STARTING COLLECTION SEQUENCE ==========");

        // ============= PHASE 1: FLY TO PLAYER =============
        if (debugMode)
            Debug.Log("[Key] PHASE 1: Key flying to player...");

        Vector3 startPos = transform.position;
        // FIX: Use cached position instead of playerTransform (which might be null after collider disabled)
        Vector3 endPos = cachedPlayerPosition + Vector3.up + new Vector3(0, groundOffset, 0);

        float elapsed = 0f;

        while (elapsed < collectionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectionDuration;

            // Horizontal lerp
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);

            // Vertical arc
            float arcProgress = Mathf.Sin(t * Mathf.PI);
            currentPos.y += arcProgress * collectionArcHeight;

            transform.position = currentPos;

            yield return null;
        }

        // Snap to final position
        transform.position = endPos;

        if (debugMode)
            Debug.Log("[Key] ✓ Key reached player!");

        // ============= PHASE 2: POST-COLLECTION DELAY =============
        if (debugMode)
            Debug.Log($"[Key] PHASE 2: Waiting {postCollectionDelay}s before fade-out...");

        yield return new WaitForSeconds(postCollectionDelay);

        // ============= PHASE 3: FADE OUT KEY (MATCHING RUNE) =============
        if (debugMode)
            Debug.Log("[Key] PHASE 3: Fading out key...");

        yield return StartCoroutine(FadeOutKey());

        // ============= PHASE 4: DISABLE KEY OBJECTS (MATCHING RUNE) =============
        if (debugMode)
            Debug.Log("[Key] PHASE 4: Disabling key objects...");

        // Disable key animator
        if (keyAnimator != null)
        {
            keyAnimator.gameObject.SetActive(false);
            if (debugMode)
                Debug.Log("[Key] ✓ Key Animator disabled");
        }

        // ============= PHASE 5: COMPLETE OBJECTIVE =============
        if (debugMode)
            Debug.Log("[Key] PHASE 5: Completing quest objective...");

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteObjectiveByTitle(requiredObjectiveTitle);
            
            if (debugMode)
                Debug.Log("[Key] ✓ Objective completed!");
        }
        else
        {
            Debug.LogError("[Key] ❌ QuestManager.Instance is NULL! Objective NOT completed!");
        }

        isCollected = true;

        // ============= PHASE 6: DESTROY KEY =============
        if (debugMode)
            Debug.Log("[Key] PHASE 6: Destroying key GameObject...");

        Destroy(gameObject);

        if (debugMode)
            Debug.Log("[Key] ✓✓✓ Collection sequence completed!");
    }

    // MATCHING RUNE PATTERN: Fade out key sprite
    private IEnumerator FadeOutKey()
    {
        if (keySprite == null)
        {
            if (debugMode)
                Debug.LogWarning("[Key] No SpriteRenderer found - skipping fade-out");
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);

            Color color = originalKeyColor;
            color.a = alpha;
            keySprite.color = color;

            yield return null;
        }

        // Ensure final alpha is 0
        Color finalColor = originalKeyColor;
        finalColor.a = 0f;
        keySprite.color = finalColor;

        if (debugMode)
            Debug.Log("[Key] ✓ Fade-out completed!");
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
            // FIX: Don't clear playerTransform if we're already collecting
            // (This prevents issues when collider is disabled during collection)
            if (!isCollecting)
            {
                isPlayerInRange = false;
                playerTransform = null;

                if (debugMode)
                    Debug.Log("[Key] Player left key range");
            }
            else if (debugMode)
            {
                Debug.Log("[Key] Player trigger exit detected but IGNORED (collection in progress)");
            }
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
        string animatorStatus = keyAnimator != null ? "✓" : "✗ MISSING";
        float totalTime = collectionDuration + postCollectionDelay + fadeOutDuration;
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"Eternal Key [{status}]\nAnimator: {animatorStatus}\n\nSEQUENCE:\n1. Player enters → Button appears\n2. Button clicked → Cache player pos\n3. Fly to player {collectionDuration}s\n4. Post-delay {postCollectionDelay}s\n5. Fade out {fadeOutDuration}s\n6. Disable key animator\n7. Complete objective\n8. Destroy GameObject\n\nTotal: {totalTime:F1}s",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
#endif
    }

    #endregion
}