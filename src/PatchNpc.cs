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
            ModEntry.SMonitor.Log($"NPC {__instance.Name} trying to get gift reaction for {gift.Name}", StardewModdingAPI.LogLevel.Debug);
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GiftFrequency))
            {
                return true;
            }
            if (AsyncBuilder.Instance.AwaitingGeneration && AsyncBuilder.Instance.SpeakingNpc == __instance)
            {
                // If we are already awaiting a generation, skip this one
                return true;
            }
                        
            // Check network availability early (Android only)
            if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
            {
                ModEntry.SMonitor.Log($"Network not available, skipping AI gift reaction for {__instance.Name}", StardewModdingAPI.LogLevel.Info);
                return true; // Use default behavior
            }
            
            AsyncBuilder.Instance.RequestNpcGiftResponse(__instance, gift, taste);
            var result = new Dialogue(__instance, null, null);
            result.exitCurrentDialogue();
            __result = result;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(NPC), nameof(NPC.tryToGetMarriageSpecificDialogue))]
    public class NPC_TryToGetMarriageSpecificDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, string dialogueKey)
        {
            ModEntry.SMonitor.Log($"NPC {__instance.Name} trying to get marriage specific dialogue with key '{dialogueKey}'", StardewModdingAPI.LogLevel.Debug);
             if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.MarriageFrequency))
            {
                return true;
            }
                       
            // Check network availability early (Android only)
            if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
            {
                ModEntry.SMonitor.Log($"Network not available, skipping AI marriage dialogue for {__instance.Name}", StardewModdingAPI.LogLevel.Info);
                return true; // Use default behavior
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
            if (__result.Count == 0) return;

            var trace = new System.Diagnostics.StackTrace().GetFrame(2);
            if (
                trace.GetMethod().Name.Contains("drawDialogue")
            )
            {
                List<DialogueLine> theLine;
                var allLines = __result.Peek().dialogues;
                var nextLine = allLines.First();

                string originalLine = string.Empty;
                if (nextLine.Text == SldConstants.DialogueGenerationTag)
                {
                    ModEntry.SMonitor.Log($"NPC {__instance.Name} is generating dialogue", StardewModdingAPI.LogLevel.Debug);
                    
                    // Check network availability early (Android only)
                    if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
                    {
                        ModEntry.SMonitor.Log($"Network not available, skipping AI dialogue generation for {__instance.Name}", StardewModdingAPI.LogLevel.Info);
                        // In this context we need to return a single line of dialogue "..."
                        __result.Pop();
                        __result.Push(new Dialogue(__instance, "", "..."));
                        // Let default dialogue continue
                        return;
                    }
                    
                    __result.Pop();
                    if (allLines.Count > 1)
                    {
                        allLines = allLines.Skip(1).ToList();
                        originalLine = string.Join(" ", allLines.Select(x => x.Text));
                    }
                    AsyncBuilder.Instance.RequestNpcBasic(__instance, "default", originalLine);
                    __result = null;
                    return;
                }
                else
                {
                    ModEntry.SMonitor.Log($"NPC {__instance.Name} recording line: {nextLine.Text}", StardewModdingAPI.LogLevel.Debug);
                    var trace3 = new System.Diagnostics.StackTrace().GetFrame(2);
                    theLine = __result.Peek().dialogues;
                    if (trace3.GetMethod().Name.StartsWith("Speak"))
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
            ModEntry.SMonitor.Log($"NPC {__instance.Name} trying to retrieve dialogue with preface '{preface}' at heart level {heartLevel}", StardewModdingAPI.LogLevel.Debug);
            
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }            
            // Check network availability early (Android only)
            if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
            {
                ModEntry.SMonitor.Log($"Network not available, skipping AI dialogue retrieval for {__instance.Name}", StardewModdingAPI.LogLevel.Info);
                return true; // Use default behavior
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
            ModEntry.SMonitor.Log($"NPC {__instance.Name} checking for new dialogue at heart level {heartLevel}", StardewModdingAPI.LogLevel.Debug);
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }

            // Check network availability early (Android only)
            if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
            {
                ModEntry.SMonitor.Log($"Network not available, skipping AI new dialogue check for {__instance.Name}", StardewModdingAPI.LogLevel.Info);
                return true; // Use default behavior
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
            ModEntry.SMonitor.Log($"NPC {__instance.Name} pushing temporary dialogue with key '{translationKey}'", StardewModdingAPI.LogLevel.Debug);
            
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }
            
            // Check network availability early (Android only)
            if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
            {
                ModEntry.SMonitor.Log($"Network not available, skipping AI temporary dialogue for {__instance.Name}", StardewModdingAPI.LogLevel.Info);
                return true; // Use default behavior
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