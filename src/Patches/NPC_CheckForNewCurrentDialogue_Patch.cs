using HarmonyLib;
using StardewValley;

namespace ValleyTalk
{
    [HarmonyPatch(typeof(NPC), nameof(NPC.checkForNewCurrentDialogue))]
    public class NPC_CheckForNewCurrentDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, ref bool __result, int heartLevel, bool noPreface)
        {
            ModEntry.SMonitor.Log($"NPC {__instance.Name} checking for new dialogue at heart level {heartLevel}", StardewModdingAPI.LogLevel.Trace);
            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }

            // Check network availability early (Android only)
            if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
            {
                ModEntry.SMonitor.Log($"Network not available, skipping AI new dialogue check for {__instance.Name}", StardewModdingAPI.LogLevel.Trace);
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
}