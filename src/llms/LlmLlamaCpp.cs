using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ValleyTalk;

namespace StardewDialogue;

internal class LlmLlamaCpp : Llm
{
    public LlmLlamaCpp(string url, string promptFormat)
    {
        this.url = url;
        PromptFormat = promptFormat;
    }

    public string PromptFormat { get; }
    public override string ExtraInstructions => "Include only the new line and any responses in the output, no descriptions or explanations.";

    public override bool IsHighlySensoredModel => false;

    internal string BuildPrompt(string systemPromptString, string promptString, string responseStart = "")
    {
        return PromptFormat
            .Replace("{system}", systemPromptString)
            .Replace("{prompt}", promptString)
            .Replace("{response_start}", responseStart);
    }

    internal override async Task<string> RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {

        promptString = gameCacheString + npcCacheString + promptString;
        var fullPrompt = BuildPrompt(systemPromptString, promptString, responseStart);
        // Create a JSON object with the prompt and other parameters
        var json = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                prompt = fullPrompt,
                n_predict = n_predict,
                stream = false,
                temperature = n_predict == 1 ? 0 : 1.5,
                top_p = 0.88,
                min_p = 0.05,
                repeat_penalty = 1.05,
            }),
            Encoding.UTF8,
            "application/json"
        );

        // call out to URL passing the object as the body, and return the result
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(ModEntry.Config.QueryTimeout)
        };
        bool retry=true;
        while (retry)
        {
            try
            {
                retry=false;
                var response = await client.PostAsync(url, json);
                // Return the 'content' element of the response json
                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseString);
                
                var token_stats = responseJson.RootElement.GetProperty("timings");
                AddToStats(token_stats);
                
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    return responseJson.RootElement.GetProperty("content").GetString() ?? string.Empty;
                }
            }
            catch(Exception ex)
            {
                Log.Debug(ex.Message);
                Log.Debug("Retrying...");
                retry=true;
                Thread.Sleep(1000);
            }
        }
        return "";
    }

    internal override Dictionary<string,double>[] RunInferenceProbabilities(string fullPrompt,int n_predict = 1)
    {
      // Create a JSON object with the prompt and other parameters
        var json = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                prompt = fullPrompt,
                n_predict = n_predict,
                stream = false,
                temperature = 0.8,
                top_p = 0.88,
                min_p = 0.05,
                //repeat_penalty = 1.05,
                //presence_penalty = 0.0,
                cache_prompt = true,
                n_probs = 10
            }),
            Encoding.UTF8,
            "application/json"
        );

        // call out to URL passing the object as the body, and return the result
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(1)
        };
        bool retry=true;
        while (retry)
        {
            try
            {
                retry=false;
                var response = client.PostAsync(url, json).Result;
                // Return the 'content' element of the response json
                var responseString = response.Content.ReadAsStringAsync().Result;
                var responseJson = System.Text.Json.JsonDocument.Parse(responseString);
                
                var token_stats = responseJson.RootElement.GetProperty("timings");
                AddToStats(token_stats);
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    var result = new List<Dictionary<string, double>>();
                    var probs = responseJson.RootElement.GetProperty("completion_probabilities");
                    foreach (var prob in probs.EnumerateArray())
                    {
                        var probDict = new Dictionary<string,double>();
                        foreach (var prop in prob.GetProperty("probs").EnumerateArray())
                        {
                            if (prop.TryGetProperty("tok_str", out var token) && prop.TryGetProperty("prob", out var probability))
                            {
                                probDict[token.GetString() ?? string.Empty] = probability.GetDouble();
                            }
                        }
                        result.Add(probDict);
                    }
                    return result.ToArray();
                }
            }
            catch(Exception ex)
            {
                Log.Debug(ex.Message);
                Log.Debug("Retrying...");
                retry=true;
                Thread.Sleep(1000);
            }
        }
        return Array.Empty<Dictionary<string, double>>();
    }

}