using HarmonyLib;
using StardewValley;

namespace ValleyTalk
{

    [HarmonyPatch(typeof(NPC), nameof(NPC.addMarriageDialogue))]
    public class NPC_AddMarriageDialogue_Patch
    {
        // Add logic to handle nulls being returned - so we can skip the first porch lines
        public static bool Prefix(ref NPC __instance, string dialogue_file, string dialogue_key, bool gendered, string[] substitutions)
        {
            var dialogueRef = new MarriageDialogueReference(dialogue_file, dialogue_key, gendered, substitutions);
            if (!MarriageDialogueReference_GetDialogue_Patch.SkipGeneratedDialogue.Contains(dialogue_key))
            {
                __instance.shouldSayMarriageDialogue.Value = true;
                __instance.currentMarriageDialogue.Add(dialogueRef);
            }

            return false; // Skip original method
        }
    }        
}