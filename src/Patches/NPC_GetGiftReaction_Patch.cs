using HarmonyLib;
using StardewValley;

namespace ValleyTalk
{
    [HarmonyPatch(typeof(NPC), nameof(NPC.GetGiftReaction))]
    public class NPC_GetGiftReaction_Patch
    {
        public static bool Prefix(ref NPC __instance, ref Dialogue __result, Farmer giver, StardewValley.Object gift, int taste)
        {
            ModEntry.SMonitor.Log($"NPC {__instance.Name} trying to get gift reaction for {gift.Name}", StardewModdingAPI.LogLevel.Trace);
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
                ModEntry.SMonitor.Log($"Network not available, skipping AI gift reaction for {__instance.Name}", StardewModdingAPI.LogLevel.Trace);
                return true; // Use default behavior
            }

            AsyncBuilder.Instance.RequestNpcGiftResponse(__instance, gift, taste);
            var result = new Dialogue(__instance, null, SldConstants.DialogueSkipTag);
            result.exitCurrentDialogue();
            __result = result;
            return false; // Prevent default behavior
        }
    }
}