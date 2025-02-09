using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValleyTalk
{
    public class Util
    {
        private static StardewModdingAPI.ITranslationHelper _translationHelper => ModEntry.SHelper?.Translation;

        public static IEnumerable<NPC> GetNearbyNpcs(NPC npc)
        {
            // Check for any other NPCs within 3 squares
            var speakerLocation = npc.Tile;
            var speakerName = npc.Name;
            var npcs = Game1.currentLocation.characters.Where(x => x.CanReceiveGifts() && x.Name != speakerName);
            List<NPC> nearbyNpcs = new List<NPC>();
            foreach (var otherNpc in npcs)
            {
                var npcLocation = otherNpc.Tile;
                if (Microsoft.Xna.Framework.Vector2.Distance(speakerLocation, npcLocation) < 4.5)
                {
                    nearbyNpcs.Add(otherNpc);
                }
            }
            return nearbyNpcs;
        }

        internal static string ConcatAnd(List<string> strings)
        {
            if (strings.Count == 0)
            {
                return string.Empty;
            }
            if (strings.Count == 1)
            {
                return strings[0];
            }
            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < strings.Count; i++)
            {
                if (i == strings.Count - 1)
                {
                    builder.Append($" {GetString("generalAnd")} ");
                }
                else if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(strings[i]);
            }
            return builder.ToString();
        }

        internal static string GetString(StardewDialogue.Character npc,string key,object? tokens = null,bool returnNull = false)
        {
            if (npc == null) return string.Empty;

            if (npc.Bio.PromptOverrides.ContainsKey(key))
            {
                return npc.Bio.PromptOverrides[key];
            }
            string result = null;
            if (npc.Bio.IsMale ?? false)
            {
                result = Game1.content.LoadLocalized<string>("ValleyTalk/Prompts/"+key+".MaleNpc");
            }
            else if (!(npc.Bio.IsMale ?? true))
            {
                result = Game1.content.LoadLocalized<string>("ValleyTalk/Prompts/"+key+".FemaleNpc");
            }
            if (result == null)
            {
                result = Game1.content.LoadLocalized<string>("ValleyTalk/Prompts/"+key);
            }
            
            if (returnNull && result == null)
            {
                return null;
            }

            // Replace tokens
            if (tokens != null)
            {
                foreach (var token in tokens.GetType().GetProperties())
                {
                    result = result.Replace($"{{{token.Name}}}", token.GetValue(tokens).ToString());
                }
            }
            return result;
        }

        internal static string GetString(string key,object? tokens = null,bool returnNull = false)
        {
            var result = _translationHelper.Get(key, tokens);
            if (returnNull && !result.HasValue())
            {
                return null;
            }
            return result;
        }

        internal static T ReadLocalisedJson<T>(string basePath, string extension = "json") where T : class
        {
            foreach(var langSuffix in ModEntry.LanguageFileSuffixes)
            {
                var path = $"{basePath}{langSuffix}.{extension}";
                var result = ModEntry.SHelper.Data.ReadJsonFile<T>(path);
                if (result != null)
                {
                    return result;
                }
            }

            return default;
        }
    }
}