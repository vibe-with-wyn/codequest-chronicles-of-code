using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CaveBossAI : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float playerDetectionRange = 10f;
    [SerializeField] private float npcDetectionRange = 10f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string npcTag = "NPC";
    
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 4f;
    [SerializeField] private float attackCooldownGlobal = 2f;
    [SerializeField] private float hurtDuration = 0.5f;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private bool canChasePlayer = true;
    [SerializeField] private bool canChaseNPC = true;
    
    [Header("Attack Configuration")]
    [SerializeField] private CaveBossAttackData[] attacks = new CaveBossAttackData[2];
    
    [Header("Battle Settings")]
    [SerializeField] private bool requireBattleStart = true;
    [Tooltip("Temporarily disable Attack2 (spell cast) for debugging")]
    [SerializeField] private bool disableAttack2 = true;
    
    [Header("Collider References")]
    [Tooltip("The body collider that should receive damage (drag your CapsuleCollider2D here)")]
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
    private enum BossState { Idle, InCombat, Attacking, Cooldown, Hurt, Dead }
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
    private float[] attackCooldownTimers = new float[2];
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

    void Start()
    {
        InitializeComponents();
        InitializeAttackSystem();
        SetupDetectionColliders();
        ValidateAttackData();
        ValidateBodyCollider();
        
        Debug.Log("Cave Boss AI initialized");
    }

    void Update()
    {
        if (isDead) return;
        
        CheckDeath();
        UpdateAttackCooldowns();
        HandleHurtRecovery();
        SelectBestTarget();
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
                Debug.Log("Boss: Auto-found body collider (CapsuleCollider2D)");
            }
        }
        
        if (animator == null)
            Debug.LogError("Animator not found on Cave Boss!");
        if (rb == null)
            Debug.LogError("Rigidbody2D not found on Cave Boss!");
    }

    private void ValidateBodyCollider()
    {
        if (bodyCollider == null)
        {
            Debug.LogError("Boss body collider not assigned! Drag the boss's CapsuleCollider2D into the Body Collider field!");
        }
        else
        {
            if (bodyCollider.isTrigger)
            {
                Debug.LogWarning("Boss body collider is set as trigger! It should be solid (non-trigger) to receive damage.");
                bodyCollider.isTrigger = false;
            }
            
            Debug.Log($"Boss body collider validated: {bodyCollider.name} (trigger={bodyCollider.isTrigger})");
        }
    }

    private void InitializeAttackSystem()
    {
        attackCooldownTimers = new float[attacks.Length];
        for (int i = 0; i < attackCooldownTimers.Length; i++)
        {
            attackCooldownTimers[i] = 0f;
        }
        
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
        attackCollider.radius = 1.5f;
        
        bossAttackCollider = attackColliderTransform.GetComponent<BossAttackCollider>();
        if (bossAttackCollider == null)
        {
            bossAttackCollider = attackColliderTransform.gameObject.AddComponent<BossAttackCollider>();
        }
        
        attackColliderTransform.gameObject.SetActive(false);
        
        Debug.Log("Boss attack system initialized");
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
        
        Debug.Log($"Boss detection colliders configured: Player={playerDetectionRange}, NPC={npcDetectionRange}");
    }

    private void ValidateAttackData()
    {
        if (attacks == null || attacks.Length != 2)
        {
            Debug.LogWarning("Cave Boss should have exactly 2 attacks configured!");
            attacks = new CaveBossAttackData[2]
            {
                new CaveBossAttackData 
                { 
                    attackName = "Claw Swipe", 
                    animatorTrigger = "Attack1", 
                    damage = 25, 
                    cooldown = 3f,
                    attackType = CaveBossAttackData.AttackType.Melee
                },
                new CaveBossAttackData 
                { 
                    attackName = "Death Hand", 
                    animatorTrigger = "Attack2", 
                    damage = 35, 
                    cooldown = 5f,
                    attackType = CaveBossAttackData.AttackType.SpellCast,
                    spellSpawnCount = 1,
                    spellSpawnRadius = 2f
                }
            };
        }
        
        Debug.Log($"Boss has {attacks.Length} attacks configured");
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
            
            case BossState.InCombat:
                HandleInCombatState();
                break;
            
            case BossState.Attacking:
                HandleAttackingState();
                break;
            
            case BossState.Cooldown:
                HandleCooldownState();
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
        
        SetAnimatorBool("isMoving", false);
        
        if (requireBattleStart && !battleStarted)
        {
            return;
        }
        
        if (currentTarget != null)
        {
            TransitionToState(BossState.InCombat);
        }
    }

    private void HandleInCombatState()
    {
        if (currentTarget == null)
        {
            TransitionToState(BossState.Idle);
            return;
        }
        
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        
        if (distanceToTarget <= attackRange)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            
            SetAnimatorBool("isMoving", false);
            
            float directionToTarget = Mathf.Sign(currentTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToTarget);
            
            if (CanAttack())
            {
                TransitionToState(BossState.Attacking);
            }
        }
        else
        {
            Vector2 direction = (currentTarget.position - transform.position).normalized;
            
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }
            
            UpdateFacingDirection(direction.x);
            SetAnimatorBool("isMoving", true);
            
            Debug.Log($"Boss chasing target - Distance: {distanceToTarget:F2}, Attack Range: {attackRange:F2}, Facing: {(facingDirection > 0 ? "RIGHT" : "LEFT")}");
        }
    }

    private void HandleAttackingState()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        SetAnimatorBool("isMoving", false);
        
        if (currentTarget != null)
        {
            float directionToTarget = Mathf.Sign(currentTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToTarget);
        }
    }

    private void HandleCooldownState()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        SetAnimatorBool("isMoving", false);
        
        if (Time.time >= lastAttackTime + attackCooldownGlobal)
        {
            if (currentTarget != null && (battleStarted || !requireBattleStart))
            {
                TransitionToState(BossState.InCombat);
            }
            else
            {
                TransitionToState(BossState.Idle);
            }
        }
    }

    private void HandleHurtState()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        SetAnimatorBool("isMoving", false);
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
        
        Debug.Log($"Boss state transition: {oldState} -> {newState}");
        
        switch (newState)
        {
            case BossState.Attacking:
                PerformRandomAttack();
                break;
        }
    }
    #endregion

    #region Combat System
    private void PerformRandomAttack()
    {
        if (isAttacking || currentTarget == null) return;
        
        if (currentTarget != null)
        {
            float directionToTarget = Mathf.Sign(currentTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToTarget);
        }
        
        int[] availableAttacks = GetAvailableAttacks();
        
        if (availableAttacks.Length == 0)
        {
            Debug.Log("No attacks available - waiting for cooldowns");
            TransitionToState(BossState.Cooldown);
            return;
        }
        
        int selectedAttackIndex = availableAttacks[Random.Range(0, availableAttacks.Length)];
        CaveBossAttackData selectedAttack = attacks[selectedAttackIndex];
        
        Debug.Log($"Boss selected: {selectedAttack.attackName} (Attack{selectedAttackIndex + 1}, Type: {selectedAttack.attackType})");
        
        isAttacking = true;
        
        if (animator != null && HasAnimatorParameter(selectedAttack.animatorTrigger, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(selectedAttack.animatorTrigger);
            Debug.Log($"Boss using {selectedAttack.attackName} (Trigger: {selectedAttack.animatorTrigger})");
        }
        else
        {
            Debug.LogError($"Animator trigger '{selectedAttack.animatorTrigger}' not found!");
        }
        
        StartCoroutine(ExecuteAttackSequence(selectedAttack, selectedAttackIndex));
    }

    private IEnumerator ExecuteAttackSequence(CaveBossAttackData attackData, int attackIndex)
    {
        Debug.Log($"[Boss Attack] Starting {attackData.attackName} sequence");
        
        yield return new WaitForSeconds(attackData.attackDelay);
        
        if (attackData.attackType == CaveBossAttackData.AttackType.Melee)
        {
            ActivateMeleeAttack(attackData);
        }
        else if (attackData.attackType == CaveBossAttackData.AttackType.SpellCast)
        {
            Debug.Log("[Boss Attack] Attack2 (Spell Cast) is currently disabled");
        }
        
        float remainingDuration = attackData.animationDuration - attackData.attackDelay;
        if (remainingDuration > 0)
        {
            yield return new WaitForSeconds(remainingDuration);
        }
        
        if (attackData.attackType == CaveBossAttackData.AttackType.Melee)
        {
            DeactivateMeleeAttack();
        }
        
        attackCooldownTimers[attackIndex] = attackData.cooldown;
        lastAttackTime = Time.time;
        
        isAttacking = false;
        
        Debug.Log($"[Boss Attack] {attackData.attackName} sequence complete");
        
        TransitionToState(BossState.Cooldown);
    }

    private void ActivateMeleeAttack(CaveBossAttackData attackData)
    {
        if (attackColliderTransform == null || bossAttackCollider == null || attackCollider == null)
        {
            Debug.LogError("Melee attack collider components not set up!");
            return;
        }
        
        attackColliderTransform.gameObject.SetActive(true);
        bossAttackCollider.SetDamage(attackData.damage);
        
        Vector3 attackPosition = transform.position + new Vector3(
            facingDirection * attackData.attackColliderOffset.x,
            attackData.attackColliderOffset.y,
            0
        );
        attackColliderTransform.position = attackPosition;
        attackCollider.radius = attackData.attackColliderRadius;
        
        Debug.Log($"Boss melee attack activated: Pos={attackPosition}, Radius={attackData.attackColliderRadius}, Damage={attackData.damage}, Facing={(facingDirection > 0 ? "RIGHT" : "LEFT")}");
    }

    private void DeactivateMeleeAttack()
    {
        if (attackColliderTransform != null)
        {
            attackColliderTransform.gameObject.SetActive(false);
        }
    }

    private int[] GetAvailableAttacks()
    {
        List<int> available = new List<int>();
        
        for (int i = 0; i < attacks.Length; i++)
        {
            if (i == 1 && disableAttack2)
            {
                Debug.Log("Attack2 is disabled - skipping");
                continue;
            }
            
            if (attackCooldownTimers[i] <= 0f)
            {
                available.Add(i);
            }
        }
        
        return available.ToArray();
    }

    private bool CanAttack()
    {
        if (Time.time < lastAttackTime + attackCooldownGlobal)
        {
            return false;
        }
        
        return GetAvailableAttacks().Length > 0;
    }

    private void UpdateAttackCooldowns()
    {
        for (int i = 0; i < attackCooldownTimers.Length; i++)
        {
            if (attackCooldownTimers[i] > 0f)
            {
                attackCooldownTimers[i] -= Time.deltaTime;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isHurt) return;
        
        Debug.Log($"Boss taking damage: {damage}");
        
        isHurt = true;
        hurtRecoveryTimer = hurtDuration;
        
        // CRITICAL: Don't transition to Hurt state during attack - let attack finish
        if (currentState != BossState.Attacking)
        {
            TransitionToState(BossState.Hurt);
        }
        
        // FIXED: Trigger hurt animation immediately regardless of state
        if (animator != null && HasAnimatorParameter("Hurt", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hurt");
            Debug.Log("Boss hurt animation triggered");
        }
        else
        {
            Debug.LogError("Hurt trigger not found in animator!");
        }
        
        // Apply damage to health
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            Debug.Log($"Boss HP: {enemyHealth.GetCurrentHealth()}/{enemyHealth.GetMaxHealth()}");
        }
        else
        {
            Debug.LogError("EnemyHealth component not found on boss!");
        }
    }

    private void HandleHurtRecovery()
    {
        if (!isHurt) return;
        
        hurtRecoveryTimer -= Time.deltaTime;
        if (hurtRecoveryTimer <= 0f)
        {
            isHurt = false;
            
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
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
            
            if (distanceToTarget <= attackRange && CanAttack())
            {
                TransitionToState(BossState.Attacking);
            }
            else
            {
                TransitionToState(BossState.InCombat);
            }
        }
        else
        {
            TransitionToState(BossState.Idle);
        }
    }
    #endregion

    #region Target Selection
    private void SelectBestTarget()
    {
        if (canChasePlayer && isPlayerDetected && playerTransform != null)
        {
            PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive())
            {
                currentTarget = playerTransform;
                return;
            }
        }
        
        if (canChaseNPC && isNPCDetected && npcTransform != null)
        {
            currentTarget = npcTransform;
            return;
        }
        
        currentTarget = null;
    }
    #endregion

    #region Helper Methods
    // FIXED: Match skeleton/golem facing logic exactly
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
                    // FIXED: Inverted for boss sprite (which faces LEFT by default)
                    // If your boss sprite faces LEFT, flip when going RIGHT
                    // If your boss sprite faces RIGHT, use: spriteRenderer.flipX = facingDirection < 0;
                    spriteRenderer.flipX = facingDirection > 0;
                }
                
                Debug.Log($"Boss now facing: {(facingDirection > 0 ? "RIGHT" : "LEFT")}, flipX={spriteRenderer.flipX}");
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
        
        Debug.Log("Cave Boss died! Starting death sequence...");
        
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
            Debug.Log("Boss death animation triggered");
        }
        else
        {
            Debug.LogError("Die trigger not found in animator!");
        }
        
        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        Debug.Log("Boss death sequence started");
        
        yield return new WaitForSeconds(2.5f);
        
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
        
        Debug.Log("Boss death sequence complete");
    }

    public void StartBattle()
    {
        battleStarted = true;
        Debug.Log("Cave Boss battle started!");
        
        if (currentTarget != null)
        {
            TransitionToState(BossState.InCombat);
        }
    }

    public CapsuleCollider2D GetBodyCollider()
    {
        return bodyCollider;
    }
    
    // NEW: Public method to check if boss can receive damage on a specific collider
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
            Debug.Log($"Boss detected player: {other.name}");
            playerTransform = other.transform;
            isPlayerDetected = true;
        }
        
        if (other.CompareTag(npcTag) || other.name.Contains("Arin"))
        {
            Debug.Log($"Boss detected NPC: {other.name}");
            npcTransform = other.transform;
            isNPCDetected = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && other.transform == playerTransform)
        {
            Debug.Log("Player left boss detection range");
            isPlayerDetected = false;
            playerTransform = null;
        }
        
        if ((other.CompareTag(npcTag) || other.name.Contains("Arin")) && other.transform == npcTransform)
        {
            Debug.Log("NPC left boss detection range");
            isNPCDetected = false;
            npcTransform = null;
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
        
        if (currentTarget != null && Application.isPlaying)
        {
            Gizmos.color = battleStarted ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            
            float distance = Vector2.Distance(transform.position, currentTarget.position);
            string facingText = facingDirection > 0 ? "LEFT" : "RIGHT";
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, 
                $"Distance: {distance:F2}\nAttack Range: {attackRange:F2}\nBattle: {(battleStarted ? "ACTIVE" : "WAITING")}\nState: {currentState}\nFacing: {facingText}");
            #endif
        }
        
        if (attacks != null && attacks.Length > 0 && Application.isPlaying)
        {
            var meleeAttack = attacks[0];
            if (meleeAttack.attackType == CaveBossAttackData.AttackType.Melee)
            {
                Vector3 attackPos = transform.position + new Vector3(
                    facingDirection * meleeAttack.attackColliderOffset.x,
                    meleeAttack.attackColliderOffset.y,
                    0
                );
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(attackPos, meleeAttack.attackColliderRadius);
            }
        }
    }
    #endregion
}