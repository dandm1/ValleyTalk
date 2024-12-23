using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using LlamaDialogue;
using Serilog;

namespace StardewDialogue;

internal class LlmOpenAi : LlmOpenAiBase, IGetModelNames
{
    public LlmOpenAi(string apiKey, string modelName = null)
    {
        url = "https://api.openai.com";
        this.apiKey = apiKey;
        this.modelName = modelName ?? "gpt-4o";
    }

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => false;

    public string[] GetModelNames()
    {
        return CoreGetModelNames();
    }
}
