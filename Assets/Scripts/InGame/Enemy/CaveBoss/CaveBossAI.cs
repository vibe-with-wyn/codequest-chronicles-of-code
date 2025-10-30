using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// NightBorne Boss AI - Continuously chases nearest target using circular detection
/// Players can pass through the boss without being blocked
/// </summary>
public class CaveBossAI : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("How far the boss can detect the player")]
    [SerializeField] private float playerDetectionRange = 10f;

    [Tooltip("How far the boss can detect NPC Arin")]
    [SerializeField] private float npcDetectionRange = 10f;

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string npcTag = "NPC";

    [Header("Combat Settings")]
    [Tooltip("Distance at which boss stops moving and attacks")]
    [SerializeField] private float attackRange = 3f;

    [Tooltip("Minimum time between attacks")]
    [SerializeField] private float attackCooldown = 2f;

    [Tooltip("How long the boss stays in hurt state before recovering")]
    [SerializeField] private float hurtDuration = 0.5f;

    [Header("Movement Settings")]
    [Tooltip("Speed when chasing player or NPC")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Tooltip("Should the boss chase the player?")]
    [SerializeField] private bool canChasePlayer = true;

    [Tooltip("Should the boss chase NPC Arin?")]
    [SerializeField] private bool canChaseNPC = true;

    [Header("Attack Configuration")]
    [Tooltip("Damage dealt by the boss attack")]
    [SerializeField] private int attackDamage = 25;

    [Tooltip("Delay after animation starts before damage collider activates")]
    [SerializeField] private float attackDelay = 0.4f;

    [Tooltip("How long the attack collider stays active")]
    [SerializeField] private float attackActiveDuration = 0.4f;

    [Tooltip("Total duration of the attack animation")]
    [SerializeField] private float attackAnimationDuration = 1.0f;

    [Tooltip("Horizontal offset of attack collider from boss position")]
    [SerializeField] private Vector2 attackColliderOffset = new Vector2(2f, 0f);

    [Tooltip("Radius of the attack collider")]
    [SerializeField] private float attackColliderRadius = 1.5f;

    [Header("Battle Settings")]
    [Tooltip("Does the boss require StartBattle() to be called before attacking?")]
    [SerializeField] private bool requireBattleStart = true;

    [Header("Collider References")]
    [Tooltip("The body collider that receives damage (drag your CapsuleCollider2D here)")]
    [SerializeField] private CapsuleCollider2D bodyCollider;

    [SerializeField] private CircleCollider2D playerDetectionCollider;
    [SerializeField] private CircleCollider2D npcDetectionCollider;
    [SerializeField] private Transform attackColliderTransform;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private EnemyHealth enemyHealth;

    // State Management
    private enum BossState { Idle, Chasing, Attacking, Hurt, Dead }
    private BossState currentState = BossState.Idle;
    private BossState previousState = BossState.Idle;

    // Target Management
    private Transform currentTarget;
    private Transform playerTransform;
    private Transform npcTransform;
    private bool isPlayerDetected = false;
    private bool isNPCDetected = false;
    private bool battleStarted = false;

    // Attack Management
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    private BossAttackCollider bossAttackCollider;
    private CircleCollider2D attackCollider;

    // Hurt Management
    private bool isHurt = false;
    private float hurtRecoveryTimer = 0f;

    // Direction
    private float facingDirection = 1f;

    // Death
    private bool isDead = false;

    public static event System.Action<CaveBossAI> BossDefeated;

    void Start()
    {
        InitializeComponents();
        InitializeAttackSystem();
        SetupDetectionColliders();
        ValidateBodyCollider();
        SetupPassthroughCollision();
        
        Debug.Log("NightBorne Boss AI initialized - continuous chase with passthrough collision");
    }

    void Update()
    {
        if (isDead) return;

        CheckDeath();
        HandleHurtRecovery();
        SelectNearestTarget();
        ProcessStateMachine();
    }

    #region Initialization
    private void InitializeComponents()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<CapsuleCollider2D>();
            if (bodyCollider != null)
            {
                Debug.Log("NightBorne: Auto-found body collider (CapsuleCollider2D)");
            }
        }

        if (animator == null)
            Debug.LogError("Animator not found on NightBorne Boss!");
        if (rb == null)
            Debug.LogError("Rigidbody2D not found on NightBorne Boss!");
    }

    private void ValidateBodyCollider()
    {
        if (bodyCollider == null)
        {
            Debug.LogError("NightBorne body collider not assigned! Drag the boss's CapsuleCollider2D into the Body Collider field!");
        }
        else
        {
            if (bodyCollider.isTrigger)
            {
                Debug.LogWarning("NightBorne body collider is set as trigger! It should be solid (non-trigger) to receive damage.");
                bodyCollider.isTrigger = false;
            }

            Debug.Log($"NightBorne body collider validated: {bodyCollider.name} (trigger={bodyCollider.isTrigger})");
        }
    }

    private void SetupPassthroughCollision()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null && bodyCollider != null)
            {
                Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
                Debug.Log($"NightBorne: Player can now pass through boss (collision ignored)");
            }
        }

        GameObject[] npcs = GameObject.FindGameObjectsWithTag(npcTag);
        foreach (GameObject npc in npcs)
        {
            Collider2D npcCollider = npc.GetComponent<Collider2D>();
            if (npcCollider != null && bodyCollider != null)
            {
                Physics2D.IgnoreCollision(bodyCollider, npcCollider, true);
                Debug.Log($"NightBorne: NPC {npc.name} can now pass through boss");
            }
        }
    }

    private void InitializeAttackSystem()
    {
        if (attackColliderTransform == null)
        {
            attackColliderTransform = transform.Find("BossAttackCollider");

            if (attackColliderTransform == null)
            {
                GameObject attackColliderObj = new GameObject("BossAttackCollider");
                attackColliderObj.transform.SetParent(transform);
                attackColliderObj.transform.localPosition = Vector3.zero;
                attackColliderTransform = attackColliderObj.transform;
            }
        }

        attackCollider = attackColliderTransform.GetComponent<CircleCollider2D>();
        if (attackCollider == null)
        {
            attackCollider = attackColliderTransform.gameObject.AddComponent<CircleCollider2D>();
        }

        attackCollider.isTrigger = true;
        attackCollider.radius = attackColliderRadius;

        bossAttackCollider = attackColliderTransform.GetComponent<BossAttackCollider>();
        if (bossAttackCollider == null)
        {
            bossAttackCollider = attackColliderTransform.gameObject.AddComponent<BossAttackCollider>();
        }

        attackColliderTransform.gameObject.SetActive(false);

        Debug.Log("NightBorne attack system initialized");
    }

    private void SetupDetectionColliders()
    {
        if (playerDetectionCollider == null)
        {
            CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
            if (colliders.Length > 0)
            {
                playerDetectionCollider = colliders[0];
            }
            else
            {
                playerDetectionCollider = gameObject.AddComponent<CircleCollider2D>();
            }
        }

        playerDetectionCollider.isTrigger = true;
        playerDetectionCollider.radius = playerDetectionRange;

        if (npcDetectionCollider == null)
        {
            CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
            if (colliders.Length > 1)
            {
                npcDetectionCollider = colliders[1];
            }
            else
            {
                npcDetectionCollider = gameObject.AddComponent<CircleCollider2D>();
            }
        }

        npcDetectionCollider.isTrigger = true;
        npcDetectionCollider.radius = npcDetectionRange;

        Debug.Log($"NightBorne detection colliders configured: Player={playerDetectionRange}, NPC={npcDetectionRange}");
    }
    #endregion

    #region State Machine
    private void ProcessStateMachine()
    {
        switch (currentState)
        {
            case BossState.Idle:
                HandleIdleState();
                break;

            case BossState.Chasing:
                HandleChasingState();
                break;

            case BossState.Attacking:
                HandleAttackingState();
                break;

            case BossState.Hurt:
                HandleHurtState();
                break;

            case BossState.Dead:
                HandleDeadState();
                break;
        }
    }

    private void HandleIdleState()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        SetAnimatorBool("isRunning", false);

        // Wait for battle to start if required
        if (requireBattleStart && !battleStarted)
        {
            return;
        }

        // If we have a target, start chasing immediately
        if (currentTarget != null)
        {
            TransitionToState(BossState.Chasing);
        }
    }

    // UPDATED: Continuous chase - only stops when in attack range
    private void HandleChasingState()
    {
        if (currentTarget == null)
        {
            TransitionToState(BossState.Idle);
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Check if target is in attack range AND attack is ready
        if (distanceToTarget <= attackRange && CanAttack() && !isAttacking)
        {
            // Stop and attack
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }

            SetAnimatorBool("isRunning", false);

            float directionToTarget = Mathf.Sign(currentTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToTarget);

            TransitionToState(BossState.Attacking);
        }
        else
        {
            // CONTINUOUS CHASE - never stop until in attack range
            Vector2 direction = (currentTarget.position - transform.position).normalized;

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }

            UpdateFacingDirection(direction.x);
            SetAnimatorBool("isRunning", true);

            string targetName = currentTarget == playerTransform ? "PLAYER" : "NPC Arin";
            string cooldownStatus = CanAttack() ? "Ready" : $"Cooldown: {(attackCooldown - (Time.time - lastAttackTime)):F1}s";
            Debug.Log($"NightBorne chasing {targetName} - Distance: {distanceToTarget:F2}, Attack: {cooldownStatus}");
        }
    }

    private void HandleAttackingState()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        SetAnimatorBool("isRunning", false);

        if (currentTarget != null)
        {
            float directionToTarget = Mathf.Sign(currentTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToTarget);
        }
    }

    private void HandleHurtState()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        SetAnimatorBool("isRunning", false);
    }

    private void HandleDeadState()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void TransitionToState(BossState newState)
    {
        if (currentState != BossState.Hurt && currentState != BossState.Dead)
        {
            previousState = currentState;
        }

        BossState oldState = currentState;
        currentState = newState;

        Debug.Log($"NightBorne state transition: {oldState} -> {newState}");

        switch (newState)
        {
            case BossState.Attacking:
                PerformAttack();
                break;
        }
    }
    #endregion

    #region Combat System
    private void PerformAttack()
    {
        if (isAttacking || currentTarget == null) return;

        if (currentTarget != null)
        {
            float directionToTarget = Mathf.Sign(currentTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToTarget);
        }

        isAttacking = true;

        string targetName = currentTarget == playerTransform ? "PLAYER" : "NPC Arin";

        if (animator != null && HasAnimatorParameter("Attack", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Attack");
            Debug.Log($"NightBorne attacking {targetName}!");
        }
        else
        {
            Debug.LogError("Attack trigger not found in NightBorne animator!");
        }

        StartCoroutine(ExecuteAttackSequence());
    }

    private IEnumerator ExecuteAttackSequence()
    {
        Debug.Log($"[NightBorne Attack] Starting attack sequence");

        yield return new WaitForSeconds(attackDelay);

        ActivateMeleeAttack();

        yield return new WaitForSeconds(attackActiveDuration);

        DeactivateMeleeAttack();

        float remainingDuration = attackAnimationDuration - attackDelay - attackActiveDuration;
        if (remainingDuration > 0)
        {
            yield return new WaitForSeconds(remainingDuration);
        }

        lastAttackTime = Time.time;
        isAttacking = false;

        Debug.Log($"[NightBorne Attack] Attack sequence complete - resuming chase");

        // After attack, immediately return to chasing
        if (currentTarget != null && (battleStarted || !requireBattleStart))
        {
            TransitionToState(BossState.Chasing);
        }
        else
        {
            TransitionToState(BossState.Idle);
        }
    }

    private void ActivateMeleeAttack()
    {
        if (attackColliderTransform == null || bossAttackCollider == null || attackCollider == null)
        {
            Debug.LogError("NightBorne melee attack collider components not set up!");
            return;
        }

        attackColliderTransform.gameObject.SetActive(true);
        bossAttackCollider.SetDamage(attackDamage);

        Vector3 attackPosition = transform.position + new Vector3(
            facingDirection * attackColliderOffset.x,
            attackColliderOffset.y,
            0
        );
        attackColliderTransform.position = attackPosition;
        attackCollider.radius = attackColliderRadius;

        Debug.Log($"NightBorne melee attack activated: Pos={attackPosition}, Radius={attackColliderRadius}, Damage={attackDamage}");
    }

    private void DeactivateMeleeAttack()
    {
        if (attackColliderTransform != null)
        {
            attackColliderTransform.gameObject.SetActive(false);
        }
    }

    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        Debug.Log($"NightBorne taking damage: {damage} (isHurt={isHurt})");

        if (animator != null && HasAnimatorParameter("Hurt", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hurt");
            Debug.Log("NightBorne hurt animation triggered");
        }

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            Debug.Log($"NightBorne HP: {enemyHealth.GetCurrentHealth()}/{enemyHealth.GetMaxHealth()}");
        }

        if (!isHurt)
        {
            isHurt = true;
            hurtRecoveryTimer = hurtDuration;

            if (currentState != BossState.Attacking)
            {
                previousState = currentState;
                TransitionToState(BossState.Hurt);
            }
        }
    }

    private void HandleHurtRecovery()
    {
        if (!isHurt) return;

        hurtRecoveryTimer -= Time.deltaTime;
        if (hurtRecoveryTimer <= 0f)
        {
            isHurt = false;
            Debug.Log("Boss hurt state RESET - visual feedback complete");

            if (currentState == BossState.Hurt)
            {
                ReturnToPreviousBehavior();
            }
        }
    }

    private void ReturnToPreviousBehavior()
    {
        if (currentTarget != null && (battleStarted || !requireBattleStart))
        {
            TransitionToState(BossState.Chasing);
        }
        else
        {
            TransitionToState(BossState.Idle);
        }
    }
    #endregion

    #region Target Selection
    private void SelectNearestTarget()
    {
        Transform nearestTarget = null;
        float playerDistance = float.MaxValue;
        float npcDistance = float.MaxValue;

        if (canChasePlayer && playerTransform != null)
        {
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive())
            {
                playerDistance = Vector2.Distance(transform.position, playerTransform.position);

                if (playerDistance > playerDetectionRange * 2f)
                {
                    Debug.Log($"Player is too far ({playerDistance:F2} > {playerDetectionRange * 2f}) - clearing reference");
                    playerTransform = null;
                    isPlayerDetected = false;
                    playerDistance = float.MaxValue;
                }
            }
            else
            {
                if (playerTransform != null)
                {
                    Debug.Log("Player is dead - clearing reference");
                    playerTransform = null;
                }
                isPlayerDetected = false;
                playerDistance = float.MaxValue;
            }
        }

        if (canChaseNPC && npcTransform != null)
        {
            npcDistance = Vector2.Distance(transform.position, npcTransform.position);

            if (npcDistance > npcDetectionRange * 2f)
            {
                Debug.Log($"NPC is too far ({npcDistance:F2} > {npcDetectionRange * 2f}) - clearing reference");
                npcTransform = null;
                isNPCDetected = false;
                npcDistance = float.MaxValue;
            }
        }

        if (playerDistance < float.MaxValue)
        {
            nearestTarget = playerTransform;
            Debug.Log($"★ TARGETING PLAYER (Distance: {playerDistance:F2}, NPC Distance: {npcDistance:F2})");
        }
        else if (npcDistance < float.MaxValue)
        {
            nearestTarget = npcTransform;
            Debug.Log($"★ TARGETING NPC (Player unavailable, NPC Distance: {npcDistance:F2})");
        }

        if (currentTarget != nearestTarget)
        {
            if (nearestTarget != null)
            {
                Vector2 directionToTarget = (nearestTarget.position - transform.position).normalized;
                UpdateFacingDirection(directionToTarget.x);
                
                string targetName = nearestTarget == playerTransform ? "PLAYER ★" : "NPC Arin";
                Debug.Log($"NightBorne now targeting: {targetName} (Distance: {(nearestTarget == playerTransform ? playerDistance : npcDistance):F2})");
            }
            else if (currentTarget != null)
            {
                Debug.Log("NightBorne lost all targets");
            }
            currentTarget = nearestTarget;
        }
    }
    #endregion

    #region Helper Methods
    private void UpdateFacingDirection(float directionX)
    {
        if (Mathf.Abs(directionX) > 0.1f)
        {
            float newFacing = Mathf.Sign(directionX);

            if (newFacing != facingDirection)
            {
                facingDirection = newFacing;

                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = facingDirection < 0;
                }

                Debug.Log($"NightBorne now facing: {(facingDirection > 0 ? "RIGHT" : "LEFT")}, flipX={spriteRenderer.flipX}");
            }
        }
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType paramType)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == paramType)
                return true;
        }
        return false;
    }

    private void SetAnimatorBool(string paramName, bool value)
    {
        if (animator != null && HasAnimatorParameter(paramName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(paramName, value);
        }
    }

    private void CheckDeath()
    {
        if (enemyHealth != null && !enemyHealth.IsAlive() && !isDead)
        {
            OnDeath();
        }
    }

    private void OnDeath()
    {
        if (isDead) return;

        isDead = true;
        TransitionToState(BossState.Dead);

        Debug.Log("NightBorne Boss died! Starting death sequence...");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (bodyCollider != null) bodyCollider.enabled = false;
        if (playerDetectionCollider != null) playerDetectionCollider.enabled = false;
        if (npcDetectionCollider != null) npcDetectionCollider.enabled = false;

        if (attackColliderTransform != null)
            attackColliderTransform.gameObject.SetActive(false);

        if (animator != null && HasAnimatorParameter("Die", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Die");
            Debug.Log("NightBorne death animation triggered");
        }
        else
        {
            Debug.LogError("Die trigger not found in NightBorne animator!");
        }

        // REMOVED: Quest completion from here - it will be triggered by PostBossConversationController after dialogue
        // NEW: Only notify listeners (for cinematic sequence to start)
        try
        {
            BossDefeated?.Invoke(this);
            Debug.Log("BossDefeated event invoked - PostBossConversationController will handle quest completion after dialogue");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error invoking BossDefeated: {e.Message}");
        }

        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        Debug.Log("NightBorne death sequence started");

        yield return new WaitForSeconds(1.8f);

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (animator != null)
        {
            animator.enabled = false;
        }

        transform.position = new Vector3(-10000f, -10000f, 0f);
        gameObject.SetActive(false);

        Debug.Log("NightBorne death sequence complete");
    }

    public void StartBattle()
    {
        battleStarted = true;
        Debug.Log("NightBorne battle started!");

        if (currentTarget != null)
        {
            TransitionToState(BossState.Chasing);
        }
    }

    public CapsuleCollider2D GetBodyCollider()
    {
        return bodyCollider;
    }

    public bool IsBodyCollider(Collider2D hitCollider)
    {
        return bodyCollider != null && hitCollider == bodyCollider;
    }

    public bool IsDetectionCollider(Collider2D hitCollider)
    {
        if (playerDetectionCollider != null && hitCollider == playerDetectionCollider)
            return true;
        if (npcDetectionCollider != null && hitCollider == npcDetectionCollider)
            return true;
        return false;
    }
    #endregion

    #region Detection System
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"NightBorne detected PLAYER: {other.name}");
            playerTransform = other.transform;
            isPlayerDetected = true;
        }

        if (other.CompareTag(npcTag) || other.name.Contains("Arin"))
        {
            Debug.Log($"NightBorne detected NPC: {other.name}");
            npcTransform = other.transform;
            isNPCDetected = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && other.transform == playerTransform)
        {
            Debug.Log("Player left NightBorne detection range (but still tracking)");
            isPlayerDetected = false;
        }

        if ((other.CompareTag(npcTag) || other.name.Contains("Arin")) && other.transform == npcTransform)
        {
            Debug.Log("NPC left NightBorne detection range (but still tracking)");
            isNPCDetected = false;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerDetected = true;
        }

        if (other.CompareTag(npcTag) || other.name.Contains("Arin"))
        {
            isNPCDetected = true;
        }
    }
    #endregion

    #region Debug
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, npcDetectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange * 2f);

        if (currentTarget != null && Application.isPlaying)
        {
            Gizmos.color = battleStarted ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, currentTarget.position);

            float distance = Vector2.Distance(transform.position, currentTarget.position);
            string targetName = currentTarget == playerTransform ? "PLAYER" : "NPC Arin";

            string cooldownStatus = CanAttack() ? "READY" : $"CD: {(attackCooldown - (Time.time - lastAttackTime)):F1}s";

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
                $"Target: {targetName} (Nearest)\nDistance: {distance:F2}\nAttack: {cooldownStatus}\nBattle: {(battleStarted ? "ACTIVE" : "WAITING")}\nState: {currentState}\nHurt: {isHurt}");
#endif
        }

        if (Application.isPlaying)
        {
            Vector3 attackPos = transform.position + new Vector3(
                facingDirection * attackColliderOffset.x,
                attackColliderOffset.y,
                0
            );
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPos, attackColliderRadius);
        }
    }
    #endregion
}