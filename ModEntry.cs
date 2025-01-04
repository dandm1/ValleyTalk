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

        private static void GetLocale()
        {
            if (_locale != null) return;
            
            try
            {
                _locale = CultureInfo.GetCultureInfo(SHelper.Translation.Locale);
            }
            catch (Exception _)
            {
                _locale = null;
            }
            if (_locale == null)
            {
                _locale = CultureInfo.GetCultureInfo("en-US");
            }   
        }

        public override object GetApi()
        {
            return new ModConfig();
        }

        public override void Entry(IModHelper helper)
        {
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
            LlmMap = new Dictionary<string, Type>
            {
#if DEBUG
                {"Dummy", typeof(LlmDummy)},
#endif
                {"LlamaCpp", typeof(LlmLlamaCpp)},
                {"Google", typeof(LlmGemini)},
                {"Anthropic", typeof(LlmClaude)},
                {"OpenAI", typeof(LlmOpenAi)},
                {"Mistral", typeof(LlmMistral)},
                {"OpenAiCompatible", typeof(LlmOAICompatible)}
            };
            if (!LlmMap.TryGetValue(Config.Provider, out var llmType))
            {
                Log.Error($"Invalid LLM type: {Config.Provider}");
                return;
            }

            Llm.SetLlm(llmType, modelName: Config.ModelName, apiKey: Config.ApiKey, url: Config.ServerAddress, promptFormat: Config.PromptFormat);

            DialogueBuilder.Instance.Config = Config;

            SHelper = helper;

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