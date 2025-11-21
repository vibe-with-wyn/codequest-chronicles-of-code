using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles portal detection and player fade-out transition to "Journey's Pause" scene
/// Player slowly vanishes when entering the portal zone, then scene transitions
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PortalTransition : MonoBehaviour
{
    [Header("Target Scene")]
    [Tooltip("Scene to load after fade-out")]
    [SerializeField] private string targetSceneName = "Journey Pause";

    [Header("Fade Settings")]
    [Tooltip("Duration of player fade-out effect (seconds)")]
    [SerializeField] private float fadeOutDuration = 2.5f;
    
    [Tooltip("Delay before starting fade after player enters portal")]
    [SerializeField] private float initialDelay = 0.5f;
    
    [Tooltip("Delay after fade completes before loading scene")]
    [SerializeField] private float postFadeDelay = 0.5f;

    [Header("Portal Animation")]
    [Tooltip("Animator for portal visual effects (optional)")]
    [SerializeField] private Animator portalAnimator;
    
    [Tooltip("Trigger name for portal activation animation (optional)")]
    [SerializeField] private string activationTriggerName = "Activate";

    [Header("Audio")]
    [Tooltip("Audio source for portal sound effects (optional)")]
    [SerializeField] private AudioSource portalAudioSource;
    
    [Tooltip("Sound to play when portal activates (optional)")]
    [SerializeField] private AudioClip portalActivationSound;

    [Header("Player Control")]
    [Tooltip("Disable player movement during transition?")]
    [SerializeField] private bool disablePlayerMovement = true;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // State Management
    private bool isTransitioning = false;
    private bool hasTriggered = false;
    
    // Player Components
    private Transform playerTransform;
    private SpriteRenderer playerSpriteRenderer;
    private Color originalPlayerColor;
    private MonoBehaviour playerMovementScript; // CHANGED: Use MonoBehaviour instead of PlayerMovement
    private Rigidbody2D playerRb;

    void Awake()
    {
        // Ensure collider is set to trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"PortalTransition on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }

        // Auto-find portal animator if not assigned
        if (portalAnimator == null)
        {
            portalAnimator = GetComponent<Animator>();
            if (portalAnimator == null)
            {
                portalAnimator = GetComponentInChildren<Animator>();
            }
            
            if (portalAnimator != null && debugMode)
            {
                Debug.Log($"[Portal] Auto-found Animator component: {portalAnimator.gameObject.name}");
            }
        }

        // Auto-find audio source if not assigned
        if (portalAudioSource == null)
        {
            portalAudioSource = GetComponent<AudioSource>();
            if (portalAudioSource != null && debugMode)
            {
                Debug.Log("[Portal] Auto-found AudioSource component");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only trigger once and only for player
        if (hasTriggered || isTransitioning || !other.CompareTag("Player"))
            return;

        if (debugMode)
        {
            Debug.Log($"[Portal] Player entered portal zone! Starting transition to '{targetSceneName}'");
        }

        hasTriggered = true;
        playerTransform = other.transform;

        // Cache player components
        CachePlayerComponents();

        // Start transition sequence
        StartCoroutine(PortalTransitionSequence());
    }

    private void CachePlayerComponents()
    {
        if (playerTransform == null)
        {
            Debug.LogError("[Portal] Player transform is null!");
            return;
        }

        // Get player sprite renderer (check player and children)
        playerSpriteRenderer = playerTransform.GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer == null)
        {
            playerSpriteRenderer = playerTransform.GetComponentInChildren<SpriteRenderer>();
        }

        if (playerSpriteRenderer != null)
        {
            originalPlayerColor = playerSpriteRenderer.color;
            if (debugMode)
            {
                Debug.Log($"[Portal] Found player SpriteRenderer: {playerSpriteRenderer.gameObject.name}");
            }
        }
        else
        {
            Debug.LogError("[Portal] Could not find player SpriteRenderer!");
        }

        // Get player movement component (to disable controls)
        // FIXED: Use GetComponent with string name to avoid type reference issues
        if (disablePlayerMovement)
        {
            // Try to find PlayerMovement script by name
            MonoBehaviour[] scripts = playerTransform.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script.GetType().Name == "PlayerMovement")
                {
                    playerMovementScript = script;
                    if (debugMode)
                    {
                        Debug.Log("[Portal] Found PlayerMovement script");
                    }
                    break;
                }
            }

            playerRb = playerTransform.GetComponent<Rigidbody2D>();
            
            if (playerMovementScript == null && debugMode)
            {
                Debug.LogWarning("[Portal] PlayerMovement script not found - will only stop velocity");
            }
        }
    }

    private IEnumerator PortalTransitionSequence()
    {
        isTransitioning = true;

        // ============= PHASE 1: INITIAL DELAY & PORTAL ACTIVATION =============
        if (debugMode)
        {
            Debug.Log($"[Portal] PHASE 1: Portal activated! Waiting {initialDelay}s before fade-out...");
        }

        // Trigger portal animation (if available)
        TriggerPortalAnimation();

        // Play portal sound (if available)
        PlayPortalSound();

        yield return new WaitForSeconds(initialDelay);

        // ============= PHASE 2: DISABLE PLAYER CONTROLS =============
        if (debugMode)
        {
            Debug.Log("[Portal] PHASE 2: Disabling player controls...");
        }

        DisablePlayerControls();

        // ============= PHASE 3: FADE OUT PLAYER =============
        if (debugMode)
        {
            Debug.Log($"[Portal] PHASE 3: Fading out player over {fadeOutDuration}s...");
        }

        yield return StartCoroutine(FadeOutPlayer());

        // ============= PHASE 4: POST-FADE DELAY =============
        if (debugMode)
        {
            Debug.Log($"[Portal] PHASE 4: Player fully vanished! Waiting {postFadeDelay}s before scene transition...");
        }

        yield return new WaitForSeconds(postFadeDelay);

        // ============= PHASE 5: LOAD TARGET SCENE =============
        if (debugMode)
        {
            Debug.Log($"[Portal] PHASE 5: Loading scene '{targetSceneName}'...");
        }

        LoadTargetScene();
    }

    private void TriggerPortalAnimation()
    {
        if (portalAnimator == null) return;

        if (portalAnimator.runtimeAnimatorController == null)
        {
            if (debugMode)
            {
                Debug.LogWarning("[Portal] Animator has no controller - skipping animation");
            }
            return;
        }

        // Check if trigger exists
        bool hasTrigger = false;
        foreach (var param in portalAnimator.parameters)
        {
            if (param.name == activationTriggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasTrigger = true;
                break;
            }
        }

        if (hasTrigger)
        {
            portalAnimator.SetTrigger(activationTriggerName);
            if (debugMode)
            {
                Debug.Log($"[Portal] ✓ Portal animation triggered: '{activationTriggerName}'");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning($"[Portal] Trigger parameter '{activationTriggerName}' not found in Animator");
        }
    }

    private void PlayPortalSound()
    {
        if (portalAudioSource != null && portalActivationSound != null)
        {
            portalAudioSource.PlayOneShot(portalActivationSound);
            if (debugMode)
            {
                Debug.Log("[Portal] ✓ Portal sound played");
            }
        }
    }

    private void DisablePlayerControls()
    {
        if (!disablePlayerMovement) return;

        // Disable player movement script (using generic MonoBehaviour reference)
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
            if (debugMode)
            {
                Debug.Log("[Portal] ✓ Player movement script disabled");
            }
        }

        // Stop player velocity
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            if (debugMode)
            {
                Debug.Log("[Portal] ✓ Player velocity stopped");
            }
        }
    }

    private IEnumerator FadeOutPlayer()
    {
        if (playerSpriteRenderer == null)
        {
            Debug.LogError("[Portal] Cannot fade player - SpriteRenderer is null!");
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);

            // Apply fading alpha to player sprite
            Color color = originalPlayerColor;
            color.a = alpha;
            playerSpriteRenderer.color = color;

            yield return null;
        }

        // Ensure final alpha is 0
        Color finalColor = originalPlayerColor;
        finalColor.a = 0f;
        playerSpriteRenderer.color = finalColor;

        if (debugMode)
        {
            Debug.Log("[Portal] ✓ Player fade-out completed!");
        }
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[Portal] Target scene name is empty! Cannot load scene.");
            return;
        }

        // Check if scene exists in build settings
        int sceneIndex = SceneManager.GetSceneByName(targetSceneName).buildIndex;
        if (sceneIndex < 0)
        {
            Debug.LogWarning($"[Portal] Scene '{targetSceneName}' not found in build settings. Attempting direct load...");
        }

        // Load scene using LoadingScreenController if available
        if (LoadingScreenController.Instance != null)
        {
            if (debugMode)
            {
                Debug.Log($"[Portal] Loading scene via LoadingScreenController: '{targetSceneName}'");
            }
            LoadingScreenController.Instance.LoadScene(targetSceneName);
        }
        else
        {
            // Fallback: Direct scene load
            if (debugMode)
            {
                Debug.Log($"[Portal] Loading scene directly: '{targetSceneName}'");
            }
            SceneManager.LoadScene(targetSceneName);
        }
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = hasTriggered ? Color.gray : new Color(0f, 1f, 1f, 0.5f); // Cyan when active

            if (col is BoxCollider2D boxCol)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCol.offset, boxCol.size);
            }
            else if (col is CircleCollider2D circleCol)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCol.offset, circleCol.radius);
            }
        }

#if UNITY_EDITOR
        string statusText = hasTriggered ? "TRIGGERED" : (isTransitioning ? "TRANSITIONING..." : "READY");
        float totalTime = initialDelay + fadeOutDuration + postFadeDelay;
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"PORTAL TO: {targetSceneName}\nStatus: [{statusText}]\n\nSEQUENCE:\n1. Player enters → Delay {initialDelay}s\n2. Disable controls\n3. Fade out {fadeOutDuration}s\n4. Delay {postFadeDelay}s\n5. Load scene\n\nTotal Time: {totalTime}s",
            new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan } });
#endif
    }

    // PUBLIC API for manual triggering (if needed)
    public void ManualTriggerPortal()
    {
        if (hasTriggered || isTransitioning)
        {
            Debug.LogWarning("[Portal] Portal already triggered or transitioning!");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            hasTriggered = true;
            playerTransform = player.transform;
            CachePlayerComponents();
            StartCoroutine(PortalTransitionSequence());
        }
        else
        {
            Debug.LogError("[Portal] Player not found for manual trigger!");
        }
    }
}