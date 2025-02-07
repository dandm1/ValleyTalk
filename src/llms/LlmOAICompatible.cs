using ValleyTalk;

namespace StardewDialogue;

internal class LlmOAICompatible : LlmOpenAiBase, IGetModelNames
{
    public LlmOAICompatible(string apiKey, string url, string modelName = null)
    {
        if (url.EndsWith("/")) url = url.Substring(0, url.Length - 1);
        if (url.EndsWith("/chat/completions")) url = url.Substring(0, url.Length - 16);
        if (url.EndsWith("/v1")) url = url.Substring(0, url.Length - 3);
        
        this.url = url;

        this.apiKey = apiKey;
        this.modelName = modelName ?? "mistral-large-latest";
    }

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => false;

    public string[] GetModelNames()
    {
        return CoreGetModelNames();
    }
}