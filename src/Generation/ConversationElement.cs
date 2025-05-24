using System;

namespace ValleyTalk
{
    public class ConversationElement
    {
        public string Text { get; set; }
        public bool IsPlayerLine { get; set; }
        public Guid Id { get; }

        public ConversationElement(string text, bool isPlayerLine)
        {
            Text = text;
            IsPlayerLine = isPlayerLine;
            Id = Guid.NewGuid();
        }
    }
}