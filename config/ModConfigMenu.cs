using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using GenericModConfigMenu;
using StardewDialogue;
using StardewModdingAPI;
namespace LlamaDialogue
{
    internal static class ModConfigMenu
    {
        private static IGenericModConfigMenuApi ConfigMenu;
        private static IManifest ModManifest;
        internal static void Register(ModEntry modEntry)
        {
            var Config = ModEntry.Config;

            ModManifest = modEntry.ModManifest;

            ConfigMenu = GetConfigMenu(modEntry);

            // register mod
            ConfigMenu.Register(
                mod: ModManifest,
                reset: () => ModEntry.Config = new ModConfig(),
                save: () => modEntry.Helper.WriteConfig(ModEntry.Config)
            );

            // add some config options
            ConfigMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable mod",
                tooltip: () => "Enable or disable the mod",
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );
            ConfigMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Debug logging",
                tooltip: () => "Enable or disable detailed logging of prompts and responses",
                getValue: () => Config.Debug,
                setValue: value => Config.Debug = value
            );
            // Create a string array of the options in the LlmType enum
            var llmTypes = ModEntry.LlmMap.Keys.ToArray();
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Model provider",
                getValue: () => Config.Provider,
                setValue: value => 
                { 
                    Config.Provider = value; 
                    ConfigMenu.Unregister(ModManifest);
                    Register(modEntry);
                },
                allowedValues: llmTypes
            );
            var llmType = ModEntry.LlmMap[Config.Provider];
            var constructorParameters = llmType.GetConstructors().First().GetParameters().Select(x => x.Name).ToArray();
            if (constructorParameters.Contains("apiKey", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => "API Key",
                    getValue: () => Config.ApiKey,
                    setValue: value => Config.ApiKey = value
                );
            }
            if (constructorParameters.Contains("modelName", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => "Model name",
                    getValue: () => Config.ModelName,
                    setValue: value => Config.ModelName = value,
                    allowedValues: GetModelNames()
                );
            }
            if (constructorParameters.Contains("url", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => "Server address",
                    getValue: () => Config.ServerAddress,
                    setValue: value => Config.ServerAddress = value
                );
            }
            if (constructorParameters.Contains("promptFormat", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => "Prompt format",
                    getValue: () => Config.PromptFormat,
                    setValue: value => Config.PromptFormat = value
                );
            }
            ConfigMenu.OnFieldChanged(
                mod: ModManifest, onChange: OnChange
            );
        }

        private static string[] GetModelNames()
        {
            var provider = ModEntry.LlmMap[ModEntry.Config.Provider];
            if (provider.GetInterfaces().Any(x => x.Name == "IGetModelNames"))
            {
                var paramsDict = new Dictionary<string, string>()
                {
                    { "apiKey", ModEntry.Config.ApiKey },
                    { "modelName", ModEntry.Config.ModelName },
                    { "url", ModEntry.Config.ServerAddress },
                    { "promptFormat", ModEntry.Config.PromptFormat }
                };
                var instance = Llm.CreateInstance(provider, paramsDict);
                return ((IGetModelNames)instance).GetModelNames();
            }
            else
            {
                return new string[] { };
            }
        }

        private static IGenericModConfigMenuApi GetConfigMenu(ModEntry modEntry)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            return modEntry.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu"); 
        }

        private static void OnChange(string arg1, object arg2)
        {
            var changeTest = "A string";

            //throw new NotImplementedException();
        }
    }
}