using UnityEngine;
using System.Collections;

/// <summary>
/// Evil Wizard AI - Stationary guardian that chases player within detection zone
/// Does NOT patrol - waits at original position and returns after chase
/// Has two attack types with delayed activation system
/// </summary>
[System.Serializable]
public class WizardAttackData
{
    [Header("Attack Settings")]
    public string triggerName = "Attack1";
    public int damage = 20;
    public float cooldown = 2.5f;
    public float activeTime = 0.5f;

    [Header("Attack Timing")]
    [Tooltip("Delay before attack collider activates (when spell/staff extends)")]
    [SerializeField] private float attackDelay = 0.4f;

    [Header("Attack Range")] // NEW: Individual attack range for each attack
    [Tooltip("Distance at which this specific attack can be triggered")]
    [SerializeField] private float attackRange = 3f;

    public float AttackDelay => attackDelay;
    public float AttackRange => attackRange; // NEW: Expose attack range
}

public class EvilWizardAI : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("How far the wizard can detect the player")]
    [SerializeField] private float detectionRange = 10f;
    
    // REMOVED: Global attackRange - now each attack has its own range

    [Header("Movement Settings")]
    [Tooltip("Speed when chasing player")]
    [SerializeField] private float chaseSpeed = 3f;
    
    [Tooltip("Speed when returning to original position")]
    [SerializeField] private float returnSpeed = 2f;
    
    [Tooltip("How close wizard needs to be to original position before stopping")]
    [SerializeField] private float returnStopDistance = 0.2f;

    [Header("Combat Settings")]
    [Tooltip("How long the wizard stays in hurt state before recovering")]
    [SerializeField] private float hurtDuration = 0.5f;
    
    [Tooltip("Time before wizard stops chasing if player is lost")]
    [SerializeField] private float playerLossDelay = 2f;
    
    [Tooltip("X and Y offset for attack collider positioning")]
    [SerializeField] private Vector2 attackColliderOffset = new Vector2(1.5f, 0f);

    [Header("Attack Configuration")]
    [Tooltip("Configure both wizard attacks (Attack1 and Attack2) with individual ranges")]
    [SerializeField] private WizardAttackData[] attacks = new WizardAttackData[2];

    [Header("Collider References")]
    [Tooltip("Wizard's body collider - takes damage from player attacks")]
    [SerializeField] private CapsuleCollider2D bodyCollider;
    
    [Tooltip("Detection zone - triggers player detection")]
    [SerializeField] private CircleCollider2D detectionCollider;

    [Header("Other References")]
    [Tooltip("Reference to WizardAttackCollider GameObject")]
    [SerializeField] private Transform attackColliderTransform;
    
    [SerializeField] private LayerMask groundLayerMask = 1;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // Core Components
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private EvilWizardAttackCollider wizardAttackCollider;
    private CircleCollider2D attackCollider;
    private Transform animatorChild;

    // State Variables
    private Vector3 originalPosition;
    private enum State { Idle, Chase, Attack, Returning, Hurt, Dead }
    private State currentState;
    private State previousState;
    private float lastAttackTime;
    private float facingDirection = 1f;
    private bool isPlayerDetected;
    private float playerDetectedTimer;
    private bool isAttacking;
    private bool isHurt;
    private bool isDead;
    private bool deathProcessed = false;

    // Attack delay tracking
    private bool isAttackColliderPending = false;
    
    // NEW: Store which attack we're currently using
    private WizardAttackData currentAttackData;

    void Start()
    {
        InitializeComponents();
        SetupColliders();
        InitializeState();
        ValidateAttackData();
        
        if (debugMode)
            Debug.Log($"Evil Wizard AI initialized at position: {originalPosition}");
    }

    void Update()
    {
        if (isDead) return;

        HandleHurtRecovery();
        HandlePlayerDetection();
        ProcessStateMachine();
        CheckPlayerLoss();
    }

    #region Initialization
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();

        animatorChild = transform.Find("Animator");
        if (animatorChild != null)
        {
            animator = animatorChild.GetComponent<Animator>();
            spriteRenderer = animatorChild.GetComponent<SpriteRenderer>();
            if (debugMode)
                Debug.Log("Evil Wizard: Found Animator child successfully");
        }
        else
        {
            Debug.LogError("Evil Wizard: Animator child not found! Requires 'Animator' child GameObject");
        }

        SetupWizardAttackCollider();
    }

    private void SetupColliders()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<CapsuleCollider2D>();
            if (bodyCollider != null && debugMode)
                Debug.Log("Evil Wizard: Auto-found body collider (CapsuleCollider2D)");
            else
                Debug.LogError("Evil Wizard: Body collider (CapsuleCollider2D) not found!");
        }

        if (detectionCollider == null)
        {
            detectionCollider = GetComponent<CircleCollider2D>();
            if (detectionCollider == null)
                detectionCollider = gameObject.AddComponent<CircleCollider2D>();
            
            if (debugMode)
                Debug.Log("Evil Wizard: Auto-found/created detection collider");
        }

        if (detectionCollider != null)
        {
            detectionCollider.isTrigger = true;
            detectionCollider.radius = detectionRange;
            if (debugMode)
                Debug.Log($"Evil Wizard: Detection collider configured (radius={detectionRange})");
        }

        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false; // CRITICAL: Body must be solid to receive damage
        }

        ValidateColliderSetup();
    }

    private void ValidateColliderSetup()
    {
        if (bodyCollider == null)
            Debug.LogError("Evil Wizard: Body collider not assigned!");
        
        if (detectionCollider == null)
            Debug.LogError("Evil Wizard: Detection collider not assigned!");
        
        if (attackCollider == null)
            Debug.LogError("Evil Wizard: Attack collider not assigned!");
    }

    private void SetupWizardAttackCollider()
    {
        if (attackColliderTransform == null)
            attackColliderTransform = transform.Find("WizardAttackCollider");

        if (attackColliderTransform != null)
        {
            wizardAttackCollider = attackColliderTransform.GetComponent<EvilWizardAttackCollider>();
            attackCollider = attackColliderTransform.GetComponent<CircleCollider2D>();

            if (attackColliderTransform.gameObject.activeSelf)
            {
                attackColliderTransform.gameObject.SetActive(false);
                if (debugMode)
                    Debug.Log("Evil Wizard: WizardAttackCollider initially disabled");
            }

            if (wizardAttackCollider != null && attackCollider != null && debugMode)
            {
                Debug.Log("Evil Wizard: Attack collider system properly initialized");
            }
            else
            {
                Debug.LogError("Evil Wizard: WizardAttackCollider script or CircleCollider2D not found!");
            }
        }
        else
        {
            Debug.LogError("Evil Wizard: WizardAttackCollider child GameObject not found!");
        }
    }

    private void InitializeState()
    {
        originalPosition = transform.position;
        
        currentState = State.Idle;
        previousState = State.Idle;
        ResetAnimatorParameters();

        // UPDATED: Initialize with proper attack ranges
        if (attacks == null || attacks.Length == 0)
        {
            attacks = new WizardAttackData[2]
            {
                new WizardAttackData { triggerName = "Attack1", damage = 20, cooldown = 2.5f, activeTime = 0.5f }, // Attack1: 3f range (set in inspector)
                new WizardAttackData { triggerName = "Attack2", damage = 25, cooldown = 3.0f, activeTime = 0.5f }  // Attack2: 4f range (set in inspector)
            };
        }
        
        if (debugMode)
            Debug.Log($"Evil Wizard: Original position stored at {originalPosition}");
    }

    private void ValidateAttackData()
    {
        if (attacks == null || attacks.Length == 0)
        {
            Debug.LogWarning("Evil Wizard: No attack data configured!");
        }
        else if (debugMode)
        {
            Debug.Log($"Evil Wizard: {attacks.Length} attacks configured");
            for (int i = 0; i < attacks.Length; i++)
            {
                Debug.Log($"  - {attacks[i].triggerName}: Range={attacks[i].AttackRange}, Damage={attacks[i].damage}, Cooldown={attacks[i].cooldown}");
            }
        }
    }
    #endregion

    #region State Machine
    private void ProcessStateMachine()
    {
        switch (currentState)
        {
            case State.Idle: HandleIdle(); break;
            case State.Chase: HandleChase(); break;
            case State.Attack: HandleAttack(); break;
            case State.Returning: HandleReturning(); break;
            case State.Hurt: HandleHurt(); break;
            case State.Dead: HandleDead(); break;
        }
    }

    private void HandleIdle()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        SetAnimatorBool("isRunning", false);
    }

    private void HandleChase()
    {
        if (player == null || !IsPlayerAliveAndValid())
        {
            if (debugMode)
                Debug.Log("Evil Wizard: Player lost or dead - returning to position");
            
            LosePlayer();
            TransitionToState(State.Returning);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer > detectionRange * 1.5f)
        {
            if (debugMode)
                Debug.Log("Evil Wizard: Player too far - returning to position");
            
            LosePlayer();
            TransitionToState(State.Returning);
            return;
        }

        // NEW: Check if player is in range of ANY attack
        if (IsPlayerInAnyAttackRange() && !isAttacking)
        {
            TransitionToState(State.Attack);
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * chaseSpeed, rb.linearVelocity.y);
        
        UpdateFacingDirection(direction.x);
        SetAnimatorBool("isRunning", true);

        if (debugMode && Time.frameCount % 60 == 0)
            Debug.Log($"Evil Wizard: Chasing player - Distance: {distanceToPlayer:F2}");
    }

    private void HandleAttack()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        SetAnimatorBool("isRunning", false);

        if (player == null || !IsPlayerAliveAndValid())
        {
            if (debugMode)
                Debug.Log("Evil Wizard: Player lost during attack - returning");
            
            LosePlayer();
            TransitionToState(State.Returning);
            return;
        }

        FacePlayer();

        // NEW: Check if player is still in any attack range
        if (!IsPlayerInAnyAttackRange())
        {
            if (debugMode)
                Debug.Log("Evil Wizard: Player moved out of all attack ranges - resuming chase");
            
            TransitionToState(State.Chase);
            return;
        }

        // NEW: Use cooldown from current attack data
        float cooldown = currentAttackData != null ? currentAttackData.cooldown : 2.5f;
        
        if (Time.time > lastAttackTime + cooldown && !isAttacking)
        {
            PerformAttack();
        }
    }

    private void HandleReturning()
    {
        float distanceToOriginal = Vector2.Distance(transform.position, originalPosition);

        if (distanceToOriginal <= returnStopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            SetAnimatorBool("isRunning", false);
            transform.position = new Vector3(originalPosition.x, transform.position.y, transform.position.z);
            TransitionToState(State.Idle);
            
            if (debugMode)
                Debug.Log("Evil Wizard: Returned to original position");
            
            return;
        }

        Vector2 direction = (originalPosition - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * returnSpeed, rb.linearVelocity.y);
        
        UpdateFacingDirection(direction.x);
        SetAnimatorBool("isRunning", true);

        if (debugMode && Time.frameCount % 60 == 0)
            Debug.Log($"Evil Wizard: Returning to position - Distance: {distanceToOriginal:F2}");
    }

    private void HandleHurt()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        SetAnimatorBool("isRunning", false);
    }

    private void HandleDead()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }
    #endregion

    #region Combat System
    private void PerformAttack()
    {
        if (!IsPlayerAliveAndValid())
        {
            LosePlayer();
            TransitionToState(State.Returning);
            return;
        }

        if (!IsPlayerInAnyAttackRange())
        {
            if (debugMode)
                Debug.Log("Evil Wizard: Player out of range - switching to chase");
            
            TransitionToState(State.Chase);
            return;
        }

        isAttacking = true;
        
        // NEW: Select best attack for current distance
        currentAttackData = SelectBestAttackForDistance();

        if (HasAnimatorParameter(currentAttackData.triggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(currentAttackData.triggerName);
            lastAttackTime = Time.time;

            ScheduleDelayedAttackCollider(currentAttackData);
            Invoke(nameof(ResetAttackState), 1.0f);

            if (debugMode)
                Debug.Log($"Evil Wizard: Performing {currentAttackData.triggerName} (Range: {currentAttackData.AttackRange}) - collider activates after {currentAttackData.AttackDelay}s");
        }
        else
        {
            Debug.LogError($"Evil Wizard: Attack trigger '{currentAttackData.triggerName}' not found!");
            isAttacking = false;
        }
    }

    // NEW: Select the best attack based on player distance
    private WizardAttackData SelectBestAttackForDistance()
    {
        if (player == null || attacks.Length == 0) 
            return attacks[0];

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Find attacks that can reach the player
        WizardAttackData bestAttack = null;
        float smallestRangeDifference = float.MaxValue;

        foreach (var attack in attacks)
        {
            // Check if this attack can reach the player
            if (distanceToPlayer <= attack.AttackRange)
            {
                float rangeDifference = attack.AttackRange - distanceToPlayer;
                
                // Prefer the attack with the smallest range that can still reach
                // (closer range = more precise attack selection)
                if (rangeDifference < smallestRangeDifference)
                {
                    smallestRangeDifference = rangeDifference;
                    bestAttack = attack;
                }
            }
        }

        // If no attack can reach, use the longest range attack
        if (bestAttack == null)
        {
            float longestRange = 0f;
            foreach (var attack in attacks)
            {
                if (attack.AttackRange > longestRange)
                {
                    longestRange = attack.AttackRange;
                    bestAttack = attack;
                }
            }
        }

        if (debugMode)
            Debug.Log($"Evil Wizard: Selected {bestAttack.triggerName} for distance {distanceToPlayer:F2} (Attack Range: {bestAttack.AttackRange})");

        return bestAttack ?? attacks[0];
    }

    // NEW: Check if player is in range of ANY attack
    private bool IsPlayerInAnyAttackRange()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        foreach (var attack in attacks)
        {
            if (distanceToPlayer <= attack.AttackRange)
            {
                if (debugMode && Time.frameCount % 60 == 0)
                    Debug.Log($"Evil Wizard: Player in range of {attack.triggerName} (Distance: {distanceToPlayer:F2}, Range: {attack.AttackRange})");
                
                return true;
            }
        }

        return false;
    }

    private void ScheduleDelayedAttackCollider(WizardAttackData attackData)
    {
        if (isDead || !IsPlayerAliveAndValid())
        {
            if (debugMode)
                Debug.Log("Evil Wizard: Attack collider schedule CANCELLED - invalid state");
            return;
        }

        isAttackColliderPending = true;

        Invoke(nameof(ActivateDelayedAttackCollider), attackData.AttackDelay);
        Invoke(nameof(DisableWizardAttackCollider), attackData.AttackDelay + attackData.activeTime);

        if (debugMode)
            Debug.Log($"Evil Wizard: Attack collider scheduled - Delay={attackData.AttackDelay}s, Active={attackData.activeTime}s");
    }

    private void ActivateDelayedAttackCollider()
    {
        if (!isAttackColliderPending || isDead || !IsPlayerAliveAndValid())
        {
            isAttackColliderPending = false;
            
            if (debugMode)
                Debug.Log("Evil Wizard: Attack collider activation CANCELLED");
            
            return;
        }

        SetupWizardAttackCollider(currentAttackData);
        isAttackColliderPending = false;

        if (debugMode)
            Debug.Log("Evil Wizard: Attack collider NOW ACTIVE!");
    }

    private void SetupWizardAttackCollider(WizardAttackData attackData)
    {
        if (attackColliderTransform != null && wizardAttackCollider != null && attackCollider != null)
        {
            attackColliderTransform.gameObject.SetActive(true);
            wizardAttackCollider.SetDamage(attackData.damage);

            Vector3 attackPosition = transform.position + new Vector3(
                facingDirection * attackColliderOffset.x,
                attackColliderOffset.y,
                0
            );
            attackColliderTransform.position = attackPosition;

            if (debugMode)
                Debug.Log($"Evil Wizard: Attack collider activated - Pos={attackPosition}, Radius={attackCollider.radius}, Damage={attackData.damage}");
        }
        else
        {
            Debug.LogError("Evil Wizard: Attack collider setup failed - missing components!");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isHurt) return;

        isHurt = true;

        if (currentState != State.Attack)
        {
            previousState = currentState;
            TransitionToState(State.Hurt);
        }

        if (HasAnimatorParameter("Hurt", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hurt");
        }

        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
            health.TakeDamage(damage);
        
        if (debugMode)
            Debug.Log($"Evil Wizard: Took {damage} damage from player attack");
    }

    public void OnDeath()
    {
        if (isDead || deathProcessed) return;

        isDead = true;
        deathProcessed = true;
        TransitionToState(State.Dead);

        Debug.Log("Evil Wizard died - starting death sequence");

        StartCoroutine(HandleWizardDeathSequence());
    }

    private IEnumerator HandleWizardDeathSequence()
    {
        PerformImmediateDeathCleanup();
        TriggerDeathAnimation();

        yield return new WaitForSeconds(1.5f);

        if (animator != null)
        {
            animator.enabled = false;
        }

        PermanentlyHideWizard();
        this.enabled = false;

        Debug.Log("Evil Wizard permanently removed from game");
    }

    private void PerformImmediateDeathCleanup()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CancelInvoke(nameof(ActivateDelayedAttackCollider));
        isAttackColliderPending = false;

        if (bodyCollider != null) bodyCollider.enabled = false;
        if (detectionCollider != null) detectionCollider.enabled = false;
        if (attackCollider != null) attackCollider.enabled = false;

        DisableWizardAttackCollider();
        if (attackColliderTransform != null)
            attackColliderTransform.gameObject.SetActive(false);

        player = null;
        isAttacking = false;
        isHurt = false;

        if (debugMode)
            Debug.Log("Evil Wizard: Death cleanup completed");
    }

    private void TriggerDeathAnimation()
    {
        if (animator != null && HasAnimatorParameter("Die", AnimatorControllerParameterType.Trigger))
        {
            try
            {
                ResetAnimatorParameters();
                animator.SetTrigger("Die");
                Debug.Log("Evil Wizard: Death animation triggered");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Evil Wizard: Failed to trigger death animation - {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Evil Wizard: Die trigger not found in animator");
        }
    }

    private void PermanentlyHideWizard()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (animatorChild != null)
        {
            animatorChild.gameObject.SetActive(false);
        }

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in allRenderers)
        {
            renderer.enabled = false;
        }

        transform.position = new Vector3(-10000f, -10000f, 0f);
        transform.localScale = Vector3.zero;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.gameObject.SetActive(false);
        }

        StartCoroutine(FinalDeactivation());
    }

    private IEnumerator FinalDeactivation()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        gameObject.SetActive(false);
        Debug.Log("Evil Wizard: GameObject deactivated");
    }
    #endregion

    #region Helper Methods
    private void HandleHurtRecovery()
    {
        if (!isHurt) return;

        if (Time.time > lastAttackTime + hurtDuration)
        {
            isHurt = false;
            if (currentState == State.Hurt)
            {
                ReturnToPreviousBehavior();
            }
        }
    }

    private void ReturnToPreviousBehavior()
    {
        if (player != null && IsPlayerInAnyAttackRange())
            TransitionToState(State.Attack);
        else if (player != null)
            TransitionToState(State.Chase);
        else
            TransitionToState(State.Returning);
    }

    private void HandlePlayerDetection()
    {
        if (isPlayerDetected)
            playerDetectedTimer = 0f;
        else
            playerDetectedTimer += Time.deltaTime;

        isPlayerDetected = false;
    }

    private void CheckPlayerLoss()
    {
        if (player != null && playerDetectedTimer > playerLossDelay)
        {
            if (debugMode)
                Debug.Log("Evil Wizard: Player lost due to timeout - returning");
            
            LosePlayer();
            
            if (currentState == State.Chase || currentState == State.Attack)
            {
                TransitionToState(State.Returning);
            }
        }
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        UpdateFacingDirection(directionToPlayer.x);
    }

    private bool IsPlayerAliveAndValid()
    {
        if (player == null) return false;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        return playerHealth != null && playerHealth.IsAlive();
    }

    private void ResetAttackState() => isAttacking = false;

    private void DisableWizardAttackCollider()
    {
        if (attackColliderTransform != null)
        {
            attackColliderTransform.gameObject.SetActive(false);
        }

        CancelInvoke(nameof(ActivateDelayedAttackCollider));
        isAttackColliderPending = false;
    }

    private void TransitionToState(State newState)
    {
        if (currentState != State.Hurt && currentState != State.Dead)
            previousState = currentState;

        State oldState = currentState;
        currentState = newState;

        if (debugMode)
            Debug.Log($"Evil Wizard: State transition - {oldState} -> {newState}");
    }

    private void UpdateFacingDirection(float moveDirection)
    {
        if (Mathf.Abs(moveDirection) > 0.1f)
        {
            facingDirection = moveDirection > 0 ? 1f : -1f;
            if (spriteRenderer != null)
                spriteRenderer.flipX = facingDirection < 0;
        }
    }

    private void LosePlayer()
    {
        if (player != null && debugMode)
        {
            Debug.Log("Evil Wizard: Losing player reference");
        }
        player = null;
    }
    #endregion

    #region Animation Methods
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
        if (animator == null) return;

        if (HasAnimatorParameter(paramName, AnimatorControllerParameterType.Bool))
            animator.SetBool(paramName, value);
    }

    private void ResetAnimatorParameters()
    {
        if (animator == null) return;

        if (HasAnimatorParameter("isRunning", AnimatorControllerParameterType.Bool))
            animator.SetBool("isRunning", false);

        string[] triggers = { "Attack1", "Attack2", "Hurt", "Die" };
        foreach (string trigger in triggers)
        {
            if (HasAnimatorParameter(trigger, AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger(trigger);
        }
    }
    #endregion

    #region Player Detection
    void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Player") && detectionCollider != null)
        {
            if (IsDetectionColliderTrigger(other))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsAlive())
                {
                    return;
                }

                isPlayerDetected = true;

                if (player == null && currentState != State.Attack && currentState != State.Hurt)
                {
                    player = other.transform;
                    TransitionToState(State.Chase);
                    
                    if (debugMode)
                        Debug.Log("Evil Wizard: Player detected - starting chase");
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Player"))
        {
            if (debugMode)
                Debug.Log("Evil Wizard: Player left detection zone");
        }
    }

    private bool IsDetectionColliderTrigger(Collider2D other)
    {
        if (detectionCollider != null && other.bounds.Intersects(detectionCollider.bounds))
        {
            if (attackCollider != null && attackColliderTransform != null && attackColliderTransform.gameObject.activeSelf)
            {
                if (other.bounds.Intersects(attackCollider.bounds) && isAttacking)
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public void OnAttackAnimationComplete()
    {
        isAttacking = false;
        DisableWizardAttackCollider();

        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsAlive())
            {
                LosePlayer();
                TransitionToState(State.Returning);
                return;
            }

            if (IsPlayerInAnyAttackRange())
            {
                currentState = State.Attack;
            }
            else
            {
                TransitionToState(State.Chase);
            }
        }
        else
        {
            TransitionToState(State.Returning);
        }
    }

    public void OnHurtAnimationComplete()
    {
        if (debugMode)
            Debug.Log("Evil Wizard: Hurt animation complete");
    }
    #endregion

    #region Debug Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // NEW: Draw each attack's individual range
        if (attacks != null && attacks.Length > 0)
        {
            for (int i = 0; i < attacks.Length; i++)
            {
                // Different colors for different attacks
                Gizmos.color = i == 0 ? Color.red : new Color(1f, 0.5f, 0f); // Red for Attack1, Orange for Attack2
                Gizmos.DrawWireSphere(transform.position, attacks[i].AttackRange);
            }
        }

        Vector3 originalPos = Application.isPlaying ? originalPosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(originalPos, 0.5f);
        Gizmos.DrawLine(transform.position, originalPos);

        if (attackCollider != null)
        {
            Vector3 attackPos = transform.position + new Vector3(
                facingDirection * attackColliderOffset.x,
                attackColliderOffset.y,
                0
            );
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPos, attackCollider.radius);
        }

        if (player != null && Application.isPlaying)
        {
            Gizmos.color = currentState == State.Chase ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

#if UNITY_EDITOR
        if (Application.isPlaying && attacks != null && attacks.Length > 0)
        {
            string attackInfo = "";
            for (int i = 0; i < attacks.Length; i++)
            {
                attackInfo += $"\n{attacks[i].triggerName}: Range={attacks[i].AttackRange}";
            }
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
                $"Evil Wizard\nState: {currentState}\nOriginal Pos: {originalPosition}{attackInfo}",
                new GUIStyle() { normal = new GUIStyleState() { textColor = Color.yellow } });
        }
#endif
    }
    #endregion

    #region Public Methods
    public bool IsBodyCollider(Collider2D hitCollider)
    {
        return bodyCollider != null && hitCollider == bodyCollider;
    }

    public bool IsDetectionCollider(Collider2D hitCollider)
    {
        return detectionCollider != null && hitCollider == detectionCollider;
    }

    public CapsuleCollider2D GetBodyCollider()
    {
        return bodyCollider;
    }
    #endregion
}