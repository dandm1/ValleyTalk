using System;
using System.Collections.Generic;
using System.Linq;
using GenericModConfigMenu;
using StardewDialogue;
using StardewModdingAPI;
namespace ValleyTalk
{
    internal static class ModConfigMenu
    {
        private static IGenericModConfigMenuApi ConfigMenu;
        private static IManifest ModManifest;
        private static ModEntry _modEntry;
        internal static void Register(ModEntry modEntry)
        {
            _modEntry = modEntry;
            var Config = ModEntry.Config;

            ModManifest = modEntry.ModManifest;

            ConfigMenu = GetConfigMenu(modEntry);
            if (ConfigMenu == null)
            {
                modEntry.Monitor.Log("Generic Mod Config Menu is not installed. Skipping config menu registration.", LogLevel.Warn);
                return;
            }

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
#if DEBUG
            ConfigMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Debug logging",
                tooltip: () => "Enable or disable detailed logging of prompts and responses",
                getValue: () => Config.Debug,
                setValue: value => Config.Debug = value
            );
#endif
            // Create a string array of the options in the LlmType enum
            var llmTypes = ModEntry.LlmMap.Keys.ToArray();
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Model provider",
                getValue: () => Config.Provider,
                setValue: value => 
                {
                    if (value == Config.Provider) return;
                    Config.ApiKey = "";
                    Config.Provider = value; 
                    ConfigMenu.Unregister(ModManifest);
                    Register(_modEntry);
                },
                allowedValues: llmTypes,
                fieldId: "Provider"
            );
            var llmType = ModEntry.LlmMap[Config.Provider];
            var constructorParameters = llmType.GetConstructors().First().GetParameters().Select(x => x.Name).ToArray();
            if (constructorParameters.Contains("apiKey", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => "API Key",
                    getValue: () => Config.ApiKey,
                    setValue: (value) =>{ Config.ApiKey = value; SetLlm(); },
                    fieldId: "ApiKey",
                    width: 500,
                    height: 48
                );
            }

            if (constructorParameters.Contains("modelName", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => "Model name",
                    getValue: () => Config.ModelName,
                    setValue: (value) =>
                    { 
                        Config.ModelName = value; SetLlm(); 
                    },
                    fieldId: "ModelName",
                    width: 400,
                    height: 48
                );
            }
            if (constructorParameters.Contains("url", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => "Server address",
                    getValue: () => Config.ServerAddress,
                    setValue: (value) =>{ Config.ServerAddress = value; SetLlm(); },
                    width: 500,
                    height: 48
                );
            }
            if (constructorParameters.Contains("promptFormat", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => "Prompt format",
                    getValue: () => Config.PromptFormat,
                    setValue: (value) =>{ Config.PromptFormat = value; SetLlm(); },
                    width: 500,
                    height: 48
                );
            }
            ConfigMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Apply translation (experimental)",
                tooltip: () => "Apply experimental instructions to the AI to translate responses into the game language",
                getValue: () => Config.ApplyTranslation,
                setValue: (value) =>{ Config.ApplyTranslation = value; }
            );
            ConfigMenu.AddParagraph(
                mod: ModManifest,
                text: () => {
                    var names = GetModelNames().ToList();
                    names.Sort();
                    if (names.Count() == 0) return $"Unable to get model names for {Config.Provider} (maybe the API key wasn't set when this menu was opened?)";
                    return $"The models available on provider {Config.Provider} are: {string.Join("\n", GetModelNames())}"; 
                }
                    
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

        private static void SetLlm()
        {
            Llm.SetLlm(ModEntry.LlmMap[ModEntry.Config.Provider], apiKey: ModEntry.Config.ApiKey, modelName: ModEntry.Config.ModelName, url: ModEntry.Config.ServerAddress, promptFormat: ModEntry.Config.PromptFormat);
        }
    }
}