using System.Collections.Generic;
using System.Linq;
using StardewDialogue;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ValleyTalk;

public class PromptCache
{
    public static PromptCache Instance {get; private set;} = new PromptCache();
    private Dictionary<string,string> _promptCache = null;

    private PromptCache()
    {
        ModEntry.SHelper.Events.Content.AssetRequested += (sender, e) =>
        {
            if (e.Name.IsEquivalentTo(VtConstants.PromptsPath))
            {
                e.LoadFrom(() => new Dictionary<string, string>(), AssetLoadPriority.Exclusive);
            }
        };
        ModEntry.SHelper.Events.Content.AssetsInvalidated += (object? sender, AssetsInvalidatedEventArgs e) =>
        {
            if (e.NamesWithoutLocale.Any(an => an.IsEquivalentTo(VtConstants.PromptsPath)))
            {
                _promptCache = null;
            }
        };
    }
    
    private string _promptLocaleCache = string.Empty;
    private Gender? _promptGenderCache;
    public Dictionary<string,string> Cache => RefreshPromptCache();
    private Dictionary<string,string> RefreshPromptCache()
    {
        if (_promptLocaleCache != ModEntry.Language || _promptGenderCache != Game1.getPlayerOrEventFarmer()?.Gender || _promptCache == null || _promptCache.Count == 0)
        {
            _promptLocaleCache = ModEntry.Language;
            _promptGenderCache = Game1.getPlayerOrEventFarmer()?.Gender;
            _promptCache = new Dictionary<string,string>();
            Dictionary<string,string> promptDict;
            try
            {
                promptDict = Game1.content.Load<Dictionary<string,string>>(VtConstants.PromptsPath);
            }
            catch (System.Exception ex)
            {
                ModEntry.SMonitor.Log("Failed to load prompts - disabling mod.", StardewModdingAPI.LogLevel.Error);
                ModEntry.SMonitor.Log("Exception: " + ex.Message, StardewModdingAPI.LogLevel.Error);
                ModEntry.Config.EnableMod = false;
                return _promptCache;
            }
            foreach (var entry in promptDict)
            {
                if (entry.Value is string && !entry.Value.ToString().StartsWith("(no translation"))
                {
                    _promptCache.Add(entry.Key, Game1.content.PreprocessString(entry.Value.ToString()));
                }
            }
        }
        return _promptCache;
    }

}