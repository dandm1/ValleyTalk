using HarmonyLib;
using StardewValley;
using System;
using System.Threading.Tasks;
using System.Collections.Generic; // Add reference to ValleyTalk namespace for TextInputHandler

namespace ValleyTalk
{
    [HarmonyPatch(typeof(MarriageDialogueReference), nameof(MarriageDialogueReference.GetDialogue))]
    public class MarriageDialogueReference_GetDialogue_Patch
    {
        
        public static List<string> SkipGeneratedDialogue = new List<string>
        {
            "NPC.cs.4463", // #$e#I also filled {0}'s water bowl.
            "NPC.cs.4462", // I got up early and watered some crops for you. I hope it makes your job a little easier today.
            "NPC.cs.4470", // I got up early to water some crops and they were already done! You've really got this place under control.$h
            "NPC.cs.4474", // I got up early and fed all the farm animals. I hope that makes your job a little easier today.
            "NPC.cs.4481",  // I spent the morning repairing a few of the fences. They should be as good as new.
            "MultiplePetBowls_watered", // I filled all the pet bowls with water.   
        };
        private static List<string> AddToNextDialogue = new List<string>();
        public static bool Prefix(ref MarriageDialogueReference __instance, ref Dialogue __result, NPC n)
        {
            ModEntry.SMonitor.Log($"MarriageDialogueReference.GetDialogue called for {n.Name} with key {__instance.DialogueKey}", StardewModdingAPI.LogLevel.Trace);
            var trace = new System.Diagnostics.StackTrace().GetFrames();

            if (!DialogueBuilder.Instance.PatchNpc(n, ModEntry.Config.MarriageFrequency))
            {
                return true;
            }
            if (SkipGeneratedDialogue.Contains(__instance.DialogueKey))
            {
                try
                {
                    // Look up the canon line
                    string text = __instance.DialogueFile + ":" + __instance.DialogueKey;
                    string text2 = __instance.IsGendered ? Game1.LoadStringByGender(n.Gender, text, __instance.Substitutions) : Game1.content.LoadString(text, __instance.Substitutions);
                    AddToNextDialogue.Add(text2);
                }
                catch (Exception)
                {
                    // If we can't find the canon line, just skip it
                }
                // Skip the morning chores dialogue generation for these specific keys
                __result = new Dialogue(n, __instance.DialogueKey, null);
                return false;
            }
            if (AsyncBuilder.Instance.AwaitingGeneration && AsyncBuilder.Instance.SpeakingNpc == n)
            {
                // If we are already awaiting a generation, skip this one
                return true;
            }
            Dialogue result;
            if (trace[2].GetMethod().Name.Contains("checkAction"))
            {
                result = new Dialogue(n, __instance.DialogueKey, SldConstants.DialogueGenerationTag);
            }
            else
            {
                Task<Dialogue> resultTask;
                if (AddToNextDialogue.Count > 0)
                {
                    try
                    {
                        string text = __instance.DialogueFile + ":" + __instance.DialogueKey;
                        string text2 = __instance.IsGendered ? Game1.LoadStringByGender(n.Gender, text, __instance.Substitutions) : Game1.content.LoadString(text, __instance.Substitutions);
                        AddToNextDialogue.Add(text2);
                    }
                    catch (Exception)
                    {
                        // If we can't find the canon line, just skip it
                    }
                    // If we have any lines to add to the next dialogue, do so
                    var nextDialogue = string.Join(" ", AddToNextDialogue);
                    AddToNextDialogue.Clear();
                    resultTask = DialogueBuilder.Instance.Generate(n, __instance.DialogueKey, nextDialogue);
                }
                else
                {
                    resultTask = DialogueBuilder.Instance.Generate(n, __instance.DialogueKey);
                }
                result = resultTask.Result;
            }

            if (result != null)
            {
                __result = result;
                return false;
            }

            return true;
        }
    }
}