using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class QuizChoice
{
    public string choiceText;
    public bool isCorrect;
    
    public QuizChoice(string text, bool correct)
    {
        choiceText = text;
        isCorrect = correct;
    }
}

[System.Serializable]
public class QuizQuestion
{
    [Header("Question Content")]
    public string questionText;
    public List<QuizChoice> choices = new List<QuizChoice>();
    
    [Header("Feedback")]
    [TextArea(2, 4)]
    public string hintOnWrongAnswer;
    
    [TextArea(2, 4)]
    public string explanationOnCorrect;
    
    public QuizQuestion(string question)
    {
        questionText = question;
        choices = new List<QuizChoice>();
    }
    
    public int GetCorrectAnswerIndex()
    {
        for (int i = 0; i < choices.Count; i++)
        {
            if (choices[i].isCorrect)
                return i;
        }
        return -1;
    }
}

[CreateAssetMenu(fileName = "MiniQuestData", menuName = "Quest System/Mini Quest Data")]
public class MiniQuestData : ScriptableObject
{
    [Header("Mini Quest Information")]
    public string miniQuestId = "Q2_Obj3_HelloWorld";
    public string miniQuestTitle = "Hello World: Java Basics";
    
    [TextArea(2, 4)]
    public string instructions = "Answer all questions correctly to complete this quest objective.";
    
    [Header("Questions")]
    public List<QuizQuestion> questions = new List<QuizQuestion>();
    
    [Header("Completion")]
    public string objectiveTitleToComplete = "Complete the Hello World Altar Quiz";
    
    public int GetTotalQuestions()
    {
        return questions.Count;
    }
    
    public QuizQuestion GetQuestion(int index)
    {
        if (index >= 0 && index < questions.Count)
            return questions[index];
        return null;
    }
}