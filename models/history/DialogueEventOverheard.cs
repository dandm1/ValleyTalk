using System;
using System.Collections.Generic;
using System.Linq;
using ValleyTalk;

namespace StardewDialogue;

internal class DialogueEventOverheard : IHistory
{
    public string name;
    public List<StardewValley.DialogueLine> dialogues;

    public DialogueEventOverheard(string name, List<StardewValley.DialogueLine> filteredDialogues)
    {
        this.name = name;
        this.dialogues = filteredDialogues;
    }

    public string Format(string npcName)
    {
        var totalDialogue = string.Join(" : ", dialogues?.Select(x => x.Text) ?? new List<string>());
        return ModEntry.SHelper.Translation.Get("historyOverheardFormat", new { name= name, totalDialogue= totalDialogue });
    }
}