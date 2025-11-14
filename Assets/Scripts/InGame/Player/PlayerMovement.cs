using UnityEngine;
using System.Collections;

[System.Serializable]
public class PlayerAttackData
{
    [Header("Attack Settings")]
    public int damage = 10;

    [Header("Attack Timing")]
    [Tooltip("Delay before damage collider activates (sync with animation)")]
    [SerializeField] private float damageDelay = 0.2f;

    [Tooltip("How long the damage collider stays active")]
    [SerializeField] private float damageActiveDuration = 0.3f;

    [Tooltip("Total animation duration")]
    public float animationDuration = 0.5f;

    [Header("Fireball Specific Timing")]
    [Tooltip("For Attack 4 (Fireball): Delay before fireball spawns after cast animation starts. Set to 0.6s for 6-frame animation at 60fps.")]
    [SerializeField] private float fireballSpawnDelay = 0.6f;

    [Header("Attack Collider Settings")]
    [Tooltip("Offset from player position (will flip with facing direction)")]
    [SerializeField] private Vector2 attackColliderOffset = new Vector2(1.0f, 0f);

    [Tooltip("Radius of the attack collider")]
    [SerializeField] private float colliderRadius = 0.5f;

    // Public accessors
    public float DamageDelay => damageDelay;
    public float DamageActiveDuration => damageActiveDuration;
    public Vector2 AttackColliderOffset => attackColliderOffset;
    public float ColliderRadius => colliderRadius;
    public float FireballSpawnDelay => fireballSpawnDelay; // NEW: Accessor for fireball spawn delay
}

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float walkSpeed = 3f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CircleCollider2D attackCollider;

    [Header("Attack Configuration")]
    [SerializeField] private PlayerAttackData[] attackData = new PlayerAttackData[5];

    [Header("Fireball Configuration")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform fireballSpawnPoint;
    [SerializeField] private Vector3 fireballSpawnOffset = new Vector3(1.5f, 0.5f, 0);
    [SerializeField] private Vector3 fireballScale = Vector3.one;
    [SerializeField] private bool useSpawnPointOffset = true;
    [SerializeField] private Vector3 spawnPointOffset = new Vector3(0.5f, 0, 0);

    // Core Components
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;
    private UIController uiController;
    private PlayerHealth playerHealth;
    private PlayerAttackCollider playerAttackCollider;

    // State Variables
    private bool isGrounded;
    private bool isJumping;
    private bool isRunning;
    private bool isAttacking;
    private bool isIntroWalking;
    private bool isDead;
    private bool isHurt;
    private bool isRespawning;
    private float facingDirection = 1f;

    // Attack state tracking
    private bool isAttackColliderPending = false;
    private bool isFireballAttackPending = false;
    private PlayerAttackData currentAttackData;

    // Store original physics values
    private float originalGravityScale;
    private RigidbodyType2D originalBodyType;

    void Start()
    {
        InitializeComponents();
        ValidateAttackData();
        ValidateFireballSetup();
        StoreOriginalPhysicsValues();

        Debug.Log($"PlayerMovement initialized on {gameObject.name}");
    }

    void Update()
    {
        CheckDeathState();

        // Block ALL actions when dead or respawning
        if (isDead || isRespawning)
        {
            HandleDeadState();
            return;
        }

        if (isIntroWalking)
        {
            HandleIntroWalking();
            return;
        }

        HandleNormalMovement();
        UpdateFireballSpawnPoint();
        UpdateAttackColliderPosition();
    }

    #region Initialization
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponentInChildren<Animator>();
        uiController = Object.FindFirstObjectByType<UIController>();
        playerHealth = GetComponent<PlayerHealth>();

        if (attackCollider != null)
        {
            playerAttackCollider = attackCollider.GetComponent<PlayerAttackCollider>();
            if (playerAttackCollider == null)
                playerAttackCollider = attackCollider.gameObject.AddComponent<PlayerAttackCollider>();

            attackCollider.gameObject.SetActive(false);
            Debug.Log("Attack collider properly initialized");
        }
        else
        {
            Debug.LogError("Attack collider not assigned in inspector!");
        }

        InitializeFireballSpawnPoint();
        ValidateComponents();
    }

    private void StoreOriginalPhysicsValues()
    {
        if (rb != null)
        {
            originalGravityScale = rb.gravityScale;
            originalBodyType = rb.bodyType;
            Debug.Log($"Original physics stored: GravityScale={originalGravityScale}, BodyType={originalBodyType}");
        }
    }

    private void ValidateComponents()
    {
        if (animator == null) Debug.LogError("Animator not found on Player!");
        if (playerHealth == null) Debug.LogError("PlayerHealth component not found on Player!");
        if (attackCollider == null) Debug.LogError("Attack collider not assigned!");
    }

    private void ValidateAttackData()
    {
        if (attackData == null || attackData.Length != 5)
        {
            Debug.LogError("Attack data must contain exactly 5 attacks!");
            attackData = new PlayerAttackData[5]
            {
                new PlayerAttackData { damage = 10, animationDuration = 0.5f },
                new PlayerAttackData { damage = 15, animationDuration = 0.5f },
                new PlayerAttackData { damage = 12, animationDuration = 0.5f },
                new PlayerAttackData { damage = 20, animationDuration = 1.0f },
                new PlayerAttackData { damage = 25, animationDuration = 0.6f }
            };
        }

        // Log attack configurations including fireball spawn delay for Attack 4
        for (int i = 0; i < attackData.Length; i++)
        {
            string logMessage = $"Attack {i + 1}: Damage={attackData[i].damage}, Delay={attackData[i].DamageDelay}s, " +
                               $"Active={attackData[i].DamageActiveDuration}s, Offset={attackData[i].AttackColliderOffset}, " +
                               $"Radius={attackData[i].ColliderRadius}";

            // Add fireball spawn delay info for Attack 4
            if (i == 3) // Attack 4 (index 3)
            {
                logMessage += $", FireballSpawnDelay={attackData[i].FireballSpawnDelay}s";
            }

            Debug.Log(logMessage);
        }
    }

    private void InitializeFireballSpawnPoint()
    {
        if (fireballSpawnPoint == null)
        {
            Transform spawnPoint = transform.Find("FireballSpawnPoint");
            if (spawnPoint != null)
            {
                fireballSpawnPoint = spawnPoint;
                Debug.Log("Auto-found FireballSpawnPoint child");
            }
            else
            {
                Debug.LogWarning("FireballSpawnPoint child not found. Will use player position with offset.");
            }
        }

        UpdateFireballSpawnPoint();
    }

    private void UpdateFireballSpawnPoint()
    {
        if (fireballSpawnPoint != null)
        {
            Vector3 baseOffset = useSpawnPointOffset ? spawnPointOffset : Vector3.zero;
            Vector3 finalOffset = new Vector3(baseOffset.x * facingDirection, baseOffset.y, baseOffset.z);
            fireballSpawnPoint.position = transform.position + finalOffset;
        }
    }

    private void ValidateFireballSetup()
    {
        if (fireballPrefab == null)
        {
            Debug.LogError("Fireball prefab not assigned! Attack 4 will not work properly.");
            return;
        }

        if (fireballPrefab.activeSelf)
        {
            Debug.LogWarning("Fireball prefab should be inactive in the project. Setting it inactive now.");
            fireballPrefab.SetActive(false);
        }

        FireballProjectile fireballScript = fireballPrefab.GetComponent<FireballProjectile>();
        if (fireballScript == null)
            Debug.LogError("Fireball prefab must have FireballProjectile script!");

        Rigidbody2D fireballRb = fireballPrefab.GetComponent<Rigidbody2D>();
        if (fireballRb == null)
            Debug.LogError("Fireball prefab must have Rigidbody2D component!");

        CircleCollider2D fireballCollider = fireballPrefab.GetComponent<CircleCollider2D>();
        if (fireballCollider == null)
            Debug.LogError("Fireball prefab must have CircleCollider2D component!");

        Debug.Log("Fireball setup validated successfully");
    }
    #endregion

    #region State Management
    private void CheckDeathState()
    {
        if (playerHealth == null) return;

        bool wasAlive = !isDead && !isRespawning;
        bool isCurrentlyAlive = playerHealth.IsAlive();

        if (wasAlive && !isCurrentlyAlive)
            OnPlayerDeath();
        else if (!wasAlive && isCurrentlyAlive && !playerHealth.IsRespawning())
            OnPlayerRespawn();

        isDead = !isCurrentlyAlive;
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player movement COMPLETELY disabled due to death");

        isDead = true;
        isRespawning = true;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        SetCollidersEnabled(false);
        DisableAllAttackEffects();
        ResetAllStates();

        if (animator != null)
        {
            ResetAnimatorToDefaultState();
            StartCoroutine(TriggerDeathAnimation());
        }
    }

    private void OnPlayerRespawn()
    {
        Debug.Log("Player movement RE-ENABLED after respawn");

        isDead = false;
        isRespawning = false;

        PerformCompleteRespawnReset();

        if (animator != null)
            ResetAnimatorToDefaultState();
    }

    private void PerformCompleteRespawnReset()
    {
        Debug.Log("Performing complete respawn reset...");

        ResetAllStates();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = originalBodyType;
            rb.gravityScale = originalGravityScale;
            rb.totalForce = Vector2.zero;
            rb.totalTorque = 0f;

            Debug.Log($"Physics reset: BodyType={rb.bodyType}, GravityScale={rb.gravityScale}, Velocity={rb.linearVelocity}");
        }

        ForceGroundingCheck();
        SetCollidersEnabled(true);

        facingDirection = 1f;
        if (spriteRenderer != null)
            spriteRenderer.flipX = false;

        CancelAllInvokes();

        Debug.Log("Complete respawn reset finished");
    }

    private void ForceGroundingCheck()
    {
        isJumping = false;
        isGrounded = false;

        Collider2D[] groundColliders = Physics2D.OverlapBoxAll(
            transform.position + Vector3.down * 0.1f,
            new Vector2(0.8f, 0.1f),
            0f
        );

        foreach (Collider2D col in groundColliders)
        {
            if (col.CompareTag("Ground"))
            {
                isGrounded = true;
                Debug.Log("Force grounding check: Player is grounded");
                break;
            }
        }
    }

    private void CancelAllInvokes()
    {
        CancelInvoke(nameof(ActivateDelayedAttackCollider));
        CancelInvoke(nameof(DisableAttackCollider));
        CancelInvoke(nameof(ResetAttackState));
        CancelInvoke(nameof(ExecuteFireballSpawn));
        CancelInvoke(nameof(ResetHurtState));
        Debug.Log("All invokes cancelled during respawn reset");
    }

    private void SetCollidersEnabled(bool enabled)
    {
        CapsuleCollider2D[] colliders = GetComponents<CapsuleCollider2D>();
        foreach (var col in colliders)
            col.enabled = enabled;
    }

    private void ResetAllStates()
    {
        isAttacking = false;
        isHurt = false;
        isRunning = false;
        isJumping = false;
        isAttackColliderPending = false;
        isFireballAttackPending = false;

        Debug.Log("All player states reset");
    }

    private void HandleDeadState()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        DisableAllAttackEffects();

        SetAnimatorBool("isRunning", false);
        SetAnimatorBool("isWalking", false);
        SetAnimatorBool("isJumping", false);
        SetAnimatorBool("isFalling", false);
    }

    private void DisableAllAttackEffects()
    {
        CancelInvoke(nameof(ActivateDelayedAttackCollider));
        CancelInvoke(nameof(DisableAttackCollider));
        CancelInvoke(nameof(ResetAttackState));
        CancelInvoke(nameof(ExecuteFireballSpawn));

        if (attackCollider != null)
            attackCollider.gameObject.SetActive(false);

        isAttacking = false;
        isAttackColliderPending = false;
        isFireballAttackPending = false;
    }

    public void OnRespawnComplete()
    {
        Debug.Log("PlayerMovement: Respawn completion notification received");

        isRespawning = false;
        PerformCompleteRespawnReset();
        StartCoroutine(VerifyRespawnState());
    }

    private IEnumerator VerifyRespawnState()
    {
        yield return new WaitForEndOfFrame();

        if (rb != null)
        {
            Debug.Log($"Post-respawn verification: Velocity={rb.linearVelocity}, BodyType={rb.bodyType}, " +
                     $"GravityScale={rb.gravityScale}, Grounded={isGrounded}, Jumping={isJumping}");

            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f || Mathf.Abs(rb.linearVelocity.y) > 0.1f)
            {
                Debug.LogWarning("Detected residual velocity after respawn - forcing zero");
                rb.linearVelocity = Vector2.zero;
            }
        }
    }
    #endregion

    #region Movement Handling
    private void HandleNormalMovement()
    {
        if (isDead || isRespawning) return;

        if (isHurt)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        ProcessMovementInput();
        HandleJump();
        HandleAnimations();
    }

    private void ProcessMovementInput()
    {
        if (isDead || isRespawning || isAttacking) return;

        float moveInput = 0f;
        if (uiController != null)
        {
            if (uiController.IsMovingLeft) moveInput = -1f;
            else if (uiController.IsMovingRight) moveInput = 1f;
        }

        if (!isAttacking)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            if (moveInput != 0 && !isHurt)
            {
                facingDirection = moveInput > 0 ? 1f : -1f;
                spriteRenderer.flipX = facingDirection < 0;
            }

            isRunning = moveInput != 0 && !isHurt;
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            isRunning = false;
        }
    }

    private void HandleJump()
    {
        if (isDead || isRespawning || isAttacking) return;

        if (uiController != null && uiController.IsJumping && isGrounded && !isHurt && !isJumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;

            SetAnimatorBool("isJumping", true);
            uiController.ResetJump();

            Debug.Log($"Jump executed: JumpForce={jumpForce}, NewVelocity={rb.linearVelocity}");
        }
    }

    private void HandleAnimations()
    {
        if (isDead || isRespawning) return;

        SetAnimatorBool("isRunning", isRunning && !isAttacking);
        SetAnimatorBool("isWalking", false);

        if (rb.linearVelocity.y < 0 && !isGrounded)
            SetAnimatorBool("isFalling", true);
        else
            SetAnimatorBool("isFalling", false);
    }

    private void HandleIntroWalking()
    {
        if (isDead || isRespawning) return;

        spriteRenderer.flipX = false;
        facingDirection = 1f;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName("Walk") && !stateInfo.IsName("RoranWalk"))
        {
            SetAnimatorBool("isWalking", true);
        }

        SetAnimatorBool("isRunning", false);
        SetAnimatorBool("isWalking", true);
    }
    #endregion

    #region Attack System
    public void TriggerAttack(int attackIndex)
    {
        if (isIntroWalking || isAttacking || isDead || isHurt || isRespawning)
        {
            if (isDead || isRespawning)
                Debug.Log("Attack BLOCKED - Player is dead or respawning");
            else if (isAttacking)
                Debug.Log("Attack BLOCKED - Already attacking");
            return;
        }

        if (!IsValidAttackIndex(attackIndex)) return;

        string attackTrigger = $"Attack{attackIndex}";

        if (HasAnimatorParameter(attackTrigger, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(attackTrigger);
            isAttacking = true;

            currentAttackData = attackData[attackIndex - 1];

            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (attackIndex == 4)
            {
                ScheduleFireballAttack();
            }
            else
            {
                ScheduleDelayedAttackCollider(currentAttackData);
            }

            float duration = GetAttackAnimationDuration(attackIndex);
            Invoke(nameof(ResetAttackState), duration);

            Debug.Log($"Attack {attackIndex} initiated - Movement BLOCKED for {duration}s");
        }
        else
        {
            Debug.LogError($"{attackTrigger} trigger not found in animator");
        }
    }

    private void ScheduleDelayedAttackCollider(PlayerAttackData attack)
    {
        if (isDead || isRespawning)
        {
            Debug.Log("Attack collider schedule CANCELLED - Player is dead or respawning");
            return;
        }

        isAttackColliderPending = true;

        Invoke(nameof(ActivateDelayedAttackCollider), attack.DamageDelay);
        Invoke(nameof(DisableAttackCollider), attack.DamageDelay + attack.DamageActiveDuration);

        Debug.Log($"Attack collider scheduled: Delay={attack.DamageDelay}s, Active={attack.DamageActiveDuration}s, " +
                 $"Total={attack.DamageDelay + attack.DamageActiveDuration}s");
    }

    private void ActivateDelayedAttackCollider()
    {
        if (!isAttackColliderPending || isDead || isRespawning || currentAttackData == null)
        {
            if (isDead || isRespawning)
                Debug.Log("Attack collider activation CANCELLED - Player is dead or respawning");
            else if (!isAttackColliderPending)
                Debug.Log("Attack collider activation CANCELLED - Attack was cancelled");

            isAttackColliderPending = false;
            return;
        }

        SetupAttackCollider(currentAttackData);
        isAttackColliderPending = false;

        Debug.Log("Attack collider NOW ACTIVE - Enemies can be hit!");
    }

    private void UpdateAttackColliderPosition()
    {
        if (attackCollider != null && attackCollider.gameObject.activeSelf && currentAttackData != null)
        {
            Vector3 offset = new Vector3(
                currentAttackData.AttackColliderOffset.x * facingDirection,
                currentAttackData.AttackColliderOffset.y,
                0
            );

            attackCollider.transform.position = transform.position + offset;
        }
    }

    private void SetupAttackCollider(PlayerAttackData attack)
    {
        if (isDead || isRespawning || attackCollider == null || playerAttackCollider == null)
        {
            if (isDead || isRespawning)
                Debug.Log("Attack collider setup BLOCKED - Player is dead or respawning");
            return;
        }

        attackCollider.gameObject.SetActive(true);
        playerAttackCollider.SetDamage(attack.damage);

        Vector3 offset = new Vector3(
            attack.AttackColliderOffset.x * facingDirection,
            attack.AttackColliderOffset.y,
            0
        );
        attackCollider.transform.position = transform.position + offset;

        attackCollider.radius = attack.ColliderRadius;

        Debug.Log($"Attack collider activated: Position={attackCollider.transform.position}, " +
                 $"Offset={offset}, Radius={attack.ColliderRadius}, Damage={attack.damage}, FacingDir={facingDirection}");
    }

    private void ScheduleFireballAttack()
    {
        if (isDead || isRespawning)
        {
            Debug.Log("Fireball attack BLOCKED - Player is dead or respawning");
            return;
        }

        isFireballAttackPending = true;

        // UPDATED: Use the dedicated fireball spawn delay from Attack 4 data
        float fireballDelay = currentAttackData != null ? currentAttackData.FireballSpawnDelay : 0.6f;
        Invoke(nameof(ExecuteFireballSpawn), fireballDelay);

        Debug.Log($"Fireball attack scheduled to spawn after {fireballDelay}s (synced with cast animation)");
    }

    private void ExecuteFireballSpawn()
    {
        if (!isFireballAttackPending || isDead || isRespawning || fireballPrefab == null)
        {
            if (isDead || isRespawning)
                Debug.Log("Fireball spawn BLOCKED - Player is dead or respawning");
            else if (!isFireballAttackPending)
                Debug.Log("Fireball spawn BLOCKED - Attack was cancelled");
            else
                Debug.LogError("Fireball spawn BLOCKED - Fireball prefab is null!");

            isFireballAttackPending = false;
            return;
        }

        var attack = GetAttackData(4);
        if (attack == null)
        {
            Debug.LogError("Attack 4 data not found!");
            isFireballAttackPending = false;
            return;
        }

        Vector3 spawnPosition = CalculateFireballSpawnPosition();
        Vector2 fireballDirection = new Vector2(facingDirection, 0);

        Debug.Log($"Executing fireball spawn at position: {spawnPosition} with direction: {fireballDirection}");

        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);

        if (fireball == null)
        {
            Debug.LogError("Failed to instantiate fireball!");
            isFireballAttackPending = false;
            return;
        }

        fireball.SetActive(true);
        fireball.transform.localScale = fireballScale;

        FireballProjectile fireballScript = fireball.GetComponent<FireballProjectile>();
        if (fireballScript != null)
        {
            fireballScript.Initialize(attack.damage, fireballDirection, facingDirection);
            Debug.Log($"Fireball successfully spawned with {attack.damage} damage in direction {fireballDirection} " +
                     $"at scale {fireballScale} facing {facingDirection}");
        }
        else
        {
            Debug.LogError("FireballProjectile script not found on instantiated fireball!");
            Destroy(fireball);
        }

        isFireballAttackPending = false;
    }

    private Vector3 CalculateFireballSpawnPosition()
    {
        Vector3 spawnPosition;

        if (fireballSpawnPoint != null)
        {
            spawnPosition = fireballSpawnPoint.position;
            Debug.Log($"Using spawn point position: {spawnPosition}");
        }
        else
        {
            Vector3 adjustedOffset = new Vector3(
                fireballSpawnOffset.x * facingDirection,
                fireballSpawnOffset.y,
                fireballSpawnOffset.z
            );
            spawnPosition = transform.position + adjustedOffset;
            Debug.Log($"Using player position with offset: {spawnPosition} (offset: {adjustedOffset})");
        }

        return spawnPosition;
    }

    private void DisableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.gameObject.SetActive(false);
            Debug.Log("Attack collider disabled");
        }

        isAttackColliderPending = false;
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        DisableAttackCollider();
        currentAttackData = null;

        Debug.Log("Attack state reset - Movement ENABLED");
    }

    private bool IsValidAttackIndex(int attackIndex)
    {
        return attackIndex >= 1 && attackIndex <= attackData.Length;
    }

    private PlayerAttackData GetAttackData(int attackIndex)
    {
        if (!IsValidAttackIndex(attackIndex)) return null;
        return attackData[attackIndex - 1];
    }

    private float GetAttackAnimationDuration(int attackIndex)
    {
        var attack = GetAttackData(attackIndex);
        if (attack == null) return 0.5f;

        return attack.animationDuration / (uiController?.CharacterSpeedMultiplier ?? 1f);
    }
    #endregion

    #region Animation Helper Methods
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
        if (HasAnimatorParameter(paramName, AnimatorControllerParameterType.Bool))
            animator.SetBool(paramName, value);
    }

    private void ResetAnimatorToDefaultState()
    {
        if (animator == null) return;

        SetAnimatorBool("isRunning", false);
        SetAnimatorBool("isWalking", false);
        SetAnimatorBool("isJumping", false);
        SetAnimatorBool("isFalling", false);

        string[] triggers = { "Attack1", "Attack2", "Attack3", "Attack4", "Attack5", "Hurt", "Die" };
        foreach (string trigger in triggers)
        {
            if (HasAnimatorParameter(trigger, AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger(trigger);
        }

        animator.Update(0f);

        Debug.Log("Animator reset to default state");
    }

    private IEnumerator TriggerDeathAnimation()
    {
        yield return null;

        if (HasAnimatorParameter("Die", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Die");
            Debug.Log("Death animation triggered");
        }
    }
    #endregion

    #region Collision Handling
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || isRespawning) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            isJumping = false;
            if (!isIntroWalking)
            {
                SetAnimatorBool("isJumping", false);
                SetAnimatorBool("isFalling", false);
            }

            Debug.Log($"Player grounded: isGrounded={isGrounded}, isJumping={isJumping}");
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (isDead || isRespawning) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            Debug.Log($"Player left ground: isGrounded={isGrounded}");
        }
    }
    #endregion

    #region Special States
    public void TriggerHurt()
    {
        if (isDead || isRespawning) return;

        isHurt = true;

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (HasAnimatorParameter("Hurt", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Hurt");
        }

        Invoke(nameof(ResetHurtState), 0.5f);
    }

    private void ResetHurtState()
    {
        isHurt = false;
    }

    public void TriggerDeath()
    {
        if (isDead) return;

        isDead = true;
        isRespawning = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        DisableAllAttackEffects();
        ResetAllStates();

        if (animator != null)
        {
            ResetAnimatorToDefaultState();

            if (HasAnimatorParameter("Die", AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger("Die");
            }
        }
    }

    public void SetIntroWalking(bool walking)
    {
        if (isDead || isRespawning) return;

        isIntroWalking = walking;

        if (walking)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            ResetAnimatorToDefaultState();
            StartCoroutine(StartWalkSequence());
        }
        else
        {
            SetAnimatorBool("isWalking", false);
            ResetAnimatorToDefaultState();
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private IEnumerator StartWalkSequence()
    {
        yield return new WaitForFixedUpdate();
        SetAnimatorBool("isWalking", true);
    }
    #endregion

    #region Public Properties and Methods
    public bool CanPerformAttack()
    {
        return !isAttacking && !isDead && !isHurt && isGrounded && !isJumping && !isIntroWalking && !isRespawning;
    }

    public bool IsAlive() => !isDead && !isRespawning;
    public int GetCurrentHealth() => playerHealth?.GetCurrentHealth() ?? 0;
    public int GetMaxHealth() => playerHealth?.GetMaxHealth() ?? 100;
    public float GetFacingDirection() => facingDirection;
    public float GetWalkSpeed() => walkSpeed;
    #endregion
}