using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostBossConversationController : MonoBehaviour
{
    [Header("Approach Settings")]
    [SerializeField] private float approachSpeed = 2.5f;
    [SerializeField] private float conversationDistance = 3.5f;
    [SerializeField] private float stopDistance = 0.3f;
    [SerializeField] private float startDelayAfterDeath = 0.8f;

    [Header("Conversation Settings")]
    [SerializeField] private float dialogueDelayAfterApproach = 1.5f;
    [Tooltip("Additional delay before starting dialogue conversation after both characters face each other")]
    [SerializeField] private float finalDialogueDelay = 0.5f;
    
    [Header("Post-Dialogue Settings")]
    [SerializeField] private Vector3 arinExitDirection = Vector3.left;
    [SerializeField] private float exitDistance = 15f;
    [SerializeField] private Vector3 cabinLocation = Vector3.zero;
    [SerializeField] private float cabinArrivalDistance = 1.0f;
    
    [Header("Cabin Walk Speed")]
    [SerializeField] private float cabinWalkSpeed = 9f; // NEW: Speed when walking to cabin (9 units/sec)
    
    [Header("UI Canvas Reference")]
    [SerializeField] private CanvasGroup uiCanvasGroup;

    private ArinNPCAI arinAI;
    private Animator arinAnimator;
    private Rigidbody2D arinRb;
    private SpriteRenderer arinSprite;
    private PlayerMovement playerMovement;
    private UIController uiController;
    private bool runningSequence;
    
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
                Debug.Log("[PostBoss] Auto-found UI CanvasGroup");
            }
        }
    }

    private void OnBossDefeated(CaveBossAI boss)
    {
        if (runningSequence) return;

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
        Debug.Log("[PostBoss] PHASE 0: Disabling Arin's combat behaviors for mentoring mode...");
        DisableArinCombatBehaviors();
        yield return null;

        // ============= PHASE 1: HIDE UI COMPLETELY =============
        Debug.Log("[PostBoss] PHASE 1: Boss dying - HIDING UI COMPLETELY...");
        HideUIImmediately();
        yield return new WaitForEndOfFrame();

        // Wait for boss death animation to settle
        Debug.Log("[PostBoss] Waiting for death animation to settle...");
        yield return new WaitForSeconds(startDelayAfterDeath);

        // ============= PHASE 2: DISABLE ARIN AI & STOP MOVEMENT =============
        Debug.Log("[PostBoss] PHASE 2: Disabling Arin AI for cinematic...");
        
        bool wasAIEnabled = arinAI.enabled;
        arinAI.enabled = false;
        Debug.Log("[PostBoss] Arin AI disabled for cinematic sequence");

        // Stop any residual velocity
        arinRb.linearVelocity = new Vector2(0, arinRb.linearVelocity.y);

        // ============= PHASE 3: CALCULATE CONVERSATION DISTANCE =============
        Debug.Log("[PostBoss] PHASE 3: Calculating conversation distance...");
        
        float currentDistance = Vector2.Distance(arinAI.transform.position, player.position);
        
        if (currentDistance < conversationDistance)
        {
            Debug.Log($"[PostBoss] Characters too close ({currentDistance:F2} < {conversationDistance:F2}) - creating proper spacing...");
            
            Vector2 directionFromPlayer = (arinAI.transform.position - player.position).normalized;
            Vector3 properDistancePos = player.position + (Vector3)directionFromPlayer * conversationDistance;
            
            float moveTime = 0f;
            float moveDistance = Vector2.Distance(arinAI.transform.position, properDistancePos);
            float moveDuration = moveDistance / approachSpeed;
            
            while (moveTime < moveDuration)
            {
                moveTime += Time.deltaTime;
                float t = Mathf.Clamp01(moveTime / moveDuration);
                arinAI.transform.position = Vector3.Lerp(arinAI.transform.position, properDistancePos, t);
                
                if (arinSprite != null)
                    arinSprite.flipX = (directionFromPlayer.x < 0);
                
                if (arinAnimator != null)
                    TrySetAnimatorBool(arinAnimator, "isMoving", true);
                
                yield return null;
            }
            
            arinRb.linearVelocity = new Vector2(0, arinRb.linearVelocity.y);
            if (arinAnimator != null)
                TrySetAnimatorBool(arinAnimator, "isMoving", false);
            
            Debug.Log($"[PostBoss] Arin repositioned to proper conversation distance of {conversationDistance}m");
        }
        else if (currentDistance > conversationDistance)
        {
            Debug.Log($"[PostBoss] Characters too far ({currentDistance:F2} > {conversationDistance:F2}) - moving closer...");
            
            while (Vector2.Distance(arinAI.transform.position, player.position) > conversationDistance)
            {
                Vector2 dir = (player.position - arinAI.transform.position).normalized;
                arinRb.linearVelocity = new Vector2(dir.x * approachSpeed, arinRb.linearVelocity.y);

                if (arinSprite != null) 
                    arinSprite.flipX = (dir.x < 0);

                if (arinAnimator != null)
                    TrySetAnimatorBool(arinAnimator, "isMoving", true);

                yield return null;
            }

            Debug.Log("[PostBoss] Arin reached proper conversation distance");

            arinRb.linearVelocity = new Vector2(0, arinRb.linearVelocity.y);
            if (arinAnimator != null) 
                TrySetAnimatorBool(arinAnimator, "isMoving", false);
        }

        // ============= PHASE 4: FACE EACH OTHER FOR CONVERSATION =============
        Debug.Log("[PostBoss] PHASE 4: Both characters facing each other...");
        FaceBothCharactersTogether(player);

        // ============= PHASE 5: WAIT FOR CINEMATIC POSITIONING =============
        Debug.Log($"[PostBoss] PHASE 5: Waiting {dialogueDelayAfterApproach}s for cinematic positioning...");
        yield return new WaitForSeconds(dialogueDelayAfterApproach);

        // ============= PHASE 6: FINAL DELAY BEFORE DIALOGUE =============
        Debug.Log($"[PostBoss] PHASE 6: Final delay before dialogue: {finalDialogueDelay}s...");
        yield return new WaitForSeconds(finalDialogueDelay);

        // ============= PHASE 7: START DIALOGUE =============
        Debug.Log("[PostBoss] PHASE 7: Starting post-boss conversation with Arin");
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

    /// <summary>
    /// Called when dialogue with Arin completes
    /// </summary>
    private void OnDialogueCompleted(string conversationId)
    {
        if (conversationId != "Arin_01_PostBoss") return;

        Debug.Log("[PostBoss] Dialogue completed - starting exit sequence");
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnConversationCompleted -= OnDialogueCompleted;

        StartCoroutine(HandleExitSequence());
    }

    /// <summary>
    /// PHASE 8: Arin walks to cabin with new walk speed while player UI is restored
    /// </summary>
    private IEnumerator HandleExitSequence()
    {
        Debug.Log("[PostBoss] PHASE 8: Exit sequence - Arin heading to cabin/exit...");

        Debug.Log("[PostBoss] Restoring UI for player to follow Arin...");
        yield return StartCoroutine(RestoreUIGradually());

        // Disable Arin AI to control her movement manually
        arinAI.enabled = false;
        Debug.Log("[PostBoss] Arin AI disabled for guided walk to cabin");
        
        Vector3 arinStartPos = arinAI.transform.position;
        Vector3 cabinTarget = cabinLocation != Vector3.zero ? cabinLocation : arinStartPos + (arinExitDirection.normalized * exitDistance);
        
        float distance = Vector3.Distance(arinStartPos, cabinTarget);
        // NEW: Use cabinWalkSpeed instead of hardcoded 2f
        float moveDuration = distance / cabinWalkSpeed;
        float moveTime = 0f;
        
        Debug.Log($"[PostBoss] Arin walking to cabin at speed {cabinWalkSpeed} units/sec");
        Debug.Log($"[PostBoss] Arin walking from {arinStartPos} to cabin at {cabinTarget} (distance: {distance:F2}m, duration: {moveDuration:F2}s)");
        
        while (moveTime < moveDuration)
        {
            moveTime += Time.deltaTime;
            float t = Mathf.Clamp01(moveTime / moveDuration);
            arinAI.transform.position = Vector3.Lerp(arinStartPos, cabinTarget, t);
            
            Vector2 directionToTarget = (cabinTarget - arinAI.transform.position).normalized;
            if (arinSprite != null)
                arinSprite.flipX = (directionToTarget.x < 0);
            
            if (arinAnimator != null)
                TrySetAnimatorBool(arinAnimator, "isMoving", true);
            
            yield return null;
        }
        
        // Ensure final position is exactly at cabin
        arinAI.transform.position = cabinTarget;
        arinRb.linearVelocity = new Vector2(0, arinRb.linearVelocity.y);
        if (arinAnimator != null)
            TrySetAnimatorBool(arinAnimator, "isMoving", false);
        
        Debug.Log("[PostBoss] Arin arrived at cabin destination");
        
        // Keep AI disabled - Arin stays in mentoring mode
        // Combat behaviors will be re-enabled for final boss fight later
        Debug.Log("[PostBoss] Arin remains in mentoring mode - combat behaviors permanently disabled until final boss fight");

        Debug.Log("[PostBoss] Exit sequence complete - player can follow Arin to cabin");
        runningSequence = false;
    }

    /// <summary>
    /// NEW: Permanently disable Arin's combat-related behaviors
    /// Clears boss detection and prevents any re-engagement with defeated boss
    /// </summary>
    private void DisableArinCombatBehaviors()
    {
        if (arinAI == null) return;

        // Use reflection to access private fields and disable combat
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        
        // Clear cave boss target reference so Arin won't try to attack it
        System.Reflection.FieldInfo bossCaveBossTargetField = typeof(ArinNPCAI).GetField("caveBossTarget", bindingFlags);
        if (bossCaveBossTargetField != null)
        {
            bossCaveBossTargetField.SetValue(arinAI, null);
            Debug.Log("[PostBoss] Cleared caveBossTarget reference - Arin will not re-engage with defeated boss");
        }

        // Set battleStarted flag to true and keep it true to prevent boss detection
        System.Reflection.FieldInfo battleStartedField = typeof(ArinNPCAI).GetField("battleStarted", bindingFlags);
        if (battleStartedField != null)
        {
            battleStartedField.SetValue(arinAI, true);
            Debug.Log("[PostBoss] Set battleStarted to true - prevents re-triggering boss battle");
        }

        // Set isBossInBattleRange to false
        System.Reflection.FieldInfo isBossInBattleRangeField = typeof(ArinNPCAI).GetField("isBossInBattleRange", bindingFlags);
        if (isBossInBattleRangeField != null)
        {
            isBossInBattleRangeField.SetValue(arinAI, false);
            Debug.Log("[PostBoss] Set isBossInBattleRange to false");
        }

        // Set isBossInAttackProximity to false
        System.Reflection.FieldInfo isBossInAttackProximityField = typeof(ArinNPCAI).GetField("isBossInAttackProximity", bindingFlags);
        if (isBossInAttackProximityField != null)
        {
            isBossInAttackProximityField.SetValue(arinAI, false);
            Debug.Log("[PostBoss] Set isBossInAttackProximity to false");
        }

        Debug.Log("[PostBoss] ✓ Arin's combat behaviors PERMANENTLY DISABLED");
        Debug.Log("[PostBoss] ✓ Arin will NOT detect or attack the defeated boss");
        Debug.Log("[PostBoss] ✓ Arin is now in PERMANENT MENTORING MODE");
        Debug.Log("[PostBoss] ✓ Combat behaviors will be re-enabled for final boss fight only");
    }

    /// <summary>
    /// Hide UI immediately - NOW INCLUDES SPRITE RENDERER ICONS
    /// </summary>
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

            Debug.Log("[PostBoss] UI HIDDEN immediately - all alpha set to 0 (including icons and sprite renderers)");
        }
    }

    /// <summary>
    /// Store original UI states and set alpha to 0 - NOW INCLUDES SPRITE RENDERER ICONS
    /// </summary>
    private void StoreOriginalStatesAndHide()
    {
        if (uiCanvasGroup != null)
        {
            // Get all UI Images (includes icons)
            allImages = uiCanvasGroup.GetComponentsInChildren<Image>(true);
            originalImageColors = new Color[allImages.Length];
            
            for (int i = 0; i < allImages.Length; i++)
            {
                originalImageColors[i] = allImages[i].color;
                Color hiddenColor = allImages[i].color;
                hiddenColor.a = 0f;
                allImages[i].color = hiddenColor;
            }

            // Get all SpriteRenderer components in UI (icon sprites)
            uiSpriteRenderers = uiCanvasGroup.GetComponentsInChildren<SpriteRenderer>(true);
            originalUISpriteColors = new Color[uiSpriteRenderers.Length];
            
            for (int i = 0; i < uiSpriteRenderers.Length; i++)
            {
                originalUISpriteColors[i] = uiSpriteRenderers[i].color;
                Color hiddenColor = uiSpriteRenderers[i].color;
                hiddenColor.a = 0f;
                uiSpriteRenderers[i].color = hiddenColor;
                Debug.Log($"[PostBoss] Hidden UI SpriteRenderer icon: {uiSpriteRenderers[i].gameObject.name}");
            }

            // Get all UI Texts
            allTexts = uiCanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
            originalTextColors = new Color[allTexts.Length];
            for (int i = 0; i < allTexts.Length; i++)
            {
                originalTextColors[i] = allTexts[i].color;
                Color hiddenColor = allTexts[i].color;
                hiddenColor.a = 0f;
                allTexts[i].color = hiddenColor;
            }

            // Disable interactivity for buttons and sliders
            Button[] buttons = uiCanvasGroup.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.interactable = false;
            }

            Slider[] sliders = uiCanvasGroup.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                slider.interactable = false;
            }

            Debug.Log($"[PostBoss] Stored and hidden: {allImages.Length} images, {uiSpriteRenderers.Length} sprite renderers, {allTexts.Length} texts, {buttons.Length} buttons, {sliders.Length} sliders");
        }
    }

    /// <summary>
    /// Restore UI gradually
    /// </summary>
    private IEnumerator RestoreUIGradually()
    {
        Debug.Log("[PostBoss] Starting UI restoration process...");

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
        {
            Debug.Log("[PostBoss] Found UIController, reinitializing buttons...");
            uiCtrl.ReinitializeButtons();
            Debug.Log("[PostBoss] UIController buttons reinitialized successfully");
        }
        else
        {
            Debug.LogError("[PostBoss] UIController not found during UI restoration!");
        }

        Debug.Log("[PostBoss] UI restoration process completed - all UI visible and functional again");
    }

    /// <summary>
    /// Restore original UI states - NOW RESTORES SPRITE RENDERER ICONS
    /// </summary>
    private void RestoreOriginalStates()
    {
        if (uiCanvasGroup != null)
        {
            // Restore all Image components
            if (allImages != null && originalImageColors != null)
            {
                for (int i = 0; i < allImages.Length && i < originalImageColors.Length; i++)
                {
                    if (allImages[i] != null)
                        allImages[i].color = originalImageColors[i];
                }
            }

            // Restore all SpriteRenderer components (icons)
            if (uiSpriteRenderers != null && originalUISpriteColors != null)
            {
                for (int i = 0; i < uiSpriteRenderers.Length && i < originalUISpriteColors.Length; i++)
                {
                    if (uiSpriteRenderers[i] != null)
                    {
                        uiSpriteRenderers[i].color = originalUISpriteColors[i];
                        Debug.Log($"[PostBoss] Restored UI SpriteRenderer icon: {uiSpriteRenderers[i].gameObject.name}");
                    }
                }
            }

            // Restore all Text components
            if (allTexts != null && originalTextColors != null)
            {
                for (int i = 0; i < allTexts.Length && i < originalTextColors.Length; i++)
                {
                    if (allTexts[i] != null)
                        allTexts[i].color = originalTextColors[i];
                }
            }

            // Restore all Button components
            Button[] buttons = uiCanvasGroup.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.interactable = true;
            }

            // Restore all Slider components
            Slider[] sliders = uiCanvasGroup.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                slider.interactable = true;
            }

            Debug.Log($"[PostBoss] Restored: {allImages?.Length ?? 0} images, {uiSpriteRenderers?.Length ?? 0} sprite renderers, {allTexts?.Length ?? 0} texts, {buttons?.Length ?? 0} buttons, {sliders?.Length ?? 0} sliders");
        }
    }

    /// <summary>
    /// Make both the player and Arin face each other
    /// </summary>
    private void FaceBothCharactersTogether(Transform player)
    {
        Vector2 directionArinToPlayer = (player.position - arinAI.transform.position).normalized;
        Vector2 directionPlayerToArin = (arinAI.transform.position - player.position).normalized;

        if (arinSprite != null)
        {
            arinSprite.flipX = (directionArinToPlayer.x < 0);
            Debug.Log($"[PostBoss] Arin facing: {(directionArinToPlayer.x < 0 ? "LEFT" : "RIGHT")} towards player");
        }

        if (playerMovement != null)
        {
            SpriteRenderer playerSprite = playerMovement.GetComponent<SpriteRenderer>();
            if (playerSprite == null)
                playerSprite = playerMovement.GetComponentInChildren<SpriteRenderer>();
            
            if (playerSprite != null)
            {
                playerSprite.flipX = (directionPlayerToArin.x < 0);
                Debug.Log($"[PostBoss] Player facing: {(directionPlayerToArin.x < 0 ? "LEFT" : "RIGHT")} towards Arin");
            }
        }

        Debug.Log("[PostBoss] Both characters now facing each other at proper distance - ready for conversation");
    }

    /// <summary>
    /// Safely set animator bool parameter
    /// </summary>
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
}