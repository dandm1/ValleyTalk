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

        private static Dictionary<int,string> freqs = new Dictionary<int, string>()
                    {
                        { 0, "Never (0%)" },
                        { 1, "Rarely (25%)" },
                        { 2, "Occasionally (50%)" },
                        { 3, "Mostly (75%)" },
                        { 4, "Always (100%)" }
                    };
        internal static void Register(ModEntry modEntry)
        {
            _modEntry = modEntry;
            var Config = ModEntry.Config;

            ModManifest = modEntry.ModManifest;

            ConfigMenu = GetConfigMenu(modEntry);
            if (ConfigMenu == null)
            {
                modEntry.Monitor.Log(Util.GetString("configGmcmNotInstalled", returnNull: true) ?? "Generic Mod Config Menu not installed.", LogLevel.Warn);
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
                name: () => Util.GetString("configEnable", returnNull: true) ?? "Enable Mod",
                tooltip: () => Util.GetString("configEnableTooltip", returnNull: true) ?? "Enable or disable the mod.",
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );
#if DEBUG
            ConfigMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Util.GetString("configLogging", returnNull: true) ?? "Enable Logging",
                tooltip: () => Util.GetString("configLoggingTooltip", returnNull: true) ?? "Enable or disable logging of prompts and responses.",
                getValue: () => Config.Debug,
                setValue: value => Config.Debug = value
            );
#endif
            // Create a string array of the options in the LlmType enum
            var llmTypes = ModEntry.LlmMap.Keys.ToArray();
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configProvider", returnNull: true) ?? "AI Model Provider",
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
                    name: () => Util.GetString("configApiKey", returnNull: true) ?? "API Key",
                    tooltip: () => Util.GetString("configApiKeyTooltip", returnNull: true) ?? "API Key for the AI model provider.",
                    getValue: () => Config.ApiKey,
                    setValue: (value) =>{ Config.ApiKey = value; SetLlm(); },
                    fieldId: "ApiKey"
                );
            }

            if (constructorParameters.Contains("modelName", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => Util.GetString("configModelName", returnNull: true) ?? "Model Name",
                    tooltip: () => Util.GetString("configModelNameTooltip", returnNull: true) ?? "Name of the AI model to use.",
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
                    name: () => Util.GetString("configServerAddress", returnNull: true) ?? "Server Address",
                    tooltip: () => Util.GetString("configServerAddressTooltip", returnNull: true) ?? "URL of the server for local and Open AI compatible models.",
                    getValue: () => Config.ServerAddress,
                    setValue: (value) =>{ Config.ServerAddress = value; SetLlm(); }
                );
            }
            if (constructorParameters.Contains("promptFormat", StringComparer.OrdinalIgnoreCase))
            {
                ConfigMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => Util.GetString("configPromptFormat", returnNull: true) ?? "Propmpt Format",
                    tooltip: () => Util.GetString("configPromptFormatTooltip", returnNull: true) ?? "Format of the prompt to send to the AI model.",
                    getValue: () => Config.PromptFormat,
                    setValue: (value) =>{ Config.PromptFormat = value; SetLlm(); }
                );
            }
            ConfigMenu.AddBoolOption(
                mod: ModManifest,
                name: () => Util.GetString("configTranslation", returnNull: true) ?? "Translate Outputs",
                tooltip: () => Util.GetString("configTranslationTooltip", returnNull: true) ?? "Translate the AI model outputs to the game language (without i18n pack).",
                getValue: () => Config.ApplyTranslation,
                setValue: (value) =>{ Config.ApplyTranslation = value; }
            );
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configFrequencyGeneral", returnNull: true) ?? "Frequency of general lines",
                tooltip: () => Util.GetString("configFrequencyGeneralTooltip", returnNull: true) ?? "How often should the mod generate general lines.",
                getValue: () => freqs[Config.GeneralFrequency],
                setValue: (value) =>{ Config.GeneralFrequency = freqs.First(x => x.Value == value).Key; },
                allowedValues: freqs.Values.ToArray()
            );
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configFrequencyGift", returnNull: true) ?? "Frequency of gift responses",
                tooltip: () => Util.GetString("configFrequencyGiftTooltip", returnNull: true) ?? "How often should the mod generate gift responses.",
                getValue: () => freqs[Config.GiftFrequency],
                setValue: (value) =>{ Config.GiftFrequency = freqs.First(x => x.Value == value).Key; },
                allowedValues: freqs.Values.ToArray()
            );
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configFrequencyMarriage", returnNull: true) ?? "Frequency of marriage lines",
                tooltip: () => Util.GetString("configFrequencyMarriageTooltip", returnNull: true) ?? "How often should the mod generate marriage lines.",
                getValue: () => freqs[Config.MarriageFrequency],
                setValue: (value) =>{ Config.MarriageFrequency = freqs.First(x => x.Value == value).Key; },
                allowedValues: freqs.Values.ToArray()
            );
            ConfigMenu.AddTextOption(
                mod: ModManifest,
                name: () => Util.GetString("configDiableForCharacters", returnNull: true) ?? "Disable for characters",
                tooltip: () => Util.GetString("configDiableForCharactersTooltip", returnNull: true) ?? "Comma-separated list of villagers to disable the mod for, e.g. (\"Abigail,Leah,Sam\")",
                getValue: () => Config.DisableCharacters,
                setValue: (value) =>{ Config.DisableCharacters = value; }
            );
            ConfigMenu.AddParagraph(
                mod: ModManifest,
                text: () => {
                    var names = GetModelNames().ToList();
                    names.Sort();
                    if (names.Count() == 0) return Util.GetString("configNoModels", new { Provider = Config.Provider }, returnNull: true) ?? $"Unable to get model names for {Config.Provider} (maybe the API key wasn't set when this menu was opened?)";
                    var modelString = string.Join(", \n", names);
                    return Util.GetString("configModels", new { Provider = Config.Provider, Models = modelString }, returnNull: true) ?? $"The models available on provider {Config.Provider} are:\n{modelString}";
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