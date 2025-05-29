using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json; // Changed
using Newtonsoft.Json.Linq; // Added
using System.Threading;
using System.Threading.Tasks;
using ValleyTalk;
using ValleyTalk.Platform;

namespace StardewDialogue;

internal class LlmLlamaCpp : Llm
{
    public LlmLlamaCpp(string url, string promptFormat)
    {
        this.url = url;
        PromptFormat = promptFormat;
    }

    public string PromptFormat { get; }
    public override string ExtraInstructions => "Include only the new line and any responses in the output, no descriptions or explanations.";

    public override bool IsHighlySensoredModel => false;

    internal string BuildPrompt(string systemPromptString, string promptString, string responseStart = "")
    {
        return PromptFormat
            .Replace("{system}", systemPromptString)
            .Replace("{prompt}", promptString)
            .Replace("{response_start}", responseStart);
    }

    internal override async Task<string> RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {

        promptString = gameCacheString + npcCacheString + promptString;
        var fullPrompt = BuildPrompt(systemPromptString, promptString, responseStart);
        // Create a JSON object with the prompt and other parameters
        var json = new StringContent(
            JsonConvert.SerializeObject(new // Changed
            {
                prompt = fullPrompt,
                n_predict = n_predict,
                stream = false,
                temperature = n_predict == 1 ? 0 : 1.5,
                top_p = 0.88,
                min_p = 0.05,
                repeat_penalty = 1.05,
            }),
            Encoding.UTF8,
            "application/json"
        );

        // call out to URL passing the object as the body, and return the result
        bool retry = true;
        
        // Check network availability on Android
        if (AndroidHelper.IsAndroid && !NetworkHelper.IsNetworkAvailable())
        {
            throw new InvalidOperationException("Network not available");
        }
        
        while (retry)
        {
            try
            {
                retry = false;
                string responseString;
                
                if (AndroidHelper.IsAndroid)
                {
                    var jsonData = JsonConvert.SerializeObject(new
                    {
                        prompt = fullPrompt,
                        n_predict = n_predict,
                        stream = false,
                        temperature = n_predict == 1 ? 0 : 1.5,
                        top_p = 0.88,
                        min_p = 0.05,
                        repeat_penalty = 1.05,
                    });
                    responseString = await NetworkHelper.MakeRequestAsync(url, jsonData);
                }
                else
                {
                    var client = new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(ModEntry.Config.QueryTimeout)
                    };
                    var response = await client.PostAsync(url, json);
                    responseString = await response.Content.ReadAsStringAsync();
                }
                
                var responseJson = JObject.Parse(responseString); // Changed
                
                var token_stats = responseJson["timings"] as JObject; // Changed and cast to JObject
                AddToStats(token_stats); // No change needed here now
                
                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    var contentToken = responseJson["content"];
                    return contentToken?.ToString() ?? string.Empty; // Changed
                }
            }
            catch(Exception ex)
            {
                Log.Debug(ex.Message);
                Log.Debug("Retrying...");
                retry=true;
                Thread.Sleep(1000);
            }
        }
        return "";
    }

    internal override Dictionary<string,double>[] RunInferenceProbabilities(string fullPrompt,int n_predict = 1)
    {
      // Create a JSON object with the prompt and other parameters
        var json = new StringContent(
            JsonConvert.SerializeObject(new // Changed
            {
                prompt = fullPrompt,
                n_predict = n_predict,
                stream = false,
                temperature = 0.8,
                top_p = 0.88,
                min_p = 0.05,
                //repeat_penalty = 1.05,
                //presence_penalty = 0.0,
                cache_prompt = true,
                n_probs = 10
            }),
            Encoding.UTF8,
            "application/json"
        );

        // call out to URL passing the object as the body, and return the result
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(1)
        };
        bool retry=true;
        while (retry)
        {
            try
            {
                retry=false;
                var response = client.PostAsync(url, json).Result;
                // Return the 'content' element of the response json
                var responseString = response.Content.ReadAsStringAsync().Result;
                var responseJson = JObject.Parse(responseString); // Changed
                
                var token_stats = responseJson["timings"] as JObject; // Changed and cast to JObject
                AddToStats(token_stats); // No change needed here now

                if (responseJson == null)
                {
                    throw new Exception("Failed to parse response");
                }
                else
                {
                    var result = new List<Dictionary<string, double>>();
                    var probsToken = responseJson["completion_probabilities"]; // Changed
                    if (probsToken is JArray probsArray) // Changed
                    {
                        foreach (var prob in probsArray)
                        {
                            var probDict = new Dictionary<string,double>();
                            var innerProbsToken = prob["probs"];
                            if (innerProbsToken is JArray innerProbsArray) // Changed
                            {
                                foreach (var prop in innerProbsArray)
                                {
                                    var token = prop["tok_str"]?.ToString(); // Changed
                                    var probability = prop["prob"]?.Value<double>(); // Changed
                                    if (token != null && probability.HasValue)
                                    {
                                        probDict[token] = probability.Value;
                                    }
                                }
                            }
                            result.Add(probDict);
                        }
                    }
                    return result.ToArray();
                }
            }
            catch(Exception ex)
            {
                Log.Debug(ex.Message);
                Log.Debug("Retrying...");
                retry=true;
                Thread.Sleep(1000);
            }
        }
        return Array.Empty<Dictionary<string, double>>();
    }

}