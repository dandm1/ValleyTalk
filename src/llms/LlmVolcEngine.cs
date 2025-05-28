using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValleyTalk;

namespace StardewDialogue;

internal class LlmVolcEngine : Llm, IGetModelNames
{
    protected string apiKey;
    protected string modelName;

    record PromptElement
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public LlmVolcEngine(string apiKey, string modelName = null)
    {
        url = "https://ark.cn-beijing.volces.com/api/v3";

        this.apiKey = apiKey;
        this.modelName = modelName ?? "doubao-1.5-pro";
    }

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => false;

    public string[] GetModelNames()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return Array.Empty<string>();
        }
        return CoreGetModelNames();
    }

    internal override async Task<string> RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        var inputString = JsonConvert.SerializeObject(new
            {
                model = modelName,
                max_tokens = n_predict,
                messages = new PromptElement[]
                { 
                    new()
                    {
                        role = "system",
                        content = systemPromptString
                    },
                    new()
                    {
                        role = "user",
                        content = gameCacheString + npcCacheString + promptString
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
        var fullUrl = $"{url}/chat/completions";
        while (retry > 0)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                request.Content = json;
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
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
                    
                    if (!responseJson.TryGetValue("choices", out var choicesToken) || !(choicesToken is JArray choicesArray) || !choicesArray.HasValues) { retry--; continue; }

                    var firstChoice = choicesArray.FirstOrDefault();
                    if (firstChoice == null) { retry--; continue; }

                    var messageToken = firstChoice["message"];
                    if (messageToken == null) { retry--; continue; }

                    var contentToken = messageToken["content"];
                    if (contentToken == null) { retry--; continue; }
                    
                    var text = contentToken.ToString();
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

    public string[] CoreGetModelNames(Dictionary<string, string> extraHeaders = null)
    {
        if (extraHeaders == null)
        {
            extraHeaders = new Dictionary<string, string>();
        }
        try 
        {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(1)
        };
        var fullUrl = $"{url}/models";
        var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        foreach (var header in extraHeaders)
        {
            request.Headers.Add(header.Key, header.Value);
        }
        var response = client.SendAsync(request).Result;
        var responseString = response.Content.ReadAsStringAsync().Result;
        var responseJson = JObject.Parse(responseString);
        var dataToken = responseJson["data"];
        if (!(dataToken is JArray modelsArray))
        {
            return Array.Empty<string>();
        }

        var modelNames = new List<string>();
        foreach (var model in modelsArray)
        {
            var idToken = model["id"];
            if (idToken != null)
            {
                modelNames.Add(idToken.ToString());
            }
        }
        return modelNames.ToArray();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return Array.Empty<string>();
        }
    }
}