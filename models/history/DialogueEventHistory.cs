using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

internal class DialogueEventHistory : IHistory
{
    public DialogueEventHistory(IEnumerable<NPC> listeners, IEnumerable<DialogueLine> dialogues, string eventName = "")
    {
        Dialogues = dialogues;
        Listeners = listeners;
        EventName = eventName;
    }

    public string Format(string npcName)
    {
        var totalDialogue = string.Join(" : ", Dialogues.Select(x => x.Text));
        var allListeners = string.Join(", ", Listeners.Select(x => x.Name));
        return $"{npcName} speaking to {allListeners} and the farmer{(string.IsNullOrWhiteSpace(EventName) ? "" : $" at {EventName}")} : {totalDialogue}";
    }

    public IEnumerable<DialogueLine> Dialogues { get; }
    public IEnumerable<NPC> Listeners { get; }
    public string EventName { get; }
}
