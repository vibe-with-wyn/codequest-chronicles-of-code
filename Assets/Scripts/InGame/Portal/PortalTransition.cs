using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class PortalTransition : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string targetSceneName = "Journey Pause";

    [Header("Fade Settings")]
    [SerializeField] private float fadeOutDuration = 2.5f;
    [SerializeField] private float initialDelay = 0.5f;
    [SerializeField] private float postFadeDelay = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource portalAudioSource;
    [SerializeField] private AudioClip portalActivationSound;

    [Header("Player Control")]
    [SerializeField] private bool disablePlayerMovement = true;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private bool isTransitioning = false;
    private bool hasTriggered = false;

    private Transform playerTransform;
    private SpriteRenderer[] playerSpriteRenderers;
    private Color[] originalPlayerColors;
    private MonoBehaviour playerMovementScript;
    private Rigidbody2D playerRb;

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"PortalTransition on {gameObject.name}: Collider was not set as trigger. Fixed automatically.");
        }

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
        if (hasTriggered || isTransitioning || !other.CompareTag("Player"))
            return;

        if (debugMode)
            Debug.Log($"[Portal] Player entered portal zone → starting transition to '{targetSceneName}'");

        hasTriggered = true;
        playerTransform = other.transform;
        CachePlayerComponents();
        StartCoroutine(PortalTransitionSequence());
    }

    private void CachePlayerComponents()
    {
        if (playerTransform == null)
        {
            Debug.LogError("[Portal] Player transform is null!");
            return;
        }

        playerSpriteRenderers = playerTransform.GetComponentsInChildren<SpriteRenderer>(true);
        if (playerSpriteRenderers != null && playerSpriteRenderers.Length > 0)
        {
            originalPlayerColors = new Color[playerSpriteRenderers.Length];
            for (int i = 0; i < playerSpriteRenderers.Length; i++)
            {
                originalPlayerColors[i] = playerSpriteRenderers[i].color;
            }

            if (debugMode)
                Debug.Log($"[Portal] Found {playerSpriteRenderers.Length} SpriteRenderer(s) under player for fading");
        }
        else
        {
            if (debugMode)
                Debug.LogWarning("[Portal] No SpriteRenderer found under player. Fade will be skipped.");
        }

        if (disablePlayerMovement)
        {
            MonoBehaviour[] scripts = playerTransform.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script.GetType().Name == "PlayerMovement")
                {
                    playerMovementScript = script;
                    if (debugMode)
                        Debug.Log("[Portal] Found PlayerMovement script");
                    break;
                }
            }

            playerRb = playerTransform.GetComponent<Rigidbody2D>();

            if (playerMovementScript == null && debugMode)
                Debug.LogWarning("[Portal] PlayerMovement script not found - will only stop velocity");
        }
    }

    private IEnumerator PortalTransitionSequence()
    {
        isTransitioning = true;

        if (debugMode)
            Debug.Log($"[Portal] PHASE 1: Waiting {initialDelay}s before fade-out...");
        PlayPortalSound();
        yield return new WaitForSeconds(initialDelay);

        if (debugMode)
            Debug.Log("[Portal] PHASE 2: Disabling player controls...");
        DisablePlayerControls();

        if (debugMode)
            Debug.Log($"[Portal] PHASE 3: Fading out player over {fadeOutDuration}s...");
        yield return StartCoroutine(FadeOutPlayer());

        if (debugMode)
            Debug.Log($"[Portal] PHASE 4: Waiting {postFadeDelay}s before scene load...");
        yield return new WaitForSeconds(postFadeDelay);

        if (debugMode)
            Debug.Log($"[Portal] PHASE 5: Loading scene '{targetSceneName}' with loading screen reuse...");
        LoadTargetScene();
    }

    private void PlayPortalSound()
    {
        if (portalAudioSource != null && portalActivationSound != null)
        {
            portalAudioSource.PlayOneShot(portalActivationSound);
            if (debugMode)
                Debug.Log("[Portal] ✓ Portal sound played");
        }
    }

    private void DisablePlayerControls()
    {
        if (!disablePlayerMovement) return;

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
            if (debugMode)
                Debug.Log("[Portal] ✓ Player movement disabled");
        }

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            if (debugMode)
                Debug.Log("[Portal] ✓ Player velocity zeroed");
        }
    }

    private IEnumerator FadeOutPlayer()
    {
        if (playerSpriteRenderers == null || playerSpriteRenderers.Length == 0)
        {
            yield return new WaitForSeconds(fadeOutDuration);
            if (debugMode)
                Debug.Log("[Portal] Skipped fade (no SpriteRenderer found).");
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float alpha = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < playerSpriteRenderers.Length; i++)
            {
                if (playerSpriteRenderers[i] == null) continue;
                Color c = originalPlayerColors[i];
                c.a = alpha;
                playerSpriteRenderers[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < playerSpriteRenderers.Length; i++)
        {
            if (playerSpriteRenderers[i] == null) continue;
            Color c = originalPlayerColors[i];
            c.a = 0f;
            playerSpriteRenderers[i].color = c;
        }

        if (debugMode)
            Debug.Log("[Portal] ✓ Player fully faded out");
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[Portal] Target scene name is empty!");
            return;
        }

        if (LoadingScreenController.Instance != null)
        {
            if (debugMode)
                Debug.Log($"[Portal] Using existing LoadingScreenController to load '{targetSceneName}'");
            LoadingScreenController.Instance.LoadScene(targetSceneName);
        }
        else
        {
            if (debugMode)
                Debug.Log($"[Portal] Loading screen not present. Redirecting through 'Loading Screen' scene for '{targetSceneName}'");
            LoadingScreenController.TargetSceneName = targetSceneName;
            SceneManager.LoadScene("Loading Screen");
        }
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = hasTriggered ? Color.gray : new Color(0f, 1f, 1f, 0.5f);
            if (col is BoxCollider2D b) Gizmos.DrawWireCube(transform.position + (Vector3)b.offset, b.size);
            else if (col is CircleCollider2D c) Gizmos.DrawWireSphere(transform.position + (Vector3)c.offset, c.radius);
        }

#if UNITY_EDITOR
        string status = hasTriggered ? "TRIGGERED" : (isTransitioning ? "TRANSITIONING..." : "READY");
        float total = initialDelay + fadeOutDuration + postFadeDelay;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"PORTAL TO: {targetSceneName}\nStatus: {status}\n\nSequence:\n1. Delay {initialDelay}s\n2. Disable controls\n3. Fade {fadeOutDuration}s\n4. Delay {postFadeDelay}s\n5. Load\nTotal: {total}s",
            new GUIStyle { normal = new GUIStyleState { textColor = Color.cyan } });
#endif
    }

    public void ManualTriggerPortal()
    {
        if (hasTriggered || isTransitioning)
        {
            Debug.LogWarning("[Portal] Already triggered or in progress");
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
            Debug.LogError("[Portal] Player not found for manual trigger");
        }
    }
}