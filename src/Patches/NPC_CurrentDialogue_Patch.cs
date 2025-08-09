using HarmonyLib;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace ValleyTalk
{
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
                List<StardewValley.DialogueLine> theLine;
                var allLines = __result.Peek().dialogues;
                var nextLine = allLines.First();

                string originalLine = string.Empty;
                if (nextLine.Text == SldConstants.DialogueGenerationTag)
                {
                    ModEntry.SMonitor.Log($"NPC {__instance.Name} is generating dialogue", StardewModdingAPI.LogLevel.Trace);

                    // Check network availability early (Android only)
                    if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
                    {
                        ModEntry.SMonitor.Log($"Network not available, skipping AI dialogue generation for {__instance.Name}", StardewModdingAPI.LogLevel.Trace);
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
                    Game1.currentSpeaker = __instance;
                    __result.Clear();
                    return;
                }
                else
                {
                    ModEntry.SMonitor.Log($"NPC {__instance.Name} recording line: {nextLine.Text}", StardewModdingAPI.LogLevel.Trace);
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
                foreach (var npc in Util.GetNearbyNpcs(__instance))
                {
                    DialogueBuilder.Instance.AddOverheardLine(npc, __instance, theLine);
                }
            }
        }
    }
}