using UnityEngine;
using System.Collections;

/// <summary>
/// QUEST 3: Handles player's return to cabin for lecture dialogue
/// - Objective 1: Player returns to cabin (trigger zone)
/// - Objective 2: Arin initiates dialogue and gives lecture (Arin stays at current position)
/// - After dialogue: Objective 3 activates (collect runes) - Arin remains idle at cabin
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Quest3CabinReturnController : MonoBehaviour
{
    [Header("Quest 3 Settings")]
    [Tooltip("Quest ID for Quest 3")]
    [SerializeField] private string questId = "Q3_Types"; // VERIFY THIS MATCHES YOUR QUEST DATA

    [Tooltip("Objective 1: Return to cabin")]
    [SerializeField] private string returnToCabinObjective = "Return to Arin's Cabin";

    [Tooltip("Objective 2: Listen to lecture")]
    [SerializeField] private string lectureObjective = "Listen to Arin's Lesson: Data Types";

    [Tooltip("Dialogue conversation ID for Arin's lecture")]
    [SerializeField] private string lectureDialogueId = "Arin_03_TypesLecture"; // FIXED: was "Arin_03_DataTypes"

    [Header("Conversation Settings")]
    [Tooltip("Distance Arin should be from player during conversation")]
    [SerializeField] private float conversationDistance = 5.0f;

    [Tooltip("Speed Arin approaches player (if needed)")]
    [SerializeField] private float approachSpeed = 2.0f;

    [Tooltip("How close Arin needs to be before stopping")]
    [SerializeField] private float stopDistance = 0.1f;

    [Header("Distance Thresholds")]
    [Tooltip("If Arin is closer than this, FORCE move away to reach conversation distance")]
    [SerializeField] private float tooCloseThreshold = 3.0f;

    [Tooltip("If Arin is farther than this, approach continuously until reaching conversation distance")]
    [SerializeField] private float tooFarThreshold = 7.0f;

    [Tooltip("If Arin is THIS close (overlapping/same position), use forced separation")]
    [SerializeField] private float overlappingThreshold = 0.5f;

    [Header("Forced Separation Settings")]
    [Tooltip("Default direction when characters overlap (1 = right, -1 = left)")]
    [SerializeField] private float defaultSeparationDirection = 1f;

    [Header("Timing")]
    [Tooltip("Delay before starting greeting dialogue after player enters cabin")]
    [SerializeField] private float greetingDialogueDelay = 0.5f;

    [Tooltip("Extra delay after facing each other to ensure orientation is correct")]
    [SerializeField] private float postFacingDelay = 0.3f;

    [Header("UI References")]
    [SerializeField] private CanvasGroup uiCanvasGroup;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool hasTriggered = false;
    private bool isDialogueActive = false;
    private ArinNPCAI arinAI;
    private Animator arinAnimator;
    private Rigidbody2D arinRb;
    private SpriteRenderer arinSprite;
    private PlayerMovement playerMovement;
    private UIController uiController;

    // Store original physics for Arin
    private RigidbodyType2D originalArinBodyType;
    private float originalArinGravity;
    private Vector3 arinOriginalPosition; // NEW: Store Arin's position before movement

    // UI state storage
    private bool originalInteractable;
    private bool originalBlocksRaycasts;
    private float originalAlpha;
    private Color[] originalImageColors;
    private Color[] originalTextColors;
    private Color[] originalSpriteColors;
    private UnityEngine.UI.Image[] allImages;
    private TMPro.TextMeshProUGUI[] allTexts;
    private SpriteRenderer[] allSprites;

    // Movement strategy enums
    private enum MovementStrategy
    {
        MoveCloser,
        MoveAway,
        MinorAdjustment
    }

    private enum MoveDirection
    {
        Closer,
        Away,
        Adjust
    }

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"Quest3CabinReturnController on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        if (QuestManager.Instance == null)
        {
            Debug.LogError("[Q3Cabin] QuestManager not found in scene!");
            return;
        }

        QuestData currentQuest = QuestManager.Instance.GetCurrentQuest();
        if (currentQuest == null || currentQuest.questId != questId)
        {
            if (debugMode)
            {
                Debug.Log($"[Q3Cabin] Quest 3 not active yet. Current quest: {currentQuest?.questId ?? "None"}");
            }
            return;
        }

        if (debugMode)
        {
            Debug.Log($"[Q3Cabin] Player returned to cabin! Starting Quest 3 dialogue sequence...");
        }

        hasTriggered = true;
        StartCoroutine(HandleCabinReturnSequence());
    }

    private IEnumerator HandleCabinReturnSequence()
    {
        // ============= PHASE 1: COMPLETE OBJECTIVE 1 (RETURN TO CABIN) =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 1: Completing 'Return to cabin' objective...");
        }

        QuestManager.Instance.CompleteObjectiveByTitle(returnToCabinObjective);

        yield return new WaitForSeconds(0.5f);

        // ============= PHASE 2: CACHE COMPONENTS =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 2: Caching components...");
        }

        CacheComponents();

        if (arinAI == null)
        {
            Debug.LogError("[Q3Cabin] Arin NPC not found! Cannot start dialogue.");
            yield break;
        }

        Transform player = FindPlayer();
        if (player == null)
        {
            Debug.LogError("[Q3Cabin] Player not found!");
            yield break;
        }

        // ============= PHASE 3: STORE ARIN'S ORIGINAL POSITION =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 3: Storing Arin's original position...");
        }

        arinOriginalPosition = arinAI.transform.position;
        Debug.Log($"[Q3Cabin] Arin's original position: {arinOriginalPosition}");

        // ============= PHASE 4: HIDE UI =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 4: Hiding UI...");
        }

        HideUIImmediately();
        yield return new WaitForEndOfFrame();

        // ============= PHASE 5: PREPARE ARIN FOR DIALOGUE =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 5: Preparing Arin for dialogue...");
        }

        // Disable AI but keep Arin in place
        arinAI.enabled = false;
        arinRb.linearVelocity = Vector2.zero;

        originalArinBodyType = arinRb.bodyType;
        originalArinGravity = arinRb.gravityScale;

        arinRb.bodyType = RigidbodyType2D.Dynamic;
        arinRb.gravityScale = originalArinGravity;

        // ============= PHASE 6: INTELLIGENT DISTANCE ADJUSTMENT =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 6: Adjusting Arin's position for conversation...");
        }

        yield return StartCoroutine(AdjustArinDistanceIntelligently(player));

        // ============= PHASE 7: ENSURE COMPLETE STOP =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 7: Ensuring Arin is completely stopped...");
        }

        if (arinRb != null)
        {
            arinRb.linearVelocity = Vector2.zero;
        }

        if (arinAnimator != null)
        {
            TrySetAnimatorBool(arinAnimator, "isMoving", false);
        }

        yield return null;
        yield return null;

        // ============= PHASE 8: FACE EACH OTHER =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 8: Characters facing each other...");
        }

        FaceBothCharactersTogether(player);

        yield return new WaitForSeconds(postFacingDelay);

        // ============= PHASE 9: VERIFY FACING =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 9: Verifying character facing...");
        }

        VerifyAndCorrectFacing(player);

        // ============= PHASE 10: WAIT BEFORE DIALOGUE =============
        yield return new WaitForSeconds(greetingDialogueDelay);

        // ============= PHASE 11: START LECTURE DIALOGUE =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 11: Starting lecture dialogue...");
        }

        isDialogueActive = true;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartConversation(lectureDialogueId); // Now uses correct ID
            DialogueManager.Instance.OnConversationCompleted += OnLectureCompleted;
        }
        else
        {
            Debug.LogWarning("[Q3Cabin] DialogueManager not found. Simulating lecture completion.");
            yield return new WaitForSeconds(2f);
            OnLectureCompleted(lectureDialogueId);
        }
    }

    #region Intelligent Distance Adjustment (Same as PostBoss)

    private IEnumerator AdjustArinDistanceIntelligently(Transform player)
    {
        Vector3 playerPos = player.position;
        Vector3 arinPos = arinAI.transform.position;

        float currentDistance = Vector2.Distance(
            new Vector2(arinPos.x, arinPos.y),
            new Vector2(playerPos.x, playerPos.y)
        );

        Debug.Log($"[Q3Cabin] Initial distance check:");
        Debug.Log($"  - Current distance: {currentDistance:F2} units");
        Debug.Log($"  - Target distance: {conversationDistance:F2} units");

        if (currentDistance >= conversationDistance - stopDistance &&
            currentDistance <= conversationDistance + stopDistance)
        {
            Debug.Log("[Q3Cabin] ✓ Distance is perfect - no adjustment needed");
            yield break;
        }

        if (currentDistance <= overlappingThreshold)
        {
            Debug.LogWarning($"[Q3Cabin] ⚠ OVERLAPPING DETECTED! Distance: {currentDistance:F2} units");
            yield return StartCoroutine(ForceSeparation(player));
            yield break;
        }

        MovementStrategy strategy = DetermineMovementStrategy(currentDistance);

        switch (strategy)
        {
            case MovementStrategy.MoveCloser:
                Debug.Log($"[Q3Cabin] → Arin is TOO FAR ({currentDistance:F2} units) - approaching");
                yield return StartCoroutine(MoveArinUntilExactDistance(player, MoveDirection.Closer));
                break;

            case MovementStrategy.MoveAway:
                Debug.Log($"[Q3Cabin] ← Arin is TOO CLOSE ({currentDistance:F2} units) - backing away");
                yield return StartCoroutine(MoveArinUntilExactDistance(player, MoveDirection.Away));
                break;

            case MovementStrategy.MinorAdjustment:
                Debug.Log($"[Q3Cabin] ↔ Minor distance adjustment needed");
                yield return StartCoroutine(MoveArinUntilExactDistance(player, MoveDirection.Adjust));
                break;
        }

        float finalDistance = Vector2.Distance(
            new Vector2(arinAI.transform.position.x, arinAI.transform.position.y),
            new Vector2(player.position.x, player.position.y)
        );
        Debug.Log($"[Q3Cabin] ✓ Final distance: {finalDistance:F2} units (target: {conversationDistance:F2})");
    }

    private IEnumerator ForceSeparation(Transform player)
    {
        Debug.Log("[Q3Cabin] FORCED SEPARATION SEQUENCE INITIATED");

        Vector3 playerPos = player.position;
        float separationDirection = DetermineIntelligentSeparationDirection(playerPos, arinAI.transform.position);

        Vector3 targetPosition = playerPos + new Vector3(
            separationDirection * conversationDistance,
            0f,
            0f
        );

        Debug.Log($"[Q3Cabin] Forced separation target: {targetPosition}");

        int iterationCount = 0;
        const int maxIterations = 500;

        while (iterationCount < maxIterations)
        {
            iterationCount++;

            Vector3 currentArinPos = arinAI.transform.position;
            float distanceToTarget = Vector2.Distance(
                new Vector2(currentArinPos.x, currentArinPos.y),
                new Vector2(targetPosition.x, targetPosition.y)
            );

            if (distanceToTarget <= stopDistance)
            {
                Debug.Log($"[Q3Cabin] ✓ Forced separation complete after {iterationCount} iterations");
                break;
            }

            Vector2 moveDir = (targetPosition - currentArinPos).normalized;
            float step = approachSpeed * Time.deltaTime;

            if (step >= distanceToTarget)
            {
                arinAI.transform.position = targetPosition;
            }
            else
            {
                Vector2 horizontalVelocity = new Vector2(moveDir.x * approachSpeed, arinRb.linearVelocity.y);
                arinRb.linearVelocity = horizontalVelocity;
            }

            // Update sprite facing during movement (PostBoss behavior)
            if (arinSprite != null && Mathf.Abs(moveDir.x) > 0.01f)
            {
                arinSprite.flipX = (moveDir.x < 0);
            }

            if (arinAnimator != null)
            {
                TrySetAnimatorBool(arinAnimator, "isMoving", true);
            }

            yield return null;
        }

        arinRb.linearVelocity = Vector2.zero;
        if (arinAnimator != null)
        {
            TrySetAnimatorBool(arinAnimator, "isMoving", false);
        }
    }

    private float DetermineIntelligentSeparationDirection(Vector3 playerPos, Vector3 arinPos)
    {
        if (playerMovement != null)
        {
            float playerFacing = playerMovement.GetFacingDirection();
            if (Mathf.Abs(playerFacing) > 0.01f)
            {
                float behindPlayer = -playerFacing;
                Debug.Log($"[Q3Cabin] Separation: Behind player (direction={behindPlayer})");
                return behindPlayer;
            }
        }

        Debug.Log($"[Q3Cabin] Separation: Using default direction ({defaultSeparationDirection})");
        return defaultSeparationDirection;
    }

    private MovementStrategy DetermineMovementStrategy(float currentDistance)
    {
        if (currentDistance < tooCloseThreshold)
            return MovementStrategy.MoveAway;
        else if (currentDistance > tooFarThreshold)
            return MovementStrategy.MoveCloser;
        else
            return MovementStrategy.MinorAdjustment;
    }

    private IEnumerator MoveArinUntilExactDistance(Transform player, MoveDirection moveDirection)
    {
        Debug.Log($"[Q3Cabin] Starting continuous movement: {moveDirection}");

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

            if (Mathf.Abs(currentDistance - conversationDistance) <= stopDistance)
            {
                Debug.Log($"[Q3Cabin] ✓ ARRIVED at target distance after {iterationCount} iterations!");
                break;
            }

            Vector2 playerToArin = new Vector2(arinPos.x - playerPos.x, arinPos.y - playerPos.y);

            if (playerToArin.magnitude < overlappingThreshold)
            {
                Debug.LogWarning("[Q3Cabin] Overlapping detected - triggering forced separation");
                yield return StartCoroutine(ForceSeparation(player));
                yield break;
            }

            playerToArin.Normalize();

            Vector3 targetPosition = playerPos + new Vector3(
                playerToArin.x * conversationDistance,
                playerToArin.y * conversationDistance,
                0f
            );

            Vector2 moveDir = (targetPosition - arinPos).normalized;

            Vector2 horizontalVelocity = new Vector2(moveDir.x * approachSpeed, arinRb.linearVelocity.y);
            arinRb.linearVelocity = horizontalVelocity;

            // Update sprite facing during movement (PostBoss behavior)
            if (arinSprite != null && Mathf.Abs(moveDir.x) > 0.01f)
            {
                arinSprite.flipX = (moveDir.x < 0);
            }

            if (arinAnimator != null)
            {
                TrySetAnimatorBool(arinAnimator, "isMoving", true);
            }

            yield return null;
        }

        arinRb.linearVelocity = Vector2.zero;
        if (arinAnimator != null)
        {
            TrySetAnimatorBool(arinAnimator, "isMoving", false);
        }
    }

    #endregion

    #region Character Facing Logic

    private void FaceBothCharactersTogether(Transform player)
    {
        if (arinAI == null || player == null)
        {
            Debug.LogError("[Q3Cabin] Cannot face characters - missing references!");
            return;
        }

        Vector3 arinPosition = arinAI.transform.position;
        Vector3 playerPosition = player.position;
        float relativeX = playerPosition.x - arinPosition.x;

        Debug.Log($"[Q3Cabin] Facing calculation:");
        Debug.Log($"  - Arin position: {arinPosition}");
        Debug.Log($"  - Player position: {playerPosition}");
        Debug.Log($"  - Relative X: {relativeX:F2}");

        if (arinSprite != null)
        {
            bool arinShouldFaceLeft = relativeX < 0;
            arinSprite.flipX = arinShouldFaceLeft;
            Debug.Log($"  - Arin facing: {(arinShouldFaceLeft ? "LEFT" : "RIGHT")} (flipX={arinShouldFaceLeft})");
        }

        if (playerMovement != null)
        {
            SpriteRenderer playerSprite = playerMovement.GetComponent<SpriteRenderer>();
            if (playerSprite == null)
                playerSprite = playerMovement.GetComponentInChildren<SpriteRenderer>();

            if (playerSprite != null)
            {
                bool playerShouldFaceLeft = -relativeX < 0;
                playerSprite.flipX = playerShouldFaceLeft;
                Debug.Log($"  - Player facing: {(playerShouldFaceLeft ? "LEFT" : "RIGHT")} (flipX={playerShouldFaceLeft})");
            }
        }

        Debug.Log("[Q3Cabin] ✓ Both characters oriented to face each other");
    }

    private void VerifyAndCorrectFacing(Transform player)
    {
        if (arinAI == null || player == null)
            return;

        Vector3 arinPosition = arinAI.transform.position;
        Vector3 playerPosition = player.position;
        float relativeX = playerPosition.x - arinPosition.x;

        bool needsCorrection = false;

        if (arinSprite != null)
        {
            bool shouldFaceLeft = relativeX < 0;
            if (arinSprite.flipX != shouldFaceLeft)
            {
                Debug.LogWarning($"[Q3Cabin] ⚠ Correcting Arin's facing!");
                arinSprite.flipX = shouldFaceLeft;
                needsCorrection = true;
            }
        }

        if (playerMovement != null)
        {
            SpriteRenderer playerSprite = playerMovement.GetComponent<SpriteRenderer>();
            if (playerSprite == null)
                playerSprite = playerMovement.GetComponentInChildren<SpriteRenderer>();

            if (playerSprite != null)
            {
                bool shouldFaceLeft = -relativeX < 0;
                if (playerSprite.flipX != shouldFaceLeft)
                {
                    Debug.LogWarning($"[Q3Cabin] ⚠ Correcting Player's facing!");
                    playerSprite.flipX = shouldFaceLeft;
                    needsCorrection = true;
                }
            }
        }

        if (needsCorrection)
        {
            Debug.Log("[Q3Cabin] ✓ Facing corrected!");
        }
        else
        {
            Debug.Log("[Q3Cabin] ✓ Facing verification passed");
        }
    }

    #endregion

    #region Dialogue Completion Handlers

    private void OnLectureCompleted(string conversationId)
    {
        if (conversationId != lectureDialogueId) return;
        
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] ✓ Lecture dialogue completed!");
        }
        
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnConversationCompleted -= OnLectureCompleted;
        
        StartCoroutine(HandlePostLectureSequence());
    }

    private IEnumerator HandlePostLectureSequence()
    {
        // ============= PHASE 12: COMPLETE OBJECTIVE 2 (LECTURE) =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 12: Completing 'Listen to lecture' objective...");
        }

        QuestManager.Instance.CompleteObjectiveByTitle(lectureObjective);

        yield return new WaitForSeconds(0.5f);

        // ============= PHASE 13: RESTORE UI =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 13: Restoring UI...");
        }

        yield return StartCoroutine(RestoreUIGradually());

        // ============= PHASE 14: KEEP ARIN STATIONARY =============
        if (debugMode)
        {
            Debug.Log("[Q3Cabin] PHASE 14: Setting Arin to idle state at cabin...");
        }

        FinalizeArinState();

        isDialogueActive = false;

        if (debugMode)
        {
            Debug.Log("[Q3Cabin] ✓✓✓ Quest 3 dialogue completed! Arin waiting at cabin, Objective 3 (Collect Runes) now active.");
        }
    }

    private void FinalizeArinState()
    {
        // Stop all movement
        if (arinRb != null)
        {
            arinRb.linearVelocity = Vector2.zero;
            arinRb.bodyType = originalArinBodyType;
            arinRb.gravityScale = originalArinGravity;
        }

        // Set to idle animation
        if (arinAnimator != null)
        {
            TrySetAnimatorBool(arinAnimator, "isMoving", false);
        }

        // Face forward (default facing)
        if (arinSprite != null)
        {
            arinSprite.flipX = false;
        }

        // CRITICAL: Keep AI disabled so Arin stays at cabin
        if (arinAI != null)
        {
            arinAI.enabled = false;
        }

        Debug.Log($"[Q3Cabin] Arin is now in idle state at cabin position: {arinAI.transform.position}");
        Debug.Log("[Q3Cabin] Arin AI disabled - will remain at cabin until Quest 3 is completed");
    }

    #endregion

    #region Helper Methods

    private void CacheComponents()
    {
        if (arinAI == null) arinAI = Object.FindFirstObjectByType<ArinNPCAI>();
        if (arinAI != null)
        {
            if (arinAnimator == null) arinAnimator = arinAI.GetComponentInChildren<Animator>();
            if (arinRb == null) arinRb = arinAI.GetComponent<Rigidbody2D>();
            if (arinSprite == null) arinSprite = arinAI.GetComponentInChildren<SpriteRenderer>();
        }

        if (playerMovement == null) playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        if (uiController == null) uiController = Object.FindFirstObjectByType<UIController>();

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

            StoreOriginalStatesAndHide();

            if (debugMode)
            {
                Debug.Log("[Q3Cabin] UI hidden");
            }
        }
    }

    private void StoreOriginalStatesAndHide()
    {
        if (uiCanvasGroup != null)
        {
            allImages = uiCanvasGroup.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            originalImageColors = new Color[allImages.Length];

            for (int i = 0; i < allImages.Length; i++)
            {
                originalImageColors[i] = allImages[i].color;
                Color hiddenColor = allImages[i].color;
                hiddenColor.a = 0f;
                allImages[i].color = hiddenColor;
            }

            allTexts = uiCanvasGroup.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            originalTextColors = new Color[allTexts.Length];

            for (int i = 0; i < allTexts.Length; i++)
            {
                originalTextColors[i] = allTexts[i].color;
                Color hiddenColor = allTexts[i].color;
                hiddenColor.a = 0f;
                allTexts[i].color = hiddenColor;
            }

            allSprites = uiCanvasGroup.GetComponentsInChildren<SpriteRenderer>(true);
            originalSpriteColors = new Color[allSprites.Length];

            for (int i = 0; i < allSprites.Length; i++)
            {
                originalSpriteColors[i] = allSprites[i].color;
                Color hiddenColor = allSprites[i].color;
                hiddenColor.a = 0f;
                allSprites[i].color = hiddenColor;
            }

            UnityEngine.UI.Button[] buttons = uiCanvasGroup.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (UnityEngine.UI.Button button in buttons)
            {
                button.interactable = false;
            }

            UnityEngine.UI.Slider[] sliders = uiCanvasGroup.GetComponentsInChildren<UnityEngine.UI.Slider>(true);
            foreach (UnityEngine.UI.Slider slider in sliders)
            {
                slider.interactable = false;
            }
        }
    }

    private IEnumerator RestoreUIGradually()
    {
        uiCanvasGroup.alpha = originalAlpha;
        uiCanvasGroup.interactable = originalInteractable;
        uiCanvasGroup.blocksRaycasts = originalBlocksRaycasts;

        yield return null;

        RestoreOriginalStates();

        yield return null;

        Canvas.ForceUpdateCanvases();

        yield return null;

        if (uiController != null)
        {
            uiController.ReinitializeButtons();
        }

        if (debugMode)
        {
            Debug.Log("[Q3Cabin] UI fully restored");
        }
    }

    private void RestoreOriginalStates()
    {
        if (uiCanvasGroup != null)
        {
            if (allImages != null && originalImageColors != null)
            {
                for (int i = 0; i < allImages.Length && i < originalImageColors.Length; i++)
                {
                    if (allImages[i] != null)
                        allImages[i].color = originalImageColors[i];
                }
            }

            if (allTexts != null && originalTextColors != null)
            {
                for (int i = 0; i < allTexts.Length && i < originalTextColors.Length; i++)
                {
                    if (allTexts[i] != null)
                        allTexts[i].color = originalTextColors[i];
                }
            }

            if (allSprites != null && originalSpriteColors != null)
            {
                for (int i = 0; i < allSprites.Length && i < originalSpriteColors.Length; i++)
                {
                    if (allSprites[i] != null)
                        allSprites[i].color = originalSpriteColors[i];
                }
            }

            UnityEngine.UI.Button[] buttons = uiCanvasGroup.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (UnityEngine.UI.Button button in buttons)
            {
                button.interactable = true;
            }

            UnityEngine.UI.Slider[] sliders = uiCanvasGroup.GetComponentsInChildren<UnityEngine.UI.Slider>(true);
            foreach (UnityEngine.UI.Slider slider in sliders)
            {
                slider.interactable = true;
            }
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
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = hasTriggered ? Color.gray : Color.green;

            if (col is BoxCollider2D boxCol)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            }
            else if (col is CircleCollider2D circleCol)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
            }
        }

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
            $"QUEST 3: Cabin Return Trigger\n{returnToCabinObjective}\n{lectureObjective}", 
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.green } });
        #endif
    }

    #endregion
}