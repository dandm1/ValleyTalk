namespace StardewDialogue;

internal enum LlmType
{
    LlamaCpp,
    Gemini,
    Claude,
    OpenAi,
    Mistral
#if DEBUG
    ,Dummy
#endif
}