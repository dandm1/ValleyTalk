using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace LlamaDialogue
{
    public partial class ModEntry : Mod
    {
        private static IMonitor SMonitor;
        private static IModHelper SHelper;
        public static ModConfig Config;


        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            DialogueGenerator.Instance.Config = Config;
            
            SHelper = helper;
            SMonitor = Monitor;
            
            var harmony = new Harmony(ModManifest.UniqueID);

            harmony.Patch(
                original: AccessTools.Constructor(typeof(Dialogue), new Type[] { typeof(NPC), typeof(string), typeof(string) }),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(Dialogue_Postfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Dialogue), "prepareCurrentDialogueForDisplay"),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(Dialogue_prepareCurrentDialogueForDisplay_Prefix))
            );

            helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
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