using System;
using System.Collections.Generic;
using System.Linq; // Added
using System.Net.Http;
using System.Text;
using Newtonsoft.Json; // Changed
using Newtonsoft.Json.Linq; // Added
using System.Threading;
using System.Threading.Tasks;
using ValleyTalk;

namespace StardewDialogue;

internal abstract class LlmOpenAiBase : Llm
{
    protected string apiKey;
    protected string modelName;

    record PromptElement
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    internal override async Task<string> RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        var inputString = JsonConvert.SerializeObject(new // Changed
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
                var responseString = await response.Content.ReadAsStringAsync(); // Changed .Result to await
                var responseJson = JObject.Parse(responseString); // Changed
                
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    
                    if (!responseJson.TryGetValue("choices", out var choicesToken) || choicesToken.Type == JTokenType.Null) { retry--; continue; } // Changed
                    var choicesArray = choicesToken as JArray;
                    if (choicesArray == null || !choicesArray.HasValues) { retry--; continue; } // Changed

                    var firstChoice = choicesArray.FirstOrDefault();
                    if (firstChoice == null) { retry--; continue; } // Changed

                    var messageToken = firstChoice["message"];
                    if (messageToken == null || messageToken.Type == JTokenType.Null) { retry--; continue; } // Changed

                    var contentToken = messageToken["content"];
                    if (contentToken == null || contentToken.Type == JTokenType.Null) { retry--; continue; } // Changed
                    
                    var text = contentToken.ToString(); // Changed
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
        var responseJson = JObject.Parse(responseString); // Changed
        var dataToken = responseJson["data"];
        if (dataToken == null || dataToken.Type == JTokenType.Null || !(dataToken is JArray modelsArray)) // Changed and added checks
        {
            return Array.Empty<string>(); // Return empty array if data is not as expected
        }

        var modelNames = new List<string>();
        foreach (var model in modelsArray)
        {
            var idToken = model["id"];
            if (idToken != null && idToken.Type != JTokenType.Null)
            {
                modelNames.Add(idToken.ToString()); // Changed
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