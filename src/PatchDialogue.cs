using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Threading.Tasks;
using StardewDialogue;
using ValleyTalk;
using System.Collections.Generic; // Add reference to ValleyTalk namespace for TextInputHandler

namespace ValleyTalk
{
    [HarmonyPatch(typeof(MarriageDialogueReference), nameof(MarriageDialogueReference.GetDialogue))]
    public class MarriageDialogueReference_GetDialogue_Patch
    {
        
        private static List<string> _skipGeneratedDialogue = new List<string>
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
            if (_skipGeneratedDialogue.Contains(__instance.DialogueKey))
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

    [HarmonyPatch(typeof(Dialogue), nameof(Dialogue.chooseResponse))]
    public class Dialogue_ChooseResponse_Patch
    {
        private static System.Reflection.FieldInfo isLastDialogueInteractiveField;
        private static System.Reflection.FieldInfo finishedLastDialogueField;
        private static System.Reflection.MethodInfo parseDialogueStringMethod;
        private static string respondString;

        static Dialogue_ChooseResponse_Patch()
        {

            isLastDialogueInteractiveField = typeof(Dialogue).GetField("isLastDialogueInteractive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            finishedLastDialogueField = typeof(Dialogue).GetField("finishedLastDialogue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            //isCurrentStringContinuedOnNextScreenField = typeof(Dialogue).GetField("isCurrentStringContinuedOnNextScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            parseDialogueStringMethod = typeof(Dialogue).GetMethod("parseDialogueString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            respondString = Util.GetString("outputRespond");
        }

        public static bool Prefix(ref Dialogue __instance, ref bool __result, Response response)
        {
            ModEntry.SMonitor.Log($"Dialogue.chooseResponse called with response key: {response.responseKey}", StardewModdingAPI.LogLevel.Trace);
            if (!DialogueBuilder.Instance.PatchNpc(__instance.speaker))
            {
                return true;
            }
            if (__instance.getResponseOptions().Any(r => !r.responseKey.StartsWith(SldConstants.DialogueKeyPrefix)))
            {
                return true;
            }
            if (response.responseKey == $"{SldConstants.DialogueKeyPrefix}Silent")
            {
                DialogueBuilder.Instance.AddConversation(__instance.speaker, "", isPlayerLine: true);
                __result = true;
                return false;
            }
            
            // Get the current dialogue string from __instance
            // If the last entry is "Respond:", remove it
            var dialogueStrings = __instance.dialogues;
            if (dialogueStrings.Last().Text == respondString)
            {
                dialogueStrings.RemoveAt(dialogueStrings.Count - 1);
            }

            var previous = DialogueBuilder.Instance.LastContext.ChatHistory;
            var dialogueStringIEnum = dialogueStrings.Where(x => !previous.Any(y => y.Text.Contains(x.Text)) && x.Text != "skip");
            
            previous.AddRange(dialogueStringIEnum.Select(x => new ConversationElement(x.Text, false)));

            string farmerReponse = response.responseText;
            if (response.responseKey == $"{SldConstants.DialogueKeyPrefix}TypedResponse")
            {
                // Request deferred text input
                TextInputManager.RequestTextInput(
                    Util.GetString("uiYourResponse"), 
                    __instance.speaker, 
                    __instance.speaker.LoadedDialogueKey ?? "default",
                    previous);
                
                // Exit the current dialogue to allow text input
                __result = true;
                return false;
            }
            
            // Set the isLastDialogueInteractive flag to false using reflection
            finishedLastDialogueField.SetValue(__instance, false);

            AsyncBuilder.Instance.RequestNpcResponse(
                __instance.speaker, 
                previous.AddItem(new ConversationElement(farmerReponse, true)).ToArray()
            );

            __result = true;
            return false;
        }
    }

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