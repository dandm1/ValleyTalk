using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ValleyTalk;

namespace ValleyTalk;

internal class ConversationHistory : IHistory
{
    private readonly Guid _id;

    [Obsolete("Use ConversationElements instead")]
    public string[] chatHistory
    {
        get => ConversationElements.Select(ce => ce.Text).ToArray();
        set
        {
            ConversationElements.Clear();
            for (int i = 0; i < value.Length; i++)
            {
                ConversationElements.Add(new ConversationElement(value[i], i % 2 != 0));
            }
        }
    }
    public bool ShouldSerializechatHistory() => false;
    [JsonIgnore]
    public Guid Id => _id;

    public List<ConversationElement> ConversationElements { get; set; } = new List<ConversationElement>();


    [JsonConstructor]
    public ConversationHistory()
    {
        _id = Guid.NewGuid();
    }

    public ConversationHistory(List<ConversationElement> chatHistory)
    {
        ConversationElements = chatHistory;
        _id = chatHistory.FirstOrDefault()?.Id ?? Guid.NewGuid();
    }

    public string Format(string npcName)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < ConversationElements.Count; i++)
        {
            builder.Append(!ConversationElements[i].IsPlayerLine ? $"- {npcName}: {ConversationElements[i].Text}" : $"- {Util.GetString("generalFarmerLabel")}: {ConversationElements[i].Text}");
            builder.Append(" --- ");
        }
        return Util.GetString("historyConversationFormat", new { builder = builder });
    }
}