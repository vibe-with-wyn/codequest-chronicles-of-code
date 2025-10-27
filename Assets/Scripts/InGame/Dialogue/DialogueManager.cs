using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private DialogueDatabase dialogueDatabase;

    [Header("UI (Optional)")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button nextButton;

    public event Action<string> OnConversationStarted;
    public event Action<string> OnConversationCompleted;

    private Conversation current;
    private int index = -1;
    private bool uiBuiltAtRuntime = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureUI();
        Hide();
    }

    private void EnsureUI()
    {
        if (panel != null && speakerText != null && bodyText != null && nextButton != null)
            return;

        // Build a very simple UI if not provided
        uiBuiltAtRuntime = true;

        GameObject canvasGO = new GameObject("DialogueCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var cg = canvasGO.AddComponent<CanvasGroup>();
        panel = cg;

        GameObject bg = new GameObject("Panel");
        bg.transform.SetParent(canvasGO.transform);
        var img = bg.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.05f, 0.05f);
        rt.anchorMax = new Vector2(0.95f, 0.3f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        GameObject speakerGO = new GameObject("Speaker");
        speakerGO.transform.SetParent(bg.transform);
        speakerText = speakerGO.AddComponent<TextMeshProUGUI>();
        var srt = speakerText.rectTransform;
        srt.anchorMin = new Vector2(0.02f, 0.7f);
        srt.anchorMax = new Vector2(0.5f, 0.95f);
        srt.offsetMin = srt.offsetMax = Vector2.zero;
        speakerText.fontSize = 28;
        speakerText.alignment = TextAlignmentOptions.Left;
        speakerText.text = "";

        GameObject bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(bg.transform);
        bodyText = bodyGO.AddComponent<TextMeshProUGUI>();
        var brt = bodyText.rectTransform;
        brt.anchorMin = new Vector2(0.02f, 0.1f);
        brt.anchorMax = new Vector2(0.98f, 0.75f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        bodyText.fontSize = 24;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.enableWordWrapping = true;
        bodyText.text = "";

        GameObject btnGO = new GameObject("NextButton");
        btnGO.transform.SetParent(bg.transform);
        nextButton = btnGO.AddComponent<Button>();
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(1, 1, 1, 0.2f);
        var btnRt = btnImg.rectTransform;
        btnRt.anchorMin = new Vector2(0.8f, 0.1f);
        btnRt.anchorMax = new Vector2(0.98f, 0.3f);
        btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;

        var btnLabelGO = new GameObject("Label");
        btnLabelGO.transform.SetParent(btnGO.transform);
        var btnLabel = btnLabelGO.AddComponent<TextMeshProUGUI>();
        btnLabel.text = "Next";
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.fontSize = 22;
        var blrt = btnLabel.rectTransform;
        blrt.anchorMin = new Vector2(0, 0);
        blrt.anchorMax = new Vector2(1, 1);
        blrt.offsetMin = blrt.offsetMax = Vector2.zero;

        nextButton.onClick.AddListener(Next);
    }

    private void Show()
    {
        if (panel != null) panel.alpha = 1f;
        if (panel != null) panel.interactable = true;
        if (panel != null) panel.blocksRaycasts = true;
    }

    private void Hide()
    {
        if (panel != null) panel.alpha = 0f;
        if (panel != null) panel.interactable = false;
        if (panel != null) panel.blocksRaycasts = false;
    }

    public void StartConversation(string conversationId)
    {
        if (dialogueDatabase == null)
        {
            Debug.LogError("DialogueManager: DialogueDatabase not assigned!");
            return;
        }

        current = dialogueDatabase.GetConversation(conversationId);
        if (current == null || current.lines == null || current.lines.Count == 0)
        {
            Debug.LogWarning($"DialogueManager: Conversation '{conversationId}' not found or empty.");
            return;
        }

        index = -1;
        Show();
        OnConversationStarted?.Invoke(conversationId);
        Next();
    }

    public void Next()
    {
        if (current == null) { Hide(); return; }

        index++;
        if (index >= current.lines.Count)
        {
            string endedId = current.conversationId;
            current = null;
            Hide();
            OnConversationCompleted?.Invoke(endedId);
            return;
        }

        var line = current.lines[index];
        if (speakerText != null) speakerText.text = line.speaker;
        if (bodyText != null) bodyText.text = line.text;
        Debug.Log($"[{line.speaker}] {line.text}");
    }
}