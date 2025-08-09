using HarmonyLib;
using StardewValley;
using System;

namespace ValleyTalk
{
    [HarmonyPatch(typeof(NPC), "_PushTemporaryDialogue")]
    public class NPC_PushTemporaryDialogue_Patch
    {
        public static bool Prefix(ref NPC __instance, string translationKey)
        {
            ModEntry.SMonitor.Log($"NPC {__instance.Name} pushing temporary dialogue with key '{translationKey}'", StardewModdingAPI.LogLevel.Trace);

            if (!DialogueBuilder.Instance.PatchNpc(__instance, ModEntry.Config.GeneralFrequency, true))
            {
                return true;
            }

            // Check network availability early (Android only)
            if (!NetworkAvailabilityChecker.IsNetworkAvailableWithRetry())
            {
                ModEntry.SMonitor.Log($"Network not available, skipping AI temporary dialogue for {__instance.Name}", StardewModdingAPI.LogLevel.Trace);
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