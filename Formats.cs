namespace LlamaDialogue;

public static class PromptFormats
{
    public static PromptFormatter Llama3Format => new PromptFormatter(@"<|start_header_id|>system<|end_header_id|>

{0}<|eot_id|><|start_header_id|>user<|end_header_id|>

{1}<|eot_id|><|start_header_id|>assistant<|end_header_id|>

","<|eot_id|>");
    public static PromptFormatter CommandRFormat => new PromptFormatter(@"<|START_OF_TURN_TOKEN|><|SYSTEM_TOKEN|>{0}<|END_OF_TURN_TOKEN|><|START_OF_TURN_TOKEN|><|USER_TOKEN|>{1}<|END_OF_TURN_TOKEN|><|START_OF_TURN_TOKEN|><|CHATBOT_TOKEN|>","<|END_OF_TURN_TOKEN|>");
    public static PromptFormatter MistralFormat => new PromptFormatter("{0}\n\n<s>[INST]{1}[/INST]","</s>");
}
