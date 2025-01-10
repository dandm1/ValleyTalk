using HarmonyLib;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValleyTalk
{
    [HarmonyPatch(typeof(NPC), nameof(NPC.GetGiftReaction))]
    public class NPC_GetGiftReaction_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, Farmer giver, StardewValley.Object gift, int taste)
        {
            if (!DialogueBuilder.Instance.PatchNpc(__instance,ModEntry.Config.GiftFrequency))
            {
                return true;
            }
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
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.MarriageFrequency))
            {
                return true;
            }
            if (dialogueKey.StartsWith("funReturn_") || dialogueKey.StartsWith("jobReturn_"))
            {
                __result = new Dialogue(__instance, dialogueKey, SldConstants.DialogueGenerationTag);
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
#if DEBUG
            ModEntry.SMonitor.Log($"Entering CurrentDialogue: {__instance.Name} {__result.Count}", StardewModdingAPI.LogLevel.Debug);
#endif
            if (__result.Count == 0) return;

#if DEBUG
            for(int i = 1; i < 10; i++)
            {
                var traceInt = new System.Diagnostics.StackTrace().GetFrame(i);
                ModEntry.SMonitor.Log($"Trace: {i}: {traceInt.GetMethod().Name} {traceInt.GetFileLineNumber()}", StardewModdingAPI.LogLevel.Debug);
            }
#endif
            var trace = new System.Diagnostics.StackTrace().GetFrame(2);
            if (
                trace.GetMethod().Name == "drawDialogue" 
            )
            {
                List<DialogueLine> theLine;
                var allLines = __result.Peek().dialogues;
                var nextLine = allLines.First();
#if DEBUG
                ModEntry.SMonitor.Log($"NextLine: {nextLine.Text}", StardewModdingAPI.LogLevel.Debug);
#endif
                string originalLine = string.Empty;
                if (nextLine.Text == SldConstants.DialogueGenerationTag)
                {
                    __result.Pop();
                    if (allLines.Count > 1)
                    {
                        allLines = allLines.Skip(1).ToList();
                        originalLine = string.Join(" ", allLines.Select(x => x.Text));
                    }
                    var newDialogue = DialogueBuilder.Instance.Generate(__instance, "default", originalLine);
                    if (newDialogue != null)
                    {
                        __result.Push(newDialogue);
                    }
                    theLine = newDialogue.dialogues;
                    DialogueBuilder.Instance.AddDialogueLine(__instance, newDialogue.dialogues);
                }
                else
                {
                    var trace3 = new System.Diagnostics.StackTrace().GetFrame(2);
                    theLine = __result.Peek().dialogues;
                    if (trace3.GetMethod().Name == "Speak")
                    {
                        var theEvent = Game1.currentLocation.currentEvent;
                        var festivalName = theEvent.FestivalName;
                        DialogueBuilder.Instance.AddEventLine(__instance, theEvent.actors, festivalName, theLine);
                    }
                    else
                    {
                        var sourceLine = trace.GetILOffset();
                        if (sourceLine <= minLine)
                        {
                            DialogueBuilder.Instance.AddDialogueLine(__instance, theLine);
                            minLine = sourceLine;
                        }
                    }
                }
                foreach(var npc in Util.GetNearbyNpcs(__instance))
                {
                    DialogueBuilder.Instance.AddOverheardLine(npc, __instance, theLine);
                }
            }
        }
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.tryToRetrieveDialogue))]
    public class NPC_TryToRetrieveDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, string preface, int heartLevel, string appendToEnd)
        {
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }
            __result = new Dialogue(__instance, $"{preface}_{heartLevel}", SldConstants.DialogueGenerationTag);
            return false;
        }
        
    }

    [HarmonyPatch(typeof(NPC), nameof(NPC.checkForNewCurrentDialogue))]
    public class NPC_CheckForNewCurrentDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref bool __result, int heartLevel, bool noPreface)
        {
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }
            if (Game1.player.currentLocation.Name == "Saloon" || Game1.player.currentLocation.Name == "IslandSouth")
            {
                var newDialogue = new Dialogue(__instance, Game1.player.currentLocation.Name, SldConstants.DialogueGenerationTag);
                __instance.CurrentDialogue.Push(newDialogue);
                __result = true;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(NPC), "_PushTemporaryDialogue")]
    public class NPC_PushTemporaryDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, string translationKey)
        {
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }
            try
            {
                if (translationKey.StartsWith("Resort"))
                {
                    string path = $"Resort_Marriage{translationKey[6..]}";
                    if (Game1.content.LoadStringReturnNullIfNotFound(path) != null)
                    {
                        translationKey = path;
                    }
                }
                if (__instance.CurrentDialogue.Count != 0 && !(__instance.CurrentDialogue.Peek().temporaryDialogueKey != translationKey))
                {
                    return true;
                }
                var originalString = Game1.content.LoadString(translationKey);
                originalString = $"{SldConstants.DialogueGenerationTag}#{originalString}";
                __instance.CurrentDialogue.Push(new Dialogue(__instance, translationKey, originalString)
                {
                    removeOnNextMove = true,
                    temporaryDialogueKey = translationKey
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

}