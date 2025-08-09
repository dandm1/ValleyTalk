using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ValleyTalk;

internal class LlmDummy : Llm
{
    Random rand;
    public LlmDummy()
    {
        rand = new Random();
    }

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => true;

    internal override Task<string> RunInference(string systemPromptString, string gameCacheString, string npcCacheString, string promptString, string responseStart = "",int n_predict = 2048,string cacheContext="")
    {
        if (rand.NextDouble()<0.5)
        {
            return Task.FromResult("- LLM generated string.");
        }
        else
        {
            return Task.FromResult("- LLM generated question\n% One answer\n% Another answer\n% A third answer");
        }
    }

    internal override Dictionary<string, double>[] RunInferenceProbabilities(string fullPrompt, int n_predict = 1)
    {
        throw new System.NotImplementedException();
    }
}