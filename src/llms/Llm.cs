using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Added
using System.Threading.Tasks;
using ValleyTalk;

namespace ValleyTalk;

internal abstract class Llm
{
    internal static Llm Instance {get; private set;}
    = new LlmDummy();
    
    internal static void SetLlm(Type llmType, string url ="", string promptFormat="", string apiKey="", string modelName = null)
    {
        var paramsDict = new Dictionary<string, string>
        {
            {"url", url},
            {"promptFormat", promptFormat},
            {"apiKey", apiKey},
            {"modelName", modelName}
        };
        Llm instance = CreateInstance(llmType, paramsDict);
        Instance = instance;
        DialogueBuilder.Instance.LlmDisabled = CheckConnection(apiKey, modelName).Result;
    }

    private static async Task<bool> CheckConnection(string apiKey, string modelName)
    {
        if (ModEntry.Config.SuppressConnectionCheck)
            return false;

        var response = await Instance.RunInference("You are performing LLM connection testing", "Please just ", "respond with ", "'Connection successful'");
        if (response.Length < 5)
        {
            ModEntry.SMonitor.Log($"Failed to connect to the model. ", StardewModdingAPI.LogLevel.Error);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ModEntry.SMonitor.Log(Util.GetString("modelCheckApiKey", returnNull: true) ?? "API key is not provided. Please check the configuration.", StardewModdingAPI.LogLevel.Error);
            }
            else
            {
                var getList = Llm.Instance as IGetModelNames;
                if (getList != null)
                {
                    if (string.IsNullOrWhiteSpace(modelName))
                    {
                        ModEntry.SMonitor.Log(Util.GetString("modelCheckModelName", returnNull: true) ?? "Model name is not provided. Usually this is requires, please check the configuration.", StardewModdingAPI.LogLevel.Error);
                    }
                    var modelNames = getList.GetModelNames();
                    if (modelNames.Any())
                    {
                        if (modelName.Contains(modelName))
                        {
                            ModEntry.SMonitor.Log(Util.GetString("modelCheckValidModelName", returnNull: true) ?? "Can retreive model names and model name is a valid option.  Possible causes - model is not a text generation model, insecure endpoint specified or incorrect API key (some providers).", StardewModdingAPI.LogLevel.Error);
                        }
                        else
                        {
                            ModEntry.SMonitor.Log(Util.GetString("modelCheckCantGenerate", returnNull: true) ?? "Can retreive model names but not generate dialogue. Check the model name is correctly configured.", StardewModdingAPI.LogLevel.Error);
                        }
                        if ((Llm.Instance is LlmOAICompatible || Llm.Instance is LlmLlamaCpp) && !Llm.Instance.url.Contains("https"))
                        {
                            ModEntry.SMonitor.Log(Util.GetString("modelCheckInsecure", returnNull: true) ?? "The server address specified does not use a secure connection (https). This can block text generation.", StardewModdingAPI.LogLevel.Error);
                        }
                    }
                    else
                    {
                        ModEntry.SMonitor.Log(Util.GetString("modelCheckGetNames", returnNull: true) ?? "Unable to get model names or generate dialogue.  Please check the API Key is correctly entered.", StardewModdingAPI.LogLevel.Error);
                    }
                }
                else
                {
                    ModEntry.SMonitor.Log(Util.GetString("modelCheckGenericError", returnNull: true) ?? "Please check the server address and details.", StardewModdingAPI.LogLevel.Error);
                }
            }
            return true;
        }
        else
        {
            ModEntry.SMonitor.Log(Util.GetString("modelCheckSuccess", returnNull: true) ?? "Connected to the model successfully.", StardewModdingAPI.LogLevel.Info);
            return false;
        }
    }

    public static Llm CreateInstance(Type llmType, Dictionary<string, string> paramsDict)
    {
        // Find the best constructor
        var constructor = llmType.GetConstructors().OrderByDescending(x => x.GetParameters().Length).First();
        // Construct an array of parameters by name matching
        var parameters = constructor.GetParameters().Select(x =>
        {
            if (paramsDict.TryGetValue(x.Name, out var value))
            {
                return Convert.ChangeType(value, x.ParameterType);
            }
            return x.HasDefaultValue ? x.DefaultValue : null;
        }).ToArray();
        var instance = (Llm)Activator.CreateInstance(llmType, parameters);
        return instance;
    }

    protected string url;
    private long _totalPrompts;
    private double _totalPromptTime;
    private long _totalInference;
    private double _totalInferenceTime;

    public abstract bool IsHighlySensoredModel { get; }

    public abstract string ExtraInstructions { get; }

    public string TokenStats => $"Prompt: {_totalPrompts} tokens in {_totalPromptTime}ms, Inference: {_totalInference} tokens in {_totalInferenceTime}ms";
    internal abstract Task<string> RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="");
    
    internal abstract Dictionary<string,double>[] RunInferenceProbabilities(string fullPrompt,int n_predict = 1);

    protected void AddToStats(JObject token_stats) // Changed JsonElement to JObject
    {
        if (token_stats == null) return; // Added null check

        _totalPrompts += token_stats.Value<long?>("prompt_n") ?? 0; // Changed to use JObject access
        _totalPromptTime += token_stats.Value<double?>("prompt_ms") ?? 0.0; // Changed to use JObject access
        _totalInference += token_stats.Value<long?>("predicted_n") ?? 0; // Changed to use JObject access
        _totalInferenceTime += token_stats.Value<double?>("predicted_ms") ?? 0.0; // Changed to use JObject access
    }

    internal double[] GetProbabilities(string prompt, string[][] options)
    {
        // Build a map from tokens to option numbers
        var map = BuildMap(options);

        var result = FindTokensRecursive(prompt, map, string.Empty);
        return result;
    }

    private static Dictionary<string, int> BuildMap(string[][] options)
    {
        var map = new Dictionary<string, int>();
        for (int i = 0; i < options.Length; i++)
        {
            foreach (var option in options[i])
            {
                map[option] = i;
            }
        }

        return map;
    }

    private double[] FindTokensRecursive(string prompt, Dictionary<string, int> map, string prefix)
    {
        var maxOut = map.Max(x => x.Value);
        var fullPrompt = prompt + prefix;
        var tokens = RunInferenceProbabilities(fullPrompt, 1)[0];
        var result = new double[maxOut + 1];
        foreach (var token in tokens)
        {
            if (token.Value == 0) continue;

            if (map.TryGetValue(prefix + token.Key, out var value))
            {
                result[value] += token.Value;
            }
            else if (map.Any(x => x.Key.StartsWith(prefix + token.Key)))
            {
                var recurse = FindTokensRecursive(prompt, map, prefix + token.Key);
                for (int i = 0; i < recurse.Length; i++)
                {
                    result[i] += recurse[i] * token.Value;
                }
            }
        }
        return result;
    }
}
