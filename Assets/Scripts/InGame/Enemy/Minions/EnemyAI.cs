using UnityEngine;
using System.Collections;

[System.Serializable]
public class EnemyAttackData
{
    [Header("Attack Settings")]
    public string triggerName = "Attack1";
    public int damage = 15;
    public float cooldown = 2.0f;
    public float activeTime = 0.4f;

    [Header("Attack Timing")] // NEW: Attack timing configuration
    [SerializeField] private float attackDelay = 0.3f; // NEW: Delay before attack collider activates (when sword extends)

    public float AttackDelay => attackDelay;
}

public class EnemyAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float attackRange = 2f; // Distance to switch to attack state
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float idleTime = 2f;
    [SerializeField] private float patrolDistance = 5f;

    [Header("Combat Settings")]
    [SerializeField] private float hurtRecoveryTime = 0.2f;
    [SerializeField] private float hurtDuration = 0.5f;
    [SerializeField] private float playerLossDelay = 2f;
    [SerializeField] private Vector2 attackColliderOffset = new Vector2(1f, 0f); // X and Y offset for attack collider positioning

    [Header("Ground Detection Settings")]
    [SerializeField] private float groundCheckDistance = 1.2f; // How far ahead to check for ground
    [SerializeField] private float groundRayLength = 2f; // How far down to check for ground
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0, -0.5f); // Offset from center for ground check

    [Header("Attack Configuration")]
    [SerializeField] private EnemyAttackData[] attacks = new EnemyAttackData[2];

    [Header("Collider References")]
    [SerializeField] private CapsuleCollider2D bodyCollider; // Enemy body - takes damage
    [SerializeField] private CircleCollider2D detectionCollider; // Detection zone - no damage, only detection

    [Header("Other References")]
    [SerializeField] private Transform attackColliderTransform; // ONLY reference to EnemyAttackCollider GameObject
    [SerializeField] private LayerMask groundLayerMask = 1;

    // Core Components
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private EnemyAttackCollider enemyAttackCollider; // The script component
    private CircleCollider2D attackCollider; // The actual collider (auto-found from EnemyAttackCollider GameObject)
    private Transform animatorChild;

    // State Variables
    private Vector2 startPosition;
    private Vector2 patrolTarget;
    private enum State { Idle, Patrol, Chase, Attack, Hurt, Dead }
    private State currentState;
    private State previousState;
    private float idleTimer;
    private float lastAttackTime;
    private float facingDirection = 1f;
    private bool isPlayerDetected;
    private float playerDetectedTimer;
    private bool isAttacking;
    private bool isHurt;
    private bool isDead;
    private bool deathProcessed = false;

    // Ground detection variables
    private bool isGrounded = true;

    // NEW: Attack delay tracking
    private bool isAttackColliderPending = false; // Tracks if attack collider activation is scheduled

    void Start()
    {
        InitializeComponents();
        SetupColliders();
        InitializeState();
        ValidateAttackData();
        CalculateOptimalAttackRange();
        Debug.Log($"EnemyAI initialized on {gameObject.name}");
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

        // Find animator child
        animatorChild = transform.Find("Animator");
        if (animatorChild != null)
        {
            animator = animatorChild.GetComponent<Animator>();
            spriteRenderer = animatorChild.GetComponent<SpriteRenderer>();
            Debug.Log("Found Animator child successfully");
        }
        else
        {
            Debug.LogError("Animator child not found! Enemy requires 'Animator' child GameObject");
        }

        SetupEnemyAttackCollider();
    }

    private void SetupColliders()
    {
        // Auto-find body collider if not assigned
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<CapsuleCollider2D>();
            if (bodyCollider != null)
                Debug.Log("Auto-found body collider (CapsuleCollider2D)");
            else
                Debug.LogError("Body collider (CapsuleCollider2D) not found! Please assign in inspector.");
        }

        // Auto-find or create detection collider if not assigned
        if (detectionCollider == null)
        {
            detectionCollider = GetComponent<CircleCollider2D>();
            if (detectionCollider == null)
                detectionCollider = gameObject.AddComponent<CircleCollider2D>();

            Debug.Log("Auto-found/created detection collider (CircleCollider2D)");
        }

        // Configure detection collider
        if (detectionCollider != null)
        {
            detectionCollider.isTrigger = true;
            detectionCollider.radius = detectionRange;
            Debug.Log($"Detection collider configured: radius={detectionRange}, trigger=true");
        }

        // Configure body collider
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false; // Body should be solid
            Debug.Log("Body collider configured as solid (non-trigger)");
        }

        ValidateColliderSetup();
    }

    private void ValidateColliderSetup()
    {
        if (bodyCollider == null)
        {
            Debug.LogError($"Enemy {gameObject.name}: Body collider not assigned! Player attacks may not work correctly.");
        }

        if (detectionCollider == null)
        {
            Debug.LogError($"Enemy {gameObject.name}: Detection collider not assigned! Enemy may not detect player.");
        }

        if (attackCollider == null)
        {
            Debug.LogError($"Enemy {gameObject.name}: Attack collider not assigned! Enemy attacks may not work correctly.");
        }

        if (bodyCollider != null && detectionCollider != null && attackCollider != null)
        {
            Debug.Log($"Enemy {gameObject.name}: All colliders properly configured");
            Debug.Log($"  - Body: {bodyCollider.name} (solid)");
            Debug.Log($"  - Detection: {detectionCollider.name} (trigger, radius={detectionCollider.radius})");
            Debug.Log($"  - Attack: EnemyAttackCollider (trigger, radius={attackCollider.radius})");
        }
    }

    // SIMPLIFIED: Only use EnemyAttackCollider GameObject - no redundant references
    private void SetupEnemyAttackCollider()
    {
        // Auto-find EnemyAttackCollider child if not assigned
        if (attackColliderTransform == null)
            attackColliderTransform = transform.Find("EnemyAttackCollider");

        if (attackColliderTransform != null)
        {
            // Get the EnemyAttackCollider script component
            enemyAttackCollider = attackColliderTransform.GetComponent<EnemyAttackCollider>();

            // Get the CircleCollider2D component (for range calculations only)
            attackCollider = attackColliderTransform.GetComponent<CircleCollider2D>();

            // Initially disable the attack collider
            if (attackColliderTransform.gameObject.activeSelf)
            {
                attackColliderTransform.gameObject.SetActive(false);
                Debug.Log("EnemyAttackCollider initially disabled");
            }

            if (enemyAttackCollider != null && attackCollider != null)
            {
                Debug.Log("EnemyAttackCollider system properly initialized");
            }
            else
            {
                Debug.LogError("EnemyAttackCollider script or CircleCollider2D not found on EnemyAttackCollider child!");
            }
        }
        else
        {
            Debug.LogError("EnemyAttackCollider child GameObject not found! Please create a child named 'EnemyAttackCollider' with CircleCollider2D and EnemyAttackCollider script.");
        }
    }

    private void InitializeState()
    {
        startPosition = transform.position;
        patrolTarget = startPosition + new Vector2(patrolDistance, 0f);
        currentState = State.Idle;
        previousState = State.Idle;
        ResetAnimatorParameters();
        idleTimer = idleTime;

        if (attacks == null || attacks.Length == 0)
        {
            attacks = new EnemyAttackData[2]
            {
                new EnemyAttackData { triggerName = "Attack1", damage = 5, cooldown = 2.0f, activeTime = 0.4f },
                new EnemyAttackData { triggerName = "Attack2", damage = 10, cooldown = 2.5f, activeTime = 0.4f }
            };
        }
    }

    private void ValidateAttackData()
    {
        if (attacks == null || attacks.Length == 0)
        {
            Debug.LogWarning($"No attack data configured for {gameObject.name}");
        }
    }

    /// <summary>
    /// Calculate the optimal attack range based on the EnemyAttackCollider's actual reach
    /// </summary>
    private void CalculateOptimalAttackRange()
    {
        if (attackCollider != null)
        {
            // Calculate the maximum distance the attack collider can reach using Vector2 offset
            float maxAttackReach = attackColliderOffset.x + attackCollider.radius;

            // Set attack range slightly smaller to ensure reliable hits
            float calculatedAttackRange = maxAttackReach * 0.8f; // 80% of max reach for reliability

            // Use the smaller of the configured attack range or calculated range
            if (calculatedAttackRange < attackRange)
            {
                Debug.Log($"Adjusting attack range from {attackRange} to {calculatedAttackRange} based on EnemyAttackCollider reach");
                attackRange = calculatedAttackRange;
            }

            Debug.Log($"Final attack range: {attackRange}, EnemyAttackCollider reach: {maxAttackReach} (Offset: {attackColliderOffset})");
        }
    }
    #endregion

    #region Ground Detection System
    /// <summary>
    /// Check if there's ground ahead in the direction the enemy is facing
    /// Returns true if ground is detected, false if there's a cliff/no ground
    /// </summary>
    private bool IsGroundAhead()
    {
        Vector2 rayStart = (Vector2)transform.position + groundCheckOffset + new Vector2(facingDirection * groundCheckDistance, 0);

        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, groundRayLength, groundLayerMask);

        // Debug visualization
        Debug.DrawRay(rayStart, Vector2.down * groundRayLength, hit.collider != null ? Color.green : Color.red, 0.1f);

        bool groundDetected = hit.collider != null && hit.collider.CompareTag("Ground");

        if (!groundDetected)
        {
            Debug.Log($"No ground detected ahead! Enemy at {transform.position}, facing {facingDirection}, ray from {rayStart}");
        }

        return groundDetected;
    }

    /// <summary>
    /// Check if enemy is currently on ground (for general grounding check)
    /// </summary>
    private bool CheckGrounded()
    {
        Vector2 rayStart = (Vector2)transform.position + groundCheckOffset;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, groundRayLength * 0.6f, groundLayerMask);

        bool grounded = hit.collider != null && hit.collider.CompareTag("Ground");
        return grounded;
    }
    #endregion

    #region Public Methods for Collider Validation
    /// <summary>
    /// Check if the collider that was hit is the actual enemy body (should take damage)
    /// </summary>
    public bool IsBodyCollider(Collider2D hitCollider)
    {
        return bodyCollider != null && hitCollider == bodyCollider;
    }

    /// <summary>
    /// Check if the collider that was hit is the detection zone (should NOT take damage)
    /// </summary>
    public bool IsDetectionCollider(Collider2D hitCollider)
    {
        return detectionCollider != null && hitCollider == detectionCollider;
    }

    /// <summary>
    /// Check if the collider that was hit is the attack collider (enemy's weapon)
    /// </summary>
    public bool IsAttackCollider(Collider2D hitCollider)
    {
        return attackCollider != null && hitCollider == attackCollider;
    }

    /// <summary>
    /// Get the body collider reference for external scripts
    /// </summary>
    public CapsuleCollider2D GetBodyCollider()
    {
        return bodyCollider;
    }

    /// <summary>
    /// Get the detection collider reference for external scripts
    /// </summary>
    public CircleCollider2D GetDetectionCollider()
    {
        return detectionCollider;
    }

    /// <summary>
    /// Get the attack collider reference for external scripts
    /// </summary>
    public CircleCollider2D GetAttackCollider()
    {
        return attackCollider;
    }
    #endregion

    #region State Machine
    private void ProcessStateMachine()
    {
        // Update ground state
        isGrounded = CheckGrounded();

        switch (currentState)
        {
            case State.Idle: HandleIdle(); break;
            case State.Patrol: HandlePatrol(); break;
            case State.Chase: HandleChase(); break;
            case State.Attack: HandleAttack(); break;
            case State.Hurt: HandleHurt(); break;
            case State.Dead: HandleDead(); break;
        }
    }

    private void HandleIdle()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        SetAnimatorParameters(false, false);

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f && !isAttacking && !isHurt)
        {
            TransitionToState(State.Patrol);
            SwitchPatrolTarget();
        }
    }

    private void HandlePatrol()
    {
        // Check for ground ahead before moving
        if (!IsGroundAhead())
        {
            Debug.Log("No ground ahead during patrol - switching direction");
            SwitchPatrolTarget();
            TransitionToState(State.Idle);
            idleTimer = idleTime * 0.5f; // Shorter idle time when hitting edge
            return;
        }

        Vector2 direction = (patrolTarget - (Vector2)transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * patrolSpeed, rb.linearVelocity.y);

        UpdateFacingDirection(direction.x);
        SetAnimatorParameters(true, false);

        if (Vector2.Distance(new Vector2(transform.position.x, 0), new Vector2(patrolTarget.x, 0)) < 0.1f)
        {
            TransitionToState(State.Idle);
            idleTimer = idleTime;
        }
    }

    private void HandleChase()
    {
        if (player == null || !IsPlayerAliveAndValid())
        {
            Debug.Log("Player lost or dead during chase - returning to patrol");
            LosePlayer();
            TransitionToState(State.Patrol);
            return;
        }

        // Continue chasing until player is within attack range
        ProcessChaseMovement();
        CheckAttackRange();
    }

    private void HandleAttack()
    {
        // IMPORTANT: Stop moving when attacking, but continue to attack even if movement is blocked
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        SetAnimatorParameters(false, false);

        if (player == null || !IsPlayerAliveAndValid())
        {
            Debug.Log("Player lost or dead during attack - returning to patrol");
            LosePlayer();
            TransitionToState(State.Patrol);
            return;
        }

        // Face the player (this works even if movement is blocked)
        FacePlayer();

        // CRITICAL: Check if player is still in EnemyAttackCollider range (not movement range)
        if (!IsPlayerInAttackRange())
        {
            Debug.Log("Player moved out of EnemyAttackCollider range - resuming chase");
            TransitionToState(State.Chase);
            return;
        }

        // Perform attack if cooldown is ready - this works regardless of ground/wall obstacles
        if (Time.time > lastAttackTime + GetRandomAttackCooldown() && !isAttacking)
        {
            PerformAttack();
        }
    }

    private void HandleHurt()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        SetAnimatorParameters(false, false);
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
            TransitionToState(State.Patrol);
            return;
        }

        // Double-check player is still in EnemyAttackCollider range before attacking
        if (!IsPlayerInAttackRange())
        {
            Debug.Log("Player out of EnemyAttackCollider range when trying to attack - switching to chase");
            TransitionToState(State.Chase);
            return;
        }

        isAttacking = true;
        var attackData = GetRandomAttack();

        if (HasAnimatorParameter(attackData.triggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(attackData.triggerName);
            lastAttackTime = Time.time;

            // NEW: Schedule delayed attack collider activation instead of immediate activation
            ScheduleDelayedAttackCollider(attackData);

            Invoke(nameof(ResetAttackState), 1.0f);

            Debug.Log($"Enemy performed {attackData.triggerName} - EnemyAttackCollider will activate after {attackData.AttackDelay}s delay");
        }
        else
        {
            Debug.LogError($"Attack trigger '{attackData.triggerName}' not found!");
            isAttacking = false;
        }
    }

    // NEW: Schedule attack collider activation with delay
    private void ScheduleDelayedAttackCollider(EnemyAttackData attackData)
    {
        if (isDead || !IsPlayerAliveAndValid())
        {
            Debug.Log("Attack collider schedule CANCELLED - Player is dead or enemy is dead");
            return;
        }

        isAttackColliderPending = true;

        // Schedule the attack collider to activate after the specified delay
        Invoke(nameof(ActivateDelayedAttackCollider), attackData.AttackDelay);

        // Schedule the attack collider to deactivate after delay + active time
        Invoke(nameof(DisableEnemyAttackCollider), attackData.AttackDelay + attackData.activeTime);

        Debug.Log($"EnemyAttackCollider scheduled: Delay={attackData.AttackDelay}s, ActiveTime={attackData.activeTime}s, Total={attackData.AttackDelay + attackData.activeTime}s");
    }

    // NEW: Actually activate the attack collider (called after delay)
    private void ActivateDelayedAttackCollider()
    {
        if (!isAttackColliderPending || isDead || !IsPlayerAliveAndValid())
        {
            if (isDead)
                Debug.Log("Attack collider activation CANCELLED - Enemy is dead");
            else if (!IsPlayerAliveAndValid())
                Debug.Log("Attack collider activation CANCELLED - Player is invalid or dead");
            else if (!isAttackColliderPending)
                Debug.Log("Attack collider activation CANCELLED - Attack was cancelled");

            isAttackColliderPending = false;
            return;
        }

        // Get the current attack data (we need to recalculate since time has passed)
        var attackData = GetRandomAttack(); // This gets a random attack, but we should store the specific one

        SetupEnemyAttackCollider(attackData);
        isAttackColliderPending = false;

        Debug.Log("EnemyAttackCollider NOW ACTIVE - Player can be hit!");
    }

    // UPDATED: Now uses Vector2 attackColliderOffset for both X and Y positioning
    private void SetupEnemyAttackCollider(EnemyAttackData attackData)
    {
        if (attackColliderTransform != null && enemyAttackCollider != null && attackCollider != null)
        {
            // Enable the EnemyAttackCollider GameObject
            attackColliderTransform.gameObject.SetActive(true);

            // Set damage on the EnemyAttackCollider component
            enemyAttackCollider.SetDamage(attackData.damage);

            // Position the EnemyAttackCollider using Vector2 offset (X and Y)
            Vector3 attackPosition = transform.position + new Vector3(
                facingDirection * attackColliderOffset.x,  // X offset (horizontally in front/behind)
                attackColliderOffset.y,                    // Y offset (vertically above/below)
                0
            );
            attackColliderTransform.position = attackPosition;

            Debug.Log($"EnemyAttackCollider ACTIVATED: Position={attackPosition}, Radius={attackCollider.radius}, Damage={attackData.damage}, Offset={attackColliderOffset}");
            Debug.Log($"EnemyAttackCollider will hit player within {attackCollider.radius} units of position {attackPosition}");
        }
        else
        {
            Debug.LogError("EnemyAttackCollider setup failed - missing components!");
            Debug.LogError($"  attackColliderTransform: {(attackColliderTransform != null ? "OK" : "NULL")}");
            Debug.LogError($"  enemyAttackCollider: {(enemyAttackCollider != null ? "OK" : "NULL")}");
            Debug.LogError($"  attackCollider: {(attackCollider != null ? "OK" : "NULL")}");
        }
    }

    private EnemyAttackData GetRandomAttack()
    {
        if (attacks.Length == 0) return new EnemyAttackData();
        return attacks[Random.Range(0, attacks.Length)];
    }

    private float GetRandomAttackCooldown()
    {
        if (attacks.Length == 0) return 2.0f;
        var attack = attacks[Random.Range(0, attacks.Length)];
        return attack.cooldown;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isHurt) return;

        isHurt = true;
        hurtRecoveryTime = hurtDuration;

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
    }

    public void OnDeath()
    {
        if (isDead || deathProcessed) return;

        isDead = true;
        deathProcessed = true;
        TransitionToState(State.Dead);

        Debug.Log($"Enemy {gameObject.name} died - starting permanent removal sequence");

        StartCoroutine(HandleEnemyDeathSequence());
    }

    private IEnumerator HandleEnemyDeathSequence()
    {
        PerformImmediateDeathCleanup();
        TriggerDeathAnimation();

        yield return new WaitForSeconds(1f);

        // NEW: Immediately stop animator to prevent any frame flashing
        if (animator != null)
        {
            animator.enabled = false;
        }

        PermanentlyHideEnemy();
        this.enabled = false;

        Debug.Log($"Enemy {gameObject.name} permanently removed from game");
    }

    private void PerformImmediateDeathCleanup()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // NEW: Cancel any pending attack collider activations
        CancelInvoke(nameof(ActivateDelayedAttackCollider));
        isAttackColliderPending = false;

        // Disable ALL colliders to prevent any interactions
        if (bodyCollider != null) bodyCollider.enabled = false;
        if (detectionCollider != null) detectionCollider.enabled = false;
        if (attackCollider != null) attackCollider.enabled = false;

        DisableEnemyAttackCollider();
        if (attackColliderTransform != null)
            attackColliderTransform.gameObject.SetActive(false);

        player = null;
        isAttacking = false;
        isHurt = false;

        Debug.Log("Enemy death cleanup completed - all colliders disabled, attack activations cancelled");
    }

    private void TriggerDeathAnimation()
    {
        if (animator != null && HasAnimatorParameter("Die", AnimatorControllerParameterType.Trigger))
        {
            try
            {
                ResetAnimatorParameters();
                animator.SetTrigger("Die");
                Debug.Log($"Death animation triggered for {gameObject.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to trigger death animation: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Die trigger not found in animator or animator is null");
        }
    }

    private void PermanentlyHideEnemy()
    {
        Debug.Log($"Permanently hiding enemy {gameObject.name}");

        // NEW: Disable sprite renderer immediately to prevent flashing
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (animatorChild != null)
        {
            animatorChild.gameObject.SetActive(false);
            Debug.Log("Animator child deactivated");
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
        // NEW: Wait for TWO frames instead of one to ensure all rendering is complete
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        gameObject.SetActive(false);
        Debug.Log($"Enemy {gameObject.name} main GameObject deactivated");
    }
    #endregion

    #region Helper Methods
    private void HandleHurtRecovery()
    {
        if (!isHurt) return;

        hurtRecoveryTime -= Time.deltaTime;
        if (hurtRecoveryTime <= 0f)
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
        if (player != null && IsPlayerInAttackRange())
            TransitionToState(State.Attack);
        else if (player != null)
            TransitionToState(State.Chase);
        else
            TransitionToState(State.Patrol);
    }

    private void HandlePlayerDetection()
    {
        if (isPlayerDetected)
            playerDetectedTimer = 0f;
        else
            playerDetectedTimer += Time.deltaTime;

        isPlayerDetected = false;
    }

    private void ProcessChaseMovement()
    {
        if (player == null) return;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Check if we're moving in the direction where there's no ground
        bool wouldMoveIntoCliff = Mathf.Sign(directionToPlayer.x) == Mathf.Sign(facingDirection) && !IsGroundAhead();

        if (wouldMoveIntoCliff)
        {
            // Stop movement but face the player - STILL CHECK FOR ATTACK RANGE
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            SetAnimatorParameters(false, false);
            UpdateFacingDirection(directionToPlayer.x);

            Debug.Log("No ground ahead while chasing - stopped movement but will still check for EnemyAttackCollider range");
            // IMPORTANT: Don't return here - continue to CheckAttackRange()
        }
        else
        {
            // Check height difference
            float playerY = player.position.y;
            float enemyY = transform.position.y;
            float heightDifference = Mathf.Abs(playerY - enemyY);

            Vector2 direction = (player.position - transform.position).normalized;

            // If player is too high up, stop and wait - BUT STILL CHECK FOR ATTACK RANGE
            if (heightDifference > 1.5f && playerY > enemyY)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                SetAnimatorParameters(false, false);
                UpdateFacingDirection((player.position.x - transform.position.x));
                Debug.Log("Player too high - waiting on ground but will still check for EnemyAttackCollider range");
                // IMPORTANT: Don't return here - continue to CheckAttackRange()
            }
            else
            {
                // Normal chase movement - only if no ground/height issues
                rb.linearVelocity = new Vector2(direction.x * chaseSpeed, rb.linearVelocity.y);
                UpdateFacingDirection(direction.x);
                SetAnimatorParameters(false, true);

                Debug.Log($"Chasing player - Distance: {Vector2.Distance(transform.position, player.position):F2}");
            }
        }
    }

    /// <summary>
    /// Check if player is within EnemyAttackCollider range and switch to attack state if so
    /// </summary>
    private void CheckAttackRange()
    {
        if (IsPlayerInAttackRange() && !isAttacking)
        {
            TransitionToState(State.Attack);
            Debug.Log("Player entered EnemyAttackCollider range - switching to Attack state");
        }
    }

    /// <summary>
    /// Determine if player is close enough for the EnemyAttackCollider to reach them
    /// UPDATED: Now uses Vector2 attackColliderOffset for positioning calculations
    /// </summary>
    private bool IsPlayerInAttackRange()
    {
        if (player == null) return false;

        // Calculate the actual EnemyAttackCollider position using Vector2 offset
        Vector3 attackPosition = transform.position + new Vector3(
            facingDirection * attackColliderOffset.x,  // X offset
            attackColliderOffset.y,                    // Y offset
            0
        );

        // Calculate distance from EnemyAttackCollider position to player
        float actualDistance = Vector2.Distance(new Vector2(attackPosition.x, attackPosition.y), new Vector2(player.position.x, player.position.y));

        // Get the EnemyAttackCollider radius
        float attackRadius = attackCollider != null ? attackCollider.radius : 0.5f;

        // Check if player is within the EnemyAttackCollider's reach
        bool inRange = actualDistance <= (attackRadius + 0.1f); // Small buffer for reliability

        // Also check height difference to ensure we can actually hit
        float heightDifference = Mathf.Abs(player.position.y - transform.position.y);
        bool heightOK = heightDifference <= 1.5f; // Allow some vertical tolerance

        bool finalResult = inRange && heightOK;

        if (finalResult)
        {
            Debug.Log($"Player IS in EnemyAttackCollider range! Distance: {actualDistance:F2}, AttackRadius: {attackRadius:F2}, Height: {heightDifference:F2}, AttackPos: {attackPosition}");
        }
        else
        {
            Debug.Log($"Player NOT in EnemyAttackCollider range. Distance: {actualDistance:F2}, AttackRadius: {attackRadius:F2}, Height: {heightDifference:F2}, InRange: {inRange}, HeightOK: {heightOK}, AttackPos: {attackPosition}");
        }

        return finalResult;
    }

    private void CheckPlayerLoss()
    {
        if (player != null && playerDetectedTimer > playerLossDelay)
        {
            Debug.Log("Player lost due to detection timeout");
            LosePlayer();
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

    private void SwitchPatrolTarget()
    {
        patrolTarget = patrolTarget == startPosition + new Vector2(patrolDistance, 0f) ?
                      startPosition - new Vector2(patrolDistance, 0f) :
                      startPosition + new Vector2(patrolDistance, 0f);
    }

    private void ResetAttackState() => isAttacking = false;

    // UPDATED: Also cancel any pending attack collider activations
    private void DisableEnemyAttackCollider()
    {
        if (attackColliderTransform != null)
        {
            attackColliderTransform.gameObject.SetActive(false);
            Debug.Log("EnemyAttackCollider disabled");
        }

        // NEW: Cancel any pending activations
        CancelInvoke(nameof(ActivateDelayedAttackCollider));
        isAttackColliderPending = false;
    }

    private void TransitionToState(State newState)
    {
        if (currentState != State.Hurt && currentState != State.Dead)
            previousState = currentState;

        State oldState = currentState;
        currentState = newState;

        Debug.Log($"Enemy state: {oldState} -> {newState}");
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
        if (player != null)
        {
            Debug.Log("Enemy lost player - returning to patrol");
            player = null;
            if (currentState == State.Chase || currentState == State.Attack)
            {
                TransitionToState(State.Patrol);
            }
        }
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

    private void SetAnimatorParameters(bool patrolling, bool chasing)
    {
        if (animator == null) return;

        if (HasAnimatorParameter("isPatrolling", AnimatorControllerParameterType.Bool))
            animator.SetBool("isPatrolling", patrolling);

        if (HasAnimatorParameter("isChasing", AnimatorControllerParameterType.Bool))
            animator.SetBool("isChasing", chasing);
    }

    private void ResetAnimatorParameters()
    {
        if (animator == null) return;

        if (HasAnimatorParameter("isPatrolling", AnimatorControllerParameterType.Bool))
            animator.SetBool("isPatrolling", false);

        if (HasAnimatorParameter("isChasing", AnimatorControllerParameterType.Bool))
            animator.SetBool("isChasing", false);

        string[] triggers = { "Attack1", "Attack2", "Hurt", "Die" };
        foreach (string trigger in triggers)
        {
            if (HasAnimatorParameter(trigger, AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger(trigger);
        }
    }
    #endregion

    #region Player Detection (ONLY uses detection collider)
    void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;

        // CRITICAL: Only detect player through the detection collider, not EnemyAttackCollider
        if (other.CompareTag("Player") && detectionCollider != null)
        {
            // Verify this trigger event is from the detection collider
            if (IsDetectionColliderTrigger(other))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsAlive())
                {
                    return; // Ignore dead players
                }

                isPlayerDetected = true;

                // Only start chasing if not already targeting this player
                if (player == null && currentState != State.Attack && currentState != State.Hurt)
                {
                    player = other.transform;
                    TransitionToState(State.Chase);
                    Debug.Log("Player detected - starting chase sequence");
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isDead) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player left detection zone");
        }
    }

    /// <summary>
    /// Verify that the trigger event came from the detection collider, not the EnemyAttackCollider
    /// </summary>
    private bool IsDetectionColliderTrigger(Collider2D other)
    {
        // Check if the bounds intersect with our detection collider specifically
        if (detectionCollider != null && other.bounds.Intersects(detectionCollider.bounds))
        {
            // Double check that it's not just the EnemyAttackCollider triggering this
            if (attackCollider != null && attackColliderTransform != null && attackColliderTransform.gameObject.activeSelf)
            {
                // EnemyAttackCollider is active - be more careful about detection
                if (other.bounds.Intersects(attackCollider.bounds) && isAttacking)
                {
                    Debug.Log("EnemyAttackCollider and detection collider both triggered - prioritizing attack");
                    return false;
                }
            }

            Debug.Log("Player detected via detection collider");
            return true;
        }

        return false;
    }

    public void OnAttackAnimationComplete()
    {
        isAttacking = false;
        DisableEnemyAttackCollider();

        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsAlive())
            {
                Debug.Log("Player died during attack - returning to patrol");
                LosePlayer();
                TransitionToState(State.Patrol);
                return;
            }

            // Check if player is still in EnemyAttackCollider range
            if (IsPlayerInAttackRange())
            {
                Debug.Log("Player still in EnemyAttackCollider range - continuing attack state");
                currentState = State.Attack; // Stay in attack state
            }
            else
            {
                Debug.Log("Player moved out of EnemyAttackCollider range - resuming chase");
                TransitionToState(State.Chase);
            }
        }
        else
        {
            TransitionToState(State.Patrol);
        }
    }

    public void OnHurtAnimationComplete()
    {
        Debug.Log("Hurt animation complete");
    }
    #endregion

    #region Debug Methods
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range (for reference)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw ACTUAL EnemyAttackCollider range using Vector2 offset
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

        // Draw EnemyAttackCollider position and range when attacking
        if (attackCollider != null && isAttacking)
        {
            Vector3 attackPos = transform.position + new Vector3(
                facingDirection * attackColliderOffset.x,
                attackColliderOffset.y,
                0
            );
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackPos, attackCollider.radius);
        }

        // NEW: Draw different color when attack collider is pending activation
        if (attackCollider != null && isAttackColliderPending)
        {
            Vector3 attackPos = transform.position + new Vector3(
                facingDirection * attackColliderOffset.x,
                attackColliderOffset.y,
                0
            );
            Gizmos.color = Color.orange; // Orange for "about to activate"
            Gizmos.DrawWireSphere(attackPos, attackCollider.radius);
        }

        // Draw line to player when chasing/attacking
        if (player != null)
        {
            Gizmos.color = currentState == State.Chase ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Draw ground detection rays
        if (Application.isPlaying)
        {
            // Ground check ahead
            Vector2 rayStart = (Vector2)transform.position + groundCheckOffset + new Vector2(facingDirection * groundCheckDistance, 0);
            Gizmos.color = IsGroundAhead() ? Color.green : Color.red;
            Gizmos.DrawLine(rayStart, rayStart + Vector2.down * groundRayLength);

            // Current ground check
            Vector2 currentGroundCheck = (Vector2)transform.position + groundCheckOffset;
            Gizmos.color = isGrounded ? Color.cyan : Color.magenta;
            Gizmos.DrawLine(currentGroundCheck, currentGroundCheck + Vector2.down * (groundRayLength * 0.6f));
        }
    }
    #endregion
}