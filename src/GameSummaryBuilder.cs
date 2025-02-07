using System.Collections.Generic;
using System.Linq;
using System.Text;
using ValleyTalk;

namespace StardewDialogue;

internal class GameSummaryBuilder
{
    private Dictionary<string,string> gameSummaryDict;
    public GameSummaryBuilder()
    {
        gameSummaryDict = Util.ReadLocalisedJson<Dictionary<string,string>>("assets/bio/Stardew","txt");
    }

    private Dictionary<string,bool> sections = new Dictionary<string,bool>
    {
        {"Intro",false},
        {"FarmerBackground",false},
        {"Locations", true},
        {"Festivals", true},
        {"Villagers", true},
        {"Outro", false}
    };

    internal string Build()
    {
        var builder = new StringBuilder();
        foreach(var section in sections)
        {
            if (section.Value)
            {
                builder.AppendLine($"### {section.Key} :");
            }

            AppendLineIfKeyExists(builder, gameSummaryDict, section.Key);
            var subKeys = gameSummaryDict.Keys.Where(k => k.StartsWith($"{section.Key}/"));
            var subHeadings = subKeys.Select(k => k.Split('/')[1]).Distinct();
            foreach(var subHeading in subHeadings.OrderBy(s => s))
            {
                AppendLineIfKeyExists(builder, gameSummaryDict, $"{section.Key}/{subHeading}", $"- **{GetDisplay(subHeading)}** : ");
                foreach(var subKey in subKeys.Where(k => k.StartsWith($"{section.Key}/{subHeading}/")).OrderBy(k => k))
                {
                    var key = subKey.Split('/')[2];
                    AppendLineIfKeyExists(builder, gameSummaryDict, $"{section.Key}/{subHeading}/{key}", $"  - **{GetDisplay(key)}** : ");
                }
            }
        }
        
        var gameSummaryTranslations = Util.GetString("gameSummaryTranslations");
        if (!string.IsNullOrWhiteSpace(gameSummaryTranslations))
        {
            builder.AppendLine(gameSummaryTranslations);
        }
        return builder.ToString();
    }

    private static string GetDisplay(string key)
    {
        string displayKey = key;
        if (key.Contains("_"))
        {
            displayKey = key.Substring(key.IndexOf('_') + 1);
        }

        return displayKey;
    }

    private static void AppendLineIfKeyExists(StringBuilder builder, Dictionary<string, string> gameSummaryDict, string key, string prefix = "")
    {
        if (gameSummaryDict.ContainsKey(key))
        {
            builder.AppendLine(prefix + gameSummaryDict[key]);
        }
    }
}