using HarmonyLib;
using StardewValley;

namespace ValleyTalk
{
    [HarmonyPatch(typeof(NPC), nameof(NPC.tryToRetrieveDialogue))]
    public class NPC_TryToRetrieveDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, string preface, int heartLevel, string appendToEnd)
        {
            ModEntry.SMonitor.Log($"NPC {__instance.Name} trying to retrieve dialogue with preface '{preface}' at heart level {heartLevel}", StardewModdingAPI.LogLevel.Trace);

            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }
            // Check network availability early (Android only)
            if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
            {
                ModEntry.SMonitor.Log($"Network not available, skipping AI dialogue retrieval for {__instance.Name}", StardewModdingAPI.LogLevel.Trace);
                return true; // Use default behavior
            }

            __result = new Dialogue(__instance, $"{preface}_{heartLevel}", SldConstants.DialogueGenerationTag);
            return false;
        }

    }
}