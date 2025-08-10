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
        public static List<string> AddToNextDialogue = new List<string>();
        public static bool Prefix(ref MarriageDialogueReference __instance, ref Dialogue __result, NPC n)
        {
            ModEntry.SMonitor.Log($"MarriageDialogueReference.GetDialogue called for {n.Name} with key {__instance.DialogueKey}", StardewModdingAPI.LogLevel.Trace);
            var trace = new System.Diagnostics.StackTrace().GetFrames();

            if (!DialogueBuilder.Instance.PatchNpc(n, ModEntry.Config.MarriageFrequency))
            {
                return true;
            }

            if (AsyncBuilder.Instance.AwaitingGeneration && AsyncBuilder.Instance.SpeakingNpc == n)
            {
                // If we are already awaiting a generation, skip this one
                return true;
            }
            string nextDialogue = null;
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
                    // If we can't find the current canon line, just skip it
                }
                // If we have any lines to add to the next dialogue, do so
                nextDialogue = string.Join(" ", AddToNextDialogue);
                AddToNextDialogue.Clear();
            }
            Dialogue result;
            if (trace[2].GetMethod().Name.Contains("checkAction"))
            {
                var dialogueString = SldConstants.DialogueGenerationTag;
                if (!string.IsNullOrWhiteSpace(nextDialogue))
                {
                    dialogueString += "#" + nextDialogue;
                }
                result = new Dialogue(n, __instance.DialogueKey, dialogueString);
            }
            else
            {
                Task<Dialogue> resultTask;
                if (nextDialogue != null)
                {
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