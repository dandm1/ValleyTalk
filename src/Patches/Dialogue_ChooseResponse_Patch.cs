using HarmonyLib;
using StardewValley;
using System.Linq;

namespace ValleyTalk
{
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
}