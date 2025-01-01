using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using StardewValley.Network;

namespace ValleyTalk
{
    public class ModConfig
    {
        private string disableCharacters = string.Empty;

        public bool EnableMod { get; set; } = true;
        public bool Debug { get; set; } = false;
        public string Provider { get; set; } = "Anthropic";
        public string ModelName { get; set; } = "claude-3-5-haiku-latest";
        public string ServerAddress { get; set; } = "http://localhost:8080";
        public string PromptFormat { get; set; } = "[INST] {system}\n{prompt}[/INST]\n{response_start}";
        public string ApiKey { get; set; } = string.Empty;
        public bool ApplyTranslation { get; set; } = false;
        public int GeneralFrequency { get; set; } = 4;
        public int MarriageFrequency { get; set; } = 4;
        public int GiftFrequency { get; set; } = 4;
        public string DisableCharacters 
        { 
            get => disableCharacters; 
            set
            {
                disableCharacters = value;
                DisabledCharactersList = value
                            .Split(new[] {',',' ' })
                            .Select(s => s.Trim().ToTitleCase())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToList();
            }
        }
        [JsonIgnore]
        public List<string> DisabledCharactersList { get; private set; } = new List<string>();
    }
}
