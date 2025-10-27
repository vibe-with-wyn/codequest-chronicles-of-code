using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 6)]
    public string text;
}

[System.Serializable]
public class Conversation
{
    public string conversationId;
    public List<DialogueLine> lines = new List<DialogueLine>();
}

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Dialogue/Dialogue Database")]
public class DialogueDatabase : ScriptableObject
{
    [Header("All Conversations")]
    [SerializeField] private List<Conversation> conversations = new List<Conversation>();

    public Conversation GetConversation(string id) => conversations.Find(c => c.conversationId == id);
    public List<Conversation> GetAll() => new List<Conversation>(conversations);

    public void SetConversations(List<Conversation> list)
    {
        conversations = list ?? new List<Conversation>();
    }
}