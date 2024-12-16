using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

internal class DialogueHistory : IHistory
{
    public DialogueHistory(IEnumerable<DialogueLine> dialogues)
    {
        Dialogues = dialogues;
    }

    public string Format(string npcName)
    {
        var totalDialogue = string.Join(" : ", Dialogues.Select(x => x.Text));
        return $"{npcName} speaking to farmer : {totalDialogue}";
    }

    public IEnumerable<DialogueLine> Dialogues { get; }
}
