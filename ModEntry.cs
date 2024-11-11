using System;
using System.IO;
using HarmonyLib;
using Microsoft.VisualBasic;
using StardewDialogue;
using StardewModdingAPI;
using StardewValley;

namespace LlamaDialogue
{
    public partial class ModEntry : Mod
    {
        private static IMonitor SMonitor;
        public static IModHelper SHelper { get; private set; }
        public static ModConfig Config;


        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            if (!Config.EnableMod)
            {
                return;
            }

            Enum.TryParse<LlmType>(Config.UseHost, ignoreCase: true, result: out LlmType llmType);

            Llm.SetLlm(llmType, apiKey: Config.ApiKey, url: Config.ServerAddress, promptFormat: Config.PromptFormat);

            DialogueBuilder.Instance.Config = Config;
            
            SHelper = helper;
            SMonitor = Monitor;
            
            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();

            if (Config.Debug)
            {
                using (var log = new StreamWriter($"Generation.log", true))
                {
                    log.WriteLine($"###############################################");
                    log.WriteLine($"###############################################");
                    log.WriteLine($"###############################################");
                    log.WriteLine($"[{System.DateTime.Now}] Mod loaded");
                }
            }
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
        }

        private void Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (!Config.Debug)
                return;
            if (e.Button == SButton.NumLock)
			{
				var person = Game1.getCharacterFromName("Emily");
                var ds = person.CurrentDialogue;
                //Game1.warpCharacter(person, Game1.player.currentLocation, Game1.player.currentLocation. + new Microsoft.Xna.Framework.Vector2(0, 1));
                person.CurrentDialogue.Clear();
                person.addMarriageDialogue("Strings\\StringsFromCSFiles", "NPC.cs.4486", false, new string[]
                {
                    "%endearmentlower"
                });

                return;
            }
            if (e.Button == SButton.F3)
            {
                var person = Game1.getCharacterFromName("Marnie");
                person.sayHiTo(Game1.getCharacterFromName("Lewis"));
                return;
            }
        }
    }
}