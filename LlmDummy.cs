using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace StardewDialogue;

internal class LlmDummy : Llm
{
    public LlmDummy()
    {
        
    }

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => true;

    internal override string RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        return "LLM generated string.";
    }

    internal override Dictionary<string, double>[] RunInferenceProbabilities(string fullPrompt, int n_predict = 1)
    {
        throw new System.NotImplementedException();
    }
}