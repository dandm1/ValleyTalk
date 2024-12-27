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
namespace ValleyTalk
{
    public partial class ModEntry : Mod
    {
        public static IMonitor SMonitor;
        public static IModHelper SHelper { get; private set; }
        public static ModConfig Config;
        public static Dictionary<string, Type> LlmMap;
        public static bool BlockModdedContent { get; private set; } = false;
        public static string Language 
        { 
            get
            {
                if (SldConstants.Languages.ContainsKey(SHelper.Translation.LocaleEnum))
                {
                    return SldConstants.Languages[SHelper.Translation.LocaleEnum];
                }
                else
                {
                    return "English";
                }
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

            Llm.SetLlm(llmType, modelName:Config.ModelName ,apiKey: Config.ApiKey, url: Config.ServerAddress, promptFormat: Config.PromptFormat);

            DialogueBuilder.Instance.Config = Config;
            
            SHelper = helper;

            var contentPacks = SHelper.ModRegistry.GetAll().Where(p => p.IsContentPack).ToList();
            var blockedContentPacks = contentPacks.Where(p => !SldConstants.PermitListContentPacks.Contains(p.Manifest.UniqueID));
            if (blockedContentPacks.Any())
            {
                Monitor.Log("ValleyTalk: Unapproved content packs found.  Using canon dialogue and blocking non standard NPCs.", LogLevel.Warn);
                Monitor.Log($"Unapproved content packs: {string.Join(", ", blockedContentPacks.Select(p => p.Manifest.Name))}", LogLevel.Warn);
                Monitor.Log("If you are the mod author and wish to unblock your content pack, please raise a bug.", LogLevel.Warn);
                BlockModdedContent = true;
            }

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();

            Log.Debug($"[{DateTime.Now}] Mod loaded");

        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            ModConfigMenu.Register(this);
        }
    }
}