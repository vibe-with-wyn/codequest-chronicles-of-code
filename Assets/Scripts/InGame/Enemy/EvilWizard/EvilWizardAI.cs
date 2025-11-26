using UnityEngine;
using System.Collections;

[System.Serializable]
public class WizardAttackData
{
    [Header("Attack Settings")]
    public string triggerName = "Attack1";
    public int damage = 20;
    public float cooldown = 2.5f;
    public float activeTime = 0.5f;

    [Header("Attack Timing")]
    [SerializeField] private float attackDelay = 0.4f;

    [Header("Attack Range")]
    [SerializeField] private float attackRange = 3f;

    public float AttackDelay => attackDelay;
    public float AttackRange => attackRange;
}

public class EvilWizardAI : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 10f;

    [Header("Movement Settings")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float returnSpeed = 2f;
    [SerializeField] private float returnStopDistance = 0.2f;

    [Header("Combat Settings")]
    [SerializeField] private float hurtDuration = 0.5f;
    [SerializeField] private float playerLossDelay = 2f;
    [SerializeField] private Vector2 attackColliderOffset = new Vector2(1.5f, 0f);

    [Header("Death Settings")]
    [Tooltip("Duration of death animation (seconds)")]
    [SerializeField] private float deathAnimationDuration = 2f;

    [Header("Attack Configuration")]
    [SerializeField] private WizardAttackData[] attacks = new WizardAttackData[2];

    [Header("Collider References")]
    [SerializeField] private CapsuleCollider2D bodyCollider;
    [SerializeField] private CircleCollider2D detectionCollider;

    [Header("Other References")]
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
    private EvilWizardAudioController audioController;

    // State
    private Vector3 originalPosition;
    private enum State { Idle, Chase, Attack, Returning, Hurt, Dead }
    private State currentState;
    private State previousState;

    // Combat
    private float lastAttackTime;
    private float facingDirection = 1f;
    private bool isAttacking;
    private bool isAttackColliderPending;
    private WizardAttackData currentAttackData;

    // Detection
    private bool isPlayerDetected;
    private float playerDetectedTimer;

    // Hurt / Death
    private bool isHurt;
    private float hurtRecoveryTimer;
    private bool isDead;
    private bool deathProcessed;
    private bool deathSequenceStarted;

    void Start()
    {
        InitializeComponents();
        SetupColliders();
        InitializeState();
        ValidateAttackData();
        InitializeAudioController();
        if (debugMode) Debug.Log($"Evil Wizard AI initialized at {originalPosition}");
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
            if (debugMode) Debug.Log("Wizard: Animator child found");
        }
        else
        {
            Debug.LogError("Wizard: Animator child GameObject missing (name must be 'Animator')");
        }

        SetupWizardAttackCollider();
    }

    private void InitializeAudioController()
    {
        audioController = GetComponent<EvilWizardAudioController>();

        if (audioController != null)
        {
            if (debugMode) Debug.Log($"EvilWizardAudioController found on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"EvilWizardAudioController not found on {gameObject.name}. Wizard will have no sound effects.");
        }
    }

    private void SetupColliders()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<CapsuleCollider2D>();
            if (bodyCollider == null)
                Debug.LogError("Wizard: Body collider (CapsuleCollider2D) missing");
            else if (debugMode)
                Debug.Log("Wizard: Auto-found body collider");
        }

        if (detectionCollider == null)
        {
            detectionCollider = GetComponent<CircleCollider2D>();
            if (detectionCollider == null)
                detectionCollider = gameObject.AddComponent<CircleCollider2D>();
            if (debugMode) Debug.Log("Wizard: Auto-found/created detection collider");
        }

        if (detectionCollider != null)
        {
            detectionCollider.isTrigger = true;
            detectionCollider.radius = detectionRange;
        }

        if (bodyCollider != null) bodyCollider.isTrigger = false;

        ValidateColliderSetup();
    }

    private void ValidateColliderSetup()
    {
        if (bodyCollider == null) Debug.LogError("Wizard: Body collider not assigned");
        if (detectionCollider == null) Debug.LogError("Wizard: Detection collider not assigned");
        if (attackCollider == null) Debug.LogError("Wizard: Attack collider not assigned");
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
                attackColliderTransform.gameObject.SetActive(false);

            if (wizardAttackCollider == null || attackCollider == null)
                Debug.LogError("Wizard: Attack collider child lacks required components");
            else if (debugMode)
                Debug.Log("Wizard: Attack collider initialized");
        }
        else
        {
            Debug.LogError("Wizard: Missing child 'WizardAttackCollider'");
        }
    }

    private void InitializeState()
    {
        originalPosition = transform.position;
        currentState = State.Idle;
        previousState = State.Idle;
        ResetAnimatorParameters();

        if (attacks == null || attacks.Length == 0)
        {
            attacks = new WizardAttackData[2]
            {
                new WizardAttackData{ triggerName="Attack1", damage=20, cooldown=2.5f, activeTime=0.5f },
                new WizardAttackData{ triggerName="Attack2", damage=25, cooldown=3.0f, activeTime=0.5f }
            };
        }
    }

    private void ValidateAttackData()
    {
        if (attacks == null || attacks.Length == 0)
            Debug.LogWarning("Wizard: No attacks configured");
        else if (debugMode)
        {
            for (int i = 0; i < attacks.Length; i++)
                Debug.Log($"Wizard Attack[{i}] {attacks[i].triggerName} Range={attacks[i].AttackRange} Dmg={attacks[i].damage} CD={attacks[i].cooldown}");
        }
    }
    #endregion

    #region State Machine
    private void ProcessStateMachine()
    {
        if (isDead) return;

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
            LosePlayer();
            TransitionToState(State.Returning);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > detectionRange * 1.5f)
        {
            LosePlayer();
            TransitionToState(State.Returning);
            return;
        }

        if (IsPlayerInAnyAttackRange() && !isAttacking)
        {
            TransitionToState(State.Attack);
            return;
        }

        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * chaseSpeed, rb.linearVelocity.y);
        UpdateFacingDirection(dir.x);
        SetAnimatorBool("isRunning", true);
    }

    private void HandleAttack()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        SetAnimatorBool("isRunning", false);

        if (player == null || !IsPlayerAliveAndValid())
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            LosePlayer();
            TransitionToState(State.Returning);
            return;
        }

        FacePlayer();

        if (!IsPlayerInAnyAttackRange())
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            TransitionToState(State.Chase);
            return;
        }

        float cooldown = currentAttackData != null ? currentAttackData.cooldown : 2.5f;
        if (!isAttacking && Time.time > lastAttackTime + cooldown)
            PerformAttack();
    }

    private void HandleReturning()
    {
        float dist = Vector2.Distance(transform.position, originalPosition);
        if (dist <= returnStopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            SetAnimatorBool("isRunning", false);
            transform.position = new Vector3(originalPosition.x, transform.position.y, transform.position.z);
            TransitionToState(State.Idle);
            return;
        }

        Vector2 dir = (originalPosition - transform.position).normalized;
        rb.linearVelocity = new Vector2(dir.x * returnSpeed, rb.linearVelocity.y);
        UpdateFacingDirection(dir.x);
        SetAnimatorBool("isRunning", true);
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

    #region Combat
    private void PerformAttack()
    {
        if (!IsPlayerAliveAndValid())
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            LosePlayer();
            TransitionToState(State.Returning);
            return;
        }
        if (!IsPlayerInAnyAttackRange())
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            TransitionToState(State.Chase);
            return;
        }

        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        currentAttackData = SelectBestAttackForDistance();
        if (HasAnimatorParameter(currentAttackData.triggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(currentAttackData.triggerName);
            lastAttackTime = Time.time;
            
            // Play appropriate attack sound based on attack type
            if (audioController != null)
            {
                if (currentAttackData.triggerName == "Attack1")
                {
                    audioController.PlayAttack1Sound();
                }
                else if (currentAttackData.triggerName == "Attack2")
                {
                    audioController.PlayAttack2Sound();
                }
            }
            
            ScheduleDelayedAttackCollider(currentAttackData);
            Invoke(nameof(ResetAttackState), 1.0f);
        }
        else
        {
            Debug.LogError($"Wizard: Missing trigger {currentAttackData.triggerName}");
            isAttacking = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private WizardAttackData SelectBestAttackForDistance()
    {
        if (player == null || attacks == null || attacks.Length == 0) return attacks[0];
        float dist = Vector2.Distance(transform.position, player.position);

        WizardAttackData best = null;
        float smallestDiff = float.MaxValue;
        foreach (var atk in attacks)
        {
            if (dist <= atk.AttackRange)
            {
                float diff = atk.AttackRange - dist;
                if (diff < smallestDiff)
                {
                    smallestDiff = diff;
                    best = atk;
                }
            }
        }
        if (best == null)
        {
            // fallback: largest range
            float longest = 0f;
            foreach (var atk in attacks)
                if (atk.AttackRange > longest) { longest = atk.AttackRange; best = atk; }
        }
        return best ?? attacks[0];
    }

    private bool IsPlayerInAnyAttackRange()
    {
        if (player == null) return false;
        float dist = Vector2.Distance(transform.position, player.position);
        foreach (var atk in attacks)
            if (dist <= atk.AttackRange) return true;
        return false;
    }

    private void ScheduleDelayedAttackCollider(WizardAttackData data)
    {
        if (isDead || !IsPlayerAliveAndValid()) return;
        isAttackColliderPending = true;
        Invoke(nameof(ActivateDelayedAttackCollider), data.AttackDelay);
        Invoke(nameof(DisableWizardAttackCollider), data.AttackDelay + data.activeTime);
    }

    private void ActivateDelayedAttackCollider()
    {
        if (!isAttackColliderPending || isDead || !IsPlayerAliveAndValid())
        {
            isAttackColliderPending = false;
            return;
        }
        SetupWizardAttackCollider(currentAttackData);
        isAttackColliderPending = false;
    }

    private void SetupWizardAttackCollider(WizardAttackData data)
    {
        if (attackColliderTransform == null || wizardAttackCollider == null || attackCollider == null) return;

        attackColliderTransform.gameObject.SetActive(true);
        wizardAttackCollider.SetDamage(data.damage);

        Vector3 pos = transform.position + new Vector3(
            facingDirection * attackColliderOffset.x,
            attackColliderOffset.y,
            0);
        attackColliderTransform.position = pos;
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        if (!isDead) rb.bodyType = RigidbodyType2D.Dynamic;
        DisableWizardAttackCollider();
    }

    private void DisableWizardAttackCollider()
    {
        if (attackColliderTransform != null)
            attackColliderTransform.gameObject.SetActive(false);

        CancelInvoke(nameof(ActivateDelayedAttackCollider));
        isAttackColliderPending = false;
    }
    #endregion

    #region Damage / Death
    public void TakeDamage(int damage)
    {
        if (isDead || isHurt) return;

        isHurt = true;
        hurtRecoveryTimer = hurtDuration;
        rb.linearVelocity = Vector2.zero;

        if (currentState != State.Attack)
        {
            previousState = currentState;
            TransitionToState(State.Hurt);
        }

        if (HasAnimatorParameter("Hurt", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("Hurt");

        // Play hurt sound
        if (audioController != null)
        {
            audioController.PlayHurtSound();
        }

        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
            if (!health.IsAlive())
            {
                OnDeath();
                return;
            }
        }
        if (debugMode) Debug.Log($"Wizard took {damage} damage");
    }

    public void OnDeath()
    {
        if (isDead || deathProcessed) return;

        isDead = true;
        deathProcessed = true;
        deathSequenceStarted = true;
        currentState = State.Dead;
        previousState = State.Dead;

        CancelInvoke();
        StopAllCoroutines();

        PerformImmediateDeathCleanup();
        TriggerDeathAnimationSafe();

        // Play death sound
        if (audioController != null)
        {
            audioController.PlayDeathSound();
        }

        float wait = Mathf.Max(0.05f, deathAnimationDuration);
        StartCoroutine(FinalDeathRemoval(wait));
    }

    private void TriggerDeathAnimationSafe()
    {
        if (animator != null && HasAnimatorParameter("Die", AnimatorControllerParameterType.Trigger))
        {
            if (HasAnimatorParameter("Attack1", AnimatorControllerParameterType.Trigger)) animator.ResetTrigger("Attack1");
            if (HasAnimatorParameter("Attack2", AnimatorControllerParameterType.Trigger)) animator.ResetTrigger("Attack2");
            if (HasAnimatorParameter("Hurt", AnimatorControllerParameterType.Trigger)) animator.ResetTrigger("Hurt");

            SetAnimatorBool("isRunning", false);
            animator.SetTrigger("Die");
        }
    }

    private IEnumerator FinalDeathRemoval(float wait)
    {
        yield return new WaitForSeconds(wait);
        if (!isDead) yield break;
        PermanentlyHideWizard();
        if (animator != null) animator.enabled = false;
        this.enabled = false;
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

        // Stop all audio
        if (audioController != null)
        {
            audioController.StopAllSounds();
        }

        player = null;
        isAttacking = false;
        isHurt = false;
    }

    private void PermanentlyHideWizard()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (animatorChild != null) animatorChild.gameObject.SetActive(false);

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in allRenderers) r.enabled = false;

        transform.position = new Vector3(-10000f, -10000f, 0f);
        transform.localScale = Vector3.zero;

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        StartCoroutine(FinalDeactivateRoot());
    }

    private IEnumerator FinalDeactivateRoot()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        gameObject.SetActive(false);
    }
    #endregion

    #region Helper / Recovery / Detection
    private void HandleHurtRecovery()
    {
        if (!isHurt) return;
        hurtRecoveryTimer -= Time.deltaTime;
        if (hurtRecoveryTimer <= 0f)
        {
            isHurt = false;
            if (currentState == State.Hurt)
                ReturnToPreviousBehavior();
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
            LosePlayer();
            if (currentState == State.Chase || currentState == State.Attack)
                TransitionToState(State.Returning);
        }
    }

    private void FacePlayer()
    {
        if (player == null) return;
        Vector2 dir = (player.position - transform.position).normalized;
        UpdateFacingDirection(dir.x);
    }

    private bool IsPlayerAliveAndValid()
    {
        if (player == null) return false;
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        return ph != null && ph.IsAlive();
    }

    private void LosePlayer()
    {
        if (player != null && debugMode) Debug.Log("Wizard lost player reference");
        player = null;
    }

    private void UpdateFacingDirection(float moveDir)
    {
        if (Mathf.Abs(moveDir) <= 0.1f) return;
        facingDirection = moveDir > 0 ? 1f : -1f;
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingDirection < 0;
    }
    #endregion

    #region Animation Helpers
    private bool HasAnimatorParameter(string name, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }

    private void SetAnimatorBool(string param, bool value)
    {
        if (animator == null) return;
        if (HasAnimatorParameter(param, AnimatorControllerParameterType.Bool))
            animator.SetBool(param, value);
    }

    private void ResetAnimatorParameters()
    {
        if (animator == null) return;

        if (HasAnimatorParameter("isRunning", AnimatorControllerParameterType.Bool))
            animator.SetBool("isRunning", false);

        string[] triggers = { "Attack1", "Attack2", "Hurt", "Die" };
        foreach (var t in triggers)
            if (HasAnimatorParameter(t, AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger(t);
    }
    #endregion

    #region Colliders / Detection
    private bool IsDetectionColliderTrigger(Collider2D other)
    {
        if (detectionCollider != null && other.bounds.Intersects(detectionCollider.bounds))
        {
            if (attackCollider != null && attackColliderTransform != null && attackColliderTransform.gameObject.activeSelf)
            {
                if (other.bounds.Intersects(attackCollider.bounds) && isAttacking)
                    return false;
            }
            return true;
        }
        return false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
        if (!other.CompareTag("Player") || detectionCollider == null) return;

        if (IsDetectionColliderTrigger(other))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null && !ph.IsAlive()) return;

            isPlayerDetected = true;

            if (player == null && currentState != State.Attack && currentState != State.Hurt)
            {
                player = other.transform;
                TransitionToState(State.Chase);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isDead) return;
        if (other.CompareTag("Player") && debugMode)
            Debug.Log("Wizard: Player exited detection");
    }
    #endregion

    #region State Transitions
    private void TransitionToState(State newState)
    {
        if (isDead && newState != State.Dead)
        {
            if (debugMode) Debug.Log($"Wizard: Ignored transition {currentState}->{newState} (dead)");
            return;
        }

        if (currentState != State.Hurt && currentState != State.Dead)
            previousState = currentState;

        State old = currentState;
        currentState = newState;

        if (old == State.Attack && newState != State.Attack && !isDead)
            rb.bodyType = RigidbodyType2D.Dynamic;

        if (debugMode) Debug.Log($"Wizard State: {old} -> {newState}");
    }
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (attacks != null)
        {
            for (int i = 0; i < attacks.Length; i++)
            {
                Gizmos.color = i == 0 ? Color.red : new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(transform.position, attacks[i].AttackRange);
            }
        }

        Vector3 origPos = Application.isPlaying ? originalPosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origPos, 0.4f);

        if (attackCollider != null)
        {
            Vector3 atkPos = transform.position + new Vector3(
                facingDirection * attackColliderOffset.x,
                attackColliderOffset.y,
                0);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(atkPos, attackCollider.radius);
        }

        if (player != null && Application.isPlaying)
        {
            Gizmos.color = currentState == State.Chase ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f,
                $"Wizard\nState:{currentState}\nDead:{isDead}\nAttacking:{isAttacking}",
                new GUIStyle { normal = new GUIStyleState { textColor = Color.yellow } });
        }
#endif
    }
    #endregion

    #region Public API
    public bool IsBodyCollider(Collider2D c) => bodyCollider != null && c == bodyCollider;
    public bool IsDetectionCollider(Collider2D c) => detectionCollider != null && c == detectionCollider;
    public CapsuleCollider2D GetBodyCollider() => bodyCollider;
    #endregion
}