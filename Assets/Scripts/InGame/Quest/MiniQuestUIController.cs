using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the Mini Quest UI for displaying questions, choices, hints, and feedback
/// UPDATED: Now works with custom Quiz UI structure
/// </summary>
public class MiniQuestUIController : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField] private GameObject miniQuestPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    
    [Header("Title and Instructions")]
    [SerializeField] private TextMeshProUGUI titleText; // Title Box Text
    [SerializeField] private TextMeshProUGUI instructionsText; // Instruction Text
    
    [Header("Progress and Questions")]
    [SerializeField] private TextMeshProUGUI progressText; // Progress Text
    [SerializeField] private TextMeshProUGUI questionText; // Question Text
    [SerializeField] private TextMeshProUGUI feedbackText; // Feedback Text
    
    [Header("Choice Buttons")]
    [SerializeField] private Button choice1Button; // Choice 1 Button
    [SerializeField] private Button choice2Button; // Choice 2 Button
    [SerializeField] private Button choice3Button; // Choice 3 Button
    [SerializeField] private Button choice4Button; // Choice 4 Button
    
    [Header("Choice Button Texts")]
    [SerializeField] private TextMeshProUGUI choice1Text; // Choice 1 Text child
    [SerializeField] private TextMeshProUGUI choice2Text; // Choice 2 Text child
    [SerializeField] private TextMeshProUGUI choice3Text; // Choice 3 Text child
    [SerializeField] private TextMeshProUGUI choice4Text; // Choice 4 Text child
    
    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton; // Next Button (visual only)
    [SerializeField] private Button closeButton; // Close Button (visual only)
    
    [Header("Visual Elements")]
    [SerializeField] private GameObject[] decorationObjects; // Sprite Decorations
    [SerializeField] private GameObject instructionBackground; // Instruction Background
    [SerializeField] private GameObject questionBackground; // Question Background
    
    [Header("Game UI Reference")]
    [SerializeField] private CanvasGroup gameUICanvasGroup; // Main game UI to hide during quiz
    
    [Header("Display Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private Color correctColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
    [SerializeField] private Color incorrectColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hintColor = new Color(1f, 0.9f, 0.3f, 1f); // Yellow
    
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
    
    // UI State Storage
    private bool originalGameUIInteractable;
    private bool originalGameUIBlocksRaycasts;
    private float originalGameUIAlpha;
    
    void Start()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // Get or add CanvasGroup for fading
        if (miniQuestPanel != null && panelCanvasGroup == null)
        {
            panelCanvasGroup = miniQuestPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = miniQuestPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Setup button listeners
        SetupButtonListeners();
        
        // Auto-find game UI if not assigned
        if (gameUICanvasGroup == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.name != "Quiz UI") // Don't grab the quiz UI itself
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
            Debug.Log("[MiniQuestUI] Initialized with custom Quiz UI structure");
            ValidateUIReferences();
        }
    }
    
    private void ValidateUIReferences()
    {
        Debug.Log("=== MiniQuestUIController Component Validation ===");
        Debug.Log($"Mini Quest Panel: {(miniQuestPanel != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Panel Canvas Group: {(panelCanvasGroup != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Title Text: {(titleText != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Instructions Text: {(instructionsText != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Progress Text: {(progressText != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Question Text: {(questionText != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Feedback Text: {(feedbackText != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Choice 1 Button: {(choice1Button != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Choice 2 Button: {(choice2Button != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Choice 3 Button: {(choice3Button != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Choice 4 Button: {(choice4Button != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Next Button: {(nextButton != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Close Button: {(closeButton != null ? "✓" : "✗ MISSING")}");
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
        HideGameUI();
        
        // Show panel
        miniQuestPanel.SetActive(true);
        
        // Display title and instructions
        if (titleText != null) titleText.text = currentMiniQuest.miniQuestTitle;
        if (instructionsText != null) instructionsText.text = currentMiniQuest.instructions;
        
        // Fade in
        yield return StartCoroutine(FadePanel(0f, 1f, fadeInDuration));
        
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
        
        // Update progress
        if (progressText != null)
        {
            progressText.text = $"Question {currentQuestionIndex + 1} of {currentMiniQuest.GetTotalQuestions()}";
        }
        
        // Display question
        if (questionText != null)
        {
            questionText.text = question.questionText;
        }
        
        // Display choices
        DisplayChoices(question);
        
        // Clear feedback
        if (feedbackText != null)
        {
            feedbackText.text = "";
            feedbackText.color = hintColor;
        }
        
        // Hide next button, show close button
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(true);
        
        // Enable choice buttons
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
        // Ensure we have 4 choices
        if (question.choices.Count < 4)
        {
            Debug.LogWarning($"[MiniQuestUI] Question has less than 4 choices ({question.choices.Count})!");
        }
        
        // Display choice 1
        if (choice1Text != null && question.choices.Count > 0)
        {
            choice1Text.text = question.choices[0].choiceText;
            if (choice1Button != null) choice1Button.gameObject.SetActive(true);
        }
        
        // Display choice 2
        if (choice2Text != null && question.choices.Count > 1)
        {
            choice2Text.text = question.choices[1].choiceText;
            if (choice2Button != null) choice2Button.gameObject.SetActive(true);
        }
        
        // Display choice 3
        if (choice3Text != null && question.choices.Count > 2)
        {
            choice3Text.text = question.choices[2].choiceText;
            if (choice3Button != null) choice3Button.gameObject.SetActive(true);
        }
        
        // Display choice 4
        if (choice4Text != null && question.choices.Count > 3)
        {
            choice4Text.text = question.choices[3].choiceText;
            if (choice4Button != null) choice4Button.gameObject.SetActive(true);
        }
    }
    
    private void OnChoiceSelected(int choiceIndex)
    {
        if (waitingForNextQuestion)
        {
            return;
        }
        
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
        
        // Disable choice buttons to prevent multiple clicks
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
        // Highlight correct answer in green
        HighlightChoice(choiceIndex, correctColor);
        
        // Show positive feedback
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
        
        // Wait before moving to next question
        StartCoroutine(WaitAndProceedToNext());
    }
    
    private void OnIncorrectAnswer(int choiceIndex, QuizQuestion question)
    {
        // Highlight incorrect answer in red
        HighlightChoice(choiceIndex, incorrectColor);
        
        // Also highlight the correct answer in green
        int correctIndex = question.GetCorrectAnswerIndex();
        if (correctIndex >= 0)
        {
            HighlightChoice(correctIndex, correctColor);
        }
        
        // Show hint
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
        
        // Re-enable buttons after showing hint
        StartCoroutine(ReenableChoicesAfterDelay());
    }
    
    private IEnumerator WaitAndProceedToNext()
    {
        waitingForNextQuestion = true;
        
        // Show next button
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            
            // Change button appearance based on completion
            if (currentQuestionIndex >= currentMiniQuest.GetTotalQuestions() - 1)
            {
                // Last question - show "Complete" visual (you can change button color/sprite here)
                if (debugMode) Debug.Log("[MiniQuestUI] Last question - showing Complete button");
            }
            else
            {
                // Not last question - show "Next Question" visual
                if (debugMode) Debug.Log("[MiniQuestUI] Showing Next Question button");
            }
        }
        
        yield return null;
    }
    
    private IEnumerator ReenableChoicesAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplayDuration);
        
        // Re-enable choice buttons
        SetChoiceButtonsInteractable(true);
        ResetChoiceButtonColors();
        
        // Clear feedback
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
            // All questions completed!
            CompleteMiniQuest();
        }
        else
        {
            // Display next question
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
        // Fade out
        yield return StartCoroutine(FadePanel(1f, 0f, fadeOutDuration));
        
        // Hide panel
        miniQuestPanel.SetActive(false);
        
        // Restore game UI
        RestoreGameUI();
        
        isDisplaying = false;
        
        // Invoke completion callback
        onQuestCompleteCallback?.Invoke(completedSuccessfully);
    }
    
    private void HideGameUI()
    {
        if (gameUICanvasGroup != null)
        {
            originalGameUIAlpha = gameUICanvasGroup.alpha;
            originalGameUIInteractable = gameUICanvasGroup.interactable;
            originalGameUIBlocksRaycasts = gameUICanvasGroup.blocksRaycasts;
            
            gameUICanvasGroup.alpha = 0f;
            gameUICanvasGroup.interactable = false;
            gameUICanvasGroup.blocksRaycasts = false;
            
            if (debugMode)
            {
                Debug.Log("[MiniQuestUI] Game UI hidden");
            }
        }
    }
    
    private void RestoreGameUI()
    {
        if (gameUICanvasGroup != null)
        {
            gameUICanvasGroup.alpha = originalGameUIAlpha;
            gameUICanvasGroup.interactable = originalGameUIInteractable;
            gameUICanvasGroup.blocksRaycasts = originalGameUIBlocksRaycasts;
            
            if (debugMode)
            {
                Debug.Log("[MiniQuestUI] Game UI restored");
            }
        }
    }
    
    private IEnumerator FadePanel(float startAlpha, float endAlpha, float duration)
    {
        if (panelCanvasGroup == null) yield break;
        
        float elapsed = 0f;
        panelCanvasGroup.alpha = startAlpha;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        
        panelCanvasGroup.alpha = endAlpha;
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