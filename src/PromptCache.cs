using System.Collections.Generic;
using StardewValley;

namespace ValleyTalk;

public class PromptCache
{
    public static PromptCache Instance {get; private set;} = new PromptCache();

    private PromptCache()
    {
        RefreshPromptCache();
    }

    private string _promptLocaleCache = string.Empty;
    private Gender? _promptGenderCache;
    private Dictionary<string,string> _promptCache = null;
    public Dictionary<string,string> Cache => RefreshPromptCache();
    private Dictionary<string,string> RefreshPromptCache()
    {
        if (_promptLocaleCache != ModEntry.Language || _promptGenderCache != Game1.getPlayerOrEventFarmer()?.Gender || _promptCache != null )
        {
            _promptLocaleCache = ModEntry.Language;
            _promptGenderCache = Game1.getPlayerOrEventFarmer()?.Gender;
            _promptCache = new Dictionary<string,string>();
            Dictionary<string,object> promptDict;
            try
            {
                promptDict = Game1.content.LoadLocalized<Dictionary<string,object>>("ValleyTalk/Prompts");
            }
            catch (System.Exception)
            {
                ModEntry.SMonitor.Log("Failed to load prompts", StardewModdingAPI.LogLevel.Error);
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