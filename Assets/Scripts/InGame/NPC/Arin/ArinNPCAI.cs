using UnityEngine;
using System.Collections;

[System.Serializable]
public class ArinAttackData
{
    [Header("Attack Settings")]
    public string attackName = "Attack1";
    public string animatorTrigger = "Attack1";
    public int damage = 20;
    public float cooldown = 2.0f;
    public float attackRange = 3f;
    public float animationDuration = 1.0f;

    [Header("Attack Timing")]
    [Tooltip("Delay after animation starts before damage collider activates")]
    public float damageDelay = 0.3f;

    [Tooltip("How long the damage collider stays active after activation")]
    public float damageActiveDuration = 0.4f;

    [Header("Attack Collider Settings")]
    public Vector2 attackColliderOffset = new Vector2(1.5f, 0f);
    public float attackColliderRadius = 8.0f;
}

/// <summary>
/// NPC Arin AI - Follows player intelligently and attacks the boss with cooldown system
/// Can take hits from boss and play hurt animation (but never dies)
/// </summary>
public class ArinNPCAI : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("How far Arin can detect the boss to trigger battle START ONLY")]
    [SerializeField] private float bossBattleTriggerRange = 10f;

    [Tooltip("How far Arin can detect the player to follow")]
    [SerializeField] private float playerFollowRange = 8f;

    [Tooltip("How close to the boss Arin needs to be to attack (separate from detection)")]
    [SerializeField] private float bossAttackProximityRange = 12f;

    [SerializeField] private string caveBossTag = "CaveBoss";
    [SerializeField] private string playerTag = "Player";

    [Header("Combat Settings")]
    [Tooltip("Distance to stop and attack the boss")]
    [SerializeField] private float attackRange = 3.5f;

    [Tooltip("Minimum time Arin must wait between attacks (cooldown in seconds)")]
    [SerializeField] private float attackCooldownDuration = 1.5f;

    [Header("Movement Settings")]
    [Tooltip("Speed when moving/following")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("Distance to maintain from the player when following (safe side behind player)")]
    [SerializeField] private float followDistanceFromPlayer = 3.5f;

    [Tooltip("How close Arin gets to the follow position before stopping")]
    [SerializeField] private float followStopDistance = 0.3f;

    [Tooltip("Distance Arin moves toward boss when attacking")]
    [SerializeField] private float attackAdvanceDistance = 2.5f;

    [Tooltip("Delay after attacking before returning to player")]
    [SerializeField] private float returnToPlayerDelay = 0.5f;

    [Header("Attack Configuration")]
    [SerializeField] private ArinAttackData[] attacks = new ArinAttackData[4];

    [Header("Collider References")]
    [Tooltip("Circle collider for triggering battle when boss is detected (larger range)")]
    [SerializeField] private CircleCollider2D bossBattleTriggerCollider;

    [Tooltip("Circle collider for detecting player to follow (smaller range)")]
    [SerializeField] private CircleCollider2D playerFollowCollider;

    [SerializeField] private Transform attackColliderTransform;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;

    [Header("Collision Settings")]
    [Tooltip("Layer for the player - Arin won't collide with player")]
    [SerializeField] private LayerMask playerLayer;

    // State Management
    private enum NPCState { Idle, FollowingPlayer, ApproachingBoss, Attacking, ReturningToPlayer, AttackCooldown }
    private NPCState currentState = NPCState.Idle;

    // Target Management
    private Transform caveBossTarget;
    private Transform playerTarget;
    private bool isBossInBattleRange = false;
    private bool isBossInAttackProximity = false;
    private bool isPlayerInFollowRange = false;
    private bool battleStarted = false;

    // Attack Management
    private float lastAttackTime = -999f;
    private float attackCooldownTimer = 0f;
    private bool isAttacking = false;
    private float returnToPlayerTimer = 0f;

    // Direction
    private float facingDirection = 1f;

    // Attack Collider Component
    private ArinAttackCollider arinAttackCollider;
    private CircleCollider2D attackCollider;

    [Header("Quest Integration")]
    [SerializeField] private string helpObjectiveTitle = "Help the Water Magician";
    [SerializeField] private bool completeObjectiveOnBattleStart = true;

    void Start()
    {
        InitializeComponents();
        InitializeAttackSystem();
        SetupDetectionColliders();
        ValidateAttackData();

        Debug.Log("Arin NPC AI initialized - intelligent positioning with attack cooldown system");
    }

    void Update()
    {
        UpdateAttackCooldown();
        UpdateReturnToPlayerTimer();
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

        if (animator == null)
            Debug.LogError("Animator not found on Arin NPC!");

        if (rb == null)
            Debug.LogError("Rigidbody2D not found on Arin NPC!");
    }

    private void InitializeAttackSystem()
    {
        if (attackColliderTransform == null)
        {
            attackColliderTransform = transform.Find("ArinAttackCollider");

            if (attackColliderTransform == null)
            {
                GameObject attackColliderObj = new GameObject("ArinAttackCollider");
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

        arinAttackCollider = attackColliderTransform.GetComponent<ArinAttackCollider>();
        if (arinAttackCollider == null)
        {
            arinAttackCollider = attackColliderTransform.gameObject.AddComponent<ArinAttackCollider>();
        }

        attackColliderTransform.gameObject.SetActive(false);

        Debug.Log("Arin attack system initialized");
    }

    private void SetupDetectionColliders()
    {
        if (bossBattleTriggerCollider == null)
        {
            CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
            if (colliders.Length > 0)
            {
                bossBattleTriggerCollider = colliders[0];
            }
            else
            {
                bossBattleTriggerCollider = gameObject.AddComponent<CircleCollider2D>();
            }
        }

        bossBattleTriggerCollider.isTrigger = true;
        bossBattleTriggerCollider.radius = bossBattleTriggerRange;

        if (playerFollowCollider == null)
        {
            CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
            if (colliders.Length > 1)
            {
                playerFollowCollider = colliders[1];
            }
            else
            {
                playerFollowCollider = gameObject.AddComponent<CircleCollider2D>();
            }
        }

        playerFollowCollider.isTrigger = true;
        playerFollowCollider.radius = playerFollowRange;

        Debug.Log($"Arin detection colliders configured: Boss={bossBattleTriggerRange}, Player={playerFollowRange}");
    }

    private void ValidateAttackData()
    {
        if (attacks == null || attacks.Length != 4)
        {
            Debug.LogWarning("Arin should have exactly 4 attacks configured!");
            attacks = new ArinAttackData[4]
            {
                new ArinAttackData { attackName = "Water Blast", animatorTrigger = "Attack1", damage = 20, cooldown = 2.0f, attackColliderRadius = 8.0f, damageDelay = 0.3f, damageActiveDuration = 0.4f },
                new ArinAttackData { attackName = "Ice Shard", animatorTrigger = "Attack2", damage = 25, cooldown = 3.0f, attackColliderRadius = 8.0f, damageDelay = 0.4f, damageActiveDuration = 0.4f },
                new ArinAttackData { attackName = "Tidal Wave", animatorTrigger = "Attack3", damage = 30, cooldown = 4.0f, attackColliderRadius = 8.0f, damageDelay = 0.5f, damageActiveDuration = 0.5f },
                new ArinAttackData { attackName = "Frost Nova", animatorTrigger = "Attack4", damage = 35, cooldown = 5.0f, attackColliderRadius = 8.0f, damageDelay = 0.6f, damageActiveDuration = 0.5f }
            };
        }

        Debug.Log($"Arin has {attacks.Length} attacks configured");
    }
    #endregion

    #region State Machine
    private void ProcessStateMachine()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                HandleIdleState();
                break;

            case NPCState.FollowingPlayer:
                HandleFollowingPlayerState();
                break;

            case NPCState.ApproachingBoss:
                HandleApproachingBossState();
                break;

            case NPCState.Attacking:
                HandleAttackingState();
                break;

            case NPCState.ReturningToPlayer:
                HandleReturningToPlayerState();
                break;

            case NPCState.AttackCooldown:
                HandleAttackCooldownState();
                break;
        }
    }

    private void HandleIdleState()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        SetAnimatorBool("isMoving", false);

        if (isBossInBattleRange && isPlayerInFollowRange && !battleStarted)
        {
            StartBattle();
        }
        else if (isPlayerInFollowRange && playerTarget != null)
        {
            TransitionToState(NPCState.FollowingPlayer);
        }
    }

    private void HandleFollowingPlayerState()
    {
        if (playerTarget == null || !isPlayerInFollowRange)
        {
            TransitionToState(NPCState.Idle);
            return;
        }

        // Only approach boss if ready to attack AND boss is in range
        if (battleStarted && isBossInAttackProximity && caveBossTarget != null && IsAttackReady())
        {
            TransitionToState(NPCState.ApproachingBoss);
            return;
        }

        // ALWAYS maintain distance behind player - this is the safe position
        Vector3 targetPosition = CalculateSafeFollowPosition();

        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);

        if (distanceToTarget > followStopDistance)
        {
            Vector2 direction = (targetPosition - transform.position).normalized;

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }

            UpdateFacingDirection(direction.x);
            SetAnimatorBool("isMoving", true);
        }
        else
        {
            // At safe distance - stop moving
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            SetAnimatorBool("isMoving", false);
        }
    }

    private void HandleApproachingBossState()
    {
        if (caveBossTarget == null || !isBossInAttackProximity)
        {
            TransitionToState(NPCState.FollowingPlayer);
            return;
        }

        if (!IsAttackReady())
        {
            Debug.Log("Attack cooldown not ready during approach - returning to player");
            TransitionToState(NPCState.FollowingPlayer);
            return;
        }

        float distanceToBoss = Vector2.Distance(transform.position, caveBossTarget.position);

        // ONLY move forward to attack if within attack range
        if (distanceToBoss <= attackRange)
        {
            // Stop moving - ready to attack
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }

            SetAnimatorBool("isMoving", false);

            float directionToBoss = Mathf.Sign(caveBossTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToBoss);

            TransitionToState(NPCState.Attacking);
        }
        else
        {
            // Move toward boss only if far away
            Vector2 direction = (caveBossTarget.position - transform.position).normalized;

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }

            UpdateFacingDirection(direction.x);
            SetAnimatorBool("isMoving", true);

            Debug.Log($"Arin approaching boss - Distance: {distanceToBoss:F2} (Need: {attackRange})");
        }
    }

    private void HandleAttackingState()
    {
        // Stop all movement during attack
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        SetAnimatorBool("isMoving", false);
    }

    private void HandleReturningToPlayerState()
    {
        if (playerTarget == null)
        {
            TransitionToState(NPCState.Idle);
            return;
        }

        // Return to safe position behind player
        Vector3 targetPosition = CalculateSafeFollowPosition();

        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);

        if (distanceToTarget > followStopDistance)
        {
            Vector2 direction = (targetPosition - transform.position).normalized;

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }

            UpdateFacingDirection(direction.x);
            SetAnimatorBool("isMoving", true);
        }
        else
        {
            // Back at safe position
            TransitionToState(NPCState.FollowingPlayer);
        }
    }

    private void HandleAttackCooldownState()
    {
        // Stop movement during cooldown
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        SetAnimatorBool("isMoving", false);

        if (IsAttackReady())
        {
            Debug.Log($"Attack cooldown finished ({attackCooldownDuration}s) - returning to player");
            TransitionToState(NPCState.ReturningToPlayer);
        }
    }

    private void TransitionToState(NPCState newState)
    {
        NPCState oldState = currentState;
        currentState = newState;

        Debug.Log($"Arin state transition: {oldState} -> {newState}");

        switch (newState)
        {
            case NPCState.Attacking:
                PerformRandomAttack();
                break;

            case NPCState.ReturningToPlayer:
                returnToPlayerTimer = returnToPlayerDelay;
                break;
        }
    }

    private void StartBattle()
    {
        battleStarted = true;
        Debug.Log("Battle started! Arin begins combat with Boss!");

        if (completeObjectiveOnBattleStart && QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteObjectiveByTitle(helpObjectiveTitle);
            Debug.Log($"Quest objective '{helpObjectiveTitle}' completed!");
        }

        if (caveBossTarget != null)
        {
            CaveBossAI bossAI = caveBossTarget.GetComponent<CaveBossAI>();
            if (bossAI != null)
            {
                bossAI.StartBattle();
                Debug.Log("Boss battle started!");
            }
        }

        SetupPlayerCollision();
        TransitionToState(NPCState.FollowingPlayer);
    }

    private void SetupPlayerCollision()
    {
        if (playerTarget != null)
        {
            Collider2D arinCollider = GetComponent<Collider2D>();
            Collider2D playerCollider = playerTarget.GetComponent<Collider2D>();

            if (arinCollider != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(arinCollider, playerCollider, true);
                Debug.Log("Arin will not collide with player - player can pass through");
            }
        }
    }
    #endregion

    #region Combat System
    private void PerformRandomAttack()
    {
        if (isAttacking) return;

        int selectedAttackIndex = Random.Range(0, attacks.Length);
        ArinAttackData selectedAttack = attacks[selectedAttackIndex];

        Debug.Log($"Arin randomly selected: {selectedAttack.attackName} (Radius: {selectedAttack.attackColliderRadius}, Damage Delay: {selectedAttack.damageDelay}s)");

        if (caveBossTarget != null)
        {
            float directionToTarget = Mathf.Sign(caveBossTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToTarget);
        }

        isAttacking = true;

        if (animator != null && HasAnimatorParameter(selectedAttack.animatorTrigger, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(selectedAttack.animatorTrigger);
            Debug.Log($"Arin using {selectedAttack.attackName}");
        }
        else
        {
            Debug.LogError($"Animator trigger '{selectedAttack.animatorTrigger}' not found!");
        }

        StartCoroutine(ExecuteAttackSequence(selectedAttack));
    }

    private IEnumerator ExecuteAttackSequence(ArinAttackData attackData)
    {
        Debug.Log($"[Arin Attack] Starting attack sequence for {attackData.attackName}");

        yield return new WaitForSeconds(attackData.damageDelay);

        Debug.Log($"[Arin Attack] Activating damage collider after {attackData.damageDelay}s delay");
        ActivateAttackCollider(attackData);

        yield return new WaitForSeconds(attackData.damageActiveDuration);

        Debug.Log($"[Arin Attack] Deactivating damage collider after {attackData.damageActiveDuration}s active time");
        DeactivateAttackCollider();

        float remainingTime = attackData.animationDuration - attackData.damageDelay - attackData.damageActiveDuration;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        StartAttackCooldown();

        isAttacking = false;

        Debug.Log($"Attack complete - starting {attackCooldownDuration}s cooldown");

        TransitionToState(NPCState.AttackCooldown);
    }

    private void ActivateAttackCollider(ArinAttackData attackData)
    {
        if (attackColliderTransform == null || arinAttackCollider == null || attackCollider == null)
        {
            Debug.LogError("[Arin] Attack collider components not set up!");
            return;
        }

        attackColliderTransform.gameObject.SetActive(true);
        arinAttackCollider.SetDamage(attackData.damage);

        Vector3 attackPosition = transform.position + new Vector3(
            facingDirection * attackData.attackColliderOffset.x,
            attackData.attackColliderOffset.y,
            0
        );
        attackColliderTransform.position = attackPosition;

        attackCollider.radius = attackData.attackColliderRadius;

        Debug.Log($"[Arin] Attack collider activated:");
        Debug.Log($"  - Attack: {attackData.attackName}");
        Debug.Log($"  - Damage: {attackData.damage}");
        Debug.Log($"  - Position: {attackPosition}");
        Debug.Log($"  - Radius: {attackData.attackColliderRadius}");

        float effectiveRange = Mathf.Abs(attackData.attackColliderOffset.x) + attackData.attackColliderRadius;
        Debug.Log($"  - Effective Range: {effectiveRange} units from Arin");
    }

    private void DeactivateAttackCollider()
    {
        if (attackColliderTransform != null)
        {
            attackColliderTransform.gameObject.SetActive(false);
            Debug.Log("[Arin] Attack collider deactivated");
        }
    }

    private void StartAttackCooldown()
    {
        lastAttackTime = Time.time;
        attackCooldownTimer = attackCooldownDuration;
        Debug.Log($"Attack cooldown started: {attackCooldownDuration}s");
    }

    private bool IsAttackReady()
    {
        bool ready = Time.time >= lastAttackTime + attackCooldownDuration;
        return ready;
    }

    private void UpdateAttackCooldown()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;

            if (attackCooldownTimer <= 0f)
            {
                attackCooldownTimer = 0f;
            }
        }
    }

    private void UpdateReturnToPlayerTimer()
    {
        if (returnToPlayerTimer > 0f)
        {
            returnToPlayerTimer -= Time.deltaTime;
        }
    }
    #endregion

    #region Hurt System
    /// <summary>
    /// Called by boss attack collider to make Arin play hurt animation
    /// Works exactly like PlayerMovement.TriggerHurt() - just triggers animation, no state changes
    /// </summary>
    public void TakeHit()
    {
        Debug.Log("Arin got hit by boss - playing hurt animation!");

        if (animator != null && HasAnimatorParameter("Hurt", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hurt");
            Debug.Log("Arin hurt animation triggered - continuing normal behavior");
        }
        else
        {
            Debug.LogWarning("Hurt trigger not found in Arin's animator!");
        }
    }
    #endregion

    #region Smart Positioning
    /// <summary>
    /// Calculate safe follow position - ALWAYS behind player, away from boss
    /// </summary>
    private Vector3 CalculateSafeFollowPosition()
    {
        if (playerTarget == null)
            return transform.position;

        Vector3 playerPos = playerTarget.position;

        // Get player's facing direction to place Arin BEHIND the player
        float playerFacing = playerTarget.GetComponent<PlayerMovement>()?.GetFacingDirection() ?? 1f;

        // If battle NOT started, just follow behind player's current facing
        if (!battleStarted || caveBossTarget == null)
        {
            // ALWAYS place Arin behind player (opposite of player's facing direction)
            Vector3 safePos = playerPos + new Vector3(-playerFacing * followDistanceFromPlayer, 0, 0);
            Debug.Log($"Non-battle follow: Player at {playerPos}, Arin target: {safePos}, Distance: {followDistanceFromPlayer}");
            return safePos;
        }

        // BATTLE ACTIVE: Calculate position based on player-to-boss direction
        Vector3 bossPos = caveBossTarget.position;
        Vector2 playerToBoss = (bossPos - playerPos).normalized;

        // Place Arin BEHIND player (away from boss)
        Vector2 safeDirection = -playerToBoss;
        Vector3 battleSafePos = playerPos + new Vector3(safeDirection.x * followDistanceFromPlayer, 0, 0);

        Debug.Log($"Battle follow: Player={playerPos}, Boss={bossPos}, SafeDir={safeDirection.x:F2}, Arin target={battleSafePos}");
        return battleSafePos;
    }
    #endregion

    #region Helper Methods
    private void UpdateFacingDirection(float directionX)
    {
        if (Mathf.Abs(directionX) > 0.1f)
        {
            facingDirection = Mathf.Sign(directionX);

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = facingDirection < 0;
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
    #endregion

    #region Detection System
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(caveBossTag))
        {
            Debug.Log($"Arin detected Boss for battle trigger: {other.name}");
            caveBossTarget = other.transform;
            isBossInBattleRange = true;

            if (isPlayerInFollowRange && !battleStarted)
            {
                StartBattle();
            }
        }

        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Arin detected Player for following: {other.name}");
            playerTarget = other.transform;
            isPlayerInFollowRange = true;
            SetupPlayerCollision();

            if (isBossInBattleRange && !battleStarted)
            {
                StartBattle();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(caveBossTag) && other.transform == caveBossTarget)
        {
            Debug.Log("Boss left Arin's battle trigger range");
            isBossInBattleRange = false;
        }

        if (other.CompareTag(playerTag))
        {
            Debug.Log("Player left Arin's follow range");
            isPlayerInFollowRange = false;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(caveBossTag))
        {
            isBossInBattleRange = true;

            if (caveBossTarget != null)
            {
                float distanceToBoss = Vector2.Distance(transform.position, caveBossTarget.position);
                isBossInAttackProximity = distanceToBoss <= bossAttackProximityRange;
            }
        }

        if (other.CompareTag(playerTag))
        {
            isPlayerInFollowRange = true;
        }
    }
    #endregion

    #region Debug
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bossBattleTriggerRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, bossAttackProximityRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, playerFollowRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (attacks != null && attacks.Length > 0)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < attacks.Length; i++)
            {
                ArinAttackData attack = attacks[i];
                if (attack != null)
                {
                    Vector3 attackPos = transform.position + new Vector3(
                        facingDirection * attack.attackColliderOffset.x,
                        attack.attackColliderOffset.y,
                        0
                    );
                    Gizmos.DrawWireSphere(attackPos, attack.attackColliderRadius);
                }
            }
        }

        if (playerTarget != null && Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, playerTarget.position);

            Vector3 safePos = CalculateSafeFollowPosition();
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(safePos, 0.3f);
            Gizmos.DrawLine(transform.position, safePos);

#if UNITY_EDITOR
            float cooldownRemaining = Mathf.Max(0, attackCooldownTimer);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f,
                $"State: {currentState}\nCooldown: {cooldownRemaining:F1}s\nBattle: {(battleStarted ? "ACTIVE" : "WAITING")}\nDistance to Safe: {Vector2.Distance(transform.position, safePos):F2}");
#endif
        }

        if (caveBossTarget != null && Application.isPlaying)
        {
            Gizmos.color = battleStarted ? Color.red : Color.gray;
            Gizmos.DrawLine(transform.position, caveBossTarget.position);

            float distance = Vector2.Distance(transform.position, caveBossTarget.position);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(caveBossTarget.position + Vector3.up * 3f,
                $"Distance: {distance:F2}\nIn Proximity: {isBossInAttackProximity}");
#endif
        }
    }
    #endregion
}