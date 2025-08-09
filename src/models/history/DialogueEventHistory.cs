using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace ValleyTalk;

internal class DialogueEventHistory : IHistory
{
    public DialogueEventHistory(IEnumerable<NPC> listeners, IEnumerable<StardewValley.DialogueLine> dialogues, string eventName = "")
    {
        Dialogues = dialogues;
        Listeners = listeners;
        EventName = eventName;
    }

    public string Format(string npcName)
    {
        var totalDialogue = string.Join(" : ", Dialogues.Select(x => x.Text));
        var allListeners = string.Join(", ", Listeners.Select(x => x.Name));
        var festivalNameString = string.IsNullOrWhiteSpace(EventName) ? "" : Util.GetString("historyThirdPartyFestival", new { festivalName = EventName });
        return Util.GetString("historyDialogueFormat", new { npcName = npcName, allListeners = allListeners, festivalNameString = festivalNameString, totalDialogue = totalDialogue });
    }

    public IEnumerable<StardewValley.DialogueLine> Dialogues { get; }
    public IEnumerable<NPC> Listeners { get; }
    public string EventName { get; }
}
