using System.Collections;
using UnityEngine;
using TMPro;

public class PostBossConversationController : MonoBehaviour
{
    [Header("Approach Settings")]
    [SerializeField] private float approachSpeed = 2.5f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float startDelayAfterDeath = 0.8f;

    private ArinNPCAI arinAI;
    private Animator arinAnimator;
    private Rigidbody2D arinRb;
    private SpriteRenderer arinSprite;
    private bool runningSequence;

    void OnEnable()
    {
        CaveBossAI.BossDefeated += OnBossDefeated;
    }

    void OnDisable()
    {
        CaveBossAI.BossDefeated -= OnBossDefeated;
    }

    private void CacheArin()
    {
        if (arinAI == null) arinAI = Object.FindFirstObjectByType<ArinNPCAI>();
        if (arinAI != null)
        {
            if (arinAnimator == null) arinAnimator = arinAI.GetComponentInChildren<Animator>();
            if (arinRb == null) arinRb = arinAI.GetComponent<Rigidbody2D>();
            if (arinSprite == null) arinSprite = arinAI.GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void OnBossDefeated(CaveBossAI boss)
    {
        if (runningSequence) return;

        CacheArin();
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

        // Small cinematic delay
        yield return new WaitForSeconds(startDelayAfterDeath);

        // Temporarily disable Arin AI so we can drive her movement safely
        bool wasAIEnabled = arinAI.enabled;
        arinAI.enabled = false;

        // Stop any residual velocity
        arinRb.linearVelocity = new Vector2(0, arinRb.linearVelocity.y);

        // Walk horizontally towards the player
        while (Vector2.Distance(arinAI.transform.position, player.position) > stopDistance)
        {
            Vector2 dir = (player.position - arinAI.transform.position).normalized;
            float vx = Mathf.Sign(dir.x) * approachSpeed;
            arinRb.linearVelocity = new Vector2(vx, arinRb.linearVelocity.y);

            // Face player
            if (arinSprite != null) arinSprite.flipX = (vx < 0);

            // Set "isMoving" if Arin's animator has it (defensive)
            if (arinAnimator != null)
            {
                TrySetAnimatorBool(arinAnimator, "isMoving", true);
            }

            yield return null;
        }

        // Stop at target
        arinRb.linearVelocity = new Vector2(0, arinRb.linearVelocity.y);
        if (arinAnimator != null) TrySetAnimatorBool(arinAnimator, "isMoving", false);

        // Face the player
        if (arinSprite != null)
        {
            float dx = player.position.x - arinAI.transform.position.x;
            arinSprite.flipX = (dx < 0);
        }

        // Start the post-boss conversation
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartConversation("Arin_01_PostBoss");
            // Re-enable AI after dialogue ends
            DialogueManager.Instance.OnConversationCompleted += ReenableAIOnce;
        }
        else
        {
            Debug.LogWarning("DialogueManager not found. Printing dialogue to console instead.");
            // Fallback: just log and re-enable immediately
            Debug.Log("Arin: You fought bravely. Not many step into that cave and walk back out.");
            arinAI.enabled = wasAIEnabled;
            runningSequence = false;
        }
    }

    private void ReenableAIOnce(string conversationId)
    {
        if (conversationId != "Arin_01_PostBoss") return;

        DialogueManager.Instance.OnConversationCompleted -= ReenableAIOnce;

        if (arinAI != null)
        {
            arinAI.enabled = true;
        }
        runningSequence = false;
    }

    private static void TrySetAnimatorBool(Animator animator, string param, bool value)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (var p in animator.parameters)
        {
            if (p.name == param && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(param, value);
                break;
            }
        }
    }
}