using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Serilog;

namespace StardewDialogue;

internal class LlmMistral : LlmOpenAiCompatible
{
    public LlmMistral(string apiKey, string modelName = null)
    {
        url = "https://api.mistral.ai/v1/chat/completions";

        this.apiKey = apiKey;
        this.modelName = modelName ?? "mistral-large-latest";
    }

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => false;

}