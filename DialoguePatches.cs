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
    [HarmonyPatch(typeof(NPC), nameof(NPC.GetGiftReaction))]
    public class NPC_GetGiftReaction_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, Farmer giver, StardewValley.Object gift, int taste)
        {
            var dialogue = DialogueBuilder.Instance.GenerateGift(__instance, gift, taste);
            if (dialogue != null)
            {
                __result = dialogue;
                return false;
            }
            return true;
        }
    }
    
    [HarmonyPatch(typeof(NPC), nameof(NPC.tryToGetMarriageSpecificDialogue))]
    public class NPC_TryToGetMarriageSpecificDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, string dialogueKey)
        {
            if (dialogueKey.StartsWith("funReturn_") || dialogueKey.StartsWith("jobReturn_"))
            {
                __result = DialogueBuilder.Instance.Generate(__instance, dialogueKey);
                return false;
            }
            return true;
        }
    }

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

    [HarmonyPatch(typeof(NPC), nameof(NPC.CurrentDialogue), MethodType.Getter)]
    public class NPC_CurrentDialogue_Patch
    {
        private static int minLine = int.MaxValue;
        public static void Postfix(ref NPC __instance, ref Stack<Dialogue> __result)
        {
            if (__result.Count == 0) return;

            var trace = new System.Diagnostics.StackTrace().GetFrame(2);
            if (
                trace.GetMethod().Name == "drawDialogue" 
            )
            {
                var nextLine = __result.Peek().dialogues.First();
                if (nextLine.Text == SldConstants.DialogueGenerationTag)
                {
                    __result.Pop();
                    var newDialogue = DialogueBuilder.Instance.Generate(__instance, "default");
                    if (newDialogue != null)
                    {
                        __result.Push(newDialogue);
                    }
                    DialogueBuilder.Instance.AddDialogueLine(__instance, newDialogue.dialogues);
                }
                else
                {
                    var trace3 = new System.Diagnostics.StackTrace().GetFrame(2);
                    if (trace3.GetMethod().Name == "Speak")
                    {
                        var theEvent = Game1.currentLocation.currentEvent;
                        var festivalName = theEvent.FestivalName;
                        DialogueBuilder.Instance.AddEventLine(__instance, theEvent.actors, festivalName, __result.Peek().dialogues);
                    }
                    else
                    { 
                        var sourceLine = trace.GetILOffset();
                        if (sourceLine <= minLine)
                        {
                            DialogueBuilder.Instance.AddDialogueLine(__instance, __result.Peek().dialogues);
                            minLine = sourceLine;
                        }
                    }
                }
            }     
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.tryToRetrieveDialogue))]
    public class NPC_TryToRetrieveDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, string preface, int heartLevel, string appendToEnd)
        {
            __result = new Dialogue(__instance, $"{preface}_{heartLevel}", SldConstants.DialogueGenerationTag);
            return false;
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
            var dialogueString = dialogueStrings.Last().Text; // string.Join("#", dialogueStrings.Select(x => x.Text));
            var newDialogue = DialogueBuilder.Instance.GenerateResponse(__instance.speaker, new string[] { dialogueString, response.responseText });

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

    [HarmonyPatch(typeof(NPC), nameof(NPC.checkForNewCurrentDialogue))]
    public class NPC_CheckForNewCurrentDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref bool __result, int heartLevel, bool noPreface)
        {
            if (Game1.player.currentLocation.Name == "Saloon" || Game1.player.currentLocation.Name == "IslandSouth")
            {
                var newDialogue = new Dialogue(__instance, Game1.player.currentLocation.Name, SldConstants.DialogueGenerationTag);
                __instance.CurrentDialogue.Push(newDialogue);
                __result = true;
            }
            return true;
        }
    }
}