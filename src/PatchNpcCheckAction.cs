using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;

namespace ValleyTalk
{
    /// <summary>
    /// Patch for NPC.checkAction to allow initiating a conversation with typed dialogue
    /// </summary>
    [HarmonyPatch(typeof(NPC), nameof(NPC.checkAction))]
    public class PatchNpcCheckAction
    {
        private static bool wasAltKeyDown = false;

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
            bool isTypedDialogueKeyDown = ModEntry.SHelper.Input.IsDown(InitiateTypedDialogueKey);

            // If the key is not held down, let the original method handle it
            if (!isTypedDialogueKeyDown)
            {
                wasAltKeyDown = false;
                return true;
            }

            // Prevent multiple triggers from the same key press
            if (wasAltKeyDown)
            {
                return false;
            }

            wasAltKeyDown = true;

            // Make sure we can process dialogue with this NPC
            if (!DialogueBuilder.Instance.PatchNpc(__instance))
            {
                return true; // Let the original method handle NPCs we can't patch
            }

            // Show text entry dialog for the player to type their dialogue
            TextInputManager.RequestTextInput($"What do you want to say to {__instance.displayName}?", __instance, "");
            return false; // Prevent the original method from executing
            string inputText = textInputHandler.GetString();
            
            // Process the input - if not empty, start a conversation
            if (!string.IsNullOrWhiteSpace(inputText))
            {
                // Add the player's line to the conversation
                DialogueBuilder.Instance.AddConversation(__instance, inputText, isPlayerLine: true);
                
                // Generate NPC response
                var newDialogueTask = DialogueBuilder.Instance.GenerateResponse(__instance, new[] { inputText });

                try
                {
                    var newDialogue = newDialogueTask.Result;

                    if (!newDialogue.Contains("$q")) // If not a question
                    {
                        DialogueBuilder.Instance.AddConversation(__instance, newDialogue);
                    }

                    // Show the dialogue response
                    var dialogueObj = new Dialogue(__instance, null, newDialogue);
                    // Add as the next dialogue line
                    __instance.CurrentDialogue.Push(dialogueObj);
                }
                catch (Exception ex)
                {
                    ModEntry.SMonitor.Log($"Error generating response: {ex.Message}", StardewModdingAPI.LogLevel.Error);
                }
            }

            // We've handled the interaction
            //__result = true;
            return true;
        }
    }
}
