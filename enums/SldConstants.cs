using System.Collections.Generic;
using StardewValley;

namespace ValleyTalk
{
    internal class SldConstants
    {
        internal static readonly string DialogueKeyPrefix = "SLD_";
        internal static readonly string DialogueGenerationTag = "$$$%%%";
        internal static readonly string[] PermitListContentPacks = 
        new string[] {};

        public static Dictionary<LocalizedContentManager.LanguageCode,string> Languages = new Dictionary<LocalizedContentManager.LanguageCode, string>
        {
            { LocalizedContentManager.LanguageCode.en, "US English" },
            { LocalizedContentManager.LanguageCode.de, "German" },
            { LocalizedContentManager.LanguageCode.es, "Spanish" },
            { LocalizedContentManager.LanguageCode.fr, "French" },
            { LocalizedContentManager.LanguageCode.it, "Italian" },
            { LocalizedContentManager.LanguageCode.ja, "Japanese" },
            { LocalizedContentManager.LanguageCode.ko, "Korean" },
            { LocalizedContentManager.LanguageCode.pt, "Brazilian Portuguese" },
            { LocalizedContentManager.LanguageCode.ru, "Russian" },
            { LocalizedContentManager.LanguageCode.tr, "Turkish" },
            { LocalizedContentManager.LanguageCode.zh, "Simplified Chinese" },
            { LocalizedContentManager.LanguageCode.hu, "Hungarian"},
            { LocalizedContentManager.LanguageCode.th, "Thai"}
        };
    }
}