using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

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
            if (__result.Count == 0)
            {
                return;
            }
            var trace = new System.Diagnostics.StackTrace().GetFrame(2);
            if (
                trace.GetMethod().Name == "drawDialogue" 
            )
            {
                var nextLine = __result.Peek().dialogues.First();
                if (nextLine.Text == "$$$%%%")
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

    [HarmonyPatch(typeof(NPC), nameof(NPC.tryToRetrieveDialogue))]
    public class NPC_TryToRetrieveDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, string preface, int heartLevel, string appendToEnd)
        {
            __result = new Dialogue(__instance, $"{preface}_{heartLevel}", "$$$%%%");
            return false;
        }
    }
}