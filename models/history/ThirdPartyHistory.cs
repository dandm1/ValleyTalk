using System.Collections.Generic;
using System.Linq;

namespace StardewDialogue;

internal class ThirdPartyHistory : IHistory
{
    public Character character;
    public List<StardewValley.DialogueLine> filteredDialogues;
    public string festivalName;

    public ThirdPartyHistory(Character character, List<StardewValley.DialogueLine> filteredDialogues, string festivalName)
    {
        this.character = character;
        this.filteredDialogues = filteredDialogues;
        this.festivalName = festivalName;
    }

    public string Format(string npcName)
    {
        var totalDialogue = string.Join(" : ", filteredDialogues.Select(x => x.Text));
        return $"{npcName} overhead {character.Name} speaking to the farmer{(string.IsNullOrWhiteSpace(festivalName) ? "" : $" at {festivalName}")} : {totalDialogue}";

    }
}