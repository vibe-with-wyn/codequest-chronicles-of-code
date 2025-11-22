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
                "Learn what Java is, how programs start, and print a message. Answer comprehension questions about Java basics.", "Q3_Types");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 3;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Follow Arin to Her Cabin", "Go to Arin's cottage to begin your first lesson on Java fundamentals.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Listen to Arin's Lesson: Java Basics", "Learn how classes work, the main method, braces, semicolons, and System.out.println. Pay close attention—you'll be tested!");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Activate the Hello World Altar", "Answer questions about Java basics: class structure, the main method, proper syntax. Fill in code to create your first working program.");
        }

        // Q3
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q3_Types", "The Language of Types",
                "Learn basic data types and variable declarations. Collect the four Type Runes by answering questions about int, double, boolean, and String.", "Q4_StringsPrinting");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 3;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Return to Arin's Cabin", "Go back to Arin's cabin to learn about primitive data types in Java.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Listen to Arin's Lesson: Data Types", "Learn about int (whole numbers), double (decimals), boolean (true/false), and String (text). Understand when to use each type.");

            var obj2 = objectives.GetArrayElementAtIndex(2);
            SetObjective(obj2, "Collect the Type Runes (4)", "Find and answer questions for each rune: int rune, double rune, boolean rune, and String rune. Each question tests your understanding of that data type.");
            obj2.FindPropertyRelative("targetCount").intValue = 4;
        }

        // Q4
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q4_StringsPrinting", "Strings and Printing",
                "Master text manipulation: string literals, escape sequences, concatenation, and formatted output.", "Q5_SyntaxTrial");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 3;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Return to Arin's Cabin", "Go back to Arin's cabin to learn about strings and advanced printing techniques.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Listen to Arin's Lesson: Strings and Printing", "Learn string literals in double quotes, escape sequences (\\n, \\t, \\\", \\\\), concatenation with +, and the difference between print and println.");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Obtain the Path Barrier Key", "Answer questions about strings and printing. Demonstrate understanding of escape sequences and string concatenation to unlock the key.");
        }

        // Q5
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q5_SyntaxTrial", "The Syntax Trial",
                "Prove your mastery by answering 10 comprehensive questions covering all Java concepts learned: classes, methods, types, strings, and syntax rules.", "");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 3;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Return to Arin's Cabin", "Go back to Arin's cabin for a final review of all Java syntax concepts you've learned.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Listen to Arin's Final Recap", "Review all Java syntax: class structure, main method, data types (int, double, boolean, String), variable declarations, strings, escape sequences, concatenation, and println.");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Complete the Syntax Trial", "Answer 10 comprehensive questions covering all Java topics. This is your final test before becoming a true Codebreaker. Answer all questions correctly to deactivate the path barrier.");
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

        // ==================== QUEST 1: Into the Skull Cave ====================
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

        // ==================== QUEST 2: Hello, Java! ====================
        list.Add(Convo("Arin_02_JavaBasics",
            L("Arin", "Welcome to my cabin. Let's begin your journey into the world of Java programming."),
            L("Roran", "I'm ready to learn."),
            L("Arin", "Java is a powerful programming language used to build applications—from mobile apps to large enterprise systems."),
            L("Arin", "It's one of the most popular languages in the world. Banks, games, websites—many rely on Java."),
            L("Arin", "Every Java program starts with a CLASS. Think of a class as a container that holds your code."),
            L("Arin", "The class name should start with a capital letter. For example: HelloWorld, MyProgram, or Calculator."),
            L("Roran", "So every program needs a class?"),
            L("Arin", "Exactly! Without a class, Java won't even recognize your code. It's the foundation."),
            L("Arin", "Inside a class, you need a special method called MAIN. This is where your program begins execution."),
            L("Arin", "The exact signature you must memorize is:"),
            L("Arin", "public static void main(String[] args)"),
            L("Roran", "That's a lot of words. What do they mean?"),
            L("Arin", "PUBLIC means it can be accessed from anywhere. STATIC means it belongs to the class itself, not an instance."),
            L("Arin", "VOID means it doesn't return any value. And String[] args allows you to pass text arguments when running the program."),
            L("Arin", "For now, just remember the pattern. Understanding will come with practice."),
            L("Roran", "So I just need to write it exactly like that every time?"),
            L("Arin", "Yes! Every standalone Java program needs this exact main method. Think of it as the starting gate."),
            L("Arin", "Next: CURLY BRACES. They group blocks of code together. Every opening brace { needs a closing brace }."),
            L("Arin", "Your class starts with { and ends with }. Your main method also starts with { and ends with }."),
            L("Arin", "Think of braces like parentheses in math—they show what belongs together."),
            L("Roran", "So braces organize the code into sections?"),
            L("Arin", "Precisely! They define scope—what code belongs to the class, what belongs to the method."),
            L("Arin", "Inside the main method, you write STATEMENTS—instructions for the computer to execute."),
            L("Arin", "Every statement must end with a SEMICOLON ;  Think of it as a period ending a sentence."),
            L("Arin", "Without the semicolon, Java won't know where one instruction ends and another begins."),
            L("Roran", "So: class, main method, braces, and semicolons. Got it."),
            L("Arin", "Good! Now, to display text output, use: System.out.println(\"your message\");"),
            L("Arin", "println stands for 'print line'—it prints text and moves to a new line."),
            L("Arin", "The text goes inside DOUBLE QUOTES, like this: \"Hello, Oak Woods!\""),
            L("Arin", "Let me show you some examples:"),
            L("Arin", "System.out.println(\"Welcome, traveler!\");  prints: Welcome, traveler!"),
            L("Arin", "System.out.println(\"Java is powerful!\");  prints: Java is powerful!"),
            L("Arin", "System.out.println(\"Learning to code!\");  prints: Learning to code!"),
            L("Roran", "So I can print any message I want?"),
            L("Arin", "Exactly! Just put it in quotes, inside the parentheses, and end with a semicolon."),
            L("Arin", "Remember: System.out.println with parentheses, quotes around text, and a semicolon at the end."),
            L("Arin", "Here are more examples to help you understand:"),
            L("Arin", "System.out.println(\"My name is Roran\");"),
            L("Arin", "System.out.println(\"I am learning Java\");"),
            L("Arin", "System.out.println(\"This is my first program\");"),
            L("Roran", "System.out.println, quotes, semicolon. I understand."),
            L("Arin", "Perfect. Now let me show you what a complete program looks like:"),
            L("Arin", "class HelloWorld { public static void main(String[] args) { System.out.println(\"Hello!\"); } }"),
            L("Arin", "Let's break that down piece by piece:"),
            L("Arin", "First: class HelloWorld {  — This starts our class and opens its code block."),
            L("Arin", "Second: public static void main(String[] args) {  — This starts our main method."),
            L("Arin", "Third: System.out.println(\"Hello!\");  — This prints our message."),
            L("Arin", "Fourth: }  — This closes the main method."),
            L("Arin", "Fifth: }  — This closes the class."),
            L("Roran", "So every brace that opens must close?"),
            L("Arin", "Yes! If you forget to close a brace, your program won't compile. It's a common mistake."),
            L("Arin", "Here's another complete example:"),
            L("Arin", "class Welcome { public static void main(String[] args) { System.out.println(\"Welcome to Oak Woods!\"); } }"),
            L("Arin", "And one more to make sure you understand:"),
            L("Arin", "class MyFirstProgram { public static void main(String[] args) { System.out.println(\"I am a programmer!\"); } }"),
            L("Roran", "I see the pattern now. Class name, main method, print statement, close everything."),
            L("Arin", "Excellent! You're grasping the structure. That's the hardest part for beginners."),
            L("Arin", "Now, test your understanding at the Hello World Altar. You'll answer questions about Java basics."),
            L("Arin", "Remember everything: class structure, the exact main signature, println syntax, and brace matching.")
        ));

        // ==================== QUEST 3: The Language of Types ====================
        // MERGED: Arin_02b_PostHello + Arin_03_TypesLecture
        list.Add(Convo("Arin_03_TypesLecture",
            // POST QUEST 2 TRANSITION (from Arin_02b_PostHello)
            L("Arin", "You returned from the altar! I see the light of success in your eyes, Roran."),
            L("Roran", "Your lessons were clear. The questions tested everything you taught me."),
            L("Arin", "Excellent work! You've passed the first true test. Your first program runs correctly."),
            L("Arin", "So many symbols for just one sentence, you said. But now you understand why they matter."),
            L("Roran", "Symbols give structure; structure gives power. You were right."),
            L("Arin", "Every character serves a purpose. Missing one breaks everything. That's the nature of code."),
            L("Arin", "You've learned the foundation: classes, methods, statements, braces, and semicolons."),
            L("Arin", "Now comes the challenge. Most programmers stop here. But you'll go deeper."),
            L("Roran", "What's next?"),
            L("Arin", "Data. Today we learn about DATA TYPES and VARIABLES—the building blocks of programming."),
            // QUEST 3 LECTURE BEGINS
            L("Roran", "What are variables?"),
            L("Arin", "Variables are containers that store information. Think of them as labeled boxes that hold different kinds of data."),
            L("Arin", "Each variable has a TYPE and a NAME. The TYPE defines what kind of data it can hold. The NAME is how you identify it."),
            L("Roran", "Why do we need different types?"),
            L("Arin", "Because different data serves different purposes. You wouldn't store a player's name the same way you store their health points."),
            L("Arin", "Java has several primitive data types. Let me explain the four most important ones in detail:"),
            L("Arin", "First: INT. This stores whole numbers—positive, negative, or zero."),
            L("Arin", "Examples of int values: 5, -10, 0, 1000, 42, -999"),
            L("Arin", "Use int for things like: player health, score, age, quantity, level, lives remaining."),
            L("Roran", "So int is for counting things?"),
            L("Arin", "Exactly! Anything you can count without fractions: 10 apples, 3 lives, 500 gold coins."),
            L("Arin", "But remember: int cannot store decimals. 3.5 is NOT a valid int—it would need to be 3 or 4."),
            L("Arin", "Second: DOUBLE. This stores decimal numbers—numbers with fractional parts."),
            L("Arin", "Examples of double values: 3.14, -0.5, 2.0, 99.99, 1.5, 0.001"),
            L("Arin", "Use double for: speed, temperature, money amounts, precise calculations, percentages."),
            L("Roran", "So when I need the exact value with decimals?"),
            L("Arin", "Precisely! If a potion restores 2.5 health per second, you need double, not int."),
            L("Arin", "If your character moves at 3.75 units per second, you need double."),
            L("Arin", "Third: BOOLEAN. This stores true or false values—only two possibilities."),
            L("Arin", "A boolean is like a yes/no question. It can only be true OR false, never anything else."),
            L("Arin", "Examples: true, false (those are the ONLY two values a boolean can have)"),
            L("Arin", "Use boolean for: isAlive, hasKey, gameOver, canJump, isGrounded, isDead—any yes/no condition."),
            L("Roran", "So it's for checking if something is true or not?"),
            L("Arin", "Exactly! Is the player alive? true or false. Does the player have the key? true or false."),
            L("Arin", "Boolean values are crucial for making decisions in code, which you'll learn later."),
            L("Arin", "Fourth: STRING. This stores text—sequences of characters."),
            L("Arin", "Examples: \"Hello\", \"Roran\", \"Oak Woods\", \"Welcome, traveler!\", \"Game Over\""),
            L("Arin", "Strings always use DOUBLE QUOTES. Without quotes, Java thinks it's a variable name, not text."),
            L("Arin", "Notice: String is capitalized because it's technically a class, not a primitive type. But it works similarly."),
            L("Roran", "So int for whole numbers, double for decimals, boolean for true/false, and String for text."),
            L("Arin", "Perfect summary! Now let's see how to DECLARE variables—that means creating them."),
            L("Arin", "The syntax is: TYPE variableName = value;"),
            L("Arin", "Let me show you examples for each type:"),
            L("Arin", "int lives = 3;  — Creates a variable named 'lives' of type int, and assigns it the value 3."),
            L("Arin", "int score = 1000;  — Creates 'score' as an int with value 1000."),
            L("Arin", "int health = 100;  — Creates 'health' as an int with value 100."),
            L("Roran", "So I write the type first, then the name, then equals, then the value?"),
            L("Arin", "Exactly! And always end with a semicolon. Let's see double examples:"),
            L("Arin", "double speed = 2.5;  — Creates a variable named 'speed' with value 2.5."),
            L("Arin", "double temperature = 98.6;  — Creates 'temperature' with value 98.6."),
            L("Arin", "double price = 19.99;  — Creates 'price' with value 19.99."),
            L("Arin", "Now boolean examples:"),
            L("Arin", "boolean isMage = false;  — Creates 'isMage' and sets it to false."),
            L("Arin", "boolean isAlive = true;  — Creates 'isAlive' and sets it to true."),
            L("Arin", "boolean hasKey = false;  — Creates 'hasKey' and sets it to false."),
            L("Arin", "Finally, String examples:"),
            L("Arin", "String name = \"Roran\";  — Creates 'name' and assigns it the text \"Roran\"."),
            L("Arin", "String greeting = \"Hello, traveler!\";  — Creates 'greeting' with a message."),
            L("Arin", "String title = \"Codebreaker\";  — Creates 'title' with value \"Codebreaker\"."),
            L("Arin", "Notice: Strings need DOUBLE QUOTES around the text. Numbers and boolean values do NOT use quotes."),
            L("Roran", "What about the semicolon?"),
            L("Arin", "Always required! Every declaration is a statement, and every statement ends with a semicolon."),
            L("Arin", "Forget the semicolon, and your program won't compile. It's one of the most common beginner mistakes."),
            L("Arin", "Now, about variable NAMES: they are case-sensitive. 'name' and 'Name' are different variables."),
            L("Arin", "Use descriptive names: playerHealth is better than x. maxSpeed is better than y."),
            L("Arin", "Names should start with a lowercase letter and use camelCase for multiple words."),
            L("Arin", "Good examples: playerScore, isGameOver, maxAttempts, userName, currentLevel."),
            L("Arin", "Bad examples: x, y, var1, temp, a, b (not descriptive enough)."),
            L("Roran", "I can also assign values later, right?"),
            L("Arin", "Yes! You can declare first, then assign: int count;  count = 10;"),
            L("Arin", "But it's cleaner to do both at once when possible: int count = 10;"),
            L("Arin", "Remember: You MUST declare a variable with its type before using it. Java needs to know what type it is."),
            L("Arin", "Let me give you a complete program example using all four types:"),
            L("Arin", "class PlayerStats { public static void main(String[] args) { int health = 100; double speed = 2.5; boolean isAlive = true; String name = \"Roran\"; } }"),
            L("Roran", "I see! Different types for different kinds of information."),
            L("Arin", "Perfect! One more important thing: you can change a variable's value after creating it."),
            L("Arin", "For example: int lives = 3;  lives = 2;  lives = 1;  — The value changes, but the type stays int."),
            L("Arin", "But you CANNOT change the type: int lives = 3;  lives = \"text\";  — This is an error!"),
            L("Arin", "Once a variable is declared as int, it can only hold int values. Same for all types."),
            L("Arin", "Now, I have a challenge for you. The sacred grove's wards are weakening."),
            L("Arin", "To restore them, you must collect four Type Runes: int, double, boolean, and String."),
            L("Arin", "But these runes are guarded by ancient knowledge. To collect each rune, you must answer a question about that type."),
            L("Arin", "This will test your understanding. Go forth and prove you've mastered the types!")
        ));

        // ==================== QUEST 4: Strings and Printing ====================
        // MERGED: Arin_03_PostTypes + Arin_04_StringsLecture
        list.Add(Convo("Arin_04_StringsLecture",
            // POST QUEST 3 TRANSITION (from Arin_03_PostTypes)
            L("Arin", "Roran! You've returned with all four runes! The wards are stable again."),
            L("Roran", "The rune quests forced me to think deeply about when to use each type."),
            L("Arin", "That's exactly the point! Understanding when to use each type is as important as knowing they exist."),
            L("Arin", "You answered each question correctly. Your understanding of types is solid."),
            L("Arin", "One missing semicolon, one wrong type, and everything breaks. That's the nature of syntax."),
            L("Arin", "But you didn't make those mistakes. Your mind is adapting to the way programmers think."),
            L("Roran", "Every question made me reconsider what I thought I knew."),
            L("Arin", "Good! Doubt and questioning lead to deeper understanding."),
            L("Arin", "You've proven yourself again, Codebreaker. Now we dive deeper into Strings and learn advanced printing techniques."),
            // QUEST 4 LECTURE BEGINS
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
            L("Arin", "Now, test your understanding. Answer questions about strings and obtain the Path Barrier Key.")
        ));

        // ==================== QUEST 5: The Syntax Trial ====================
        // MERGED: Arin_04_PostStrings + Arin_05_SyntaxRecap
        list.Add(Convo("Arin_05_SyntaxRecap",
            // POST QUEST 4 TRANSITION (from Arin_04_PostStrings)
            L("Arin", "You've returned with the Path Barrier Key! Excellent, Roran."),
            L("Roran", "The string questions were challenging. I had to think about escape sequences carefully."),
            L("Arin", "String manipulation is one of the most practical skills a programmer has."),
            L("Roran", "I can shape messages exactly how I want them now."),
            L("Arin", "You've mastered string literals, escape sequences, and concatenation. Well done!"),
            L("Arin", "Nearly every program displays text to users. You now know how to do it correctly."),
            L("Arin", "You've learned four fundamental concepts: classes, types, variables, and strings."),
            L("Arin", "But there is one final trial remaining. The greatest challenge of all."),
            L("Roran", "What is it?"),
            L("Arin", "The Syntax Trial. You will combine everything you've learned and prove your mastery."),
            L("Arin", "Ten questions covering all topics. This is your path to becoming a true Codebreaker."),
            // QUEST 5 RECAP BEGINS
            L("Arin", "This is your greatest test in Oak Woods, Roran."),
            L("Roran", "I'm ready to prove I've learned everything."),
            L("Arin", "Let's review everything you've mastered. First: THE CLASS."),
            L("Arin", "A class is the container for your code. Every Java program needs at least one class."),
            L("Arin", "Syntax: class ClassName { ... }"),
            L("Arin", "Class names start with a capital letter. Use descriptive names."),
            L("Arin", "Next: THE MAIN METHOD."),
            L("Arin", "This is the entry point—where program execution begins."),
            L("Arin", "Signature: public static void main(String[] args) { ... }"),
            L("Arin", "You've written this a hundred times now. It's part of your programming DNA."),
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
            L("Arin", "Excellent. But remembering and applying are two different things."),
            L("Arin", "You will now face ten questions. They will test everything—your knowledge, your attention to detail, your understanding."),
            L("Arin", "Every symbol matters. Every rule matters. One mistake, and you fail."),
            L("Arin", "But I believe in you, Roran. You've proven yourself four times already."),
            L("Roran", "I won't disappoint you."),
            L("Arin", "I know you won't. Go now. Face the Syntax Trial and show Oak Woods the true measure of your skill."),
            L("Arin", "Answer all ten questions correctly, and you will earn the right to deactivate the path barrier."),
            L("Arin", "You will become a Codebreaker—one who can read and write the ancient syntax of creation itself."),
            L("Roran", "Before I go... I want to thank you, Arin."),
            L("Arin", "Thank me after you pass the trial, Roran."),
            L("Roran", "No, I need to say this now. Whether I pass or fail, you've changed my life."),
            L("Roran", "When I arrived in Oak Woods, I was lost. Code was meaningless symbols to me."),
            L("Arin", "And now?"),
            L("Roran", "Now I see the structure. The logic. The power behind every semicolon and brace."),
            L("Roran", "You've been patient with every question, thorough with every explanation."),
            L("Arin", "Teaching you reminded me why I fell in love with programming in the first place."),
            L("Arin", "Every student brings a fresh perspective. You asked the right questions, never gave up."),
            L("Roran", "What comes next for me? After the trial, I mean."),
            L("Arin", "If you pass, the path barrier falls. You'll be free to journey beyond Oak Woods."),
            L("Arin", "There's a vast world of programming out there—control flow, loops, methods, objects, so much more."),
            L("Arin", "But Java syntax will always be your foundation. Everything builds upon what you've learned here."),
            L("Roran", "I'll miss this cabin. I'll miss your lessons."),
            L("Arin", "And I'll miss having a student as dedicated as you."),
            L("Arin", "But this is the way of mentorship—the student must eventually walk their own path."),
            L("Roran", "Will I ever see you again?"),
            L("Arin", "Oak Woods will always be here. If you ever need to revisit the fundamentals, my cabin door is open."),
            L("Arin", "But I suspect your journey will take you far from here."),
            L("Roran", "I won't forget what you've taught me. Every rule, every concept—it's part of who I am now."),
            L("Arin", "That's all a teacher can hope for. Carry that knowledge with you, Roran."),
            L("Arin", "Syntax is your compass now. Let it guide you through whatever challenges you face."),
            L("Roran", "Thank you, Arin. For everything. For believing in me when I was just a confused wanderer."),
            L("Arin", "Thank you for reminding me why I teach. You've been an excellent student."),
            L("Arin", "Now go. The Syntax Trial awaits. Show Oak Woods—show yourself—what you've learned."),
            L("Roran", "I'll make you proud, mentor."),
            L("Arin", "You already have. Safe travels, Roran. May your code always compile without errors."),
            L("Roran", "Farewell, Arin. I'll carry your lessons with me always."),
            L("Arin", "Farewell, Codebreaker. Walk with confidence—you're ready for whatever comes next.")
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