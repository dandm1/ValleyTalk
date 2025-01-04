using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using ValleyTalk;

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
        var festivalNameString = string.IsNullOrWhiteSpace(EventName) ? "" : ModEntry.SHelper.Translation.Get("historyThirdPartyFestival", new { festivalName= EventName });
        return ModEntry.SHelper.Translation.Get("historyDialogueFormat", new { npcName= npcName, allListeners= allListeners, festivalNameString= festivalNameString, totalDialogue= totalDialogue });
    }

    public IEnumerable<DialogueLine> Dialogues { get; }
    public IEnumerable<NPC> Listeners { get; }
    public string EventName { get; }
}
