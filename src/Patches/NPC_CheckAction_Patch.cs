using HarmonyLib;
using StardewValley;
using StardewModdingAPI;

namespace ValleyTalk
{
    /// <summary>
    /// Patch for NPC.checkAction to allow initiating a conversation with typed dialogue
    /// </summary>
    [HarmonyPatch(typeof(NPC), nameof(NPC.checkAction))]
    public class NPC_CheckAction_Patch
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

            // Check for cases when we should not allow initiating typed dialogue
            if (
                __instance.IsInvisible ||
                __instance.isSleeping.Value ||
                !who.CanMove ||
                !wasTriggerKeyDown ||
                !DialogueBuilder.Instance.PatchNpc(__instance)
                )
            {
                return true;
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
            __result = false;
            return false; // Prevent the original method from executing
        }
    }
}
