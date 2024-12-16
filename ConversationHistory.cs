using System.Collections.Generic;
using System.Text;

namespace StardewDialogue;

internal class ConversationHistory : IHistory
{
    public string[] chatHistory;

    public ConversationHistory(string[] chatHistory)
    {
        this.chatHistory = chatHistory;
    }

    public string Format(string npcName)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < chatHistory.Length; i++)
        {
            builder.Append(i % 2 == 0 ? $"- {npcName}: {chatHistory[i]}" : $"- Farmer: {chatHistory[i]}");
            builder.Append(" --- ");
        }
        return $"Had a conversation with the farmer : {builder.ToString()}";
    }
}