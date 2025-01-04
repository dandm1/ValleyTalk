using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using ValleyTalk;

internal class DialogueHistory : IHistory
{
    public DialogueHistory(IEnumerable<DialogueLine> dialogues)
    {
        Dialogues = dialogues;
    }

    public string Format(string npcName)
    {
        var totalDialogue = string.Join(" : ", Dialogues.Select(x => x.Text));
        return ModEntry.SHelper.Translation.Get("dialogueHistoryFormat", new { npcName= npcName, totalDialogue= totalDialogue });
    }

    public IEnumerable<DialogueLine> Dialogues { get; }
}
