using System.Collections.Generic;
using System.Linq;
using ValleyTalk;

namespace StardewDialogue
{

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
        var festivalNameString = string.IsNullOrWhiteSpace(festivalName) ? "" : ModEntry.SHelper.Translation.Get("historyThirdPartyFestival", new { festivalName= festivalName });
        return ModEntry.SHelper.Translation.Get("historyThirdPartyFormat", new { npcName= npcName, Name= character.Name, festivalNameString= festivalNameString, totalDialogue= totalDialogue });

    }
}
}