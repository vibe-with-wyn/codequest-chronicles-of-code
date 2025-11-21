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

    [Header("Decoration Settings")]
    [Tooltip("Parent GameObject containing decoration child objects (optional)")]
    [SerializeField] private GameObject decorationsParent;

    [Tooltip("SpriteRenderers for decorations (auto-found if empty)")]
    [SerializeField] private SpriteRenderer[] decorationSpriteRenderers;

    [Header("Display Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    public event Action<string> OnConversationStarted;
    public event Action<string> OnConversationCompleted;

    private Conversation current;
    private int index = -1;
    private bool uiBuiltAtRuntime = false;

    // Store original decoration colors for proper restoration
    private Color[] originalDecorationColors;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure database is loaded
        EnsureDialogueDatabase();
        EnsureUI();
        InitializeDecorations();
        
        // FIXED: Hide immediately without fade animation on initialization
        HideImmediatelyOnStart();
    }

    // NEW: Instantly hide UI on game start (no coroutine, no fade)
    private void HideImmediatelyOnStart()
    {
        if (panel == null) return;

        // Instantly deactivate the panel GameObject
        panel.gameObject.SetActive(false);

        // Set panel to fully transparent
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;

        // Instantly hide decorations
        SetDecorationsAlpha(0f);
        SetDecorationsVisibility(false);

        Debug.Log("[DialogueManager] UI hidden instantly on initialization (no fade)");
    }

    // NEW: Initialize decorations (matching Quest UI pattern)
    private void InitializeDecorations()
    {
        // Auto-find decorations if not assigned
        if ((decorationSpriteRenderers == null || decorationSpriteRenderers.Length == 0) && decorationsParent != null)
        {
            decorationSpriteRenderers = decorationsParent.GetComponentsInChildren<SpriteRenderer>(true);
            Debug.Log($"[DialogueManager] Auto-found {decorationSpriteRenderers.Length} SpriteRenderer decorations");
        }

        // Store original colors for SpriteRenderers
        if (decorationSpriteRenderers != null && decorationSpriteRenderers.Length > 0)
        {
            originalDecorationColors = new Color[decorationSpriteRenderers.Length];
            for (int i = 0; i < decorationSpriteRenderers.Length; i++)
            {
                if (decorationSpriteRenderers[i] != null)
                {
                    originalDecorationColors[i] = decorationSpriteRenderers[i].color;
                }
            }
        }

        // Initially hide all decorations with alpha = 0
        SetDecorationsAlpha(0f);
        SetDecorationsVisibility(false);

        Debug.Log($"[DialogueManager] Initialized decorations: {decorationSpriteRenderers?.Length ?? 0} sprites");
    }

    // Set decorations visibility
    private void SetDecorationsVisibility(bool visible)
    {
        if (decorationsParent != null)
        {
            decorationsParent.SetActive(visible);
        }

        if (decorationSpriteRenderers != null)
        {
            foreach (SpriteRenderer spriteRenderer in decorationSpriteRenderers)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.gameObject.SetActive(visible);
                }
            }
        }
    }

    // Set decorations alpha for synchronized fading
    private void SetDecorationsAlpha(float alpha)
    {
        if (decorationSpriteRenderers != null && originalDecorationColors != null)
        {
            for (int i = 0; i < decorationSpriteRenderers.Length && i < originalDecorationColors.Length; i++)
            {
                if (decorationSpriteRenderers[i] != null)
                {
                    Color color = originalDecorationColors[i];
                    color.a = alpha;
                    decorationSpriteRenderers[i].color = color;
                }
            }
        }
    }

    // Auto-load or create fallback dialogue database
    private void EnsureDialogueDatabase()
    {
        if (dialogueDatabase == null)
        {
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

    // Create fallback conversations if database is missing
    private void CreateFallbackDatabase()
    {
        // Create a temporary instance just for this session
        var list = new List<Conversation>();

        // ==================== QUEST 1: Into the Skull Cave ====================
        list.Add(CreateConversation("Arin_01_PostBoss",
            "Arin", "You fought bravely. Not many step into this cave and walk back out.",
            "Roran", "You looked like you needed help.",
            "Arin", "I did. Thank you. You don't look like a local. What brings you to Oak Woods?",
            "Roran", "I'm new to this land. I'm searching for knowledge—and a way to survive.",
            "Arin", "Knowledge, you say? Then fate has brought you to the right place.",
            "Arin", "Here in Oak Woods, we don't wield swords or cast spells like other lands.",
            "Arin", "We command something far more powerful—the language of creation itself.",
            "Roran", "The language of creation?",
            "Arin", "Code. Here, code is more than ink on parchment—it shapes reality, commands the elements, bends the very fabric of this world.",
            "Roran", "I've heard whispers of such power, but I never believed it was real.",
            "Arin", "Oh, it's real. The Night Borne you just helped me defeat? Born from corrupted code fragments.",
            "Arin", "The wards protecting our villages? Written in precise syntax. Even the trees respond to properly structured commands.",
            "Roran", "That's... incredible. But I don't know where to begin. I've never written a line of code in my life.",
            "Arin", "Everyone starts somewhere. I was once like you—lost, uncertain, overwhelmed by symbols I didn't understand.",
            "Arin", "But a mentor took me in, just as I will take you in now.",
            "Roran", "You would teach me?",
            "Arin", "You saved my life in that battle. The least I can do is help you save your own future.",
            "Arin", "Besides, Oak Woods needs more Codebreakers—those who can read and write the ancient syntax.",
            "Roran", "Codebreakers... I like the sound of that.",
            "Arin", "Then come. My cabin isn't far from here. We'll start with the foundations.",
            "Arin", "First, I'll teach you Java—one of the oldest and most respected tongues in the realm of code.",
            "Roran", "Java. I'm ready to learn.",
            "Arin", "Good. But heed my warning: the path of a programmer is not easy.",
            "Arin", "Every symbol matters. One misplaced semicolon, one unclosed brace, and your creation crumbles.",
            "Arin", "The forest itself will test you. It recognizes syntax, rewards precision, and punishes carelessness.",
            "Roran", "I understand. I won't let you down.",
            "Arin", "I believe you won't. Now follow me closely—and stay on the path.",
            "Arin", "Oak Woods tests syntax more than strength. Wander off, and the trees might just compile you into something... unpleasant.",
            "Roran", "Noted. Staying on the path.",
            "Arin", "Smart choice. Come, the cabin awaits. Your journey as a Codebreaker begins today."
        ));

        // ==================== QUEST 2: Hello, Java! ====================
        list.Add(CreateConversation("Arin_02_JavaBasics",
            "Arin", "Welcome to my cabin. Let's begin your journey into the world of Java programming.",
            "Roran", "I'm ready to learn.",
            "Arin", "Java is a powerful programming language used to build applications—from mobile apps to large enterprise systems.",
            "Arin", "It's one of the most popular languages in the world. Banks, games, websites—many rely on Java.",
            "Arin", "Every Java program starts with a CLASS. Think of a class as a container that holds your code.",
            "Arin", "The class name should start with a capital letter. For example: HelloWorld, MyProgram, or Calculator.",
            "Roran", "So every program needs a class?",
            "Arin", "Exactly! Without a class, Java won't even recognize your code. It's the foundation.",
            "Arin", "Inside a class, you need a special method called MAIN. This is where your program begins execution.",
            "Arin", "The exact signature you must memorize is:",
            "Arin", "public static void main(String[] args)",
            "Roran", "That's a lot of words. What do they mean?",
            "Arin", "PUBLIC means it can be accessed from anywhere. STATIC means it belongs to the class itself, not an instance.",
            "Arin", "VOID means it doesn't return any value. And String[] args allows you to pass text arguments when running the program.",
            "Arin", "For now, just remember the pattern. Understanding will come with practice.",
            "Roran", "So I just need to write it exactly like that every time?",
            "Arin", "Yes! Every standalone Java program needs this exact main method. Think of it as the starting gate.",
            "Arin", "Next: CURLY BRACES. They group blocks of code together. Every opening brace { needs a closing brace }.",
            "Arin", "Your class starts with { and ends with }. Your main method also starts with { and ends with }.",
            "Arin", "Think of braces like parentheses in math—they show what belongs together.",
            "Roran", "So braces organize the code into sections?",
            "Arin", "Precisely! They define scope—what code belongs to the class, what belongs to the method.",
            "Arin", "Inside the main method, you write STATEMENTS—instructions for the computer to execute.",
            "Arin", "Every statement must end with a SEMICOLON ;  Think of it as a period ending a sentence.",
            "Arin", "Without the semicolon, Java won't know where one instruction ends and another begins.",
            "Roran", "So: class, main method, braces, and semicolons. Got it.",
            "Arin", "Good! Now, to display text output, use: System.out.println(\"your message\");",
            "Arin", "println stands for 'print line'—it prints text and moves to a new line.",
            "Arin", "The text goes inside DOUBLE QUOTES, like this: \"Hello, Oak Woods!\"",
            "Arin", "Let me show you some examples:",
            "Arin", "System.out.println(\"Welcome, traveler!\");  prints: Welcome, traveler!",
            "Arin", "System.out.println(\"Java is powerful!\");  prints: Java is powerful!",
            "Arin", "System.out.println(\"Learning to code!\");  prints: Learning to code!",
            "Roran", "So I can print any message I want?",
            "Arin", "Exactly! Just put it in quotes, inside the parentheses, and end with a semicolon.",
            "Arin", "Remember: System.out.println with parentheses, quotes around text, and a semicolon at the end.",
            "Arin", "Here are more examples to help you understand:",
            "Arin", "System.out.println(\"My name is Roran\");",
            "Arin", "System.out.println(\"I am learning Java\");",
            "Arin", "System.out.println(\"This is my first program\");",
            "Roran", "System.out.println, quotes, semicolon. I understand.",
            "Arin", "Perfect. Now let me show you what a complete program looks like:",
            "Arin", "class HelloWorld { public static void main(String[] args) { System.out.println(\"Hello!\"); } }",
            "Arin", "Let's break that down piece by piece:",
            "Arin", "First: class HelloWorld {  — This starts our class and opens its code block.",
            "Arin", "Second: public static void main(String[] args) {  — This starts our main method.",
            "Arin", "Third: System.out.println(\"Hello!\");  — This prints our message.",
            "Arin", "Fourth: }  — This closes the main method.",
            "Arin", "Fifth: }  — This closes the class.",
            "Roran", "So every brace that opens must close?",
            "Arin", "Yes! If you forget to close a brace, your program won't compile. It's a common mistake.",
            "Arin", "Here's another complete example:",
            "Arin", "class Welcome { public static void main(String[] args) { System.out.println(\"Welcome to Oak Woods!\"); } }",
            "Arin", "And one more to make sure you understand:",
            "Arin", "class MyFirstProgram { public static void main(String[] args) { System.out.println(\"I am a programmer!\"); } }",
            "Roran", "I see the pattern now. Class name, main method, print statement, close everything.",
            "Arin", "Excellent! You're grasping the structure. That's the hardest part for beginners.",
            "Arin", "Now, test your understanding at the Hello World Altar. You'll answer questions about Java basics.",
            "Arin", "Remember everything: class structure, the exact main signature, println syntax, and brace matching."
        ));

        list.Add(CreateConversation("Arin_02b_PostHello",
            "Arin", "You returned from the altar! I see the light of success in your eyes, Roran.",
            "Roran", "Your lessons were clear. The questions tested everything you taught me.",
            "Arin", "Excellent work! You've passed the first true test. Your first program runs correctly.",
            "Arin", "So many symbols for just one sentence, you said. But now you understand why they matter.",
            "Roran", "Symbols give structure; structure gives power. You were right.",
            "Arin", "Every character serves a purpose. Missing one breaks everything. That's the nature of code.",
            "Arin", "You've learned the foundation: classes, methods, statements, braces, and semicolons.",
            "Arin", "Now comes the challenge. Most programmers stop here. But you'll go deeper.",
            "Roran", "What's next?",
            "Arin", "Data. Next, we'll learn how to store values using variables and data types.",
            "Arin", "Good. We'll continue right away — gather the Type Runes next."
        ));

        // ==================== QUEST 3: The Language of Types ====================
        list.Add(CreateConversation("Arin_03_TypesLecture",
            "Arin", "Welcome back to my cabin. Today we learn about DATA TYPES and VARIABLES—the building blocks of programming.",
            "Roran", "What are variables?",
            "Arin", "Variables are containers that store information. Think of them as labeled boxes that hold different kinds of data.",
            "Arin", "Each variable has a TYPE and a NAME. The TYPE defines what kind of data it can hold. The NAME is how you identify it.",
            "Roran", "Why do we need different types?",
            "Arin", "Because different data serves different purposes. You wouldn't store a player's name the same way you store their health points.",
            "Arin", "Java has several primitive data types. Let me explain the four most important ones in detail:",
            "Arin", "First: INT. This stores whole numbers—positive, negative, or zero.",
            "Arin", "Examples of int values: 5, -10, 0, 1000, 42, -999",
            "Arin", "Use int for things like: player health, score, age, quantity, level, lives remaining.",
            "Roran", "So int is for counting things?",
            "Arin", "Exactly! Anything you can count without fractions: 10 apples, 3 lives, 500 gold coins.",
            "Arin", "But remember: int cannot store decimals. 3.5 is NOT a valid int—it would need to be 3 or 4.",
            "Arin", "Second: DOUBLE. This stores decimal numbers—numbers with fractional parts.",
            "Arin", "Examples of double values: 3.14, -0.5, 2.0, 99.99, 1.5, 0.001",
            "Arin", "Use double for: speed, temperature, money amounts, precise calculations, percentages.",
            "Roran", "So when I need the exact value with decimals?",
            "Arin", "Precisely! If a potion restores 2.5 health per second, you need double, not int.",
            "Arin", "If your character moves at 3.75 units per second, you need double.",
            "Arin", "Third: BOOLEAN. This stores true or false values—only two possibilities.",
            "Arin", "A boolean is like a yes/no question. It can only be true OR false, never anything else.",
            "Arin", "Examples: true, false (those are the ONLY two values a boolean can have)",
            "Arin", "Use boolean for: isAlive, hasKey, gameOver, canJump, isGrounded, isDead—any yes/no condition.",
            "Roran", "So it's for checking if something is true or not?",
            "Arin", "Exactly! Is the player alive? true or false. Does the player have the key? true or false.",
            "Arin", "Boolean values are crucial for making decisions in code, which you'll learn later.",
            "Arin", "Fourth: STRING. This stores text—sequences of characters.",
            "Arin", "Examples: \"Hello\", \"Roran\", \"Oak Woods\", \"Welcome, traveler!\", \"Game Over\"",
            "Arin", "Strings always use DOUBLE QUOTES. Without quotes, Java thinks it's a variable name, not text.",
            "Arin", "Notice: String is capitalized because it's technically a class, not a primitive type. But it works similarly.",
            "Roran", "So int for whole numbers, double for decimals, boolean for true/false, and String for text.",
            "Arin", "Perfect summary! Now let's see how to DECLARE variables—that means creating them.",
            "Arin", "The syntax is: TYPE variableName = value;",
            "Arin", "Let me show you examples for each type:",
            "Arin", "int lives = 3;  — Creates a variable named 'lives' of type int, and assigns it the value 3.",
            "Arin", "int score = 1000;  — Creates 'score' as an int with value 1000.",
            "Arin", "int health = 100;  — Creates 'health' as an int with value 100.",
            "Roran", "So I write the type first, then the name, then equals, then the value?",
            "Arin", "Exactly! And always end with a semicolon. Let's see double examples:",
            "Arin", "double speed = 2.5;  — Creates a variable named 'speed' with value 2.5.",
            "Arin", "double temperature = 98.6;  — Creates 'temperature' with value 98.6.",
            "Arin", "double price = 19.99;  — Creates 'price' with value 19.99.",
            "Arin", "Now boolean examples:",
            "Arin", "boolean isMage = false;  — Creates 'isMage' and sets it to false.",
            "Arin", "boolean isAlive = true;  — Creates 'isAlive' and sets it to true.",
            "Arin", "boolean hasKey = false;  — Creates 'hasKey' and sets it to false.",
            "Arin", "Finally, String examples:",
            "Arin", "String name = \"Roran\";  — Creates 'name' and assigns it the text \"Roran\".",
            "Arin", "String greeting = \"Hello, traveler!\";  — Creates 'greeting' with a message.",
            "Arin", "String title = \"Codebreaker\";  — Creates 'title' with value \"Codebreaker\".",
            "Arin", "Notice: Strings need DOUBLE QUOTES around the text. Numbers and boolean values do NOT use quotes.",
            "Roran", "What about the semicolon?",
            "Arin", "Always required! Every declaration is a statement, and every statement ends with a semicolon.",
            "Arin", "Forget the semicolon, and your program won't compile. It's one of the most common beginner mistakes.",
            "Arin", "Now, about variable NAMES: they are case-sensitive. 'name' and 'Name' are different variables.",
            "Arin", "Use descriptive names: playerHealth is better than x. maxSpeed is better than y.",
            "Arin", "Names should start with a lowercase letter and use camelCase for multiple words.",
            "Arin", "Good examples: playerScore, isGameOver, maxAttempts, userName, currentLevel.",
            "Arin", "Bad examples: x, y, var1, temp, a, b (not descriptive enough).",
            "Roran", "I can also assign values later, right?",
            "Arin", "Yes! You can declare first, then assign: int count;  count = 10;",
            "Arin", "But it's cleaner to do both at once when possible: int count = 10;",
            "Arin", "Remember: You MUST declare a variable with its type before using it. Java needs to know what type it is.",
            "Arin", "Let me give you a complete program example using all four types:",
            "Arin", "class PlayerStats { public static void main(String[] args) { int health = 100; double speed = 2.5; boolean isAlive = true; String name = \"Roran\"; } }",
            "Roran", "I see! Different types for different kinds of information.",
            "Arin", "Perfect! One more important thing: you can change a variable's value after creating it.",
            "Arin", "For example: int lives = 3;  lives = 2;  lives = 1;  — The value changes, but the type stays int.",
            "Arin", "But you CANNOT change the type: int lives = 3;  lives = \"text\";  — This is an error!",
            "Arin", "Once a variable is declared as int, it can only hold int values. Same for all types.",
            "Arin", "Now, I have a challenge for you. The sacred grove's wards are weakening.",
            "Arin", "To restore them, you must collect four Type Runes: int, double, boolean, and String.",
            "Arin", "But these runes are guarded by ancient knowledge. To collect each rune, you must answer a question about that type.",
            "Arin", "This will test your understanding. Go forth and prove you've mastered the types!"
        ));

        list.Add(CreateConversation("Arin_03_PostTypes",
            "Arin", "Roran! You've returned with all four runes! The wards are stable again.",
            "Roran", "The rune quests forced me to think deeply about when to use each type.",
            "Arin", "That's exactly the point! Understanding when to use each type is as important as knowing they exist.",
            "Arin", "You answered each question correctly. Your understanding of types is solid.",
            "Arin", "One missing semicolon, one wrong type, and everything breaks. That's the nature of syntax.",
            "Arin", "But you didn't make those mistakes. Your mind is adapting to the way programmers think.",
            "Roran", "Every question made me reconsider what I thought I knew.",
            "Arin", "Good! Doubt and questioning lead to deeper understanding.",
            "Arin", "You've proven yourself again, Codebreaker. Well done — we'll move straight into strings and printing.",
            "Arin", "Next, we dive deeper into Strings and learn advanced printing techniques."
        ));

        // ==================== QUEST 4: Strings and Printing ====================
        list.Add(CreateConversation("Arin_04_StringsLecture",
            "Arin", "Welcome back, Roran. Today we master the art of STRINGS and PRINTING.",
            "Roran", "I know strings are text, but what else should I learn?",
            "Arin", "Strings are more powerful than you think. Let's start with STRING LITERALS.",
            "Arin", "A string literal is text wrapped in double quotes: \"Hello, World!\"",
            "Arin", "You can assign it to a variable: String message = \"Welcome, traveler.\";",
            "Arin", "Now, what if you want to include special characters in your strings?",
            "Arin", "That's where ESCAPE SEQUENCES come in. They start with a backslash \\",
            "Arin", "\\n creates a NEW LINE—moves to the next line.",
            "Arin", "Example: System.out.println(\"First line\\nSecond line\");",
            "Arin", "This prints: First line (then goes to new line) Second line.",
            "Arin", "\\t creates a TAB—adds horizontal spacing.",
            "Arin", "Example: System.out.println(\"Name:\\tRoran\");",
            "Arin", "This adds tab space between Name: and Roran.",
            "Arin", "\\\" allows you to include DOUBLE QUOTES inside a string.",
            "Arin", "Example: System.out.println(\"She said, \\\"Hello!\\\" \");",
            "Arin", "This prints: She said, \"Hello!\"",
            "Arin", "\\\\ allows you to print an actual BACKSLASH.",
            "Arin", "Example: System.out.println(\"Path: C:\\\\Users\\\\Roran\");",
            "Arin", "This prints: Path: C:\\Users\\Roran",
            "Roran", "So backslash is the escape character that gives special meaning to the next character.",
            "Arin", "Precisely! Now let's talk about STRING CONCATENATION.",
            "Arin", "Concatenation means joining strings together using the PLUS SIGN +",
            "Arin", "Example: String firstName = \"Roran\"; String fullName = firstName + \" the Brave\";",
            "Arin", "Result: fullName becomes \"Roran the Brave\"",
            "Arin", "You can concatenate multiple strings: \"Hello, \" + \"my \" + \"friend!\"",
            "Arin", "You can also mix strings with variables:",
            "Arin", "int level = 5; System.out.println(\"Level: \" + level);",
            "Arin", "This prints: Level: 5",
            "Arin", "The number is automatically converted to a string and joined.",
            "Roran", "What about print versus println?",
            "Arin", "Excellent question! System.out.println adds a new line after printing.",
            "Arin", "System.out.print does NOT add a new line—the next output continues on the same line.",
            "Arin", "Example: System.out.print(\"Hello \"); System.out.print(\"World\");",
            "Arin", "Prints: Hello World (on one line)",
            "Arin", "But: System.out.println(\"Hello \"); System.out.println(\"World\");",
            "Arin", "Prints: Hello (new line) World",
            "Arin", "Use print when you want to build output on the same line. Use println when you want each output on its own line.",
            "Roran", "This is powerful! I can create formatted output now.",
            "Arin", "Exactly! Strings and escape sequences let you control exactly how your text appears.",
            "Arin", "Now, test your understanding. Answer questions about strings and obtain the Path Barrier Key."
        ));

        list.Add(CreateConversation("Arin_04_PostStrings",
            "Arin", "You've returned with the Path Barrier Key! Excellent, Roran.",
            "Roran", "The string questions were challenging. I had to think about escape sequences carefully.",
            "Arin", "String manipulation is one of the most practical skills a programmer has.",
            "Roran", "I can shape messages exactly how I want them now.",
            "Arin", "You've mastered string literals, escape sequences, and concatenation. Well done!",
            "Arin", "Nearly every program displays text to users. You now know how to do it correctly.",
            "Arin", "You've learned four fundamental concepts: classes, types, variables, and strings.",
            "Arin", "But there is one final trial remaining. The greatest challenge of all.",
            "Roran", "What is it?",
            "Arin", "The Syntax Trial. You will combine everything you've learned and prove your mastery.",
            "Arin", "Ten questions covering all topics. This is your path to becoming a true Codebreaker."
        ));

        // ==================== QUEST 5: The Syntax Trial ====================
        list.Add(CreateConversation("Arin_05_SyntaxRecap",
            "Arin", "Welcome back for the final time, Roran. This is your greatest test in Oak Woods.",
            "Roran", "I'm ready to prove I've learned everything.",
            "Arin", "Let's review everything you've mastered. First: THE CLASS.",
            "Arin", "A class is the container for your code. Every Java program needs at least one class.",
            "Arin", "Syntax: class ClassName { ... }",
            "Arin", "Class names start with a capital letter. Use descriptive names.",
            "Arin", "Next: THE MAIN METHOD.",
            "Arin", "This is the entry point—where program execution begins.",
            "Arin", "Signature: public static void main(String[] args) { ... }",
            "Arin", "You've written this a hundred times now. It's part of your programming DNA.",
            "Arin", "Next: STATEMENTS.",
            "Arin", "Statements are instructions for the computer. They go inside the main method.",
            "Arin", "Every statement ends with a SEMICOLON ;",
            "Arin", "Examples: variable declarations, method calls, assignments.",
            "Arin", "Next: BRACES AND STRUCTURE.",
            "Arin", "Curly braces { } define code blocks. Every opening brace needs a matching closing brace.",
            "Arin", "Proper indentation makes code readable, though Java doesn't require it.",
            "Arin", "Indent code inside braces for clarity.",
            "Arin", "Next: DATA TYPES.",
            "Arin", "int for whole numbers. double for decimals. boolean for true/false. String for text.",
            "Arin", "Declare variables with: type name = value;",
            "Arin", "Choose meaningful names. Use camelCase for multi-word names.",
            "Arin", "Next: STRINGS AND PRINTING.",
            "Arin", "Use System.out.println() to print with a new line. Use System.out.print() to stay on same line.",
            "Arin", "Escape sequences: \\n for new line, \\t for tab, \\\" for quotes, \\\\ for backslash.",
            "Arin", "Concatenate strings with the + operator: \"Hello \" + \"World\"",
            "Arin", "You can concatenate strings with numbers: \"Score: \" + 100",
            "Arin", "Finally: COMMENTS.",
            "Arin", "Comments are notes for humans—Java ignores them when running the program.",
            "Arin", "Single-line comment: // This is a comment",
            "Arin", "Multi-line comment: /* This can span multiple lines */",
            "Arin", "Use comments to explain complex code or leave notes for yourself.",
            "Roran", "I remember all of it. Class, main, types, strings, syntax rules.",
            "Arin", "Excellent. But remembering and applying are two different things.",
            "Arin", "You will now face ten questions. They will test everything—your knowledge, your attention to detail, your understanding.",
            "Arin", "Every symbol matters. Every rule matters. One mistake, and you fail.",
            "Arin", "But I believe in you, Roran. You've proven yourself four times already.",
            "Roran", "I won't disappoint you.",
            "Arin", "I know you won't. Go now. Face the Syntax Trial and show Oak Woods the true measure of your skill.",
            "Arin", "Answer all ten questions correctly, and you will earn the right to deactivate the path barrier.",
            "Arin", "You will become a Codebreaker—one who can read and write the ancient syntax of creation itself."
        ));

        list.Add(CreateConversation("Arin_05_Epilogue",
            "Arin", "Roran... you answered all ten questions perfectly. Your mastery is complete.",
            "Roran", "I proved I could do it. I understand Java now—truly understand it.",
            "Arin", "You've done more than understand. You've mastered the fundamentals.",
            "Arin", "You understand classes, methods, data types, variables, strings, and syntax rules.",
            "Roran", "I can finally read the forest's words—the language of code.",
            "Arin", "You've come far in a short time. What once seemed impossible is now second nature.",
            "Roran", "What comes next?",
            "Arin", "Beyond Oak Woods, the world is vast and waiting for you.",
            "Arin", "You'll learn control flow—making decisions with if statements, repeating actions with loops.",
            "Arin", "You'll master methods, arrays, objects, and more. But you've proven you can handle it.",
            "Arin", "Oak Woods recognizes you now, Roran. You are officially a Codebreaker.",
            "Arin", "The path barrier will fall. Other places will call to you. Other languages will tempt you.",
            "Roran", "But I won't forget Oak Woods. Or you.",
            "Arin", "I know. And if you ever return, I'll be here, mentor to mentor.",
            "Arin", "Syntax is your compass. Keep practicing, keep coding, and never stop learning.",
            "Arin", "The world of programming is vast. You've taken your first steps. Walk forward with confidence.",
            "Roran", "Thank you, Arin. For everything.",
            "Arin", "Thank you, Roran. For reminding me why I love teaching.",
            "Arin", "Farewell, Roran. Walk through the portal with pride.",
            "Roran", "Goodbye, mentor."
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
        {
            // UI already assigned - just ensure button listener is set up
            SetupNextButtonListener();
            return;
        }

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

        SetupNextButtonListener();
    }

    // CRITICAL: Properly setup Next button listener
    private void SetupNextButtonListener()
    {
        if (nextButton != null)
        {
            // Remove any existing listeners first
            nextButton.onClick.RemoveAllListeners();
            // Add the Next method
            nextButton.onClick.AddListener(Next);
            Debug.Log("[DialogueManager] Next button listener configured");
        }
    }

    // UPDATED: Show with synchronized fade for panel and decorations
    private void Show()
    {
        StartCoroutine(ShowDialogueCoroutine());
    }

    private IEnumerator ShowDialogueCoroutine()
    {
        if (panel != null) panel.gameObject.SetActive(true);

        // Activate decorations
        SetDecorationsVisibility(true);

        // Fade in panel and decorations simultaneously
        yield return StartCoroutine(FadePanelAndDecorations(0f, 1f, fadeInDuration));
    }

    // UPDATED: Hide with synchronized fade
    private void Hide()
    {
        StartCoroutine(HideDialogueCoroutine());
    }

    private IEnumerator HideDialogueCoroutine()
    {
        // Fade out panel and decorations simultaneously
        yield return StartCoroutine(FadePanelAndDecorations(1f, 0f, fadeOutDuration));

        if (panel != null) panel.gameObject.SetActive(false);

        // Hide decorations
        SetDecorationsVisibility(false);
    }

    // NEW: Synchronized fade for panel and decorations (matching Quest UI pattern)
    private IEnumerator FadePanelAndDecorations(float startAlpha, float endAlpha, float duration)
    {
        if (panel == null) yield break;

        float elapsed = 0f;

        // Set BOTH panel and decorations to starting alpha BEFORE loop
        panel.alpha = startAlpha;
        SetDecorationsAlpha(startAlpha);

        panel.interactable = (endAlpha > 0.5f);
        panel.blocksRaycasts = (endAlpha > 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);

            // Apply SAME alpha to both panel and decorations simultaneously
            panel.alpha = currentAlpha;
            SetDecorationsAlpha(currentAlpha);

            yield return null;
        }

        // Ensure final alpha values are set
        panel.alpha = endAlpha;
        SetDecorationsAlpha(endAlpha);

        panel.interactable = (endAlpha > 0.5f);
        panel.blocksRaycasts = (endAlpha > 0.5f);

        Debug.Log($"[DialogueManager] Synchronized fade completed: Panel and decorations alpha = {endAlpha}");
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
        if (current == null)
        {
            Hide();
            return;
        }

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

    // NEW: Public method to manually reinitialize button (if needed)
    public void ReinitializeNextButton()
    {
        SetupNextButtonListener();
        Debug.Log("[DialogueManager] Next button reinitialized");
    }
}