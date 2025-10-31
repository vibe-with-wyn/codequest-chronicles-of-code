#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class OakWoodsContentInstaller
{
    private const string ResourcesFolder = "Assets/Resources/OakWoods";
    private const string QuestDbPath = ResourcesFolder + "/QuestDatabase_OakWoods.asset";
    private const string DialogueDbPath = ResourcesFolder + "/DialogueDatabase_OakWoods.asset";

    [MenuItem("Tools/OakWoods/Generate Oak Woods Content")]
    public static void GenerateAll()
    {
        EnsureFolders();

        var questDb = ScriptableObject.CreateInstance<QuestDatabase>();
        PopulateQuestDatabase(questDb);
        AssetDatabase.CreateAsset(questDb, QuestDbPath);
        EditorUtility.SetDirty(questDb);

        var dialogueDb = ScriptableObject.CreateInstance<DialogueDatabase>();
        PopulateDialogueDatabase(dialogueDb);
        AssetDatabase.CreateAsset(dialogueDb, DialogueDbPath);
        EditorUtility.SetDirty(dialogueDb);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("OakWoods content generated.\nAssign QuestDatabase_OakWoods.asset to QuestManager.questDatabase in the scene.");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "OakWoods");
    }

    private static void PopulateQuestDatabase(QuestDatabase db)
    {
        var so = new SerializedObject(db);
        var questsProp = so.FindProperty("allQuests");
        questsProp.arraySize = 0;

        // Q1
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q1_SyntaxCave", "Into the Skull Cave",
                "Find the Skull Cave, aid the Water Magician, then defeat the Night Borne.", "Q2_HelloJava");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 3;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Find the Skull Cave", "Reach the skull-shaped cave entrance.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Help the Water Magician", "Assist Arin as the battle begins. (Auto when combat starts)");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Defeat the Night Borne", "Defeat the cave boss.");
        }

        // Q2
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q2_HelloJava", "Hello, Java!",
                "Learn what Java is, how programs start, and print a message.", "Q3_Types");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 3;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Follow Arin to Her Cabin", "Go to Arin's cottage to begin your first lesson.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Listen to Arin's Lesson: Java Basics", "Learn class, main method, braces, semicolons, and System.out.println.");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Complete the Hello World Altar", "Fill in class, main signature, and a println statement.");
        }

        // Q3 - UPDATED with lecture first
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q3_Types", "The Language of Types",
                "Learn basic data types and variable declarations to restore the grove's wards.", "Q4_StringsPrinting");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 4;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Meet Arin at the Sacred Grove", "Travel to the Sacred Grove to begin your types lesson.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Listen to Arin's Lesson: Data Types", "Learn about int, double, boolean, String, and variable declarations.");
            
            var obj2 = objectives.GetArrayElementAtIndex(2);
            SetObjective(obj2, "Collect the Type Runes (3)", "Gather runes labeled int, boolean, and String.");
            obj2.FindPropertyRelative("targetCount").intValue = 3;

            SetObjective(objectives.GetArrayElementAtIndex(3), "Fix the Declarations Tablet", "Write valid variable declarations with correct types, names, and semicolons.");
        }

        // Q4 - UPDATED with lecture first
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q4_StringsPrinting", "Strings and Printing",
                "Master text manipulation: string literals, escape sequences, and concatenation.", "Q5_SyntaxTrial");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 4;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Find Arin at the Echo Chamber", "Locate Arin at the mystical Echo Chamber.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Listen to Arin's Lesson: Strings and Printing", "Learn string literals, escape sequences (\\n, \\t, \\\", \\\\), and concatenation with +.");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Fix the Print Lines", "Fill in println calls that concatenate text and show escape sequences.");
            SetObjective(objectives.GetArrayElementAtIndex(3), "Activate the Echo Stone", "Use the Echo Stone to verify your printed lines are correct.");
        }

        // Q5 - UPDATED with lecture first
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q5_SyntaxTrial", "The Syntax Trial",
                "Prove your mastery by assembling a complete Java class with all learned concepts.", "");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 4;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Approach the Trial Monument", "Stand before the ancient Trial Monument at the forest edge.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Listen to Arin's Final Recap", "Review all Java syntax concepts: class structure, main method, types, strings, and statements.");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Complete the Syntax Trial Tablet", "Write a complete Java program with declarations, string concatenation, and println (with proper syntax).");
            SetObjective(objectives.GetArrayElementAtIndex(3), "Receive Arin's Blessing", "Conclude the Oak Woods chapter and earn the title of Codebreaker.");
        }

        so.ApplyModifiedProperties();
    }

    private static SerializedProperty AddQuest(SerializedProperty listProp)
    {
        int index = listProp.arraySize;
        listProp.InsertArrayElementAtIndex(index);
        var questProp = listProp.GetArrayElementAtIndex(index);
        questProp.FindPropertyRelative("isCompleted").boolValue = false;
        questProp.FindPropertyRelative("isActive").boolValue = false;
        questProp.FindPropertyRelative("questImage").objectReferenceValue = null;
        questProp.FindPropertyRelative("objectives").arraySize = 0;
        questProp.FindPropertyRelative("nextQuestId").stringValue = "";
        return questProp;
    }

    private static void SetQuestCore(SerializedProperty questProp, string id, string title, string desc, string nextQuestId)
    {
        questProp.FindPropertyRelative("questId").stringValue = id;
        questProp.FindPropertyRelative("questTitle").stringValue = title;
        questProp.FindPropertyRelative("questDescription").stringValue = desc;
        questProp.FindPropertyRelative("nextQuestId").stringValue = nextQuestId;
    }

    private static void SetObjective(SerializedProperty objProp, string title, string desc)
    {
        objProp.FindPropertyRelative("objectiveTitle").stringValue = title;
        objProp.FindPropertyRelative("objectiveDescription").stringValue = desc;
        objProp.FindPropertyRelative("isCompleted").boolValue = false;
        objProp.FindPropertyRelative("isActive").boolValue = false;
        objProp.FindPropertyRelative("isOptional").boolValue = false;
        objProp.FindPropertyRelative("targetCount").intValue = 1;
        objProp.FindPropertyRelative("currentCount").intValue = 0;
    }

    private static void PopulateDialogueDatabase(DialogueDatabase db)
    {
        var list = new List<Conversation>();

        // ==================== QUEST 1 ====================
        list.Add(Convo("Arin_01_PostBoss",
            L("Arin", "You fought bravely. Not many step into this cave and walk back out."),
            L("Roran", "You looked like you needed help."),
            L("Arin", "I did. Thank you. You don't look like a local. What brings you to Oak Woods?"),
            L("Roran", "I'm new to this land. I'm searching for knowledge—and a way to survive."),
            L("Arin", "Knowledge, you say? Then fate has brought you to the right place."),
            L("Arin", "Here in Oak Woods, we don't wield swords or cast spells like other lands."),
            L("Arin", "We command something far more powerful—the language of creation itself."),
            L("Roran", "The language of creation?"),
            L("Arin", "Code. Here, code is more than ink on parchment—it shapes reality, commands the elements, bends the very fabric of this world."),
            L("Roran", "I've heard whispers of such power, but I never believed it was real."),
            L("Arin", "Oh, it's real. The Night Borne you just helped me defeat? Born from corrupted code fragments."),
            L("Arin", "The wards protecting our villages? Written in precise syntax. Even the trees respond to properly structured commands."),
            L("Roran", "That's... incredible. But I don't know where to begin. I've never written a line of code in my life."),
            L("Arin", "Everyone starts somewhere. I was once like you—lost, uncertain, overwhelmed by symbols I didn't understand."),
            L("Arin", "But a mentor took me in, just as I will take you in now."),
            L("Roran", "You would teach me?"),
            L("Arin", "You saved my life in that battle. The least I can do is help you save your own future."),
            L("Arin", "Besides, Oak Woods needs more Codebreakers—those who can read and write the ancient syntax."),
            L("Roran", "Codebreakers... I like the sound of that."),
            L("Arin", "Then come. My cabin isn't far from here. We'll start with the foundations."),
            L("Arin", "First, I'll teach you Java—one of the oldest and most respected tongues in the realm of code."),
            L("Roran", "Java. I'm ready to learn."),
            L("Arin", "Good. But heed my warning: the path of a programmer is not easy."),
            L("Arin", "Every symbol matters. One misplaced semicolon, one unclosed brace, and your creation crumbles."),
            L("Arin", "The forest itself will test you. It recognizes syntax, rewards precision, and punishes carelessness."),
            L("Roran", "I understand. I won't let you down."),
            L("Arin", "I believe you won't. Now follow me closely—and stay on the path."),
            L("Arin", "Oak Woods tests syntax more than strength. Wander off, and the trees might just compile you into something... unpleasant."),
            L("Roran", "Noted. Staying on the path."),
            L("Arin", "Smart choice. Come, the cabin awaits. Your journey as a Codebreaker begins today.")
        ));

        // ==================== QUEST 2: Hello Java ====================
        list.Add(Convo("Arin_02_JavaBasics",
            L("Arin", "Welcome to my cabin. Let's begin your journey into the world of Java programming."),
            L("Roran", "I'm ready to learn."),
            L("Arin", "Java is a powerful programming language used to build applications—from mobile apps to large enterprise systems."),
            L("Arin", "Every Java program starts with a CLASS. Think of a class as a container that holds your code."),
            L("Arin", "The class name should start with a capital letter. For example: HelloWorld, MyProgram, or Calculator."),
            L("Arin", "Inside a class, you need a special method called MAIN. This is where your program begins execution."),
            L("Arin", "The exact signature you must memorize is:"),
            L("Arin", "public static void main(String[] args)"),
            L("Roran", "That's a lot of words. What do they mean?"),
            L("Arin", "PUBLIC means it can be accessed from anywhere. STATIC means it belongs to the class itself, not an instance."),
            L("Arin", "VOID means it doesn't return any value. And String[] args allows you to pass text arguments when running the program."),
            L("Arin", "For now, just remember the pattern. Understanding will come with practice."),
            L("Arin", "Next: CURLY BRACES. They group blocks of code together. Every opening brace { needs a closing brace }."),
            L("Arin", "Your class starts with { and ends with }. Your main method also starts with { and ends with }."),
            L("Arin", "Inside the main method, you write STATEMENTS—instructions for the computer to execute."),
            L("Arin", "Every statement must end with a SEMICOLON ;  Think of it as a period ending a sentence."),
            L("Roran", "So: class, main method, braces, and semicolons. Got it."),
            L("Arin", "Good! Now, to display text output, use: System.out.println(\"your message\");"),
            L("Arin", "println stands for 'print line'—it prints text and moves to a new line."),
            L("Arin", "The text goes inside DOUBLE QUOTES, like this: \"Hello, Oak Woods!\""),
            L("Arin", "Remember: System.out.println with parentheses, quotes around text, and a semicolon at the end."),
            L("Roran", "System.out.println, quotes, semicolon. I understand."),
            L("Arin", "Perfect. Here's what a complete program looks like:"),
            L("Arin", "class HelloWorld { public static void main(String[] args) { System.out.println(\"Hello!\"); } }"),
            L("Arin", "Now, practice at the Hello World Altar. Fill in the missing parts to create your first program.")
        ));

        list.Add(Convo("Arin_02b_PostHello",
            L("Arin", "Excellent work! Your first program prints a message."),
            L("Roran", "So many symbols for just one sentence."),
            L("Arin", "Symbols give structure; structure gives power. Every character matters in programming."),
            L("Arin", "Next, we'll learn how to store values using variables and data types.")
        ));

        // ==================== QUEST 3: Data Types ====================
        list.Add(Convo("Arin_03_TypesLecture",
            L("Arin", "Welcome to the Sacred Grove. This is where we learn about DATA TYPES and VARIABLES."),
            L("Roran", "What are variables?"),
            L("Arin", "Variables are containers that store information. Each variable has a TYPE and a NAME."),
            L("Arin", "The TYPE defines what kind of data the variable can hold. The NAME is how you identify it."),
            L("Arin", "Java has several primitive data types. Let me explain the most important ones:"),
            L("Arin", "INT stores whole numbers—positive, negative, or zero. Examples: 5, -10, 0, 1000."),
            L("Arin", "Use int for things like: player health, score, age, quantity."),
            L("Arin", "DOUBLE stores decimal numbers—numbers with fractional parts. Examples: 3.14, -0.5, 2.0."),
            L("Arin", "Use double for: speed, temperature, money amounts, precise calculations."),
            L("Arin", "BOOLEAN stores true or false values—only two possibilities."),
            L("Arin", "Use boolean for: isAlive, hasKey, gameOver, canJump—any yes/no condition."),
            L("Arin", "STRING stores text—sequences of characters. Examples: \"Hello\", \"Roran\", \"Oak Woods\"."),
            L("Arin", "Notice: String is capitalized because it's technically a class, not a primitive type."),
            L("Roran", "So int for whole numbers, double for decimals, boolean for true/false, and String for text."),
            L("Arin", "Exactly! Now let's see how to DECLARE variables—that means creating them."),
            L("Arin", "The syntax is: TYPE variableName = value;"),
            L("Arin", "For example: int lives = 3;"),
            L("Arin", "This creates a variable named 'lives' of type int, and assigns it the value 3."),
            L("Arin", "More examples:"),
            L("Arin", "double speed = 2.5;"),
            L("Arin", "boolean isMage = false;"),
            L("Arin", "String name = \"Roran\";"),
            L("Arin", "Notice: Strings need DOUBLE QUOTES around the text. Numbers and boolean values do NOT use quotes."),
            L("Roran", "What about the semicolon?"),
            L("Arin", "Always required! Every declaration is a statement, and every statement ends with a semicolon."),
            L("Arin", "Variable names are case-sensitive: 'name' and 'Name' are different variables."),
            L("Arin", "Use descriptive names: playerHealth is better than x. maxSpeed is better than y."),
            L("Arin", "Names should start with a lowercase letter and use camelCase for multiple words."),
            L("Arin", "Examples: playerScore, isGameOver, maxAttempts, userName."),
            L("Roran", "I can also assign values later, right?"),
            L("Arin", "Yes! You can declare first, then assign: int count;  count = 10;"),
            L("Arin", "But it's cleaner to do both at once when possible: int count = 10;"),
            L("Arin", "Remember: You MUST declare a variable with its type before using it."),
            L("Arin", "Now, the grove's wards are unstable. Collect three Type Runes: int, boolean, and String."),
            L("Arin", "Then use them at the Declarations Tablet to restore balance.")
        ));

        list.Add(Convo("Arin_03_PostTypes",
            L("Arin", "The wards are stable again. You've mastered the fundamental types!"),
            L("Roran", "One missing semicolon nearly broke everything."),
            L("Arin", "That's the nature of syntax—every symbol matters. Even one mistake can prevent your program from running."),
            L("Arin", "But don't be discouraged. Every programmer makes syntax errors. You'll get better with practice."),
            L("Arin", "Next, we'll dive deeper into Strings and learn advanced printing techniques.")
        ));

        // ==================== QUEST 4: Strings and Printing ====================
        list.Add(Convo("Arin_04_StringsLecture",
            L("Arin", "Welcome to the Echo Chamber. Here, we master the art of STRINGS and PRINTING."),
            L("Roran", "I know strings are text, but what else should I learn?"),
            L("Arin", "Strings are more powerful than you think. Let's start with STRING LITERALS."),
            L("Arin", "A string literal is text wrapped in double quotes: \"Hello, World!\""),
            L("Arin", "You can assign it to a variable: String message = \"Welcome, traveler.\";"),
            L("Arin", "Now, what if you want to include special characters in your strings?"),
            L("Arin", "That's where ESCAPE SEQUENCES come in. They start with a backslash \\"),
            L("Arin", "\\n creates a NEW LINE—moves to the next line."),
            L("Arin", "Example: System.out.println(\"First line\\nSecond line\");"),
            L("Arin", "This prints: First line (then goes to new line) Second line."),
            L("Arin", "\\t creates a TAB—adds horizontal spacing."),
            L("Arin", "Example: System.out.println(\"Name:\\tRoran\");"),
            L("Arin", "This adds tab space between Name: and Roran."),
            L("Arin", "\\\" allows you to include DOUBLE QUOTES inside a string."),
            L("Arin", "Example: System.out.println(\"She said, \\\"Hello!\\\" \");"),
            L("Arin", "This prints: She said, \"Hello!\""),
            L("Arin", "\\\\ allows you to print an actual BACKSLASH."),
            L("Arin", "Example: System.out.println(\"Path: C:\\\\Users\\\\Roran\");"),
            L("Arin", "This prints: Path: C:\\Users\\Roran"),
            L("Roran", "So backslash is the escape character that gives special meaning to the next character."),
            L("Arin", "Precisely! Now let's talk about STRING CONCATENATION."),
            L("Arin", "Concatenation means joining strings together using the PLUS SIGN +"),
            L("Arin", "Example: String firstName = \"Roran\"; String fullName = firstName + \" the Brave\";"),
            L("Arin", "Result: fullName becomes \"Roran the Brave\""),
            L("Arin", "You can concatenate multiple strings: \"Hello, \" + \"my \" + \"friend!\""),
            L("Arin", "You can also mix strings with variables:"),
            L("Arin", "int level = 5; System.out.println(\"Level: \" + level);"),
            L("Arin", "This prints: Level: 5"),
            L("Arin", "The number is automatically converted to a string and joined."),
            L("Roran", "What about print versus println?"),
            L("Arin", "Excellent question! System.out.println adds a new line after printing."),
            L("Arin", "System.out.print does NOT add a new line—the next output continues on the same line."),
            L("Arin", "Example: System.out.print(\"Hello \"); System.out.print(\"World\");"),
            L("Arin", "Prints: Hello World (on one line)"),
            L("Arin", "But: System.out.println(\"Hello \"); System.out.println(\"World\");"),
            L("Arin", "Prints: Hello (new line) World"),
            L("Arin", "Use print when you want to build output on the same line. Use println when you want each output on its own line."),
            L("Roran", "This is powerful! I can create formatted output now."),
            L("Arin", "Exactly! Strings and escape sequences let you control exactly how your text appears."),
            L("Arin", "Now, fix the broken print statements at the Echo Chamber, then activate the Echo Stone to verify your work.")
        ));

        list.Add(Convo("Arin_04_PostStrings",
            L("Arin", "Perfect! The Echo Stone resonates with your corrected output."),
            L("Roran", "I can shape messages exactly how I want them now."),
            L("Arin", "String manipulation is essential. Nearly every program displays text to users."),
            L("Arin", "You've learned literals, escape sequences, and concatenation. Well done!"),
            L("Arin", "One final trial remains: the Syntax Trial, where you'll combine everything you've learned.")
        ));

        // ==================== QUEST 5: Syntax Trial ====================
        list.Add(Convo("Arin_05_SyntaxRecap",
            L("Arin", "Stand before the Trial Monument, Roran. This is your final test in Oak Woods."),
            L("Roran", "I'm ready."),
            L("Arin", "Let's review everything you've learned. First: THE CLASS."),
            L("Arin", "A class is the container for your code. Every Java program needs at least one class."),
            L("Arin", "Syntax: class ClassName { ... }"),
            L("Arin", "Class names start with a capital letter. Use descriptive names."),
            L("Arin", "Next: THE MAIN METHOD."),
            L("Arin", "This is the entry point—where program execution begins."),
            L("Arin", "Signature: public static void main(String[] args) { ... }"),
            L("Arin", "Memorize this exact format. Every standalone Java program needs it."),
            L("Arin", "Next: STATEMENTS."),
            L("Arin", "Statements are instructions for the computer. They go inside the main method."),
            L("Arin", "Every statement ends with a SEMICOLON ;"),
            L("Arin", "Examples: variable declarations, method calls, assignments."),
            L("Arin", "Next: BRACES AND STRUCTURE."),
            L("Arin", "Curly braces { } define code blocks. Every opening brace needs a matching closing brace."),
            L("Arin", "Proper indentation makes code readable, though Java doesn't require it."),
            L("Arin", "Indent code inside braces for clarity."),
            L("Arin", "Next: DATA TYPES."),
            L("Arin", "int for whole numbers. double for decimals. boolean for true/false. String for text."),
            L("Arin", "Declare variables with: type name = value;"),
            L("Arin", "Choose meaningful names. Use camelCase for multi-word names."),
            L("Arin", "Next: STRINGS AND PRINTING."),
            L("Arin", "Use System.out.println() to print with a new line. Use System.out.print() to stay on same line."),
            L("Arin", "Escape sequences: \\n for new line, \\t for tab, \\\" for quotes, \\\\ for backslash."),
            L("Arin", "Concatenate strings with the + operator: \"Hello \" + \"World\""),
            L("Arin", "You can concatenate strings with numbers: \"Score: \" + 100"),
            L("Arin", "Finally: COMMENTS."),
            L("Arin", "Comments are notes for humans—Java ignores them when running the program."),
            L("Arin", "Single-line comment: // This is a comment"),
            L("Arin", "Multi-line comment: /* This can span multiple lines */"),
            L("Arin", "Use comments to explain complex code or leave notes for yourself."),
            L("Roran", "I remember all of it. Class, main, types, strings, syntax rules."),
            L("Arin", "Excellent. Now prove it. At the Syntax Trial Tablet, you must write a complete Java program."),
            L("Arin", "Include: a class, the main method, variable declarations, string concatenation, and println statements."),
            L("Arin", "Every brace must match. Every statement must end with a semicolon. All syntax must be perfect."),
            L("Arin", "This is your test, Codebreaker. Show me what you've learned.")
        ));

        list.Add(Convo("Arin_05_Epilogue",
            L("Arin", "Remarkable! Your program compiled perfectly. You've mastered the basics of Java syntax."),
            L("Roran", "I can finally read the forest's words—the language of code."),
            L("Arin", "You've come far in a short time. You understand classes, methods, types, strings, and syntax rules."),
            L("Arin", "These fundamentals are your foundation. Everything else in Java builds upon them."),
            L("Roran", "What comes next?"),
            L("Arin", "Beyond Oak Woods, you'll learn control flow—making decisions with if statements, repeating actions with loops."),
            L("Arin", "You'll master methods, arrays, objects, and more. But you've proven you can handle it."),
            L("Arin", "Oak Woods recognizes you now, Roran. You are officially a Codebreaker."),
            L("Roran", "Thank you, Arin. I couldn't have done this without your guidance."),
            L("Arin", "Syntax is your compass. Keep practicing, keep coding, and never stop learning."),
            L("Arin", "The world of programming is vast. You've taken your first steps. Walk forward with confidence."),
            L("Roran", "I will. Thank you, mentor."),
            L("Arin", "If fate brings you back to Oak Woods, I'll be here. For now, go forth and code, Codebreaker.")
        ));

        db.SetConversations(list);
    }

    private static Conversation Convo(string id, params DialogueLine[] lines)
    {
        return new Conversation { conversationId = id, lines = new List<DialogueLine>(lines) };
    }

    private static DialogueLine L(string speaker, string text)
    {
        return new DialogueLine { speaker = speaker, text = text };
    }
}
#endif