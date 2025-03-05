using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace StardewDialogue
{

internal abstract class LlmOpenAiBase : Llm
{
    protected string apiKey;
    protected string modelName;

    class PromptElement
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    internal override async Task<string> RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        var payload = new
        {
            model = modelName,
            max_tokens = n_predict,
            messages = new PromptElement[]
            { 
                new PromptElement
                {
                    role = "system",
                    content = systemPromptString
                },
                new PromptElement
                {
                    role = "user",
                    content = gameCacheString + npcCacheString + promptString
                }
            }
        };
        
        var inputString = JsonConvert.SerializeObject(payload);
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
        var fullUrl = $"{url}/v1/chat/completions";
        while (retry > 0)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                request.Content = json;
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                var response = await client.SendAsync(request);
                // Return the 'content' element of the response json
                var responseString = response.Content.ReadAsStringAsync().Result;
                var responseJson = JObject.Parse(responseString);
                
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    if (responseJson["choices"] == null) { retry--; continue; }
                    var choices = responseJson["choices"] as JArray;
                    if (choices == null || choices.Count == 0) { retry--; continue; }

                    var message = choices[0]["message"];
                    var text = message["content"]?.ToString();
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
            var fullUrl = $"{url}/v1/models";
            var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            foreach (var header in extraHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
            var response = client.SendAsync(request).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            var responseJson = JObject.Parse(responseString);
            var models = responseJson["data"] as JArray;
            var modelNames = new List<string>();
            if (models != null)
            {
                foreach (var model in models)
                {
                    var id = model["id"]?.ToString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        modelNames.Add(id);
                    }
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
}