
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace LlamaDialogue
{
    public class ModConfig
    {
        public bool EnableMod { get; set; } = true;
        public bool Debug { get; set; } = false;
        // IP address for the ML server
        public string ServerAddress { get; set; } = "http://mlpc:8080";
        // Hugging Face model name
        public string ModelName { get; set; } = string.Empty;
    }
}
