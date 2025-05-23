using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Threading.Tasks;
using StardewDialogue;
using ValleyTalk; // Add reference to ValleyTalk namespace for TextInputHandler

namespace ValleyTalk
{
    [HarmonyPatch(typeof(MarriageDialogueReference), nameof(MarriageDialogueReference.GetDialogue))]
    public class MarriageDialogueReference_GetDialogue_Patch
    {
        public static bool Prefix(ref MarriageDialogueReference __instance, ref Dialogue __result, NPC n)
        {
            if (!DialogueBuilder.Instance.PatchNpc(n, ModEntry.Config.MarriageFrequency))
            {
                return true;
            }
            var resultTask = DialogueBuilder.Instance.Generate(n, __instance.DialogueKey);

            var result = resultTask.Result;
            
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
            var dialogueStringIEnum = dialogueStrings.Where(x => !previous.Any(y => y.Contains(x.Text)) && x.Text != "skip").Select(x => x.Text);
            var dialogueStringConcat = string.Join(" ", dialogueStringIEnum);
            
            string farmerReponse = response.responseText;
            if (response.responseKey == $"{SldConstants.DialogueKeyPrefix}TypedResponse")
            {
                // Request deferred text input
                TextInputManager.RequestTextInput(
                    Util.GetString("yourResponse"), 
                    __instance.speaker, 
                    __instance.speaker.LoadedDialogueKey ?? "default",
                    dialogueStringConcat);
                
                // Exit the current dialogue to allow text input
                __result = true;
                return false;
            }
            
            // Set the isLastDialogueInteractive flag to false using reflection
            finishedLastDialogueField.SetValue(__instance, false);

            var key = __instance.speaker.LoadedDialogueKey;

            var newDialogueTask = DialogueBuilder.Instance.GenerateResponse(__instance.speaker, new [] { dialogueStringConcat,response.responseText}.ToArray());

            var newDialogue = newDialogueTask.Result;

            if (!newDialogue.Contains("$q"))
            {
                DialogueBuilder.Instance.AddConversation(__instance.speaker, newDialogue);
            }
            // Call parseDialogueString using reflection
            parseDialogueStringMethod.Invoke(__instance, new object[] { newDialogue, key });
            __instance.isCurrentStringContinuedOnNextScreen = true;
            isLastDialogueInteractiveField.SetValue(__instance, newDialogue.Contains("$q"));
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Dialogue), nameof(Dialogue.TryGetDialogue))]
    public class Dialogue_TryGetDialogue_Patch
    {
        public static bool Prefix(ref Dialogue __instance, ref Dialogue __result, NPC speaker, string translationKey)
        {
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