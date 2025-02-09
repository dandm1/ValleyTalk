using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewValley;
using StardewValley.Network;
using ValleyTalk;

namespace StardewDialogue;

internal class GameSummaryBuilder
{
    private Dictionary<string,object> gameSummaryDict;
    public GameSummaryBuilder()
    {
        gameSummaryDict = Game1.content.LoadLocalized<Dictionary<string,object>>("ValleyTalk/GameSummary");
    }

    internal string Build()
    {
        var builder = new StringBuilder();
        var sections = gameSummaryDict["SectionOrder"] as Dictionary<string,bool>;
        if (sections == null)
        {
            ModEntry.SMonitor.Log("GameSummary is missing SectionOrder", StardewModdingAPI.LogLevel.Error);
            return string.Empty;
        }
        foreach(var section in sections)
        {
            if (gameSummaryDict.Any(x => x.Key.StartsWith($"{section.Key}/")))
            {
                if (section.Value)
                {
                    builder.AppendLine($"### {section.Key} :");
                }
                if (gameSummaryDict.ContainsKey($"{section.Key}Intro"))
                {
                    builder.AppendLine(gameSummaryDict[$"{section.Key}Intro"].ToString());
                }
                var entry = gameSummaryDict.FirstOrDefault(x => x.Key == section.Key);
                if (entry.Value == null)
                {
                    continue;
                }
                if (entry.Value is string)
                {
                    builder.AppendLine(entry.Value.ToString());
                }
                else if (entry.Value is List<object> subDict)
                {
                    switch(section.Key)
                    {
                        case "Seasons":
                            var seasons = subDict.Select(x => x as SeasonObject);
                            foreach(var season in seasons)
                            {
                                builder.Append($"- **{season.Name}** - {season.Description} ");
                                if (season.Crops.Any())
                                {
                                    builder.Append($"{Util.GetString("seasonCrops")} {Util.ConcatAnd(season.Crops)}. ");
                                }
                                builder.AppendLine($"{Util.GetString("seasonForage")} {Util.ConcatAnd(season.Forage)}.");
                            }
                            break;
                        case "Locations":
                            var locations = subDict.Select(x => x as LocationObject);
                            var regions = locations.GroupBy(x => x.Region);
                            foreach(var region in regions)
                            {
                                foreach(var location in region)
                                {
                                    builder.AppendLine($"- **{location.Name}** - {location.Description}");
                                }
                            }
                            break;
                        default:
                            var items = subDict.Select(x => x as GeneralObject);
                            foreach(var item in items)
                            {
                                builder.AppendLine($"- **{item.Name}** - {item.Description}");
                            }
                            break;
                    }
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

    private static void AppendLineIfKeyExists(StringBuilder builder, Dictionary<string, object> gameSummaryDict, string key, string prefix = "")
    {
        if (gameSummaryDict.ContainsKey(key))
        {
            builder.AppendLine(prefix + gameSummaryDict[key].ToString());
        }
    }

    private class GeneralObject
    {
        public string id;
        public string Name;
        public string Description;
    }

    private class LocationObject : GeneralObject
    {
        public string Region;
    }

    private class SeasonObject : GeneralObject
    {
        public List<string> Crops;
        public List<string> Forage;
    }
}