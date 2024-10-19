
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LlamaDialogue
{
    public class ModConfig
    {
        public bool EnableMod { get; set; } = true;
        public bool Debug { get; set; } = false;
        // IP address for the ML server
        public bool UseLocalhost { get; set; } = true;
        public string ServerAddress { get; set; } = "http://mlpc:8080";
        public string PromptFormat { get; set; } = "[INST] {system}\n{prompt}[/INST]\n{response_start}";
        public string ApiKey { get; set; } = string.Empty;
    }
}
