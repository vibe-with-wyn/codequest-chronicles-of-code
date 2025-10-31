using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostBossConversationController : MonoBehaviour
{
    [Header("Approach Settings")]
    [SerializeField] private float approachSpeed = 2.5f;
    [SerializeField] private float conversationDistance = 5.0f;
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private float startDelayAfterDeath = 0.8f;

    [Header("Distance Thresholds")]
    [Tooltip("If Arin is closer than this, FORCE move away to reach 5 units")]
    [SerializeField] private float tooCloseThreshold = 3.0f;
    [Tooltip("If Arin is farther than this, approach continuously until reaching 5 units")]
    [SerializeField] private float tooFarThreshold = 7.0f;
    [Tooltip("If Arin is THIS close (overlapping/same position), use forced separation")]
    [SerializeField] private float overlappingThreshold = 0.5f;

    [Header("Forced Separation Settings")]
    [Tooltip("Default direction when characters overlap (1 = right, -1 = left)")]
    [SerializeField] private float defaultSeparationDirection = 1f;
    [Tooltip("If true, try to separate Arin away from boss position")]
    [SerializeField] private bool separateAwayFromBoss = true;

    [Header("Conversation Settings")]
    [SerializeField] private float dialogueDelayAfterApproach = 1.5f;
    [Tooltip("Additional delay before starting dialogue conversation after both characters face each other")]
    [SerializeField] private float finalDialogueDelay = 0.5f;

    [Header("Cabin Destination")]
    [Tooltip("Exact cabin location where Arin will stop walking")]
    [SerializeField] private Vector3 cabinLocation = Vector3.zero;
    [Tooltip("How close Arin needs to be to cabin before stopping")]
    [SerializeField] private float cabinArrivalDistance = 0.5f;

    [Header("Cabin Walk Speed")]
    [SerializeField] private float cabinWalkSpeed = 3.0f;

    [Header("UI Canvas Reference")]
    [SerializeField] private CanvasGroup uiCanvasGroup;

    private ArinNPCAI arinAI;
    private Animator arinAnimator;
    private Rigidbody2D arinRb;
    private SpriteRenderer arinSprite;
    private PlayerMovement playerMovement;
    private UIController uiController;
    private QuestUIController questUIController; // NEW: Reference to quest UI
    private bool runningSequence;
    private Vector3 bossDeathPosition;

    // Store original Rigidbody2D settings for proper restoration
    private RigidbodyType2D originalBodyType;
    private float originalGravityScale;
    private bool originalIsKinematic;

    // Store UI state for restoration
    private bool originalInteractable;
    private bool originalBlocksRaycasts;
    private float originalAlpha;
    private Color[] originalImageColors;
    private Color[] originalTextColors;
    private Color[] originalSpriteColors;
    private Image[] allImages;
    private TextMeshProUGUI[] allTexts;
    private SpriteRenderer[] allSprites;
    private SpriteRenderer[] uiSpriteRenderers;
    private Color[] originalUISpriteColors;

    void OnEnable()
    {
        CaveBossAI.BossDefeated += OnBossDefeated;
    }

    void OnDisable()
    {
        CaveBossAI.BossDefeated -= OnBossDefeated;
    }

    private void CacheComponents()
    {
        // Cache Arin components
        if (arinAI == null) arinAI = Object.FindFirstObjectByType<ArinNPCAI>();
        if (arinAI != null)
        {
            if (arinAnimator == null) arinAnimator = arinAI.GetComponentInChildren<Animator>();
            if (arinRb == null) arinRb = arinAI.GetComponent<Rigidbody2D>();
            if (arinSprite == null) arinSprite = arinAI.GetComponentInChildren<SpriteRenderer>();

            // Store original Rigidbody2D settings for restoration
            if (arinRb != null)
            {
                originalBodyType = arinRb.bodyType;
                originalGravityScale = arinRb.gravityScale;
                originalIsKinematic = (originalBodyType == RigidbodyType2D.Kinematic);
                Debug.Log($"[PostBoss] Stored original Arin physics: BodyType={originalBodyType}, GravityScale={originalGravityScale}");
            }
        }

        // Cache player components
        if (playerMovement == null) playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        if (uiController == null) uiController = Object.FindFirstObjectByType<UIController>();

        // NEW: Cache quest UI controller
        if (questUIController == null) questUIController = Object.FindFirstObjectByType<QuestUIController>();

        // Cache UI CanvasGroup if not assigned
        if (uiCanvasGroup == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                uiCanvasGroup = canvas.GetComponent<CanvasGroup>();
                Debug.Log("[PostBoss] Auto-found UI CanvasGroup");
            }
        }
    }

    private void OnBossDefeated(CaveBossAI boss)
    {
        if (runningSequence) return;

        // CRITICAL: Store boss position for intelligent separation
        if (boss != null)
        {
            bossDeathPosition = boss.transform.position;
            Debug.Log($"[PostBoss] Boss defeated at position: {bossDeathPosition}");
        }

        CacheComponents();

        if (arinAI == null || arinRb == null)
        {
            Debug.LogError("PostBossConversationController: Arin or Rigidbody2D not found!");
            return;
        }

        Transform player = FindPlayer();
        if (player == null)
        {
            Debug.LogError("PostBossConversationController: Player not found!");
            return;
        }

        StartCoroutine(HandlePostBossSequence(player));
    }

    private Transform FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        return playerObj != null ? playerObj.transform : null;
    }

    private IEnumerator HandlePostBossSequence(Transform player)
    {
        runningSequence = true;

        // ============= PHASE 0: DISABLE ARIN COMBAT BEHAVIORS =============
        Debug.Log("[PostBoss] PHASE 0: Disabling Arin's combat behaviors...");
        DisableArinCombatBehaviors();
        yield return null;

        // ============= PHASE 1: HIDE UI COMPLETELY =============
        Debug.Log("[PostBoss] PHASE 1: Hiding UI...");
        HideUIImmediately();
        yield return new WaitForEndOfFrame();

        // Wait for boss death animation to settle
        yield return new WaitForSeconds(startDelayAfterDeath);

        // ============= PHASE 2: DISABLE ARIN AI & PREPARE FOR MOVEMENT =============
        Debug.Log("[PostBoss] PHASE 2: Disabling Arin AI for cinematic...");
        arinAI.enabled = false;
        arinRb.linearVelocity = Vector2.zero;

        // FIXED: Use Dynamic with gravity for conversation approach to respect platforms
        if (arinRb != null)
        {
            arinRb.bodyType = RigidbodyType2D.Dynamic;
            arinRb.gravityScale = originalGravityScale; // Restore original gravity
            Debug.Log($"[PostBoss] Set Arin to Dynamic with gravity={originalGravityScale} for platform-aware movement");
        }

        // ============= PHASE 3: INTELLIGENT DISTANCE ADJUSTMENT =============
        Debug.Log("[PostBoss] PHASE 3: Adjusting Arin's position for conversation...");
        yield return StartCoroutine(AdjustArinDistanceIntelligently(player));

        // ============= PHASE 4: FACE EACH OTHER FOR CONVERSATION =============
        Debug.Log("[PostBoss] PHASE 4: Characters facing each other...");
        FaceBothCharactersTogether(player);

        // Ensure Arin is stationary before dialogue
        if (arinAnimator != null)
        {
            TrySetAnimatorBool(arinAnimator, "isMoving", false);
        }
        arinRb.linearVelocity = Vector2.zero;

        // ============= PHASE 5: WAIT FOR CINEMATIC POSITIONING =============
        yield return new WaitForSeconds(dialogueDelayAfterApproach);

        // ============= PHASE 6: FINAL DELAY BEFORE DIALOGUE =============
        yield return new WaitForSeconds(finalDialogueDelay);

        // ============= PHASE 7: START DIALOGUE =============
        Debug.Log("[PostBoss] PHASE 7: Starting dialogue...");
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartConversation("Arin_01_PostBoss");
            DialogueManager.Instance.OnConversationCompleted += OnDialogueCompleted;
        }
        else
        {
            Debug.LogWarning("DialogueManager not found. Simulating dialogue completion.");
            yield return new WaitForSeconds(2f);
            OnDialogueCompleted("Arin_01_PostBoss");
        }
    }

    #region Distance Adjustment (Conversation Positioning)
    private IEnumerator AdjustArinDistanceIntelligently(Transform player)
    {
        Vector3 playerPos = player.position;
        Vector3 arinPos = arinAI.transform.position;

        float currentDistance = Vector2.Distance(
            new Vector2(arinPos.x, arinPos.y),
            new Vector2(playerPos.x, playerPos.y)
        );

        Debug.Log($"[PostBoss] Initial distance check:");
        Debug.Log($"  - Current distance: {currentDistance:F2} units");
        Debug.Log($"  - Target distance: {conversationDistance:F2} units");

        // Check if adjustment is needed
        if (currentDistance >= conversationDistance - stopDistance &&
            currentDistance <= conversationDistance + stopDistance)
        {
            Debug.Log("[PostBoss] ✓ Distance is perfect - no adjustment needed");
            yield break;
        }

        // CRITICAL: Check for overlapping/same position scenario
        if (currentDistance <= overlappingThreshold)
        {
            Debug.LogWarning($"[PostBoss] ⚠ OVERLAPPING DETECTED! Distance: {currentDistance:F2} units");
            yield return StartCoroutine(ForceSeparation(player));
            yield break;
        }

        // Determine movement direction based on current distance
        MovementStrategy strategy = DetermineMovementStrategy(currentDistance);

        switch (strategy)
        {
            case MovementStrategy.MoveCloser:
                Debug.Log($"[PostBoss] → Arin is TOO FAR ({currentDistance:F2} units) - approaching");
                yield return StartCoroutine(MoveArinUntilExactDistance(player, MoveDirection.Closer));
                break;

            case MovementStrategy.MoveAway:
                Debug.Log($"[PostBoss] ← Arin is TOO CLOSE ({currentDistance:F2} units) - backing away");
                yield return StartCoroutine(MoveArinUntilExactDistance(player, MoveDirection.Away));
                break;

            case MovementStrategy.MinorAdjustment:
                Debug.Log($"[PostBoss] ↔ Minor distance adjustment needed");
                yield return StartCoroutine(MoveArinUntilExactDistance(player, MoveDirection.Adjust));
                break;
        }

        // Final verification
        float finalDistance = Vector2.Distance(
            new Vector2(arinAI.transform.position.x, arinAI.transform.position.y),
            new Vector2(player.position.x, player.position.y)
        );
        Debug.Log($"[PostBoss] ✓ Final distance: {finalDistance:F2} units (target: {conversationDistance:F2})");
    }

    private IEnumerator ForceSeparation(Transform player)
    {
        Debug.Log("[PostBoss] FORCED SEPARATION SEQUENCE INITIATED");

        Vector3 playerPos = player.position;
        float separationDirection = DetermineIntelligentSeparationDirection(playerPos, arinAI.transform.position);

        // Calculate target position: exactly conversationDistance units in separation direction
        Vector3 targetPosition = playerPos + new Vector3(
            separationDirection * conversationDistance,
            0f,
            0f
        );

        Debug.Log($"[PostBoss] Forced separation target: {targetPosition}");

        // Move Arin to the forced separation position
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
                Debug.Log($"[PostBoss] ✓ Forced separation complete after {iterationCount} iterations");
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
                // FIXED: Use velocity for horizontal movement, let gravity handle vertical
                Vector2 horizontalVelocity = new Vector2(moveDir.x * approachSpeed, arinRb.linearVelocity.y);
                arinRb.linearVelocity = horizontalVelocity;
            }

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

    private float DetermineIntelligentSeparationDirection(Vector3 playerPos, Vector3 arinPos)
    {
        // Strategy 1: Separate away from boss death position
        if (separateAwayFromBoss && bossDeathPosition != Vector3.zero)
        {
            Vector2 bossToPlayer = new Vector2(playerPos.x - bossDeathPosition.x, 0f);

            if (Mathf.Abs(bossToPlayer.x) > 0.1f)
            {
                float awayFromBoss = Mathf.Sign(bossToPlayer.x);
                Debug.Log($"[PostBoss] Separation: Away from boss (moving {(awayFromBoss > 0 ? "RIGHT" : "LEFT")})");
                return awayFromBoss;
            }
        }

        // Strategy 2: Check player's facing direction
        if (playerMovement != null)
        {
            float playerFacing = playerMovement.GetFacingDirection();
            if (Mathf.Abs(playerFacing) > 0.01f)
            {
                float behindPlayer = -playerFacing;
                Debug.Log($"[PostBoss] Separation: Behind player");
                return behindPlayer;
            }
        }

        // Strategy 3: Use default direction
        return defaultSeparationDirection;
    }

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
        Debug.Log($"[PostBoss] Starting continuous movement: {moveDirection}");

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
                Debug.Log($"[PostBoss] ✓ ARRIVED at target distance after {iterationCount} iterations!");
                break;
            }

            // Calculate direction vector from player to Arin
            Vector2 playerToArin = new Vector2(arinPos.x - playerPos.x, arinPos.y - playerPos.y);

            if (playerToArin.magnitude < overlappingThreshold)
            {
                Debug.LogWarning("[PostBoss] Overlapping detected - triggering forced separation");
                yield return StartCoroutine(ForceSeparation(player));
                yield break;
            }

            playerToArin.Normalize();

            // Calculate exact target position
            Vector3 targetPosition = playerPos + new Vector3(
                playerToArin.x * conversationDistance,
                playerToArin.y * conversationDistance,
                0f
            );

            // Calculate movement direction toward target
            Vector2 moveDir = (targetPosition - arinPos).normalized;

            // FIXED: Use velocity-based movement for platform awareness
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
    #endregion

    #region Dialogue Completion & Cabin Walk
    private void OnDialogueCompleted(string conversationId)
    {
        if (conversationId != "Arin_01_PostBoss") return;

        Debug.Log("[PostBoss] ✓ Dialogue completed - restoring UI and triggering quest");
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnConversationCompleted -= OnDialogueCompleted;

        StartCoroutine(HandlePostDialogueSequence());
    }

    // FIXED: Restore UI, complete quest, wait for quest UI auto-close, then walk to cabin
    private IEnumerator HandlePostDialogueSequence()
    {
        // ============= PHASE 8: RESTORE GAME UI =============
        Debug.Log("[PostBoss] PHASE 8: Restoring Game UI...");
        yield return StartCoroutine(RestoreUIGradually());

        // ============= PHASE 9: COMPLETE QUEST (TRIGGERS QUEST UI DISPLAY) =============
        Debug.Log("[PostBoss] PHASE 9: Completing quest objective (will trigger quest UI display)...");

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteObjectiveByTitle("Defeat the Night Borne");
            Debug.Log("[PostBoss] Quest objective 'Defeat the Night Borne' completed - Quest UI should now be visible!");
        }

        // ============= PHASE 10: WAIT FOR QUEST UI AUTO-CLOSE =============
        // FIXED: Get the auto-close duration from QuestUIController to ensure consistency
        float questAutoCloseDuration = 5f; // Default fallback

        if (questUIController != null)
        {
            // Access the autoDisplayDuration field via reflection (it's private)
            var autoDisplayDurationField = typeof(QuestUIController).GetField("autoDisplayDuration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (autoDisplayDurationField != null)
            {
                questAutoCloseDuration = (float)autoDisplayDurationField.GetValue(questUIController);
                Debug.Log($"[PostBoss] Retrieved quest auto-close duration from QuestUIController: {questAutoCloseDuration}s");
            }
            else
            {
                Debug.LogWarning("[PostBoss] Could not retrieve autoDisplayDuration - using fallback value of 5s");
            }
        }

        Debug.Log($"[PostBoss] PHASE 10: Waiting {questAutoCloseDuration}s for quest UI to auto-close (or user to click continue)...");
        yield return new WaitForSeconds(questAutoCloseDuration);

        // ============= PHASE 11: START CABIN WALK =============
        Debug.Log("[PostBoss] PHASE 11: Quest UI should be closed - starting cabin walk sequence...");
        yield return StartCoroutine(HandleCabinWalkSequence());
    }

    private IEnumerator HandleCabinWalkSequence()
    {
        Debug.Log("[PostBoss] Arin leading the way to cabin...");

        // Validate cabin location
        if (cabinLocation == Vector3.zero)
        {
            Debug.LogError("[PostBoss] ⚠ Cabin location not set! Please set X and Y values in inspector.");
            runningSequence = false;
            yield break;
        }

        Debug.Log($"[PostBoss] Cabin destination: X={cabinLocation.x}, Y={cabinLocation.y}");

        // Ensure Arin AI remains disabled during walk
        if (arinAI != null)
            arinAI.enabled = false;

        // Enable Dynamic physics with gravity for natural falling on stairs
        if (arinRb != null)
        {
            arinRb.bodyType = RigidbodyType2D.Dynamic;
            arinRb.gravityScale = originalGravityScale;
            arinRb.linearVelocity = Vector2.zero;
            Debug.Log($"[PostBoss] Physics enabled: Dynamic with gravity={originalGravityScale}");
        }

        // Calculate initial X-only distance (horizontal distance to cabin)
        float initialDistanceX = Mathf.Abs(arinAI.transform.position.x - cabinLocation.x);
        Debug.Log($"[PostBoss] Initial horizontal distance to cabin: {initialDistanceX:F2}m");

        // Walk to cabin - gravity will handle vertical movement naturally
        int frameCount = 0;
        while (true)
        {
            frameCount++;
            Vector3 currentPos = arinAI.transform.position;

            // CRITICAL: Only check HORIZONTAL distance (X-axis) for arrival
            float distanceX = Mathf.Abs(currentPos.x - cabinLocation.x);

            // Check arrival based on X distance only
            if (distanceX <= cabinArrivalDistance)
            {
                Debug.Log($"[PostBoss] ✓ Arrived at cabin after {frameCount} frames! (X distance: {distanceX:F2}m)");
                break;
            }

            // Calculate horizontal direction
            float directionX = Mathf.Sign(cabinLocation.x - currentPos.x);

            // Use velocity for horizontal movement - gravity handles stairs naturally
            arinRb.linearVelocity = new Vector2(directionX * cabinWalkSpeed, arinRb.linearVelocity.y);

            // Update sprite facing
            if (arinSprite != null)
                arinSprite.flipX = (directionX < 0);

            // CRITICAL: Manually update animation since ArinNPCAI is disabled
            if (arinAnimator != null)
                TrySetAnimatorBool(arinAnimator, "isMoving", true);

            // Log progress using X-distance only (every 60 frames)
            if (frameCount % 60 == 0)
            {
                Debug.Log($"[PostBoss] Walking to cabin: {distanceX:F2}m remaining horizontally (frame {frameCount})");
            }

            yield return null;
        }

        // CRITICAL: Stop movement and set to IDLE state
        if (arinRb != null)
        {
            arinRb.linearVelocity = Vector2.zero;
            Debug.Log("[PostBoss] Rigidbody velocity set to zero");
        }

        // CRITICAL: Manually stop animation since ArinNPCAI is disabled
        if (arinAnimator != null)
        {
            TrySetAnimatorBool(arinAnimator, "isMoving", false);
            Debug.Log("[PostBoss] Arin animation MANUALLY set to idle (isMoving = false)");
        }

        // Reset sprite to face forward (not flipped)
        if (arinSprite != null)
        {
            arinSprite.flipX = false;
            Debug.Log("[PostBoss] Arin sprite reset to face forward (flipX = false)");
        }

        // Restore original physics
        RestoreOriginalPhysics();

        Debug.Log($"[PostBoss] ✓✓✓ Arin successfully arrived at cabin and is now in IDLE state!");
        Debug.Log($"[PostBoss] Final position: {arinAI.transform.position}");
        runningSequence = false;
    }
    #endregion

    #region Helper Methods
    private void RestoreOriginalPhysics()
    {
        if (arinRb != null)
        {
            arinRb.bodyType = originalBodyType;
            arinRb.gravityScale = originalGravityScale;
            arinRb.linearVelocity = Vector2.zero;

            Debug.Log($"[PostBoss] Physics restored: BodyType={originalBodyType}, GravityScale={originalGravityScale}");
        }
    }

    private void DisableArinCombatBehaviors()
    {
        if (arinAI == null) return;

        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        System.Reflection.FieldInfo bossCaveBossTargetField = typeof(ArinNPCAI).GetField("caveBossTarget", bindingFlags);
        if (bossCaveBossTargetField != null)
            bossCaveBossTargetField.SetValue(arinAI, null);

        System.Reflection.FieldInfo battleStartedField = typeof(ArinNPCAI).GetField("battleStarted", bindingFlags);
        if (battleStartedField != null)
            battleStartedField.SetValue(arinAI, true);

        System.Reflection.FieldInfo isBossInBattleRangeField = typeof(ArinNPCAI).GetField("isBossInBattleRange", bindingFlags);
        if (isBossInBattleRangeField != null)
            isBossInBattleRangeField.SetValue(arinAI, false);

        System.Reflection.FieldInfo isBossInAttackProximityField = typeof(ArinNPCAI).GetField("isBossInAttackProximity", bindingFlags);
        if (isBossInAttackProximityField != null)
            isBossInAttackProximityField.SetValue(arinAI, false);

        Debug.Log("[PostBoss] Arin's combat behaviors disabled");
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
        }
    }

    private void StoreOriginalStatesAndHide()
    {
        if (uiCanvasGroup != null)
        {
            allImages = uiCanvasGroup.GetComponentsInChildren<Image>(true);
            originalImageColors = new Color[allImages.Length];

            for (int i = 0; i < allImages.Length; i++)
            {
                originalImageColors[i] = allImages[i].color;
                Color hiddenColor = allImages[i].color;
                hiddenColor.a = 0f;
                allImages[i].color = hiddenColor;
            }

            uiSpriteRenderers = uiCanvasGroup.GetComponentsInChildren<SpriteRenderer>(true);
            originalUISpriteColors = new Color[uiSpriteRenderers.Length];

            for (int i = 0; i < uiSpriteRenderers.Length; i++)
            {
                originalUISpriteColors[i] = uiSpriteRenderers[i].color;
                Color hiddenColor = uiSpriteRenderers[i].color;
                hiddenColor.a = 0f;
                uiSpriteRenderers[i].color = hiddenColor;
            }

            allTexts = uiCanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
            originalTextColors = new Color[allTexts.Length];
            for (int i = 0; i < allTexts.Length; i++)
            {
                originalTextColors[i] = allTexts[i].color;
                Color hiddenColor = allTexts[i].color;
                hiddenColor.a = 0f;
                allTexts[i].color = hiddenColor;
            }

            Button[] buttons = uiCanvasGroup.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
                button.interactable = false;

            Slider[] sliders = uiCanvasGroup.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
                slider.interactable = false;
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

        UIController uiCtrl = Object.FindFirstObjectByType<UIController>();
        if (uiCtrl != null)
            uiCtrl.ReinitializeButtons();

        Debug.Log("[PostBoss] Game UI fully restored - Quest UI can now display");
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

            if (uiSpriteRenderers != null && originalUISpriteColors != null)
            {
                for (int i = 0; i < uiSpriteRenderers.Length && i < originalUISpriteColors.Length; i++)
                {
                    if (uiSpriteRenderers[i] != null)
                        uiSpriteRenderers[i].color = originalUISpriteColors[i];
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

            Button[] buttons = uiCanvasGroup.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
                button.interactable = true;

            Slider[] sliders = uiCanvasGroup.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
                slider.interactable = true;
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
}