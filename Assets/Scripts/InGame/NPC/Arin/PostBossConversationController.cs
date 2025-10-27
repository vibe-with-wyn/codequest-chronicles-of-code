using System.Collections;
using UnityEngine;
using TMPro;

public class PostBossConversationController : MonoBehaviour
{
    [Header("Approach Settings")]
    [SerializeField] private float approachSpeed = 2.5f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float startDelayAfterDeath = 0.8f;

    [Header("Conversation Settings")]
    [SerializeField] private float dialogueDelayAfterApproach = 1.5f;
    [Tooltip("Additional delay before starting dialogue conversation after both characters face each other")]
    [SerializeField] private float finalDialogueDelay = 0.5f;

    private ArinNPCAI arinAI;
    private Animator arinAnimator;
    private Rigidbody2D arinRb;
    private SpriteRenderer arinSprite;
    private PlayerMovement playerMovement;
    private bool runningSequence;

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

        // Initial delay after boss death animation
        Debug.Log("[PostBoss] Waiting for death animation to settle...");
        yield return new WaitForSeconds(startDelayAfterDeath);

        // Temporarily disable Arin AI so we can drive her movement safely
        bool wasAIEnabled = arinAI.enabled;
        arinAI.enabled = false;
        Debug.Log("[PostBoss] Arin AI disabled for cinematic sequence");

        // Stop any residual velocity
        arinRb.linearVelocity = new Vector2(0, arinRb.linearVelocity.y);

        // Phase 1: Arin walks towards the player
        Debug.Log("[PostBoss] Arin beginning approach towards player...");
        while (Vector2.Distance(arinAI.transform.position, player.position) > stopDistance)
        {
            Vector2 dir = (player.position - arinAI.transform.position).normalized;
            float vx = Mathf.Sign(dir.x) * approachSpeed;
            arinRb.linearVelocity = new Vector2(vx, arinRb.linearVelocity.y);

            // Face direction of movement
            if (arinSprite != null) 
                arinSprite.flipX = (vx < 0);

            // Set "isMoving" animator parameter if it exists
            if (arinAnimator != null)
                TrySetAnimatorBool(arinAnimator, "isMoving", true);

            yield return null;
        }

        Debug.Log("[PostBoss] Arin reached approach distance");

        // Stop Arin's movement
        arinRb.linearVelocity = new Vector2(0, arinRb.linearVelocity.y);
        if (arinAnimator != null) 
            TrySetAnimatorBool(arinAnimator, "isMoving", false);

        // Phase 2: Both characters face each other
        Debug.Log("[PostBoss] Configuring facing directions for conversation...");
        FaceBothCharactersTogether(player);

        // Phase 3: Wait for player to stop moving and position themselves
        Debug.Log($"[PostBoss] Waiting {dialogueDelayAfterApproach}s for cinematic positioning...");
        yield return new WaitForSeconds(dialogueDelayAfterApproach);

        // Phase 4: Additional delay before dialogue starts (customizable for dramatic effect)
        Debug.Log($"[PostBoss] Final delay before dialogue: {finalDialogueDelay}s...");
        yield return new WaitForSeconds(finalDialogueDelay);

        // Phase 5: Start the post-boss conversation
        if (DialogueManager.Instance != null)
        {
            Debug.Log("[PostBoss] Starting post-boss conversation with Arin");
            DialogueManager.Instance.StartConversation("Arin_01_PostBoss");
            
            // Re-enable AI after dialogue ends
            DialogueManager.Instance.OnConversationCompleted += ReenableAIOnce;
        }
        else
        {
            Debug.LogWarning("DialogueManager not found. Printing dialogue to console instead.");
            Debug.Log("Arin: You fought bravely. Not many step into that cave and walk back out.");
            arinAI.enabled = wasAIEnabled;
            runningSequence = false;
        }
    }

    /// <summary>
    /// Make both the player and Arin face each other for a proper conversation stance
    /// </summary>
    private void FaceBothCharactersTogether(Transform player)
    {
        // Calculate facing directions
        Vector2 directionArinToPlayer = (player.position - arinAI.transform.position).normalized;
        Vector2 directionPlayerToArin = (arinAI.transform.position - player.position).normalized;

        // Face Arin towards player
        if (arinSprite != null)
        {
            arinSprite.flipX = (directionArinToPlayer.x < 0);
            Debug.Log($"[PostBoss] Arin facing: {(directionArinToPlayer.x < 0 ? "LEFT" : "RIGHT")} towards player");
        }

        // Face player towards Arin
        if (playerMovement != null)
        {
            // Update player's facing direction
            float playerFacingDirection = (directionPlayerToArin.x > 0) ? 1f : -1f;
            
            // Get the player's sprite renderer
            SpriteRenderer playerSprite = playerMovement.GetComponent<SpriteRenderer>();
            if (playerSprite == null)
                playerSprite = playerMovement.GetComponentInChildren<SpriteRenderer>();
            
            if (playerSprite != null)
            {
                playerSprite.flipX = (playerFacingDirection < 0);
                Debug.Log($"[PostBoss] Player facing: {(playerFacingDirection < 0 ? "LEFT" : "RIGHT")} towards Arin");
            }
        }

        Debug.Log("[PostBoss] Both characters now facing each other - ready for conversation");
    }

    private void ReenableAIOnce(string conversationId)
    {
        if (conversationId != "Arin_01_PostBoss") return;

        Debug.Log("[PostBoss] Conversation completed - re-enabling Arin AI");
        DialogueManager.Instance.OnConversationCompleted -= ReenableAIOnce;

        if (arinAI != null)
        {
            arinAI.enabled = true;
            Debug.Log("[PostBoss] Arin AI re-enabled");
        }
        runningSequence = false;
    }

    /// <summary>
    /// Safely set animator bool parameter if it exists
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