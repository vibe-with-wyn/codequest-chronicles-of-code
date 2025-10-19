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
    public float attackDelay = 0.3f; // Delay before damage is dealt (animation windup)
    
    [Header("Attack Collider Settings")]
    public Vector2 attackColliderOffset = new Vector2(1.5f, 0f);
    public float attackColliderRadius = 1.0f;
}

public class ArinNPCAI : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float bosDetectionRange = 10f; // Detection range for cave boss
    [SerializeField] private float playerDetectionRange = 5f; // NEW: Player must be this close to trigger battle
    [SerializeField] private string caveBossTag = "CaveBoss";
    [SerializeField] private string playerTag = "Player"; // NEW: Tag for player detection
    
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 3.5f; // NEW: Single attack range - if boss is within this, stop and attack
    [SerializeField] private float attackCooldownGlobal = 1.5f; // Global cooldown between any attacks
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    
    [Header("Attack Configuration")]
    [SerializeField] private ArinAttackData[] attacks = new ArinAttackData[4];
    
    [Header("Collider References")]
    [SerializeField] private CircleCollider2D bossDetectionCollider; // NEW: Renamed for clarity
    [SerializeField] private CircleCollider2D playerDetectionCollider; // NEW: Separate collider for player detection
    [SerializeField] private Transform attackColliderTransform;
    
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    
    // State Management
    private enum NPCState { Idle, MovingToTarget, InCombat, Attacking, Cooldown }
    private NPCState currentState = NPCState.Idle;
    
    // Target Management
    private Transform caveBossTarget; // The cave boss
    private bool isCaveBossDetected = false;
    private bool isPlayerNearby = false; // NEW: Tracks if player is nearby
    private bool battleStarted = false; // NEW: Tracks if battle has been initiated
    
    // Attack Management
    private float lastAttackTime = 0f;
    private float[] attackCooldownTimers = new float[4];
    private bool isAttacking = false;
    
    // Direction
    private float facingDirection = 1f; // 1 = right, -1 = left
    
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
        SetupDetectionColliders(); // UPDATED
        ValidateAttackData();
        
        Debug.Log("Arin NPC AI initialized - waiting for player to trigger battle");
    }

    void Update()
    {
        UpdateAttackCooldowns();
        ProcessStateMachine();
    }

    #region Initialization
    private void InitializeComponents()
    {
        // Auto-find components if not assigned
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        
        // Validate critical components
        if (animator == null)
            Debug.LogError("Animator not found on Arin NPC!");
        
        if (rb == null)
            Debug.LogError("Rigidbody2D not found on Arin NPC!");
    }

    private void InitializeAttackSystem()
    {
        // Initialize attack cooldown timers
        attackCooldownTimers = new float[attacks.Length];
        for (int i = 0; i < attackCooldownTimers.Length; i++)
        {
            attackCooldownTimers[i] = 0f;
        }
        
        // Setup attack collider
        if (attackColliderTransform == null)
        {
            // Try to find existing attack collider
            attackColliderTransform = transform.Find("ArinAttackCollider");
            
            // Create if doesn't exist
            if (attackColliderTransform == null)
            {
                GameObject attackColliderObj = new GameObject("ArinAttackCollider");
                attackColliderObj.transform.SetParent(transform);
                attackColliderObj.transform.localPosition = Vector3.zero;
                attackColliderTransform = attackColliderObj.transform;
            }
        }
        
        // Get or add components to attack collider
        attackCollider = attackColliderTransform.GetComponent<CircleCollider2D>();
        if (attackCollider == null)
        {
            attackCollider = attackColliderTransform.gameObject.AddComponent<CircleCollider2D>();
        }
        
        attackCollider.isTrigger = true;
        attackCollider.radius = 1.0f; // Default radius
        
        // Get or add ArinAttackCollider script
        arinAttackCollider = attackColliderTransform.GetComponent<ArinAttackCollider>();
        if (arinAttackCollider == null)
        {
            arinAttackCollider = attackColliderTransform.gameObject.AddComponent<ArinAttackCollider>();
        }
        
        // Initially disable attack collider
        attackColliderTransform.gameObject.SetActive(false);
        
        Debug.Log("Arin attack system initialized");
    }

    // UPDATED: Setup both boss detection and player detection colliders
    private void SetupDetectionColliders()
    {
        // Setup Boss Detection Collider
        if (bossDetectionCollider == null)
        {
            // Try to find existing collider
            CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
            if (colliders.Length > 0)
            {
                bossDetectionCollider = colliders[0];
            }
            else
            {
                bossDetectionCollider = gameObject.AddComponent<CircleCollider2D>();
            }
        }
        
        bossDetectionCollider.isTrigger = true;
        bossDetectionCollider.radius = bosDetectionRange;
        
        Debug.Log($"Arin boss detection collider configured: radius={bosDetectionRange}");
        
        // Setup Player Detection Collider
        if (playerDetectionCollider == null)
        {
            // Check if there's a second collider
            CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
            if (colliders.Length > 1)
            {
                playerDetectionCollider = colliders[1];
            }
            else
            {
                playerDetectionCollider = gameObject.AddComponent<CircleCollider2D>();
            }
        }
        
        playerDetectionCollider.isTrigger = true;
        playerDetectionCollider.radius = playerDetectionRange;
        
        Debug.Log($"Arin player detection collider configured: radius={playerDetectionRange}");
    }

    private void ValidateAttackData()
    {
        if (attacks == null || attacks.Length != 4)
        {
            Debug.LogWarning("Arin should have exactly 4 attacks configured!");
            attacks = new ArinAttackData[4]
            {
                new ArinAttackData { attackName = "Water Blast", animatorTrigger = "Attack1", damage = 20, cooldown = 2.0f },
                new ArinAttackData { attackName = "Ice Shard", animatorTrigger = "Attack2", damage = 25, cooldown = 3.0f },
                new ArinAttackData { attackName = "Tidal Wave", animatorTrigger = "Attack3", damage = 30, cooldown = 4.0f },
                new ArinAttackData { attackName = "Frost Nova", animatorTrigger = "Attack4", damage = 35, cooldown = 5.0f }
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
            
            case NPCState.MovingToTarget:
                HandleMovingToTargetState();
                break;
            
            case NPCState.InCombat:
                HandleInCombatState(); // NEW: Combat state - decides between moving and attacking
                break;
            
            case NPCState.Attacking:
                HandleAttackingState();
                break;
            
            case NPCState.Cooldown:
                HandleCooldownState();
                break;
        }
    }

    // UPDATED: Now checks if player is nearby before starting battle
    private void HandleIdleState()
    {
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        // Play idle animation
        SetAnimatorBool("isMoving", false);
        
        // NEW: Check if battle should start
        if (isCaveBossDetected && isPlayerNearby && !battleStarted)
        {
            StartBattle();
        }
    }

    // REMOVED: Old HandleMovingToTargetState - replaced with InCombat state
    private void HandleMovingToTargetState()
    {
        // This state is now only used for initial approach before battle starts
        // Once battle starts, all movement/combat logic is in InCombat state
        
        if (caveBossTarget == null)
        {
            TransitionToState(NPCState.Idle);
            return;
        }
        
        float distanceToBoss = Vector2.Distance(transform.position, caveBossTarget.position);
        
        // Move towards boss
        Vector2 direction = (caveBossTarget.position - transform.position).normalized;
        
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        }
        
        // Update facing direction
        UpdateFacingDirection(direction.x);
        
        // Play walk animation
        SetAnimatorBool("isMoving", true);
    }

    // NEW: Combat state - handles both movement and attack decisions
    private void HandleInCombatState()
    {
        if (caveBossTarget == null)
        {
            battleStarted = false;
            TransitionToState(NPCState.Idle);
            return;
        }
        
        float distanceToBoss = Vector2.Distance(transform.position, caveBossTarget.position);
        
        // REQUIREMENT 3: If boss is within attack range, stop and attack
        if (distanceToBoss <= attackRange)
        {
            // Stop movement
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            
            SetAnimatorBool("isMoving", false);
            
            // Face the boss
            float directionToBoss = Mathf.Sign(caveBossTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToBoss);
            
            // Try to attack if ready
            if (CanAttack())
            {
                TransitionToState(NPCState.Attacking);
            }
        }
        // REQUIREMENT 3: If boss is out of attack range, walk towards it
        else
        {
            Vector2 direction = (caveBossTarget.position - transform.position).normalized;
            
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
            }
            
            // Update facing direction
            UpdateFacingDirection(direction.x);
            
            // Play walk animation
            SetAnimatorBool("isMoving", true);
            
            Debug.Log($"Arin moving towards boss - Distance: {distanceToBoss:F2}, Attack Range: {attackRange:F2}\nBattle: {(battleStarted ? "ACTIVE" : "WAITING")}");
        }
    }

    private void HandleAttackingState()
    {
        // Stop movement during attack
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        SetAnimatorBool("isMoving", false);
        
        // Attack is handled by PerformRandomAttack() which is called when entering this state
        // This state is temporary and will transition to Cooldown automatically
    }

    // UPDATED: Returns to InCombat state after cooldown
    private void HandleCooldownState()
    {
        // Stop movement during cooldown
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        SetAnimatorBool("isMoving", false);
        
        // Check if cooldown is over
        if (Time.time >= lastAttackTime + attackCooldownGlobal)
        {
            if (caveBossTarget != null && battleStarted)
            {
                TransitionToState(NPCState.InCombat); // Return to combat state
            }
            else
            {
                TransitionToState(NPCState.Idle);
            }
        }
    }

    private void TransitionToState(NPCState newState)
    {
        NPCState oldState = currentState;
        currentState = newState;
        
        Debug.Log($"Arin state transition: {oldState} -> {newState}");
        
        // Handle state entry actions
        switch (newState)
        {
            case NPCState.Attacking:
                PerformRandomAttack();
                break;
        }
    }

    // NEW: Start battle when player is nearby
    private void StartBattle()
    {
        battleStarted = true;
        Debug.Log("Battle started! Arin begins combat with Cave Boss!");
        
        // NEW: Complete quest objective when player helps Arin
        if (completeObjectiveOnBattleStart && QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteObjectiveByTitle(helpObjectiveTitle);
            Debug.Log($"Quest objective '{helpObjectiveTitle}' completed!");
        }
        
        TransitionToState(NPCState.InCombat);
    }
    #endregion

    #region Combat System
    // UPDATED: Now truly random - selects from Attack1, Attack2, Attack3, Attack4
    private void PerformRandomAttack()
    {
        if (isAttacking) return;
        
        // REQUIREMENT 1: Get list of available attacks (off cooldown)
        int[] availableAttacks = GetAvailableAttacks();
        
        if (availableAttacks.Length == 0)
        {
            Debug.Log("No attacks available - waiting for cooldowns");
            TransitionToState(NPCState.Cooldown);
            return;
        }
        
        // REQUIREMENT 1: Select a RANDOM attack from available attacks
        int selectedAttackIndex = availableAttacks[Random.Range(0, availableAttacks.Length)];
        ArinAttackData selectedAttack = attacks[selectedAttackIndex];
        
        Debug.Log($"Arin randomly selected: {selectedAttack.attackName} (Attack{selectedAttackIndex + 1})");
        
        // Face the target
        if (caveBossTarget != null)
        {
            float directionToTarget = Mathf.Sign(caveBossTarget.position.x - transform.position.x);
            UpdateFacingDirection(directionToTarget);
        }
        
        // Trigger attack
        isAttacking = true;
        
        // Trigger animation
        if (animator != null && HasAnimatorParameter(selectedAttack.animatorTrigger, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(selectedAttack.animatorTrigger);
            Debug.Log($"Arin using {selectedAttack.attackName} (Trigger: {selectedAttack.animatorTrigger})");
        }
        else
        {
            Debug.LogError($"Animator trigger '{selectedAttack.animatorTrigger}' not found!");
        }
        
        // Schedule attack collider activation
        StartCoroutine(ExecuteAttackSequence(selectedAttack, selectedAttackIndex));
    }

    private IEnumerator ExecuteAttackSequence(ArinAttackData attackData, int attackIndex)
    {
        // Wait for attack delay (animation windup)
        yield return new WaitForSeconds(attackData.attackDelay);
        
        // Activate attack collider
        ActivateAttackCollider(attackData);
        
        // Wait for attack to complete
        yield return new WaitForSeconds(attackData.animationDuration - attackData.attackDelay);
        
        // Deactivate attack collider
        DeactivateAttackCollider();
        
        // Mark attack as used
        attackCooldownTimers[attackIndex] = attackData.cooldown;
        lastAttackTime = Time.time;
        
        isAttacking = false;
        
        // Transition to cooldown state
        TransitionToState(NPCState.Cooldown);
    }

    private void ActivateAttackCollider(ArinAttackData attackData)
    {
        if (attackColliderTransform == null || arinAttackCollider == null || attackCollider == null)
        {
            Debug.LogError("Attack collider components not set up!");
            return;
        }
        
        // Enable attack collider GameObject
        attackColliderTransform.gameObject.SetActive(true);
        
        // Set damage
        arinAttackCollider.SetDamage(attackData.damage);
        
        // Position the attack collider
        Vector3 attackPosition = transform.position + new Vector3(
            facingDirection * attackData.attackColliderOffset.x,
            attackData.attackColliderOffset.y,
            0
        );
        attackColliderTransform.position = attackPosition;
        
        // Set collider radius
        attackCollider.radius = attackData.attackColliderRadius;
        
        Debug.Log($"Arin attack collider activated: Position={attackPosition}, Radius={attackData.attackColliderRadius}, Damage={attackData.damage}");
    }

    private void DeactivateAttackCollider()
    {
        if (attackColliderTransform != null)
        {
            attackColliderTransform.gameObject.SetActive(false);
            Debug.Log("Arin attack collider deactivated");
        }
    }

    // REQUIREMENT 1: Returns all attacks that are off cooldown
    private int[] GetAvailableAttacks()
    {
        System.Collections.Generic.List<int> available = new System.Collections.Generic.List<int>();
        
        for (int i = 0; i < attacks.Length; i++)
        {
            if (attackCooldownTimers[i] <= 0f)
            {
                available.Add(i);
            }
        }
        
        return available.ToArray();
    }

    private bool CanAttack()
    {
        // Check if global cooldown is ready
        if (Time.time < lastAttackTime + attackCooldownGlobal)
        {
            return false;
        }
        
        // Check if any attack is available
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
        // Check if it's the cave boss
        if (other.CompareTag(caveBossTag))
        {
            Debug.Log($"Arin detected Cave Boss: {other.name}");
            caveBossTarget = other.transform;
            isCaveBossDetected = true;
        }
        
        // NEW: Check if it's the player
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Player entered Arin's trigger zone: {other.name}");
            isPlayerNearby = true;
            
            // REQUIREMENT 2: Start battle if both boss and player are detected
            if (isCaveBossDetected && !battleStarted)
            {
                StartBattle();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Check if cave boss left detection range
        if (other.CompareTag(caveBossTag) && other.transform == caveBossTarget)
        {
            Debug.Log("Cave Boss left Arin's detection range");
            isCaveBossDetected = false;
        }
        
        // NEW: Check if player left detection range
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Player left Arin's trigger zone");
            isPlayerNearby = false;
            // Note: Battle continues even if player leaves
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Continuously update that boss is still in range
        if (other.CompareTag(caveBossTag))
        {
            isCaveBossDetected = true;
        }
        
        // NEW: Continuously update that player is still in range
        if (other.CompareTag(playerTag))
        {
            isPlayerNearby = true;
        }
    }
    #endregion

    #region Debug
    void OnDrawGizmosSelected()
    {
        // Draw boss detection range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bosDetectionRange);
        
        // Draw player detection range (cyan) - NEW
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
        
        // Draw attack range (red) - UPDATED
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw line to target
        if (caveBossTarget != null && Application.isPlaying)
        {
            Gizmos.color = battleStarted ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, caveBossTarget.position);
            
            // Draw distance text
            float distance = Vector2.Distance(transform.position, caveBossTarget.position);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Distance: {distance:F2}\nAttack Range: {attackRange:F2}\nBattle: {(battleStarted ? "ACTIVE" : "WAITING")}");
        }
        
        // Draw attack collider positions for each attack
        if (attacks != null && Application.isPlaying)
        {
            foreach (var attack in attacks)
            {
                Vector3 attackPos = transform.position + new Vector3(
                    facingDirection * attack.attackColliderOffset.x,
                    attack.attackColliderOffset.y,
                    0
                );
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(attackPos, attack.attackColliderRadius);
            }
        }
    }
    #endregion
}