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
            SetObjective(objectives.GetArrayElementAtIndex(0), "Follow Arin to Her House", "Go to Arin’s cottage to begin your first lesson.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Listen to Arin’s Lesson: Java Basics", "Learn class, main method, braces, semicolons, and System.out.println.");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Complete the Hello World Altar", "Fill in class, main signature, and a println statement.");
        }

        // Q3
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q3_Types", "The Language of Types",
                "Learn basic types and declarations; restore the grove’s wards.", "Q4_StringsPrinting");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 3;
            var obj0 = objectives.GetArrayElementAtIndex(0);
            SetObjective(obj0, "Collect the Type Runes (3)", "Gather runes labeled int, boolean, and String.");
            obj0.FindPropertyRelative("targetCount").intValue = 3;

            SetObjective(objectives.GetArrayElementAtIndex(1), "Fix the Declarations Tablet", "Write valid variable declarations with correct types, names, and semicolons.");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Return to Arin", "Report back after stabilizing the wards.");
        }

        // Q4
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q4_StringsPrinting", "Strings and Printing",
                "Work with text: string literals, escape sequences, and concatenation.", "Q5_SyntaxTrial");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 3;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Listen to Arin’s Lesson: Strings and Printing", "Learn string literals, \\n and \\t, \\\" quotes, and using + for concatenation.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Fix the Print Lines", "Fill in println calls that concatenate text and show escape sequences.");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Activate the Echo Stone", "Use the Echo Stone to read your printed lines aloud.");
        }

        // Q5
        {
            var quest = AddQuest(questsProp);
            SetQuestCore(quest, "Q5_SyntaxTrial", "The Syntax Trial",
                "Assemble a clean class with main and valid statements to prove mastery.", "");

            var objectives = quest.FindPropertyRelative("objectives");
            objectives.arraySize = 3;
            SetObjective(objectives.GetArrayElementAtIndex(0), "Assemble the Class Skeleton", "Put the class header, main method, and braces in the right places.");
            SetObjective(objectives.GetArrayElementAtIndex(1), "Complete the Syntax Trial Tablet", "Write valid statements: declarations, string concatenation, and println (with semicolons).");
            SetObjective(objectives.GetArrayElementAtIndex(2), "Speak with Arin at the Forest Edge", "Conclude the Oak Woods chapter.");
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

        list.Add(Convo("Arin_01_PostBoss",
            L("Arin", "You fought bravely. Not many step into that cave and walk back out."),
            L("Roran", "You looked like you needed help."),
            L("Arin", "I did. Thank you. You don’t look like a local. What brings you to Oak Woods?"),
            L("Roran", "I’m new to this land. I’m searching for knowledge—and a way to survive."),
            L("Arin", "Then you’ve chosen the right power. Here, code is more than ink—it moves the world."),
            L("Roran", "I’ve heard. But I don’t know where to start."),
            L("Arin", "Start with me. I’ll mentor you. Come to my cottage. We’ll begin with Java: what it is, and how to speak it."),
            L("Roran", "Lead the way."),
            L("Arin", "Follow me to the edge of the woods. Don’t stray. The forest tests syntax more than strength.")
        ));

        list.Add(Convo("Arin_02_JavaBasics",
            L("Arin", "Let’s start simple. Java is a language used to build many kinds of applications."),
            L("Arin", "You put code inside a class. A class is a container for your program."),
            L("Arin", "Programs begin in a special method named main. Java looks for this to start."),
            L("Arin", "The exact main method you’ll use is:"),
            L("Arin", "public static void main(String[] args)"),
            L("Arin", "Curly braces { } group code. End each statement with a semicolon ;"),
            L("Arin", "To show text: System.out.println(\"your message\");"),
            L("Arin", "The class name should start with a capital letter, like HelloOak."),
            L("Arin", "Now fill in the blanks to create your first program: class, main, and a println."),
            L("Roran", "Understood. Class, main, braces, semicolons, and System.out.println.")
        ));

        list.Add(Convo("Arin_02b_PostHello",
            L("Arin", "Well done! Your first program prints a message."),
            L("Roran", "So many symbols for one sentence."),
            L("Arin", "Symbols give structure; structure gives power. Next, we’ll store values using variables and types.")
        ));

        list.Add(Convo("Arin_03_TypesLecture",
            L("Arin", "Variables store information. Each has a type and a name."),
            L("Arin", "Common types: int (whole numbers), double (decimals), boolean (true/false), String (text)."),
            L("Arin", "Examples:"),
            L("Arin", "int lives = 3;    double speed = 2.5;"),
            L("Arin", "boolean isMage = false;    String name = \"Roran\";"),
            L("Arin", "Remember: end with a semicolon. Names are case-sensitive."),
            L("Arin", "Gather the runes int, boolean, and String, then fix the Declarations Tablet.")
        ));

        list.Add(Convo("Arin_03_PostTypes",
            L("Arin", "Your types align. The grove’s wards are stable again."),
            L("Roran", "A missing semicolon breaks everything."),
            L("Arin", "That’s syntax. Next: strings and printing details.")
        ));

        list.Add(Convo("Arin_04_StringsLecture",
            L("Arin", "Strings are text in quotes, like \"Hello\"."),
            L("Arin", "Use System.out.println to print with a new line, or System.out.print to stay on the same line."),
            L("Arin", "Join pieces with +, like \"Name: \" + name."),
            L("Arin", "Escape sequences help inside quotes: \\\" for a quote, \\\\ for backslash, \\n for new line, \\t for tab."),
            L("Arin", "Fix the print lines to show your name and a two-line message using these rules.")
        ));

        list.Add(Convo("Arin_04_PostStrings",
            L("Arin", "Clear and correct. Your text rings true."),
            L("Roran", "I can shape messages exactly now."),
            L("Arin", "One final trial: build a clean class with main and valid statements.")
        ));

        list.Add(Convo("Arin_05_SyntaxRecap",
            L("Arin", "Recap: A class wraps code. main is the entry point."),
            L("Arin", "Statements end with semicolons. Braces must match."),
            L("Arin", "You can add comments: // single line, or /* multi-line */."),
            L("Arin", "Use declarations and println with concatenation to present information clearly.")
        ));

        list.Add(Convo("Arin_05_Epilogue",
            L("Arin", "You’ve mastered the basics of Java syntax. Oak Woods recognizes you."),
            L("Roran", "I can finally read the forest’s words."),
            L("Arin", "Syntax is your compass. Keep practicing, Codebreaker."),
            L("Roran", "Thank you, Arin."),
            L("Arin", "If fate loops us back, I’ll be here. For now, walk on.")
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