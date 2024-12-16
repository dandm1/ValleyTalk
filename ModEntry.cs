using System;
using System.IO;
using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.VisualBasic;
using Serilog;
using StardewDialogue;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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
            SMonitor = Monitor;
            
            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();

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
            Log.Debug($"[{DateTime.Now}] Mod loaded");

        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => ModEntry.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(ModEntry.Config)
            );

            // add some config options
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable mod",
                tooltip: () => "Enable or disable the mod",
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Debug logging",
                tooltip: () => "Enable or disable debug logging",
                getValue: () => Config.Debug,
                setValue: value => Config.Debug = value
            );
            // Create a string array of the options in the LlmType enum
            var llmTypes = Enum.GetNames(typeof(LlmType));
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Model provider",
                getValue: () => Config.UseHost,
                setValue: value => Config.UseHost = value,
                allowedValues: llmTypes
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Model name",
                getValue: () => Config.ModelName,
                setValue: value => Config.ModelName = value
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "API Key",
                getValue: () => Config.ApiKey,
                setValue: value => Config.ApiKey = value
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Server address",
                getValue: () => Config.ServerAddress,
                setValue: value => Config.ServerAddress = value
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Prompt format",
                getValue: () => Config.PromptFormat,
                setValue: value => Config.PromptFormat = value
            );
        }
    }
}