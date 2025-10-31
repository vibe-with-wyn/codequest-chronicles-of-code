using UnityEngine;
using System.Collections;

/// <summary>
/// Specialized trigger for the cabin that handles Quest 2 objectives and NPC Arin's lecture dialogue
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CabinQuestTrigger : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("Quest ID for Quest 2")]
    [SerializeField] private string questId = "Q2_HelloJava";
    
    [Tooltip("Objective 1: Enter the cabin")]
    [SerializeField] private string enterCabinObjective = "Follow Arin to Her Cabin";
    
    [Tooltip("Objective 2: Listen to Arin's lecture")]
    [SerializeField] private string lecturObjective = "Listen to Arin's Lesson: Java Basics";
    
    [Tooltip("Dialogue conversation ID for Arin's lecture")]
    [SerializeField] private string lectureDialogueId = "Arin_02_JavaBasics";
    
    [Header("NPC Settings")]
    [Tooltip("Distance Arin should be from player during lecture")]
    [SerializeField] private float conversationDistance = 3.0f;
    
    [Tooltip("Speed Arin approaches player")]
    [SerializeField] private float approachSpeed = 2.0f;
    
    [Tooltip("How close Arin needs to be before stopping")]
    [SerializeField] private float stopDistance = 0.1f;
    
    [Header("Timing")]
    [Tooltip("Delay before starting lecture dialogue after Arin arrives")]
    [SerializeField] private float dialogueDelay = 1.0f;
    
    [Header("UI References")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private bool hasTriggered = false;
    private bool isLectureActive = false;
    private ArinNPCAI arinAI;
    private Animator arinAnimator;
    private Rigidbody2D arinRb;
    private SpriteRenderer arinSprite;
    private PlayerMovement playerMovement;
    private UIController uiController;
    
    // Store original physics for Arin
    private RigidbodyType2D originalArinBodyType;
    private float originalArinGravity;
    
    // Store UI state
    private bool originalInteractable;
    private bool originalBlocksRaycasts;
    private float originalAlpha;

    void Awake()
    {
        // Ensure collider is set to trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"CabinQuestTrigger on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if already triggered
        if (hasTriggered)
        {
            return;
        }
        
        // Check if it's the player
        if (!other.CompareTag("Player"))
        {
            return;
        }
        
        // Verify QuestManager exists
        if (QuestManager.Instance == null)
        {
            Debug.LogError($"CabinQuestTrigger: QuestManager not found in scene!");
            return;
        }
        
        // Check if Quest 2 is active
        QuestData currentQuest = QuestManager.Instance.GetCurrentQuest();
        if (currentQuest == null || currentQuest.questId != questId)
        {
            if (debugMode)
            {
                Debug.Log($"CabinQuestTrigger: Quest 2 not active yet. Current quest: {currentQuest?.questId ?? "None"}");
            }
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"CabinQuestTrigger: Player entered cabin! Starting Quest 2 Objective 1 completion sequence...");
        }
        
        // Mark as triggered
        hasTriggered = true;
        
        // Start the cabin entry sequence
        StartCoroutine(HandleCabinEntrySequence());
    }

    private IEnumerator HandleCabinEntrySequence()
    {
        // ============= PHASE 1: COMPLETE OBJECTIVE 1 (ENTER CABIN) =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 1: Completing 'Enter the cabin' objective...");
        }
        
        QuestManager.Instance.CompleteObjectiveByTitle(enterCabinObjective);
        
        yield return new WaitForSeconds(0.5f); // Brief pause to let quest UI update
        
        // ============= PHASE 2: CACHE COMPONENTS =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 2: Caching components...");
        }
        
        CacheComponents();
        
        if (arinAI == null)
        {
            Debug.LogError("CabinQuestTrigger: Arin NPC not found! Cannot start lecture.");
            yield break;
        }
        
        Transform player = FindPlayer();
        if (player == null)
        {
            Debug.LogError("CabinQuestTrigger: Player not found!");
            yield break;
        }
        
        // ============= PHASE 3: HIDE UI =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 3: Hiding UI...");
        }
        
        HideUIImmediately();
        yield return new WaitForEndOfFrame();
        
        // ============= PHASE 4: DISABLE ARIN AI & PREPARE FOR MOVEMENT =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 4: Preparing Arin for lecture approach...");
        }
        
        arinAI.enabled = false;
        arinRb.linearVelocity = Vector2.zero;
        
        // Store original physics
        originalArinBodyType = arinRb.bodyType;
        originalArinGravity = arinRb.gravityScale;
        
        // Set to Dynamic with gravity for platform-aware movement
        arinRb.bodyType = RigidbodyType2D.Dynamic;
        arinRb.gravityScale = originalArinGravity;
        
        // ============= PHASE 5: MOVE ARIN TO CONVERSATION DISTANCE =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 5: Moving Arin to conversation position...");
        }
        
        yield return StartCoroutine(MoveArinToConversationDistance(player));
        
        // ============= PHASE 6: FACE EACH OTHER =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 6: Characters facing each other...");
        }
        
        FaceBothCharactersTogether(player);
        
        // Ensure Arin is stationary
        if (arinAnimator != null)
        {
            TrySetAnimatorBool(arinAnimator, "isMoving", false);
        }
        arinRb.linearVelocity = Vector2.zero;
        
        // ============= PHASE 7: WAIT BEFORE DIALOGUE =============
        yield return new WaitForSeconds(dialogueDelay);
        
        // ============= PHASE 8: START LECTURE DIALOGUE =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 8: Starting lecture dialogue...");
        }
        
        isLectureActive = true;
        
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartConversation(lectureDialogueId);
            DialogueManager.Instance.OnConversationCompleted += OnLectureCompleted;
        }
        else
        {
            Debug.LogWarning("DialogueManager not found. Simulating lecture completion.");
            yield return new WaitForSeconds(2f);
            OnLectureCompleted(lectureDialogueId);
        }
    }

    private IEnumerator MoveArinToConversationDistance(Transform player)
    {
        int iterationCount = 0;
        const int maxIterations = 1000;
        
        while (iterationCount < maxIterations)
        {
            iterationCount++;
            
            Vector3 playerPos = player.position;
            Vector3 arinPos = arinAI.transform.position;
            
            float currentDistance = Vector2.Distance(
                new Vector2(arinPos.x, arinPos.y),
                new Vector2(playerPos.x, playerPos.y)
            );
            
            // Check if arrived
            if (Mathf.Abs(currentDistance - conversationDistance) <= stopDistance)
            {
                if (debugMode)
                {
                    Debug.Log($"[CabinTrigger] ✓ Arin arrived at conversation distance after {iterationCount} iterations!");
                }
                break;
            }
            
            // Calculate direction vector from player to Arin
            Vector2 playerToArin = new Vector2(arinPos.x - playerPos.x, arinPos.y - playerPos.y);
            playerToArin.Normalize();
            
            // Calculate exact target position
            Vector3 targetPosition = playerPos + new Vector3(
                playerToArin.x * conversationDistance,
                playerToArin.y * conversationDistance,
                0f
            );
            
            // Calculate movement direction toward target
            Vector2 moveDir = (targetPosition - arinPos).normalized;
            
            // Use velocity-based movement for platform awareness
            Vector2 horizontalVelocity = new Vector2(moveDir.x * approachSpeed, arinRb.linearVelocity.y);
            arinRb.linearVelocity = horizontalVelocity;
            
            // Update sprite facing
            if (arinSprite != null && Mathf.Abs(moveDir.x) > 0.01f)
            {
                arinSprite.flipX = (moveDir.x < 0);
            }
            
            // Update animation
            if (arinAnimator != null)
            {
                TrySetAnimatorBool(arinAnimator, "isMoving", true);
            }
            
            yield return null;
        }
        
        // Stop movement
        arinRb.linearVelocity = Vector2.zero;
        if (arinAnimator != null)
        {
            TrySetAnimatorBool(arinAnimator, "isMoving", false);
        }
    }

    private void OnLectureCompleted(string conversationId)
    {
        if (conversationId != lectureDialogueId) return;
        
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] ✓ Lecture dialogue completed!");
        }
        
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnConversationCompleted -= OnLectureCompleted;
        
        StartCoroutine(HandlePostLectureSequence());
    }

    private IEnumerator HandlePostLectureSequence()
    {
        // ============= PHASE 9: COMPLETE OBJECTIVE 2 (LECTURE) =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 9: Completing 'Listen to lecture' objective...");
        }
        
        QuestManager.Instance.CompleteObjectiveByTitle(lecturObjective);
        
        yield return new WaitForSeconds(0.5f);
        
        // ============= PHASE 10: RESTORE UI =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 10: Restoring UI...");
        }
        
        RestoreUI();
        
        // ============= PHASE 11: RESTORE ARIN PHYSICS =============
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] PHASE 11: Restoring Arin's original state...");
        }
        
        RestoreArinPhysics();
        
        // Re-enable Arin AI
        if (arinAI != null)
        {
            arinAI.enabled = true;
        }
        
        isLectureActive = false;
        
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] ✓✓✓ Quest 2 lecture sequence completed successfully!");
        }
    }

    #region Helper Methods
    private void CacheComponents()
    {
        // Cache Arin components
        if (arinAI == null) arinAI = Object.FindFirstObjectByType<ArinNPCAI>();
        if (arinAI != null)
        {
            if (arinAnimator == null) arinAnimator = arinAI.GetComponentInChildren<Animator>();
            if (arinRb == null) arinRb = arinAI.GetComponent<Rigidbody2D>();
            if (arinSprite == null) arinSprite = arinAI.GetComponentInChildren<SpriteRenderer>();
        }
        
        // Cache player components
        if (playerMovement == null) playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        if (uiController == null) uiController = Object.FindFirstObjectByType<UIController>();
        
        // Cache UI CanvasGroup if not assigned
        if (uiCanvasGroup == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                uiCanvasGroup = canvas.GetComponent<CanvasGroup>();
            }
        }
    }

    private Transform FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        return playerObj != null ? playerObj.transform : null;
    }

    private void HideUIImmediately()
    {
        if (uiCanvasGroup != null)
        {
            originalAlpha = uiCanvasGroup.alpha;
            originalInteractable = uiCanvasGroup.interactable;
            originalBlocksRaycasts = uiCanvasGroup.blocksRaycasts;
            
            uiCanvasGroup.alpha = 0f;
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
            
            if (debugMode)
            {
                Debug.Log("[CabinTrigger] UI hidden");
            }
        }
    }

    private void RestoreUI()
    {
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = originalAlpha;
            uiCanvasGroup.interactable = originalInteractable;
            uiCanvasGroup.blocksRaycasts = originalBlocksRaycasts;
            
            if (uiController != null)
            {
                uiController.ReinitializeButtons();
            }
            
            if (debugMode)
            {
                Debug.Log("[CabinTrigger] UI restored");
            }
        }
    }

    private void RestoreArinPhysics()
    {
        if (arinRb != null)
        {
            arinRb.bodyType = originalArinBodyType;
            arinRb.gravityScale = originalArinGravity;
            arinRb.linearVelocity = Vector2.zero;
            
            if (debugMode)
            {
                Debug.Log($"[CabinTrigger] Arin physics restored: BodyType={originalArinBodyType}, Gravity={originalArinGravity}");
            }
        }
    }

    private void FaceBothCharactersTogether(Transform player)
    {
        Vector2 directionArinToPlayer = (player.position - arinAI.transform.position).normalized;
        Vector2 directionPlayerToArin = (arinAI.transform.position - player.position).normalized;
        
        if (arinSprite != null)
            arinSprite.flipX = (directionArinToPlayer.x < 0);
        
        if (playerMovement != null)
        {
            SpriteRenderer playerSprite = playerMovement.GetComponent<SpriteRenderer>();
            if (playerSprite == null)
                playerSprite = playerMovement.GetComponentInChildren<SpriteRenderer>();
            
            if (playerSprite != null)
                playerSprite.flipX = (directionPlayerToArin.x < 0);
        }
        
        if (debugMode)
        {
            Debug.Log("[CabinTrigger] Characters are now facing each other");
        }
    }

    private static void TrySetAnimatorBool(Animator animator, string param, bool value)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        
        foreach (var p in animator.parameters)
        {
            if (p.name == param && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(param, value);
                return;
            }
        }
    }
    #endregion

    #region Debug Gizmos
    void OnDrawGizmos()
    {
        // Draw trigger zone in editor
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = hasTriggered ? Color.gray : Color.yellow;
            
            if (col is BoxCollider2D boxCol)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            }
            else if (col is CircleCollider2D circleCol)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
            }
        }
        
        // Draw label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
            $"Cabin Quest Trigger\n{enterCabinObjective}\n{lecturObjective}", 
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
        #endif
    }
    #endregion
}