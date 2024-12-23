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
namespace ValleyTalk
{
    public partial class ModEntry : Mod
    {
        private static IMonitor SMonitor;
        public static IModHelper SHelper { get; private set; }
        public static ModConfig Config;
        public static Dictionary<string, Type> LlmMap;

        public override object GetApi()
        {
            return new ModConfig();
        }

        public override void Entry(IModHelper helper)
        {
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Config = Helper.ReadConfig<ModConfig>();

            SMonitor = Monitor;
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
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            }
            Log.Debug($"###############################################");
            Log.Debug($"###############################################");
            Log.Debug($"###############################################");


            if (!Config.EnableMod)
            {
                return;
            }

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