using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ValleyTalk;

namespace StardewDialogue;

internal class LlmGemini : Llm, IGetModelNames
{
    private string apiKey;
    private string modelName;

    public LlmGemini(string apiKey, string modelName = null)
    {
        this.apiKey = apiKey;
        this.modelName = modelName ?? "gemini-1.5-flash";

        url = $"https://generativelanguage.googleapis.com/v1beta/models/{this.modelName}:generateContent?key=";
    }

    public Dictionary<string,string> CacheContexts { get; private set; } = new Dictionary<string, string>();

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => false;

    public string[] GetModelNames()
    {
        try{
        var modelsUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key="+apiKey;
        var client = new HttpClient();
        var response = client.GetAsync(modelsUrl).Result;
        var responseString = response.Content.ReadAsStringAsync().Result;
        var responseJson = JsonDocument.Parse(responseString);
        var models = responseJson.RootElement.GetProperty("models");
        var modelNames = new List<string>();
        foreach (var model in models.EnumerateArray())
        {
            var name = model.GetProperty("name").GetString();
            if (name.StartsWith("models/"))
            {
                name = name.Substring(7);
            }
            modelNames.Add(name);
        }
        return modelNames.ToArray();
        }
        catch(Exception ex)
        {
            Log.Debug(ex.Message);
            return new string[] { };
        }
    }

    internal override async Task<string> RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        var useContext = string.Empty;

        promptString = gameCacheString + npcCacheString + promptString;
        if (!string.IsNullOrEmpty(cacheContext))
        {
            useContext = CacheContexts[cacheContext];
        }

        var json = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(new
            {
                safetySettings = new[] 
                { 
                    new {category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE"},
                    new {category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_MEDIUM_AND_ABOVE"}
                },
                system_instruction = new { parts = new { text = systemPromptString } },
                contents = new { parts = new { text = promptString } },
                generationConfig = new { maxOutputTokens = n_predict, temperature = 1.5, topP = 0.9 },
                //cachedContent= useContext
            }),
            Encoding.UTF8,
            "application/json"
        );

        // call out to URL passing the object as the body, and return the result
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(ModEntry.Config.QueryTimeout)
        };
        int retry=3;
        var fullUrl = url + apiKey;
        while (retry > 0)
        {
            try
            {
                var response = await client.PostAsync(fullUrl, json);
                // Return the 'content' element of the response json
                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseString);
                
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    
                    if (!responseJson.RootElement.TryGetProperty("candidates", out var candidates)) { retry--; continue; }
                    var candidateEnumerator = candidates.EnumerateArray();
                    if (!candidateEnumerator.MoveNext()) { retry--; continue; }
                    var candidate = candidateEnumerator.Current;
                    if (!candidate.TryGetProperty("finishReason", out var finishReason)) { retry--; continue; }
                    if (finishReason.GetString() != "STOP") { retry--; continue; }
                    if (!candidate.TryGetProperty("content", out var content)) { retry--; continue; }
                    var parts = content.GetProperty("parts").EnumerateArray();
                    if (!parts.MoveNext()) { retry--; continue; }
                    var firstPart = parts.Current;
                    var text = firstPart.GetProperty("text").GetString();
                    return text ?? string.Empty;
                }
            }
            catch(Exception ex)
            {
                Log.Debug(ex.Message);
                Log.Debug("Retrying...");
                retry--;
                Thread.Sleep(100);
            }
        }
        return "";
    }

    internal override Dictionary<string, double>[] RunInferenceProbabilities(string fullPrompt, int n_predict = 1)
    {
        throw new System.NotImplementedException();
    }
}