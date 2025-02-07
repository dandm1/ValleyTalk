using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

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
        var inputString = JsonSerializer.Serialize(new
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
                var responseJson = JsonDocument.Parse(responseString);
                
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    
                    if (!responseJson.RootElement.TryGetProperty("choices", out var content)) { retry--; continue; }
                    var completionArray = content.EnumerateArray();
                    var completion = completionArray.MoveNext();
                    if (completion == false) { retry--; continue; }

                    var message = completionArray.Current.GetProperty("message");
                    var text = message.GetProperty("content").GetString();
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
        var responseJson = JsonDocument.Parse(responseString);
        var models = responseJson.RootElement.GetProperty("data").EnumerateArray();
        var modelNames = new List<string>();
        foreach (var model in models)
        {
            modelNames.Add(model.GetProperty("id").GetString());
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