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

        // NEW: Ensure database is loaded
        EnsureDialogueDatabase();
        EnsureUI();
        Hide();
    }

    // NEW: Auto-load or create fallback dialogue database
    private void EnsureDialogueDatabase()
    {
        if (dialogueDatabase == null)
        {
            // Try to load from Resources
            dialogueDatabase = Resources.Load<DialogueDatabase>("OakWoods/DialogueDatabase_OakWoods");

            if (dialogueDatabase == null)
            {
                Debug.LogWarning("DialogueDatabase not found in Resources. Creating fallback in-memory database.");
                CreateFallbackDatabase();
            }
            else
            {
                Debug.Log("DialogueDatabase loaded from Resources/OakWoods/DialogueDatabase_OakWoods");
            }
        }
    }

    // NEW: Create fallback conversations if database is missing
    private void CreateFallbackDatabase()
    {
        // Create a temporary instance just for this session
        var list = new List<Conversation>();

        list.Add(CreateConversation("Arin_01_PostBoss",
            "Arin", "You fought bravely. Not many step into that cave and walk back out.",
            "Roran", "You looked like you needed help.",
            "Arin", "I did. Thank you. You don't look like a local. What brings you to Oak Woods?",
            "Roran", "I'm new to this land. I'm searching for knowledge—and a way to survive.",
            "Arin", "Then you've chosen the right power. Here, code is more than ink—it moves the world.",
            "Roran", "I've heard. But I don't know where to start.",
            "Arin", "Start with me. I'll mentor you. Come to my cottage. We'll begin with Java: what it is, and how to speak it.",
            "Roran", "Lead the way.",
            "Arin", "Follow me to the edge of the woods. Don't stray. The forest tests syntax more than strength."
        ));

        list.Add(CreateConversation("Arin_02_JavaBasics",
            "Arin", "Let's start simple. Java is a language used to build many kinds of applications.",
            "Arin", "You put code inside a class. A class is a container for your program.",
            "Arin", "Programs begin in a special method named main. Java looks for this to start.",
            "Arin", "The exact main method you'll use is:",
            "Arin", "public static void main(String[] args)",
            "Arin", "Curly braces { } group code. End each statement with a semicolon ;",
            "Arin", "To show text: System.out.println(\"your message\");",
            "Arin", "The class name should start with a capital letter, like HelloOak.",
            "Arin", "Now fill in the blanks to create your first program: class, main, and a println.",
            "Roran", "Understood. Class, main, braces, semicolons, and System.out.println."
        ));

        list.Add(CreateConversation("Arin_02b_PostHello",
            "Arin", "Well done! Your first program prints a message.",
            "Roran", "So many symbols for one sentence.",
            "Arin", "Symbols give structure; structure gives power. Next, we'll store values using variables and types."
        ));

        list.Add(CreateConversation("Arin_03_TypesLecture",
            "Arin", "Variables store information. Each has a type and a name.",
            "Arin", "Common types: int (whole numbers), double (decimals), boolean (true/false), String (text).",
            "Arin", "Examples:",
            "Arin", "int lives = 3;    double speed = 2.5;",
            "Arin", "boolean isMage = false;    String name = \"Roran\";",
            "Arin", "Remember: end with a semicolon. Names are case-sensitive.",
            "Arin", "Gather the runes int, boolean, and String, then fix the Declarations Tablet."
        ));

        list.Add(CreateConversation("Arin_03_PostTypes",
            "Arin", "Your types align. The grove's wards are stable again.",
            "Roran", "A missing semicolon breaks everything.",
            "Arin", "That's syntax. Next: strings and printing details."
        ));

        list.Add(CreateConversation("Arin_04_StringsLecture",
            "Arin", "Strings are text in quotes, like \"Hello\".",
            "Arin", "Use System.out.println to print with a new line, or System.out.print to stay on the same line.",
            "Arin", "Join pieces with +, like \"Name: \" + name.",
            "Arin", "Escape sequences help inside quotes: \\\" for a quote, \\\\ for backslash, \\n for new line, \\t for tab.",
            "Arin", "Fix the print lines to show your name and a two-line message using these rules."
        ));

        list.Add(CreateConversation("Arin_04_PostStrings",
            "Arin", "Clear and correct. Your text rings true.",
            "Roran", "I can shape messages exactly now.",
            "Arin", "One final trial: build a clean class with main and valid statements."
        ));

        list.Add(CreateConversation("Arin_05_SyntaxRecap",
            "Arin", "Recap: A class wraps code. main is the entry point.",
            "Arin", "Statements end with semicolons. Braces must match.",
            "Arin", "You can add comments: // single line, or /* multi-line */.",
            "Arin", "Use declarations and println with concatenation to present information clearly."
        ));

        list.Add(CreateConversation("Arin_05_Epilogue",
            "Arin", "You've mastered the basics of Java syntax. Oak Woods recognizes you.",
            "Roran", "I can finally read the forest's words.",
            "Arin", "Syntax is your compass. Keep practicing, Codebreaker.",
            "Roran", "Thank you, Arin.",
            "Arin", "If fate loops us back, I'll be here. For now, walk on."
        ));

        // Create a temporary database instance (not saved to disk)
        dialogueDatabase = ScriptableObject.CreateInstance<DialogueDatabase>();
        dialogueDatabase.SetConversations(list);

        Debug.LogWarning("FALLBACK: Using in-memory dialogue database. Run Tools > OakWoods > Generate Oak Woods Content to create persistent assets.");
    }

    private Conversation CreateConversation(string id, params string[] alternatingLines)
    {
        var lines = new List<DialogueLine>();
        for (int i = 0; i < alternatingLines.Length; i += 2)
        {
            if (i + 1 < alternatingLines.Length)
            {
                lines.Add(new DialogueLine { speaker = alternatingLines[i], text = alternatingLines[i + 1] });
            }
        }
        return new Conversation { conversationId = id, lines = lines };
    }

    private void EnsureUI()
    {
        if (panel != null && speakerText != null && bodyText != null && nextButton != null)
            return;

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
        bodyText.textWrappingMode = TextWrappingModes.Normal;
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
            Debug.LogError("DialogueManager: DialogueDatabase is null and fallback failed!");
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