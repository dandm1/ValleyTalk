using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Netcode;

namespace StardewDialogue;

internal abstract class Llm
{
    internal static Llm Instance {get; private set;}
    = new LlmLlamaCpp(
        "http://mlpc:8080/completion",
        //"<start_of_turn>user\n{system}\n{prompt}<end_of_turn>\n<start_of_turn>model\n{response_start}" //Gemma
        //"<BOS_TOKEN><|START_OF_TURN_TOKEN|><|USER_TOKEN|>{system}\n{prompt}<|END_OF_TURN_TOKEN|><|START_OF_TURN_TOKEN|><|CHATBOT_TOKEN|>{response_start}"
        //"<|im_start|>user\n{system}\n{prompt}<|im_end|>\n<|im_start|>assistant\n{response_start}" // Magnum / Qwen
        //"<|begin_of_text|><|start_header_id|>system<|end_header_id|>\n\n{system}<|eot_id|><|start_header_id|>user<|end_header_id|>\n\n{prompt}<|eot_id|><|start_header_id|>assistant<|end_header_id|>\n\n{response_start}"
        "[INST] {system}\n{prompt}[/INST]\n{response_start}"
        );
    
    internal static void SetLlm(LlmType type, string url ="", string promptFormat="", string apiKey="", string modelName = null)
    {
        switch (type)
        {
            case LlmType.Dummy:
                Instance = new LlmDummy();
                break;
            case LlmType.LlamaCpp:
                Instance = new LlmLlamaCpp(url, promptFormat);
                break;
            case LlmType.Gemini:
                Instance = new LlmGemini(apiKey);
                break;
            case LlmType.Claude:
                Instance = new LlmClaude(apiKey,modelName);
                break;
            case LlmType.OpenAi:
                Instance = new LlmOpenAi(apiKey,modelName);
                break;
            case LlmType.Mistral:
                Instance = new LlmMistral(apiKey,modelName);
                break;
            default:
                throw new NotImplementedException();
        }        
    }

    protected string url;

    private long _totalPrompts;
    private double _totalPromptTime;
    private long _totalInference;
    private double _totalInferenceTime;

    public abstract bool IsHighlySensoredModel { get; }

    public abstract string ExtraInstructions { get; }

    public string TokenStats => $"Prompt: {_totalPrompts} tokens in {_totalPromptTime}ms, Inference: {_totalInference} tokens in {_totalInferenceTime}ms";
    internal abstract string RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="");
    
    internal abstract Dictionary<string,double>[] RunInferenceProbabilities(string fullPrompt,int n_predict = 1);

    protected void AddToStats(JsonElement token_stats)
    {
        _totalPrompts += token_stats.GetProperty("prompt_n").GetInt32();
        _totalPromptTime += token_stats.GetProperty("prompt_ms").GetDouble();
        _totalInference += token_stats.GetProperty("predicted_n").GetInt32();
        _totalInferenceTime += token_stats.GetProperty("predicted_ms").GetDouble();
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
