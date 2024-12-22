using System;
using System.IO;
using HarmonyLib;
using Microsoft.VisualBasic;
using StardewDialogue;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Serilog;
namespace LlamaDialogue
{
    public partial class ModEntry : Mod
    {
        private static IMonitor SMonitor;
        public static IModHelper SHelper { get; private set; }
        public static ModConfig Config;

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

            if ( !Enum.TryParse<LlmType>(Config.UseHost, ignoreCase: true, result: out LlmType llmType))
            {
                Log.Error($"Invalid LLM type: {Config.UseHost}");
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