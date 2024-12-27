namespace ValleyTalk
{
    public class ModConfig
    {
        public bool EnableMod { get; set; } = true;
        public bool Debug { get; set; } = false;
        public string Provider { get; set; } = "Anthropic";
        public string ModelName { get; set; } = "claude-3-5-haiku-latest";
        public string ServerAddress { get; set; } = "http://localhost:8080";
        public string PromptFormat { get; set; } = "[INST] {system}\n{prompt}[/INST]\n{response_start}";
        public string ApiKey { get; set; } = string.Empty;
        public bool ApplyTranslation { get; set; } = false;
    }
}
