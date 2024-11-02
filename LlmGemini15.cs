using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace StardewDialogue;

internal class LlmGemini15 : Llm
{
    private static string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=";
    private string apiKey;

    public LlmGemini15(string apiKey)
    {
        this.apiKey = apiKey;
    }

    public Dictionary<string,string> CacheContexts { get; private set; } = new Dictionary<string, string>();

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => false;

    internal override string RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        var useContext = string.Empty;
        /*if (!string.IsNullOrEmpty(cacheContext) && !CacheContexts.ContainsKey(cacheContext))
        {
            var contextJson = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                system_instruction = new { parts = new { text = systemPromptString } },
                contents = new { parts = new { text = promptString } },
                }),
            Encoding.UTF8,
            "application/json"
            );
        }*/
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
                //cachedContent= useContext
            }),
            Encoding.UTF8,
            "application/json"
        );

        // call out to URL passing the object as the body, and return the result
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(1)
        };
        int retry=3;
        var fullUrl = url + apiKey;
        while (retry > 0)
        {
            try
            {
                var response = client.PostAsync(fullUrl, json).Result;
                // Return the 'content' element of the response json
                var responseString = response.Content.ReadAsStringAsync().Result;
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
                Console.WriteLine(ex.Message);
                Console.WriteLine("Retrying...");
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