using ValleyTalk;

namespace ValleyTalk;

internal class LlmDeepSeek : LlmOpenAiBase, IGetModelNames
{
    public LlmDeepSeek(string apiKey, string modelName = null)
    {
        url = "https://api.deepseek.com";

        this.apiKey = apiKey;
        this.modelName = modelName ?? "deepseek-chat";
    }

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => false;

    public string[] GetModelNames()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return new string[] { };
        }
        return CoreGetModelNames();
    }
}