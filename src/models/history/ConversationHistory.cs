using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using ValleyTalk;

namespace StardewDialogue;

internal class ConversationHistory : IHistory
{
    private readonly Guid _id;
    public string[] chatHistory
    {
        get => _conversationElements.ConvertAll(ce => ce.Text).ToArray();
        set
        {
            _conversationElements.Clear();
            for (int i = 0; i < value.Length; i++)
            {
                _conversationElements.Add(new ConversationElement(value[i], i % 2 != 0));
            }
        }
    }
    
    private List<ConversationElement> _conversationElements = new List<ConversationElement>();
    [JsonIgnore]
    public Guid Id => _id;
    [JsonIgnore]
    public List<ConversationElement> ConversationElements => _conversationElements;

    public ConversationHistory(string[] chatHistory)
    {
        this.chatHistory = chatHistory;
        _id = Guid.NewGuid();
    }

    public ConversationHistory(List<ConversationElement> chatHistory)
    {
        _conversationElements = chatHistory;
        _id = chatHistory.FirstOrDefault()?.Id ?? Guid.NewGuid();
    }

    public string Format(string npcName)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < _conversationElements.Count; i++)
        {
            builder.Append(!_conversationElements[i].IsPlayerLine ? $"- {npcName}: {_conversationElements[i].Text}" : $"- {Util.GetString("generalFarmerLabel")}: {_conversationElements[i].Text}");
            builder.Append(" --- ");
        }
        return Util.GetString("historyConversationFormat", new { builder = builder });
    }
}