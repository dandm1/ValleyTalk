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
        private static Dictionary<int,string> FrequencyOptions = new Dictionary<int, string>()
        {
            { 0, Util.GetString("configNever") },
            { 1, Util.GetString("configRarely") },
            { 2, Util.GetString("configOccasionally") },
            { 3, Util.GetString("configMostly") },
            { 4, Util.GetString("configAlways") }
        };
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
                modEntry.Monitor.Log(Util.GetString("configGmcmNotInstalled"), LogLevel.Warn);
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
                name: () => Util.GetString("configEnable"),
                tooltip: () => Util.GetString("configEnableTooltip"),
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );
#if DEBUG
            ConfigMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Util.GetString("configLogging"),
                tooltip: () => Util.GetString("configLoggingTooltip"),
                getValue: () => Config.Debug,
                setValue: value => Config.Debug = value
            );
#endif
            // Create a string array of the options in the LlmType enum
            var llmTypes = ModEntry.LlmMap.Keys.ToArray();
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configProvider"),
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
                    name: () => Util.GetString("configApiKey"),
                    tooltip: () => Util.GetString("configApiKeyTooltip"),
                    getValue: () => Config.ApiKey,
                    setValue: (value) =>{ Config.ApiKey = value; SetLlm(); },
                    fieldId: "ApiKey"
                );
            }

            if (constructorParameters.Contains("modelName", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => Util.GetString("configModelName"),
                    tooltip: () => Util.GetString("configModelNameTooltip"),
                    getValue: () => Config.ModelName,
                    setValue: (value) =>
                    { 
                        Config.ModelName = value; SetLlm(); 
                    },
                    fieldId: "ModelName"
                );
            }
            if (constructorParameters.Contains("url", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => Util.GetString("configServerAddress"),
                    tooltip: () => Util.GetString("configServerAddressTooltip"),
                    getValue: () => Config.ServerAddress,
                    setValue: (value) =>{ Config.ServerAddress = value; SetLlm(); }
                );
            }
            if (constructorParameters.Contains("promptFormat", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => Util.GetString("configPromptFormat"),
                    tooltip: () => Util.GetString("configPromptFormatTooltip"),
                    getValue: () => Config.PromptFormat,
                    setValue: (value) =>{ Config.PromptFormat = value; SetLlm(); }
                );
            }
            ConfigMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Util.GetString("configTranslation"),
                tooltip: () => Util.GetString("configTranslationTooltip"),
                getValue: () => Config.ApplyTranslation,
                setValue: (value) =>{ Config.ApplyTranslation = value; }
            );
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configFrequencyGeneral"),
                tooltip: () => Util.GetString("configFrequencyGeneralTooltip"),
                getValue: () => FrequencyOptions[Config.GeneralFrequency],
                setValue: (value) =>{ Config.GeneralFrequency = FrequencyOptions.First(x => x.Value == value).Key; },
                allowedValues: FrequencyOptions.Values.ToArray()
            );
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configFrequencyGift"),
                tooltip: () => Util.GetString("configFrequencyGiftTooltip"),
                getValue: () => FrequencyOptions[Config.GiftFrequency],
                setValue: (value) =>{ Config.GiftFrequency = FrequencyOptions.First(x => x.Value == value).Key; },
                allowedValues: FrequencyOptions.Values.ToArray()
            );
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configFrequencyMarriage"),
                tooltip: () => Util.GetString("configFrequencyMarriageTooltip"),
                getValue: () => FrequencyOptions[Config.MarriageFrequency],
                setValue: (value) =>{ Config.MarriageFrequency = FrequencyOptions.First(x => x.Value == value).Key; },
                allowedValues: FrequencyOptions.Values.ToArray()
            );
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configDiableForCharacters"),
                tooltip: () => Util.GetString("configDiableForCharactersTooltip"),
                getValue: () => Config.DisableCharacters,
                setValue: (value) =>{ Config.DisableCharacters = value; }
            );
            ConfigMenu.AddParagraph(
                mod: ModManifest,
                text: () => {
                    var names = GetModelNames().ToList();
                    names.Sort();
                    if (names.Count() == 0) return Util.GetString("configNoModels", new { Provider = Config.Provider });
                    return Util.GetString("configModels", new { Provider = Config.Provider, Models = string.Join(", ", names) });
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