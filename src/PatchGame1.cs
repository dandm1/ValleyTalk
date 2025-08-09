using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Add reference to ValleyTalk namespace for TextInputHandler

namespace ValleyTalk
{
    [HarmonyPatch(typeof(Game1), nameof(Game1.DrawDialogue), new Type[] { typeof(Dialogue) })]
    public class Game1_DrawDialogue_Patch
    {

        public static bool Prefix(Dialogue dialogue)
        {
            if (dialogue == null || dialogue.dialogues == null || dialogue.dialogues.Count == 0)
            {
                return true; // Allow original method to execute if no dialogues
            }

            if (dialogue.dialogues.First().Text.StartsWith(SldConstants.DialogueSkipTag))
            {
                return false; // Skip the original method
            }

            return true; // Allow original method to execute
        }
    }

}