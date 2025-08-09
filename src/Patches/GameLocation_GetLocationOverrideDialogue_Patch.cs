using HarmonyLib;
using StardewValley;

namespace ValleyTalk
{
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.GetLocationOverrideDialogue))]
    public class GameLocation_GetLocationOverrideDialogue_Patch
    {
        public static bool Prefix(ref GameLocation __instance, ref string __result, NPC character)
        {
            ModEntry.SMonitor.Log($"GameLocation.GetLocationOverrideDialogue called for {character?.Name} in {__instance.Name}", StardewModdingAPI.LogLevel.Trace);
            if (character == null)
            {
                return true;
            }
            if (!DialogueBuilder.Instance.PatchNpc(character, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }
            __result = SldConstants.DialogueGenerationTag;
            return false;
        }
    }
}