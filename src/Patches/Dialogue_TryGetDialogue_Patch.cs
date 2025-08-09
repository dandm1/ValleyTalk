using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace ValleyTalk
{

    [HarmonyPatch(typeof(Dialogue), nameof(Dialogue.TryGetDialogue))]
    public class Dialogue_TryGetDialogue_Patch
    {
        public static bool Prefix(ref Dialogue __instance, ref Dialogue __result, NPC speaker, string translationKey)
        {
            ModEntry.SMonitor.Log($"Dialogue.TryGetDialogue called for {speaker.Name} with key {translationKey}", StardewModdingAPI.LogLevel.Trace);
            if (!DialogueBuilder.Instance.PatchNpc(speaker, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }
            if (translationKey.StartsWith("Characters\\Dialogue\\rainy:"))
            {
                __result = new Dialogue(speaker, translationKey, SldConstants.DialogueGenerationTag);
                return false;
            }
            return true;
        }
    }
}