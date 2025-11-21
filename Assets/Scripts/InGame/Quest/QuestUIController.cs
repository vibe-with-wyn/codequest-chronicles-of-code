using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QuestUIController : MonoBehaviour
{
    [Header("Quest Display UI")]
    [SerializeField] private GameObject questDisplayPanel;
    [SerializeField] private Image questScrollImage;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button closeButton;
    
    [Header("Individual Objective Text Fields")]
    [SerializeField] private TextMeshProUGUI objective1Text;
    [SerializeField] private TextMeshProUGUI objective2Text;
    [SerializeField] private TextMeshProUGUI objective3Text;
    [SerializeField] private TextMeshProUGUI[] additionalObjectiveTexts;
    
    [Header("Objective Styling")]
    [SerializeField] private Color completedObjectiveColor = Color.green;
    [SerializeField] private Color activeObjectiveColor = Color.white;
    [SerializeField] private Color inactiveObjectiveColor = Color.gray;
    [SerializeField] private Sprite completedIcon;
    [SerializeField] private Sprite activeIcon;
    [SerializeField] private Sprite inactiveIcon;
    
    [Header("Objective Status Icons")]
    [SerializeField] private Image objective1Icon;
    [SerializeField] private Image objective2Icon;
    [SerializeField] private Image objective3Icon;
    [SerializeField] private Image[] additionalObjectiveIcons;
    
    [Header("Progress Display")]
    [SerializeField] private Slider questProgressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    
    [Header("Decoration Settings")]
    [SerializeField] private GameObject[] decorationObjects;
    [SerializeField] private SpriteRenderer[] decorationSpriteRenderers;
    [SerializeField] private Image[] decorationImages;
    
    [Header("Display Settings")]
    [SerializeField] private float autoDisplayDuration = 5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    [Header("Quest Scroll Sprite")]
    [SerializeField] private Sprite defaultScrollSprite;
    
    private CanvasGroup questPanelCanvasGroup;
    private bool isDisplaying = false;
    private Coroutine autoHideCoroutine;
    private List<GameObject> activeObjectiveItems = new List<GameObject>();
    
    // Store original decoration colors for proper restoration
    private Color[] originalDecorationSpriteColors;
    private Color[] originalDecorationImageColors;

    // UPDATED: TimeScale pause support - now only pauses for MANUAL display
    private float originalTimeScale = 1f;
    private bool hasPausedGame = false;
    
    // NEW: Static reference to allow UIController to check display state
    public static QuestUIController Instance { get; private set; }
    
    void Awake()
    {
        // Set singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[QuestUIController] Multiple instances detected - destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeQuestUI();
        SubscribeToQuestEvents();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromQuestEvents();

        // Clear singleton reference
        if (Instance == this)
        {
            Instance = null;
        }

        // Ensure we restore timeScale if this object is destroyed while paused
        if (hasPausedGame)
        {
            Time.timeScale = originalTimeScale;
            hasPausedGame = false;
            Debug.Log("[QuestUIController] TimeScale restored in OnDestroy");
        }
    }
    
    // NEW: Public property to check if UI is displaying
    public bool IsDisplaying => isDisplaying;
    
    private void InitializeQuestUI()
    {
        // Get or add CanvasGroup for fading
        if (questDisplayPanel != null)
        {
            questPanelCanvasGroup = questDisplayPanel.GetComponent<CanvasGroup>();
            if (questPanelCanvasGroup == null)
            {
                questPanelCanvasGroup = questDisplayPanel.AddComponent<CanvasGroup>();
            }
            
            // Initially hide the panel
            questDisplayPanel.SetActive(false);
        }
        
        // Initialize decoration handling
        InitializeDecorations();
        
        // Setup button listeners
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
        
        // Set default scroll sprite if available
        if (questScrollImage != null && defaultScrollSprite != null)
        {
            questScrollImage.sprite = defaultScrollSprite;
        }
        
        // Initialize objective text fields
        InitializeObjectiveTextFields();
        
        Debug.Log("QuestUIController initialized with conditional pause (manual display only)");
    }
    
    private void InitializeObjectiveTextFields()
    {
        // Clear all objective text fields initially
        ClearAllObjectiveTexts();
        
        Debug.Log($"Objective text fields available: Obj1: {objective1Text != null}, Obj2: {objective2Text != null}, Obj3: {objective3Text != null}");
    }
    
    private void ClearAllObjectiveTexts()
    {
        if (objective1Text != null) objective1Text.text = "";
        if (objective2Text != null) objective2Text.text = "";
        if (objective3Text != null) objective3Text.text = "";
        
        if (additionalObjectiveTexts != null)
        {
            foreach (var objText in additionalObjectiveTexts)
            {
                if (objText != null) objText.text = "";
            }
        }
        
        // Clear icons as well
        if (objective1Icon != null) objective1Icon.sprite = null;
        if (objective2Icon != null) objective2Icon.sprite = null;
        if (objective3Icon != null) objective3Icon.sprite = null;
        
        if (additionalObjectiveIcons != null)
        {
            foreach (var icon in additionalObjectiveIcons)
            {
                if (icon != null) icon.sprite = null;
            }
        }
    }
    
    private void InitializeDecorations()
    {
        // Auto-find decoration objects if not assigned
        if ((decorationObjects == null || decorationObjects.Length == 0) && 
            (decorationSpriteRenderers == null || decorationSpriteRenderers.Length == 0) &&
            (decorationImages == null || decorationImages.Length == 0))
        {
            AutoFindDecorations();
        }
        
        // Store original colors for SpriteRenderers
        if (decorationSpriteRenderers != null && decorationSpriteRenderers.Length > 0)
        {
            originalDecorationSpriteColors = new Color[decorationSpriteRenderers.Length];
            for (int i = 0; i < decorationSpriteRenderers.Length; i++)
            {
                if (decorationSpriteRenderers[i] != null)
                {
                    originalDecorationSpriteColors[i] = decorationSpriteRenderers[i].color;
                }
            }
        }
        
        // Store original colors for Images
        if (decorationImages != null && decorationImages.Length > 0)
        {
            originalDecorationImageColors = new Color[decorationImages.Length];
            for (int i = 0; i < decorationImages.Length; i++)
            {
                if (decorationImages[i] != null)
                {
                    originalDecorationImageColors[i] = decorationImages[i].color;
                }
            }
        }
        
        // Initially hide all decorations
        SetDecorationsVisibility(false);
        
        Debug.Log($"Initialized decorations: {decorationObjects?.Length ?? 0} objects, {decorationSpriteRenderers?.Length ?? 0} sprites, {decorationImages?.Length ?? 0} images");
    }
    
    private void AutoFindDecorations()
    {
        if (questDisplayPanel == null) return;
        
        // Find all SpriteRenderers in children (including inactive)
        SpriteRenderer[] foundSprites = questDisplayPanel.GetComponentsInChildren<SpriteRenderer>(true);
        if (foundSprites.Length > 0)
        {
            decorationSpriteRenderers = foundSprites;
            Debug.Log($"Auto-found {foundSprites.Length} SpriteRenderer decorations");
        }
        
        // Find all Images in children that are not the main quest UI elements
        Image[] allImages = questDisplayPanel.GetComponentsInChildren<Image>(true);
        System.Collections.Generic.List<Image> decorationImagesList = new System.Collections.Generic.List<Image>();
        
        foreach (Image img in allImages)
        {
            // Skip the main quest scroll image and objective icons
            if (img != questScrollImage && !IsObjectiveIcon(img))
            {
                decorationImagesList.Add(img);
            }
        }
        
        if (decorationImagesList.Count > 0)
        {
            decorationImages = decorationImagesList.ToArray();
            Debug.Log($"Auto-found {decorationImages.Length} Image decorations");
        }
    }
    
    private bool IsObjectiveIcon(Image img)
    {
        if (objective1Icon == img || objective2Icon == img || objective3Icon == img)
            return true;
        
        if (additionalObjectiveIcons != null)
        {
            foreach (var icon in additionalObjectiveIcons)
            {
                if (icon == img) return true;
            }
        }
        
        return false;
    }
    
    private void SetDecorationsVisibility(bool visible)
    {
        // Handle decoration GameObjects
        if (decorationObjects != null)
        {
            foreach (GameObject decoration in decorationObjects)
            {
                if (decoration != null)
                {
                    decoration.SetActive(visible);
                }
            }
        }
        
        // Handle decoration SpriteRenderers
        if (decorationSpriteRenderers != null)
        {
            foreach (SpriteRenderer spriteRenderer in decorationSpriteRenderers)
            {
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = visible ? 1f : 0f;
                    spriteRenderer.color = color;
                }
            }
        }
        
        // Handle decoration Images
        if (decorationImages != null)
        {
            foreach (Image image in decorationImages)
            {
                if (image != null)
                {
                    Color color = image.color;
                    color.a = visible ? 1f : 0f;
                    image.color = color;
                }
            }
        }
    }
    
    private void SetDecorationsAlpha(float alpha)
    {
        // Handle decoration SpriteRenderers
        if (decorationSpriteRenderers != null && originalDecorationSpriteColors != null)
        {
            for (int i = 0; i < decorationSpriteRenderers.Length && i < originalDecorationSpriteColors.Length; i++)
            {
                if (decorationSpriteRenderers[i] != null)
                {
                    Color color = originalDecorationSpriteColors[i];
                    color.a = alpha;
                    decorationSpriteRenderers[i].color = color;
                }
            }
        }
        
        // Handle decoration Images
        if (decorationImages != null && originalDecorationImageColors != null)
        {
            for (int i = 0; i < decorationImages.Length && i < originalDecorationImageColors.Length; i++)
            {
                if (decorationImages[i] != null)
                {
                    Color color = originalDecorationImageColors[i];
                    color.a = alpha;
                    decorationImages[i].color = color;
                }
            }
        }
    }
    
    private void SubscribeToQuestEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnNewQuestStarted += OnNewQuestReceived;
            QuestManager.Instance.OnQuestUpdated += OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
            QuestManager.Instance.OnObjectiveCompleted += OnObjectiveCompleted;
        }
    }
    
    private void UnsubscribeFromQuestEvents()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnNewQuestStarted -= OnNewQuestReceived;
            QuestManager.Instance.OnQuestUpdated -= OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            QuestManager.Instance.OnObjectiveCompleted -= OnObjectiveCompleted;
        }
    }
    
    // Called when a new quest starts (automatically shows for a few seconds)
    private void OnNewQuestReceived(QuestData quest)
    {
        Debug.Log($"New quest received: {quest.questTitle}");
        ShowQuestDisplay(quest, true); // true = auto-hide after duration
    }
    
    // Called when quest is updated
    private void OnQuestUpdated(QuestData quest)
    {
        Debug.Log($"Quest updated: {quest.questTitle}");
        if (isDisplaying)
        {
            UpdateQuestDisplay(quest);
        }
    }
    
    // Called when quest is completed
    private void OnQuestCompleted(QuestData quest)
    {
        Debug.Log($"Quest completed: {quest.questTitle}");
    }
    
    // Called when objective is completed
    private void OnObjectiveCompleted(QuestObjective objective, QuestData quest)
    {
        Debug.Log($"Objective completed: {objective.objectiveTitle}");
        
        // Display quest UI for a few seconds when objective is completed
        ShowQuestDisplay(quest, autoHide: true);
    }
    
    // Show quest display (called by quest log button or automatically)
    public void ShowQuestDisplay(QuestData quest, bool autoHide = false)
    {
        if (quest == null || questDisplayPanel == null) return;
        
        StartCoroutine(ShowQuestCoroutine(quest, autoHide));
    }
    
    private IEnumerator ShowQuestCoroutine(QuestData quest, bool autoHide)
    {
        isDisplaying = true;
        
        // NEW: Notify UIController to disable quest button
        NotifyUIController(false);
        
        // Update quest content
        UpdateQuestDisplay(quest);
        
        // Show continue button only for auto-display, close button for manual access
        if (continueButton != null)
            continueButton.gameObject.SetActive(autoHide);
        
        if (closeButton != null)
            closeButton.gameObject.SetActive(!autoHide);
        
        // Show panel and decorations
        questDisplayPanel.SetActive(true);
        
        // Activate decoration GameObjects
        if (decorationObjects != null)
        {
            foreach (GameObject decoration in decorationObjects)
            {
                if (decoration != null)
                {
                    decoration.SetActive(true);
                }
            }
        }
        
        // Fade in both panel and decorations simultaneously
        yield return StartCoroutine(FadeQuestDisplayAndDecorations(0f, 1f, fadeInDuration));
        
        // UPDATED: Only pause for MANUAL display (Quest Log button), NOT auto-display
        if (!autoHide && !hasPausedGame)
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            hasPausedGame = true;
            Debug.Log("[QuestUIController] Game paused (MANUAL Quest Log access only)");
        }
        else if (autoHide)
        {
            Debug.Log("[QuestUIController] Auto-display - game NOT paused (allowing gameplay to continue)");
        }
        
        // Auto-hide after duration if requested (use real-time so it works while timeScale == 0)
        if (autoHide)
        {
            autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
        }
    }
    
    // Update quest display with separate objective texts
    private void UpdateQuestDisplay(QuestData quest)
    {
        if (questTitleText != null)
            questTitleText.text = quest.questTitle;
        
        if (questDescriptionText != null)
            questDescriptionText.text = quest.questDescription;
        
        // Update quest image if provided
        if (questScrollImage != null && quest.questImage != null)
            questScrollImage.sprite = quest.questImage;
        
        // Update progress bar
        if (questProgressBar != null)
        {
            questProgressBar.value = quest.GetOverallProgress();
        }
        
        // Update progress text
        if (progressText != null)
        {
            int completed = quest.objectives.FindAll(o => o.isCompleted).Count;
            int total = quest.objectives.Count;
            progressText.text = $"{completed}/{total} objectives completed";
        }
        
        // Update objectives display
        UpdateObjectiveDisplay(quest);
    }
    
    // Update objectives with customizable colors and icons
    private void UpdateObjectiveDisplay(QuestData quest)
    {
        // Clear all texts first
        ClearAllObjectiveTexts();
        
        Debug.Log($"Updating objective display. Quest has {quest.objectives.Count} objectives");
        
        // Update each objective text field
        for (int i = 0; i < quest.objectives.Count; i++)
        {
            var objective = quest.objectives[i];
            
            // Determine color and icon based on objective state
            Color textColor;
            Sprite iconSprite;
            
            if (objective.isCompleted)
            {
                textColor = completedObjectiveColor;
                iconSprite = completedIcon;
            }
            else if (objective.isActive)
            {
                textColor = activeObjectiveColor;
                iconSprite = activeIcon;
            }
            else
            {
                textColor = inactiveObjectiveColor;
                iconSprite = inactiveIcon;
            }
            
            // Build objective text with progress if applicable
            string objectiveText = objective.objectiveTitle;
            if (objective.targetCount > 1)
            {
                objectiveText += $" ({objective.currentCount}/{objective.targetCount})";
            }
            
            // Assign to appropriate text field and icon
            switch (i)
            {
                case 0:
                    if (objective1Text != null)
                    {
                        objective1Text.text = objectiveText;
                        objective1Text.color = textColor;
                    }
                    if (objective1Icon != null)
                    {
                        objective1Icon.sprite = iconSprite;
                    }
                    break;
                case 1:
                    if (objective2Text != null)
                    {
                        objective2Text.text = objectiveText;
                        objective2Text.color = textColor;
                    }
                    if (objective2Icon != null)
                    {
                        objective2Icon.sprite = iconSprite;
                    }
                    break;
                case 2:
                    if (objective3Text != null)
                    {
                        objective3Text.text = objectiveText;
                        objective3Text.color = textColor;
                    }
                    if (objective3Icon != null)
                    {
                        objective3Icon.sprite = iconSprite;
                    }
                    break;
                default:
                    // Handle additional objectives
                    int additionalIndex = i - 3;
                    if (additionalObjectiveTexts != null && additionalIndex < additionalObjectiveTexts.Length)
                    {
                        if (additionalObjectiveTexts[additionalIndex] != null)
                        {
                            additionalObjectiveTexts[additionalIndex].text = objectiveText;
                            additionalObjectiveTexts[additionalIndex].color = textColor;
                        }
                    }
                    if (additionalObjectiveIcons != null && additionalIndex < additionalObjectiveIcons.Length)
                    {
                        if (additionalObjectiveIcons[additionalIndex] != null)
                        {
                            additionalObjectiveIcons[additionalIndex].sprite = iconSprite;
                        }
                    }
                    break;
            }
        }
    }
    
    private IEnumerator AutoHideAfterDelay()
    {
        // Use real-time so auto-hide occurs while game is paused (Time.timeScale = 0)
        yield return new WaitForSecondsRealtime(autoDisplayDuration);
        
        if (isDisplaying)
        {
            HideQuestDisplay();
        }
    }
    
    public void HideQuestDisplay()
    {
        if (!isDisplaying) return;
        
        StartCoroutine(HideQuestCoroutine());
    }
    
    private IEnumerator HideQuestCoroutine()
    {
        // UPDATED: Only restore timeScale if THIS controller paused it (manual display)
        if (hasPausedGame)
        {
            Time.timeScale = originalTimeScale;
            hasPausedGame = false;
            Debug.Log("[QuestUIController] Game unpaused (manual Quest Log closed)");
        }

        // Stop auto-hide coroutine if running
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        
        // Fade out both panel and decorations simultaneously
        yield return StartCoroutine(FadeQuestDisplayAndDecorations(1f, 0f, fadeOutDuration));
        
        // Hide panel and decorations
        questDisplayPanel.SetActive(false);
        SetDecorationsVisibility(false);
        ClearObjectiveItems();
        ClearAllObjectiveTexts();
        isDisplaying = false;
        
        // NEW: Notify UIController to enable quest button
        NotifyUIController(true);
        
        Debug.Log("Quest display and decorations hidden");
    }
    
    // NEW: Helper method to enable/disable quest button in UIController
    private void NotifyUIController(bool enableButton)
    {
        UIController uiController = Object.FindFirstObjectByType<UIController>();
        if (uiController != null)
        {
            uiController.SetQuestButtonInteractable(enableButton);
            Debug.Log($"[QuestUIController] Quest button {(enableButton ? "ENABLED" : "DISABLED")}");
        }
    }
    
    // Combined fade method for panel and decorations
    private IEnumerator FadeQuestDisplayAndDecorations(float startAlpha, float endAlpha, float duration)
    {
        if (questPanelCanvasGroup == null) yield break;
        
        float elapsed = 0f;
        
        // Set starting alpha for both panel and decorations
        questPanelCanvasGroup.alpha = startAlpha;
        SetDecorationsAlpha(startAlpha);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            
            // Apply fade to both panel and decorations simultaneously
            questPanelCanvasGroup.alpha = currentAlpha;
            SetDecorationsAlpha(currentAlpha);
            
            yield return null;
        }
        
        // Ensure final alpha values are set
        questPanelCanvasGroup.alpha = endAlpha;
        SetDecorationsAlpha(endAlpha);
        
        Debug.Log($"Fade completed: Panel and decorations alpha set to {endAlpha}");
    }
    
    // Clear all objective UI items (for dynamic prefab fallback if needed)
    private void ClearObjectiveItems()
    {
        foreach (GameObject item in activeObjectiveItems)
        {
            if (item != null)
                Destroy(item);
        }
        activeObjectiveItems.Clear();
    }
    
    // Button event handlers
    private void OnContinueButtonClicked()
    {
        Debug.Log("Continue button clicked");
        HideQuestDisplay();
    }
    
    private void OnCloseButtonClicked()
    {
        Debug.Log("Close button clicked");
        HideQuestDisplay();
    }
    
    // Public method to show current quest (called by quest log button)
    public void ShowCurrentQuest()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.HasActiveQuest())
        {
            QuestData currentQuest = QuestManager.Instance.GetCurrentQuest();
            ShowQuestDisplay(currentQuest, false); // false = manual access, show close button
        }
        else
        {
            Debug.Log("No active quest to display");
        }
    }
}
