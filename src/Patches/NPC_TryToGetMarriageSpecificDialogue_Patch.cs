using HarmonyLib;
using StardewValley;

namespace ValleyTalk
{
    [HarmonyPatch(typeof(NPC), nameof(NPC.tryToGetMarriageSpecificDialogue))]
    public class NPC_TryToGetMarriageSpecificDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, string dialogueKey)
        {
            ModEntry.SMonitor.Log($"NPC {__instance.Name} trying to get marriage specific dialogue with key '{dialogueKey}'", StardewModdingAPI.LogLevel.Trace);
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.MarriageFrequency))
            {
                return true;
            }

            // Check network availability early (Android only)
            if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
            {
                ModEntry.SMonitor.Log($"Network not available, skipping AI marriage dialogue for {__instance.Name}", StardewModdingAPI.LogLevel.Trace);
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
}