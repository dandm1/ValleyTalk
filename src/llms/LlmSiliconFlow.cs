using System;

namespace ValleyTalk;

internal class LlmSiliconFlow : LlmOpenAiBase, IGetModelNames
{
    public LlmSiliconFlow(string apiKey, string modelName = null)
        : this(apiKey, modelName, "https://api.siliconflow.com")
    {
    }

    protected LlmSiliconFlow(string apiKey, string modelName, string endpoint)
    {
        url = endpoint;
        this.apiKey = apiKey;
        this.modelName = modelName;
    }

    public override string ExtraInstructions => "";

    public override bool IsHighlySensoredModel => false;

    public string[] GetModelNames()
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return Array.Empty<string>();

        // Exclude embedding, reranking, image, and audio models from dialogue choices.
        return CoreGetModelNames(query: "?sub_type=chat");
    }
}

internal class LlmSiliconFlowCN : LlmSiliconFlow
{
    public LlmSiliconFlowCN(string apiKey, string modelName = null)
        : base(apiKey, modelName, "https://api.siliconflow.cn")
    {
    }
}
