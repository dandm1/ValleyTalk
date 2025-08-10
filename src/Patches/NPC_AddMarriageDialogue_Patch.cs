using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley;

namespace ValleyTalk
{

    [HarmonyPatch(typeof(NPC), nameof(NPC.addMarriageDialogue))]
    public class NPC_AddMarriageDialogue_Patch
    {
        private static List<string> SkipGeneratedDialogue = new List<string>
        {
            "NPC.cs.4463", // #$e#I also filled {0}'s water bowl.
            "NPC.cs.4462", // I got up early and watered some crops for you. I hope it makes your job a little easier today.
            "NPC.cs.4470", // I got up early to water some crops and they were already done! You've really got this place under control.$h
            "NPC.cs.4474", // I got up early and fed all the farm animals. I hope that makes your job a little easier today.
            "NPC.cs.4481",  // I spent the morning repairing a few of the fences. They should be as good as new.
            "MultiplePetBowls_watered", // I filled all the pet bowls with water.   
        };

        // Add logic to handle nulls being returned - so we can skip the first porch lines
        public static bool Prefix(ref NPC __instance, string dialogue_file, string dialogue_key, bool gendered, string[] substitutions)
        {
            var dialogueRef = new MarriageDialogueReference(dialogue_file, dialogue_key, gendered, substitutions);
            if (!SkipGeneratedDialogue.Contains(dialogue_key))
            {
                __instance.shouldSayMarriageDialogue.Value = true;
                __instance.currentMarriageDialogue.Add(dialogueRef);
            }
            else
            {
                try
                {
                    // Look up the canon line
                    string text = dialogueRef.DialogueFile + ":" + dialogueRef.DialogueKey;
                    string text2 = dialogueRef.IsGendered ? Game1.LoadStringByGender(__instance.Gender, text, dialogueRef.Substitutions) : Game1.content.LoadString(text, dialogueRef.Substitutions);
                    MarriageDialogueReference_GetDialogue_Patch.AddToNextDialogue.Add(text2);
                }
                catch (Exception)
                {
                    // If we can't find the canon line, just skip it
                }
            }

            return false; // Skip original method
        }
    }        
}