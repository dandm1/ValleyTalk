using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using ValleyTalk;
using System.Threading.Tasks;

namespace StardewDialogue;

internal class LlmClaude : Llm, IGetModelNames
{
    private readonly string apiKey;
    private readonly string modelName;

    class PromptElement
    {
#pragma warning disable IDE1006 // Naming Styles
        public string type { get; set; }
        public string text { get; set; }
        public object cache_control { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
    public LlmClaude(string apiKey, string modelName = null)
    {
        url = "https://api.anthropic.com/v1/messages";
        
        this.apiKey = apiKey;
        this.modelName = modelName ?? "claude-3-5-haiku-latest";
    }

    public Dictionary<string,string> CacheContexts { get; private set; } = new Dictionary<string, string>();

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => true;

    internal override async Task<string> RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        var promptCached = gameCacheString;
        var inputString = JsonConvert.SerializeObject(new
            {
                model = this.modelName,
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
            Timeout = TimeSpan.FromSeconds(ModEntry.Config.QueryTimeout)
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
                var response = await client.SendAsync(request);
                // Return the 'content' element of the response json
                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JObject.Parse(responseString);
                
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    
                    if (!responseJson.TryGetValue("content", out var contentToken) || contentToken.Type == JTokenType.Null) { retry--; continue; }
                    var contentArray = contentToken as JArray;
                    if (contentArray == null || !contentArray.HasValues) { retry--; continue; }

                    var firstContentElement = contentArray.FirstOrDefault();
                    if (firstContentElement == null || firstContentElement["text"] == null) { retry--; continue; }

                    var text = firstContentElement["text"].ToString();
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
        throw new NotImplementedException();
    }

    public string[] GetModelNames()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return new string[] { };
        }
        try 
        {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(1)
        };
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        var response = client.SendAsync(request).Result;
        var responseString = response.Content.ReadAsStringAsync().Result;
        var responseJson = JObject.Parse(responseString);
        var models = responseJson["data"] as JArray;
        var modelNames = new List<string>();
        if (models != null)
        {
            foreach (var model in models)
            {
                modelNames.Add(model["id"].ToString());
            }
        }
        return modelNames.ToArray();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return new string[] { };
        }
    }
}