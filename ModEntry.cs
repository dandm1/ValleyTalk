using System;
using System.IO;
using HarmonyLib;
using Microsoft.VisualBasic;
using StardewDialogue;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Net;
using System.Collections;
namespace ValleyTalk
{
    public partial class ModEntry : Mod
    {
        public static IMonitor SMonitor;
        public static IModHelper SHelper { get; private set; }
        public static ModConfig Config;
        public static Dictionary<string, Type> LlmMap;
        public static bool BlockModdedContent { get; private set; } = false;
        private static CultureInfo _locale;
        public static string Language 
        { 
            get
            {
                GetLocale();
                return _locale.DisplayName;
            }
        }

        public static IEnumerable<string> LanguageFileSuffixes
        {
            get
            {
                GetLocale();
                if (_locale != null && _locale.Name != "en-US")
                {
                    var workingLocal = _locale;
                    while (!string.IsNullOrEmpty(workingLocal?.Name))
                    {
                        yield return $".{workingLocal.Name}";
                        workingLocal = workingLocal.Parent;
                    }
                }
                yield return string.Empty;
            }
        }

        private static string _localeCache = string.Empty;
        private static void GetLocale()
        {
            if (_locale != null && SHelper.Translation.Locale == _localeCache) return;
            
            try
            {
                _locale = CultureInfo.GetCultureInfo(SHelper.Translation.Locale);
                _localeCache = SHelper.Translation.Locale;
            }
            catch (Exception _)
            {
                _locale = null;
                _localeCache = string.Empty;
            }
            if (_locale == null)
            {
                _locale = CultureInfo.GetCultureInfo("en-US");
                _localeCache = SHelper.Translation.Locale;
            }   
        }

        private static bool? _fixPunctuation = null;
        private static string _localeCacheFixPunctuation = string.Empty;
        public static bool FixPunctuation
        {
            get
            {
                if (_fixPunctuation == null || _localeCacheFixPunctuation != SHelper.Translation.Locale)
                {
                    var suffixes = LanguageFileSuffixes.ToList();
                    _fixPunctuation = suffixes.Count == 1 || suffixes.Any(x => x == ".en" || x == ".fr" || x == ".de" || x == ".es" || x == ".tr" || x == ".pt" || x == ".it" || x == ".nl" || x == ".pl" || x == ".id");
                    _localeCacheFixPunctuation = SHelper.Translation.Locale;
                }
                return _fixPunctuation.Value;
            }
        }

        public ModEntry()
        {
            SHelper = Helper;
        }

        public override object GetApi()
        {
            return new ModConfig();
        }

        public override void Entry(IModHelper helper)
        {
            SHelper = Helper;
            
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Config = Helper.ReadConfig<ModConfig>();

            SMonitor = Monitor;

            if (!Config.EnableMod)
            {
                return;
            }

#if DEBUG
            if (Config.Debug)
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File("Generation.log", rollingInterval: RollingInterval.Day)
                    .MinimumLevel.Debug()
                    .CreateLogger();
            }
            else
            {
#endif
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
#if DEBUG
            }
            Log.Debug($"###############################################");
            Log.Debug($"###############################################");
            Log.Debug($"###############################################");
#endif
            // Build dictionary of LLM types (things that inherit from the LLM class)
            LlmMap = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase)
            {
#if DEBUG
                {"Dummy", typeof(LlmDummy)},
#endif
                {"LlamaCpp", typeof(LlmLlamaCpp)},
                {"Google", typeof(LlmGemini)},
                {"Anthropic", typeof(LlmClaude)},
                {"OpenAI", typeof(LlmOpenAi)},
                {"Mistral", typeof(LlmMistral)},
                {"DeepSeek", typeof(LlmDeepSeek)},
                {"VolcEngine", typeof(LlmVolcEngine)},
                {"OpenAiCompatible", typeof(LlmOAICompatible)}
            };
            if (!LlmMap.TryGetValue(Config.Provider, out var llmType))
            {
                Log.Error($"Invalid LLM type: {Config.Provider}");
                return;
            }

            Llm.SetLlm(llmType, modelName: Config.ModelName, apiKey: Config.ApiKey, url: Config.ServerAddress, promptFormat: Config.PromptFormat);

            DialogueBuilder.Instance.Config = Config;

            CheckContentPacks();

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();

            Log.Debug($"[{DateTime.Now}] Mod loaded");

        }

        private void CheckContentPacks()
        {
            var contentPacks = SHelper.ModRegistry.GetAll().Where(p => p.IsContentPack).ToList();
            var blockedContentPacks = contentPacks
                .Where(p => !SldConstants.PermitListContentPacks.Contains(p.Manifest.UniqueID))
                .Where(p =>
                        !p.Manifest.ExtraFields.ContainsKey("PermitAiUse") ||
                        !(p.Manifest.ExtraFields["PermitAiUse"] as bool? ?? false)
                );
            if (blockedContentPacks.Any())
            {
                Monitor.Log("Note: Content packs have been found that don't have mod author approval for use with AI.", LogLevel.Warn);
                Monitor.Log("While content from content packs will be displayed in-game, it will not be use for AI dialogue generation.", LogLevel.Warn);
                Monitor.Log($"Content packs without author approval: {string.Join(", ", blockedContentPacks.Select(p => p.Manifest.Name))}", LogLevel.Info);
                Monitor.Log("Mod authors can permit their content to be used in dialogue generation by adding \"permitAiUse\":true to their mod's manifest.", LogLevel.Warn);
                BlockModdedContent = true;
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            ModConfigMenu.Register(this);
        }
    }
}