using HarmonyLib;
using StardewValley;
using StardewModdingAPI;

namespace ValleyTalk
{
    /// <summary>
    /// Patch for NPC.checkAction to allow initiating a conversation with typed dialogue
    /// </summary>
    [HarmonyPatch(typeof(NPC), nameof(NPC.checkAction))]
    public class PatchNpcCheckAction
    {

        /// <summary>
        /// Key to press while clicking on an NPC to initiate typed dialogue
        /// </summary>
        public static SButton InitiateTypedDialogueKey = ModEntry.Config.InitiateTypedDialogueKey;

        /// <summary>
        /// Prefix method for NPC.checkAction
        /// </summary>
        public static bool Prefix(ref NPC __instance, ref bool __result, Farmer who, GameLocation l)
        {

            // Check if the Alt key is being held down (or whatever key is configured)
            bool wasTriggerKeyDown = ModEntry.SHelper.Input.IsDown(InitiateTypedDialogueKey);

            // If the key is not held down, let the original method handle it
            if (!wasTriggerKeyDown)
            {
                return true;
            }

            // Make sure we can process dialogue with this NPC
            if (!DialogueBuilder.Instance.PatchNpc(__instance))
            {
                return true; // Let the original method handle NPCs we can't patch
            }

            DialogueBuilder.Instance.ClearContext();
            var character = DialogueBuilder.Instance.GetCharacter(__instance);  
            var prompt = Util.GetString(character, "uiStartConversation", new { Name = __instance.displayName }) ?? $"What do you want to say to {__instance.displayName}?";
            // Show text entry dialog for the player to type their dialogue
            TextInputManager.RequestTextInput
            (
                prompt,
                __instance
            );
            return false; // Prevent the original method from executing
        }
    }
}
