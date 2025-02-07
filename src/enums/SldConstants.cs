using System.Collections.Generic;
using static StardewValley.LocalizedContentManager;

namespace ValleyTalk
{
    internal class SldConstants
    {
        internal static readonly string DialogueKeyPrefix = "SLD_";
        internal static readonly string DialogueGenerationTag = "$$$%%%";
        internal static readonly string[] PermitListContentPacks = 
        new string[] {};

        public static Dictionary<LanguageCode,string> Languages = new Dictionary<LanguageCode, string>
        {
            { LanguageCode.en, "US English" },
            { LanguageCode.de, "German" },
            { LanguageCode.es, "Spanish" },
            { LanguageCode.fr, "French" },
            { LanguageCode.it, "Italian" },
            { LanguageCode.ja, "Japanese" },
            { LanguageCode.ko, "Korean" },
            { LanguageCode.pt, "Brazilian Portuguese" },
            { LanguageCode.ru, "Russian" },
            { LanguageCode.tr, "Turkish" },
            { LanguageCode.zh, "Simplified Chinese" },
            { LanguageCode.hu, "Hungarian"},
            { LanguageCode.th, "Thai"}
        };
    }
}