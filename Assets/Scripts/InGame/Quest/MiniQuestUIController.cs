using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the Mini Quest UI for displaying questions, choices, hints, and feedback
/// FIXED: Properly synchronized fade animations for panel and decorations (matching Quest UI exactly)
/// </summary>
public class MiniQuestUIController : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject miniQuestPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    
    [Header("Title and Instructions")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI instructionsText;
    
    [Header("Progress and Questions")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    
    [Header("Choice Buttons")]
    [SerializeField] private Button choice1Button;
    [SerializeField] private Button choice2Button;
    [SerializeField] private Button choice3Button;
    [SerializeField] private Button choice4Button;
    
    [Header("Choice Button Texts")]
    [SerializeField] private TextMeshProUGUI choice1Text;
    [SerializeField] private TextMeshProUGUI choice2Text;
    [SerializeField] private TextMeshProUGUI choice3Text;
    [SerializeField] private TextMeshProUGUI choice4Text;
    
    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;
    
    [Header("Decoration Settings")]
    [Tooltip("GameObjects to show/hide (optional)")]
    [SerializeField] private GameObject[] decorationObjects;
    
    [Tooltip("SpriteRenderers for synchronized fade (auto-found if empty)")]
    [SerializeField] private SpriteRenderer[] decorationSpriteRenderers;
    
    [Tooltip("Images for synchronized fade (auto-found if empty)")]
    [SerializeField] private Image[] decorationImages;
    
    [Header("Game UI Reference")]
    [Tooltip("Main game Canvas to hide during quiz")]
    [SerializeField] private CanvasGroup gameUICanvasGroup;
    
    [Header("Display Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private Color correctColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color incorrectColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hintColor = new Color(1f, 0.9f, 0.3f, 1f);
    
    [Header("Feedback Settings")]
    [SerializeField] private float feedbackDisplayDuration = 2f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    // State Management
    private MiniQuestData currentMiniQuest;
    private int currentQuestionIndex = 0;
    private bool isDisplaying = false;
    private bool waitingForNextQuestion = false;
    private System.Action<bool> onQuestCompleteCallback;
    
    // Game UI state storage
    private bool originalGameUIInteractable;
    private bool originalGameUIBlocksRaycasts;
    private float originalGameUIAlpha;
    private Color[] originalImageColors;
    private Color[] originalTextColors;
    private Color[] originalSpriteColors;
    private Image[] allImages;
    private TextMeshProUGUI[] allTexts;
    private SpriteRenderer[] allSprites;
    
    // CRITICAL: Store original decoration colors for synchronized fading
    private Color[] originalDecorationSpriteColors;
    private Color[] originalDecorationImageColors;
    
    void Start()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // Get or add CanvasGroup
        if (miniQuestPanel != null && panelCanvasGroup == null)
        {
            panelCanvasGroup = miniQuestPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = miniQuestPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // CRITICAL: Initialize decorations BEFORE hiding panel
        InitializeDecorations();
        
        // Setup button listeners
        SetupButtonListeners();
        
        // Auto-find game UI if not assigned
        if (gameUICanvasGroup == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.name != "Quiz UI")
            {
                gameUICanvasGroup = canvas.GetComponent<CanvasGroup>();
            }
        }
        
        // Initially hide panel
        if (miniQuestPanel != null)
        {
            miniQuestPanel.SetActive(false);
        }
        
        if (debugMode)
        {
            Debug.Log("[MiniQuestUI] Initialized with synchronized decoration fade");
            ValidateUIReferences();
        }
    }
    
    // CRITICAL: Initialize decorations (matching Quest UI exactly)
    private void InitializeDecorations()
    {
        // Auto-find decorations if not assigned
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
        
        // CRITICAL: Initially hide all decorations with alpha = 0
        SetDecorationsAlpha(0f);
        SetDecorationsVisibility(false);
        
        if (debugMode)
        {
            Debug.Log($"[MiniQuestUI] Decorations initialized: {decorationObjects?.Length ?? 0} objects, {decorationSpriteRenderers?.Length ?? 0} sprites, {decorationImages?.Length ?? 0} images");
        }
    }
    
    // Auto-find decorations in the Quiz UI panel
    private void AutoFindDecorations()
    {
        if (miniQuestPanel == null) return;
        
        // Find all SpriteRenderers
        SpriteRenderer[] foundSprites = miniQuestPanel.GetComponentsInChildren<SpriteRenderer>(true);
        if (foundSprites.Length > 0)
        {
            decorationSpriteRenderers = foundSprites;
            if (debugMode) Debug.Log($"[MiniQuestUI] Auto-found {foundSprites.Length} SpriteRenderer decorations");
        }
        
        // Find all Images (excluding button images)
        Image[] allPanelImages = miniQuestPanel.GetComponentsInChildren<Image>(true);
        System.Collections.Generic.List<Image> decorationImagesList = new System.Collections.Generic.List<Image>();
        
        foreach (Image img in allPanelImages)
        {
            if (!IsUIElement(img))
            {
                decorationImagesList.Add(img);
            }
        }
        
        if (decorationImagesList.Count > 0)
        {
            decorationImages = decorationImagesList.ToArray();
            if (debugMode) Debug.Log($"[MiniQuestUI] Auto-found {decorationImages.Length} Image decorations");
        }
    }
    
    // Check if Image is a UI element (button) rather than decoration
    private bool IsUIElement(Image img)
    {
        if (img.GetComponent<Button>() != null) return true;
        
        if (img == choice1Button?.GetComponent<Image>() || 
            img == choice2Button?.GetComponent<Image>() ||
            img == choice3Button?.GetComponent<Image>() ||
            img == choice4Button?.GetComponent<Image>() ||
            img == nextButton?.GetComponent<Image>() ||
            img == closeButton?.GetComponent<Image>())
        {
            return true;
        }
        
        if (img.transform == miniQuestPanel.transform) return true;
        
        return false;
    }
    
    // Set decorations visibility
    private void SetDecorationsVisibility(bool visible)
    {
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
        
        // Note: We don't change alpha here anymore - that's handled by SetDecorationsAlpha
    }
    
    // CRITICAL: Set decorations alpha for synchronized fading
    private void SetDecorationsAlpha(float alpha)
    {
        // Handle SpriteRenderers
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
        
        // Handle Images
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
    
    private void ValidateUIReferences()
    {
        Debug.Log("=== MiniQuestUIController Component Validation ===");
        Debug.Log($"Mini Quest Panel: {(miniQuestPanel != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Panel Canvas Group: {(panelCanvasGroup != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Decorations: {decorationObjects?.Length ?? 0} objects, {decorationSpriteRenderers?.Length ?? 0} sprites, {decorationImages?.Length ?? 0} images");
        Debug.Log($"Game UI Canvas Group: {(gameUICanvasGroup != null ? "✓" : "✗ MISSING")}");
        Debug.Log("=================================================");
    }
    
    private void SetupButtonListeners()
    {
        if (choice1Button != null) choice1Button.onClick.AddListener(() => OnChoiceSelected(0));
        if (choice2Button != null) choice2Button.onClick.AddListener(() => OnChoiceSelected(1));
        if (choice3Button != null) choice3Button.onClick.AddListener(() => OnChoiceSelected(2));
        if (choice4Button != null) choice4Button.onClick.AddListener(() => OnChoiceSelected(3));
        
        if (nextButton != null) nextButton.onClick.AddListener(OnNextButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseButtonClicked);
    }
    
    public void StartMiniQuest(MiniQuestData questData, System.Action<bool> onComplete)
    {
        if (questData == null)
        {
            Debug.LogError("[MiniQuestUI] Cannot start mini quest - questData is null!");
            return;
        }
        
        if (questData.questions.Count == 0)
        {
            Debug.LogError("[MiniQuestUI] Cannot start mini quest - no questions defined!");
            return;
        }
        
        currentMiniQuest = questData;
        currentQuestionIndex = 0;
        onQuestCompleteCallback = onComplete;
        
        StartCoroutine(ShowMiniQuestCoroutine());
    }
    
    private IEnumerator ShowMiniQuestCoroutine()
    {
        isDisplaying = true;
        
        // Hide game UI
        HideGameUICompletely();
        
        // Show panel
        miniQuestPanel.SetActive(true);
        
        // CRITICAL: Activate decoration GameObjects
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
        
        // Display title and instructions
        if (titleText != null) titleText.text = currentMiniQuest.miniQuestTitle;
        if (instructionsText != null) instructionsText.text = currentMiniQuest.instructions;
        
        // CRITICAL: Fade in panel AND decorations simultaneously
        yield return StartCoroutine(FadePanelAndDecorations(0f, 1f, fadeInDuration));
        
        // Display first question
        DisplayCurrentQuestion();
    }
    
    private void DisplayCurrentQuestion()
    {
        QuizQuestion question = currentMiniQuest.GetQuestion(currentQuestionIndex);
        if (question == null)
        {
            Debug.LogError($"[MiniQuestUI] Question at index {currentQuestionIndex} is null!");
            return;
        }
        
        if (progressText != null)
        {
            progressText.text = $"Question {currentQuestionIndex + 1} of {currentMiniQuest.GetTotalQuestions()}";
        }
        
        if (questionText != null)
        {
            questionText.text = question.questionText;
        }
        
        DisplayChoices(question);
        
        if (feedbackText != null)
        {
            feedbackText.text = "";
            feedbackText.color = hintColor;
        }
        
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(true);
        
        SetChoiceButtonsInteractable(true);
        ResetChoiceButtonColors();
        
        waitingForNextQuestion = false;
        
        if (debugMode)
        {
            Debug.Log($"[MiniQuestUI] Displaying question {currentQuestionIndex + 1}: {question.questionText}");
        }
    }
    
    private void DisplayChoices(QuizQuestion question)
    {
        if (question.choices.Count < 4)
        {
            Debug.LogWarning($"[MiniQuestUI] Question has less than 4 choices ({question.choices.Count})!");
        }
        
        if (choice1Text != null && question.choices.Count > 0)
        {
            choice1Text.text = question.choices[0].choiceText;
            if (choice1Button != null) choice1Button.gameObject.SetActive(true);
        }
        
        if (choice2Text != null && question.choices.Count > 1)
        {
            choice2Text.text = question.choices[1].choiceText;
            if (choice2Button != null) choice2Button.gameObject.SetActive(true);
        }
        
        if (choice3Text != null && question.choices.Count > 2)
        {
            choice3Text.text = question.choices[2].choiceText;
            if (choice3Button != null) choice3Button.gameObject.SetActive(true);
        }
        
        if (choice4Text != null && question.choices.Count > 3)
        {
            choice4Text.text = question.choices[3].choiceText;
            if (choice4Button != null) choice4Button.gameObject.SetActive(true);
        }
    }
    
    private void OnChoiceSelected(int choiceIndex)
    {
        if (waitingForNextQuestion) return;
        
        QuizQuestion question = currentMiniQuest.GetQuestion(currentQuestionIndex);
        if (question == null || choiceIndex >= question.choices.Count)
        {
            Debug.LogError($"[MiniQuestUI] Invalid choice selection: index {choiceIndex}");
            return;
        }
        
        QuizChoice selectedChoice = question.choices[choiceIndex];
        
        if (debugMode)
        {
            Debug.Log($"[MiniQuestUI] Player selected choice {choiceIndex}: {selectedChoice.choiceText} (Correct: {selectedChoice.isCorrect})");
        }
        
        SetChoiceButtonsInteractable(false);
        
        if (selectedChoice.isCorrect)
        {
            OnCorrectAnswer(choiceIndex, question);
        }
        else
        {
            OnIncorrectAnswer(choiceIndex, question);
        }
    }
    
    private void OnCorrectAnswer(int choiceIndex, QuizQuestion question)
    {
        HighlightChoice(choiceIndex, correctColor);
        
        if (feedbackText != null)
        {
            string feedback = "✓ Correct!";
            if (!string.IsNullOrEmpty(question.explanationOnCorrect))
            {
                feedback += " " + question.explanationOnCorrect;
            }
            feedbackText.text = feedback;
            feedbackText.color = correctColor;
        }
        
        if (debugMode)
        {
            Debug.Log($"[MiniQuestUI] ✓ Correct answer selected!");
        }
        
        StartCoroutine(WaitAndProceedToNext());
    }
    
    private void OnIncorrectAnswer(int choiceIndex, QuizQuestion question)
    {
        HighlightChoice(choiceIndex, incorrectColor);
        
        int correctIndex = question.GetCorrectAnswerIndex();
        if (correctIndex >= 0)
        {
            HighlightChoice(correctIndex, correctColor);
        }
        
        if (feedbackText != null)
        {
            string hint = "✗ Incorrect.";
            if (!string.IsNullOrEmpty(question.hintOnWrongAnswer))
            {
                hint += " Hint: " + question.hintOnWrongAnswer;
            }
            feedbackText.text = hint;
            feedbackText.color = incorrectColor;
        }
        
        if (debugMode)
        {
            Debug.Log($"[MiniQuestUI] ✗ Incorrect answer selected. Showing hint.");
        }
        
        StartCoroutine(ReenableChoicesAfterDelay());
    }
    
    private IEnumerator WaitAndProceedToNext()
    {
        waitingForNextQuestion = true;
        
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            
            if (currentQuestionIndex >= currentMiniQuest.GetTotalQuestions() - 1)
            {
                if (debugMode) Debug.Log("[MiniQuestUI] Last question - showing Complete button");
            }
            else
            {
                if (debugMode) Debug.Log("[MiniQuestUI] Showing Next Question button");
            }
        }
        
        yield return null;
    }
    
    private IEnumerator ReenableChoicesAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplayDuration);
        
        SetChoiceButtonsInteractable(true);
        ResetChoiceButtonColors();
        
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }
    
    private void OnNextButtonClicked()
    {
        currentQuestionIndex++;
        
        if (currentQuestionIndex >= currentMiniQuest.GetTotalQuestions())
        {
            CompleteMiniQuest();
        }
        else
        {
            DisplayCurrentQuestion();
        }
    }
    
    private void CompleteMiniQuest()
    {
        if (debugMode)
        {
            Debug.Log("[MiniQuestUI] ✓✓✓ Mini Quest completed successfully!");
        }
        
        StartCoroutine(HideMiniQuestCoroutine(true));
    }
    
    private void OnCloseButtonClicked()
    {
        if (debugMode)
        {
            Debug.Log("[MiniQuestUI] Close button clicked - cancelling quiz");
        }
        
        StartCoroutine(HideMiniQuestCoroutine(false));
    }
    
    private IEnumerator HideMiniQuestCoroutine(bool completedSuccessfully)
    {
        // CRITICAL: Fade out panel AND decorations simultaneously
        yield return StartCoroutine(FadePanelAndDecorations(1f, 0f, fadeOutDuration));
        
        // Hide panel and decorations
        miniQuestPanel.SetActive(false);
        SetDecorationsVisibility(false);
        
        // Restore game UI
        yield return StartCoroutine(RestoreGameUIGradually());
        
        isDisplaying = false;
        
        onQuestCompleteCallback?.Invoke(completedSuccessfully);
        
        if (debugMode)
        {
            Debug.Log("[MiniQuestUI] Quiz UI and decorations hidden");
        }
    }
    
    private void HideGameUICompletely()
    {
        if (gameUICanvasGroup != null)
        {
            originalGameUIAlpha = gameUICanvasGroup.alpha;
            originalGameUIInteractable = gameUICanvasGroup.interactable;
            originalGameUIBlocksRaycasts = gameUICanvasGroup.blocksRaycasts;
            
            gameUICanvasGroup.alpha = 0f;
            gameUICanvasGroup.interactable = false;
            gameUICanvasGroup.blocksRaycasts = false;
            
            StoreOriginalStatesAndHide();
            
            if (debugMode)
            {
                Debug.Log("[MiniQuestUI] Game UI hidden completely");
            }
        }
    }
    
    private void StoreOriginalStatesAndHide()
    {
        if (gameUICanvasGroup != null)
        {
            allImages = gameUICanvasGroup.GetComponentsInChildren<Image>(true);
            originalImageColors = new Color[allImages.Length];
            
            for (int i = 0; i < allImages.Length; i++)
            {
                originalImageColors[i] = allImages[i].color;
                Color hiddenColor = allImages[i].color;
                hiddenColor.a = 0f;
                allImages[i].color = hiddenColor;
            }

            allTexts = gameUICanvasGroup.GetComponentsInChildren<TextMeshProUGUI>(true);
            originalTextColors = new Color[allTexts.Length];
            
            for (int i = 0; i < allTexts.Length; i++)
            {
                originalTextColors[i] = allTexts[i].color;
                Color hiddenColor = allTexts[i].color;
                hiddenColor.a = 0f;
                allTexts[i].color = hiddenColor;
            }

            allSprites = gameUICanvasGroup.GetComponentsInChildren<SpriteRenderer>(true);
            originalSpriteColors = new Color[allSprites.Length];
            
            for (int i = 0; i < allSprites.Length; i++)
            {
                originalSpriteColors[i] = allSprites[i].color;
                Color hiddenColor = allSprites[i].color;
                hiddenColor.a = 0f;
                allSprites[i].color = hiddenColor;
            }

            Button[] buttons = gameUICanvasGroup.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.interactable = false;
            }

            Slider[] sliders = gameUICanvasGroup.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                slider.interactable = false;
            }

            if (debugMode)
            {
                Debug.Log($"[MiniQuestUI] Hidden: {allImages.Length} images, {allTexts.Length} texts, {allSprites.Length} sprites");
            }
        }
    }
    
    private IEnumerator RestoreGameUIGradually()
    {
        if (gameUICanvasGroup != null)
        {
            gameUICanvasGroup.alpha = originalGameUIAlpha;
            gameUICanvasGroup.interactable = originalGameUIInteractable;
            gameUICanvasGroup.blocksRaycasts = originalGameUIBlocksRaycasts;

            yield return null;

            RestoreOriginalStates();

            yield return null;

            Canvas.ForceUpdateCanvases();

            yield return null;

            UIController uiController = Object.FindFirstObjectByType<UIController>();
            if (uiController != null)
            {
                uiController.ReinitializeButtons();
                if (debugMode)
                {
                    Debug.Log("[MiniQuestUI] UIController buttons reinitialized");
                }
            }

            if (debugMode)
            {
                Debug.Log("[MiniQuestUI] Game UI fully restored");
            }
        }
    }
    
    private void RestoreOriginalStates()
    {
        if (gameUICanvasGroup != null)
        {
            if (allImages != null && originalImageColors != null)
            {
                for (int i = 0; i < allImages.Length && i < originalImageColors.Length; i++)
                {
                    if (allImages[i] != null)
                        allImages[i].color = originalImageColors[i];
                }
            }

            if (allTexts != null && originalTextColors != null)
            {
                for (int i = 0; i < allTexts.Length && i < originalTextColors.Length; i++)
                {
                    if (allTexts[i] != null)
                        allTexts[i].color = originalTextColors[i];
                }
            }

            if (allSprites != null && originalSpriteColors != null)
            {
                for (int i = 0; i < allSprites.Length && i < originalSpriteColors.Length; i++)
                {
                    if (allSprites[i] != null)
                        allSprites[i].color = originalSpriteColors[i];
                }
            }

            Button[] buttons = gameUICanvasGroup.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.interactable = true;
            }

            Slider[] sliders = gameUICanvasGroup.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                slider.interactable = true;
            }

            if (debugMode)
            {
                Debug.Log($"[MiniQuestUI] Restored: {allImages?.Length ?? 0} images, {allTexts?.Length ?? 0} texts, {allSprites?.Length ?? 0} sprites");
            }
        }
    }
    
    // CRITICAL: Synchronized fade for panel and decorations (matching Quest UI exactly)
    private IEnumerator FadePanelAndDecorations(float startAlpha, float endAlpha, float duration)
    {
        if (panelCanvasGroup == null) yield break;
        
        float elapsed = 0f;
        
        // CRITICAL: Set BOTH panel and decorations to starting alpha BEFORE loop
        panelCanvasGroup.alpha = startAlpha;
        SetDecorationsAlpha(startAlpha);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            
            // CRITICAL: Apply SAME alpha to both panel and decorations simultaneously
            panelCanvasGroup.alpha = currentAlpha;
            SetDecorationsAlpha(currentAlpha);
            
            yield return null;
        }
        
        // CRITICAL: Ensure final alpha values are set
        panelCanvasGroup.alpha = endAlpha;
        SetDecorationsAlpha(endAlpha);
        
        if (debugMode)
        {
            Debug.Log($"[MiniQuestUI] Synchronized fade completed: Panel and decorations alpha = {endAlpha}");
        }
    }
    
    private void HighlightChoice(int choiceIndex, Color color)
    {
        Button targetButton = null;
        
        switch (choiceIndex)
        {
            case 0: targetButton = choice1Button; break;
            case 1: targetButton = choice2Button; break;
            case 2: targetButton = choice3Button; break;
            case 3: targetButton = choice4Button; break;
        }
        
        if (targetButton != null)
        {
            ColorBlock colors = targetButton.colors;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.pressedColor = color;
            colors.selectedColor = color;
            targetButton.colors = colors;
        }
    }
    
    private void ResetChoiceButtonColors()
    {
        ResetButtonColor(choice1Button);
        ResetButtonColor(choice2Button);
        ResetButtonColor(choice3Button);
        ResetButtonColor(choice4Button);
    }
    
    private void ResetButtonColor(Button button)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = defaultColor;
            colors.highlightedColor = Color.gray;
            colors.pressedColor = Color.gray;
            colors.selectedColor = defaultColor;
            button.colors = colors;
        }
    }
    
    private void SetChoiceButtonsInteractable(bool interactable)
    {
        if (choice1Button != null) choice1Button.interactable = interactable;
        if (choice2Button != null) choice2Button.interactable = interactable;
        if (choice3Button != null) choice3Button.interactable = interactable;
        if (choice4Button != null) choice4Button.interactable = interactable;
    }
}