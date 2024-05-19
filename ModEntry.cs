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

        private static Dictionary<string, FixedDialogueData> fixedDict = new Dictionary<string, FixedDialogueData>();
        internal static void Initialize(IMonitor monitor)
        {
            SMonitor = monitor;
        }   

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            SHelper = helper;
            var harmony = new Harmony(ModManifest.UniqueID);

            harmony.Patch(
                original: AccessTools.Constructor(typeof(Dialogue), new Type[] { typeof(NPC), typeof(string), typeof(string) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Dialogue_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Constructor(typeof(DialogueBox), new Type[] { typeof(Dialogue) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Dialogue_Box_Postfix))
            );
            
            harmony.Patch(
                original: AccessTools.Method(typeof(Dialogue), nameof(Dialogue.getCurrentDialogue)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Dialogue_getCurrentDialogue_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString), new Type[] { typeof(string), typeof(object), typeof(object), typeof(object) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.LocalizedContentManager_LoadString_Prefix3)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.LocalizedContentManager_LoadString_Postfix3))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString), new Type[] { typeof(string), typeof(object), typeof(object) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.LocalizedContentManager_LoadString_Prefix2)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.LocalizedContentManager_LoadString_Postfix2))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString), new Type[] { typeof(string), typeof(object) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.LocalizedContentManager_LoadString_Prefix1)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.LocalizedContentManager_LoadString_Postfix1))
            );
            
            harmony.Patch(
                original: AccessTools.Method(typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString), new Type[] { typeof(string) }),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.LocalizedContentManager_LoadString_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.getHi)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.NPC_getHi_Postfix))
            );

            helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
        }

        private void GameLoop_SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            fixedDict.Clear();
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