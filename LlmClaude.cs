using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace StardewDialogue;

internal class LlmClaude : Llm
{
    private static string url = "https://api.anthropic.com/v1/messages";
    private string apiKey;

    class PromptElement
    {
        public string type { get; set; }
        public string text { get; set; }
        public object? cache_control { get; set; }
    }
    public LlmClaude(string apiKey)
    {
        this.apiKey = apiKey;
    }

    public Dictionary<string,string> CacheContexts { get; private set; } = new Dictionary<string, string>();

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => true;

    internal override string RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        var promptCached = gameCacheString;
        var inputString = JsonSerializer.Serialize(new
            {
                model = "claude-3-5-sonnet-20241022",
                max_tokens = n_predict,
                system = new PromptElement[]
                { 
                    new()
                    {
                        type = "text",
                        text = systemPromptString
                    },
                    new()
                    {
                        type = "text",
                        cache_control = new { type = "ephemeral" },
                        text = promptCached
                    }
                },
                messages = new[] 
                { 
                    new 
                    {
                        role = "user",
                        content = npcCacheString + promptString
                    }
                }
            });
        var json = new StringContent(
            inputString,
            Encoding.UTF8,
            "application/json"
        );

        // call out to URL passing the object as the body, and return the result
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(1)
        };

        int retry=3;
        var fullUrl = url;
        while (retry > 0)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                request.Content = json;
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Headers.Add("anthropic-beta", "prompt-caching-2024-07-31");
                var response = client.SendAsync(request).Result;
                // Return the 'content' element of the response json
                var responseString = response.Content.ReadAsStringAsync().Result;
                var responseJson = JsonDocument.Parse(responseString);
                
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    
                    if (!responseJson.RootElement.TryGetProperty("content", out var content)) { retry--; continue; }
                    var completionArray = content.EnumerateArray();
                    var completion = completionArray.MoveNext();
                    if (completion == false) { retry--; continue; }

                    var text = completionArray.Current.GetProperty("text").GetString();
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