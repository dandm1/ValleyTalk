using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using StardewValley;

namespace ValleyTalk;

internal class DialogueHistory : IHistory
{
    [JsonConstructor]
    public DialogueHistory()
    {
        Dialogues = new List<StardewValley.DialogueLine>();
    }

    public DialogueHistory(IEnumerable<StardewValley.DialogueLine> dialogues)
    {
        Dialogues = dialogues;
    }

    public string Format(string npcName)
    {
        var totalDialogue = string.Join(" : ", Dialogues.Select(x => x.Text));
        return Util.GetString("dialogueHistoryFormat", new { npcName = npcName, totalDialogue = totalDialogue });
    }

    public IEnumerable<StardewValley.DialogueLine> Dialogues { get; set; }
}
