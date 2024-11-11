using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace StardewDialogue;

internal class LlmOpenAi : Llm
{
    private static string url = "https://api.openai.com/v1/chat/completions";
    private string apiKey;

    record PromptElement
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public LlmOpenAi(string apiKey)
    {
        this.apiKey = apiKey;
    }

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => false;

    internal override string RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        var promptCached = gameCacheString;
        var inputString = JsonSerializer.Serialize(new
            {
                model = "gpt-4o",
                max_completion_tokens = n_predict,
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
        var fullUrl = url;
        while (retry > 0)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                request.Content = json;
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
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