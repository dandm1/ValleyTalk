using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq; // Added
using System.Net.Http;
using System.Text;
using Newtonsoft.Json; // Changed
using Newtonsoft.Json.Linq; // Added
using System.Threading;
using System.Threading.Tasks;
using ValleyTalk;
using ValleyTalk.Platform;

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
        
        // Use Android-compatible network helper
        string responseString;
        if (AndroidHelper.IsAndroid && NetworkHelper.IsNetworkAvailable())
        {
            responseString = NetworkHelper.MakeRequestAsync(modelsUrl).Result;
        }
        else
        {
            var client = new HttpClient();
            var response = client.GetAsync(modelsUrl).Result;
            responseString = response.Content.ReadAsStringAsync().Result;
        }
        
        var responseJson = JObject.Parse(responseString); // Changed
        var modelsToken = responseJson["models"]; // Changed
        var modelNames = new List<string>();
        if (modelsToken is JArray modelsArray) // Changed
        {
            foreach (var model in modelsArray)
            {
                var nameToken = model["name"]; // Changed
                if (nameToken != null)
                {
                    var name = nameToken.ToString(); // Changed
                    if (name.StartsWith("models/"))
                    {
                        name = name.Substring(7);
                    }
                    modelNames.Add(name);
                }
            }
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

        var jsonData = JsonConvert.SerializeObject(new // Changed
            {
                safetySettings = new[] 
                { 
                    new {category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE"},
                    new {category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_MEDIUM_AND_ABOVE"}
                },
                system_instruction = new { parts = new { text = systemPromptString } },
                contents = new { parts = new { text = promptString } },
                generationConfig = new { maxOutputTokens = n_predict, temperature = 1.5, topP = 0.9, thinkingConfig = new { thinkingBudget = 0 } },
                //cachedContent= useContext
            });

        var json = new StringContent(
            jsonData,
            Encoding.UTF8,
            "application/json"
        );

        // call out to URL passing the object as the body, and return the result
        int retry = 3;
        var fullUrl = url + apiKey;
        
        // Check network availability on Android
        if (AndroidHelper.IsAndroid && !NetworkHelper.IsNetworkAvailable())
        {
            throw new InvalidOperationException("Network not available");
        }
        
        while (retry > 0)
        {
            try
            {
                string responseString;
                if (AndroidHelper.IsAndroid)
                {
                    responseString = await NetworkHelper.MakeRequestAsync(fullUrl, jsonData);
                }
                else
                {
                    var client = new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(ModEntry.Config.QueryTimeout)
                    };
                    var response = await client.PostAsync(fullUrl, json);
                    responseString = await response.Content.ReadAsStringAsync();
                }
                
                var responseJson = JObject.Parse(responseString); // Changed
                
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    
                    if (!responseJson.TryGetValue("candidates", out var candidatesToken) || !(candidatesToken is JArray candidatesArray) || !candidatesArray.HasValues) { retry--; continue; } // Changed
                    
                    var firstCandidate = candidatesArray.FirstOrDefault();
                    if (firstCandidate == null) { retry--; continue; } // Changed

                    var finishReasonToken = firstCandidate["finishReason"];
                    if (finishReasonToken == null || finishReasonToken.ToString() != "STOP") { retry--; continue; } // Changed

                    var contentToken = firstCandidate["content"];
                    if (contentToken == null) { retry--; continue; } // Changed

                    var partsToken = contentToken["parts"];
                    if (!(partsToken is JArray partsArray) || !partsArray.HasValues) { retry--; continue; } // Changed

                    var firstPart = partsArray.FirstOrDefault();
                    if (firstPart == null) { retry--; continue; } // Changed

                    var textToken = firstPart["text"];
                    if (textToken == null) { retry--; continue; } // Changed
                    
                    var text = textToken.ToString(); // Changed
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