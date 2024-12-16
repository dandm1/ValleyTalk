using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using xTile.Dimensions;

namespace LlamaDialogue
{
    [HarmonyPatch(typeof(MarriageDialogueReference), nameof(MarriageDialogueReference.GetDialogue))]
    public class MarriageDialogueReference_GetDialogue_Patch
    {
        public static bool Prefix(ref MarriageDialogueReference __instance, ref Dialogue __result, NPC n)
        {
            var result = DialogueBuilder.Instance.Generate(n, __instance.DialogueKey);
            if (result != null)
            {
                __result = result;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.GetLocationOverrideDialogue))]
    public class GameLocation_GetLocationOverrideDialogue_Patch
    {

        public static bool Prefix(ref GameLocation __instance, ref string __result, NPC character)
        {
            if (character == null)
            {
                return true;
            }
            __result = SldConstants.DialogueGenerationTag;
            return false;
        }
    }

    [HarmonyPatch(typeof(Dialogue), nameof(Dialogue.chooseResponse))]
    public class Dialogue_ChooseResponse_Patch
    {
        private static System.Reflection.FieldInfo isLastDialogueInteractiveField;
        private static System.Reflection.FieldInfo finishedLastDialogueField;
        private static System.Reflection.MethodInfo parseDialogueStringMethod;

        static Dialogue_ChooseResponse_Patch()
        {

            isLastDialogueInteractiveField = typeof(Dialogue).GetField("isLastDialogueInteractive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            finishedLastDialogueField = typeof(Dialogue).GetField("finishedLastDialogue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            //isCurrentStringContinuedOnNextScreenField = typeof(Dialogue).GetField("isCurrentStringContinuedOnNextScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            parseDialogueStringMethod = typeof(Dialogue).GetMethod("parseDialogueString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        public static bool Prefix(ref Dialogue __instance, ref bool __result, Response response)
        {
            if (__instance.getResponseOptions().Any(r => !r.responseKey.StartsWith(SldConstants.DialogueKeyPrefix)))
            {
                return true;
            }
            if (response.responseKey == $"{SldConstants.DialogueKeyPrefix}Silent")
            {
                DialogueBuilder.Instance.AddConversation(__instance.speaker,"");
                __result = true;
                return false;
            }
            // Set the isLastDialogueInteractive flag to false using reflection
            
            finishedLastDialogueField.SetValue(__instance, false);

            var key = __instance.speaker.LoadedDialogueKey;
            // Get the current dialogue string from __instance
            // If the last entry is "Respond:", remove it
            var dialogueStrings = __instance.dialogues;
            if (dialogueStrings.Last().Text == "Respond:")
            {
                dialogueStrings.RemoveAt(dialogueStrings.Count - 1);
            }
            // Find the last index of "Respond:" in the list
            //var responseIndex = dialogueStrings.FindLastIndex(x => x.Text == "Respond:");
            //if (responseIndex >= 0)
            //{
                // Remove all entries up to and including the last "Respond:"
            //    dialogueStrings.RemoveRange(0, responseIndex + 1);
           // }
            var previous = DialogueBuilder.Instance.LastContext.ChatHistory;
            var dialogueStringIEnum = dialogueStrings.Where(x => !previous.Any(y => y.Contains(x.Text)) && x.Text != "skip").Select(x => x.Text);
            var dialogueStringConcat = string.Join(" ", dialogueStringIEnum);
            var newDialogue = DialogueBuilder.Instance.GenerateResponse(__instance.speaker, new [] { dialogueStringConcat,response.responseText}.ToArray());

            if (!newDialogue.Contains("$q"))
            {
                DialogueBuilder.Instance.AddConversation(__instance.speaker, newDialogue);
            }
            // Call parseDialogueString using reflection
            parseDialogueStringMethod.Invoke(__instance, new object[] { newDialogue, key });
            __instance.isCurrentStringContinuedOnNextScreen = true;
            isLastDialogueInteractiveField.SetValue(__instance, newDialogue.Contains("$q"));
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Dialogue), nameof(Dialogue.TryGetDialogue))]
    public class Dialogue_TryGetDialogue_Patch
    {
        public static bool Prefix(ref Dialogue __instance, ref Dialogue __result, NPC speaker, string translationKey)
        {
            if (translationKey.StartsWith("Characters\\Dialogue\\rainy:"))
            {
                __result = new Dialogue(speaker, translationKey, SldConstants.DialogueGenerationTag);
                return false;
            }
            return true;
        }
    }
}