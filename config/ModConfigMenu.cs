using System;
using GenericModConfigMenu;
using StardewDialogue;
namespace LlamaDialogue
{
    internal static class ModConfigMenu
    {
        internal static void Register(ModEntry modEntry)
        {
            var Config = ModEntry.Config;

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = modEntry.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("talkOfTown.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: modEntry.ModManifest,
                reset: () => ModEntry.Config = new ModConfig(),
                save: () => modEntry.Helper.WriteConfig(ModEntry.Config)
            );

            // add some config options
            configMenu.AddBoolOption(
                mod: modEntry.ModManifest,
                name: () => "Enable mod",
                tooltip: () => "Enable or disable the mod",
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );
            configMenu.AddBoolOption(
                mod: modEntry.ModManifest,
                name: () => "Debug logging",
                tooltip: () => "Enable or disable debug logging",
                getValue: () => Config.Debug,
                setValue: value => Config.Debug = value
            );
            // Create a string array of the options in the LlmType enum
            var llmTypes = Enum.GetNames(typeof(LlmType));
            configMenu.AddTextOption(
                mod: modEntry.ModManifest,
                name: () => "Model provider",
                getValue: () => Config.UseHost,
                setValue: value => Config.UseHost = value,
                allowedValues: llmTypes
            );
            configMenu.AddTextOption(
                mod: modEntry.ModManifest,
                name: () => "Model name",
                getValue: () => Config.ModelName,
                setValue: value => Config.ModelName = value
                
            );
            configMenu.AddTextOption(
                mod: modEntry.ModManifest,
                name: () => "API Key",
                getValue: () => Config.ApiKey,
                setValue: value => Config.ApiKey = value
            );
            configMenu.AddTextOption(
                mod: modEntry.ModManifest,
                name: () => "Server address",
                getValue: () => Config.ServerAddress,
                setValue: value => Config.ServerAddress = value
            );
            configMenu.AddTextOption(
                mod: modEntry.ModManifest,
                name: () => "Prompt format",
                getValue: () => Config.PromptFormat,
                setValue: value => Config.PromptFormat = value
            );
        }
    }
}