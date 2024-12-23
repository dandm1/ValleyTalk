using ValleyTalk;

namespace StardewDialogue;

internal class LlmOAICompatible : LlmOpenAiBase, IGetModelNames
{
    public LlmOAICompatible(string apiKey, string url, string modelName = null)
    {
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