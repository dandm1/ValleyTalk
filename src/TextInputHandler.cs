using StardewValley;
using System;
using System.Threading.Tasks;
using StardewModdingAPI.Events;

namespace ValleyTalk
{
    /// <summary>
    /// Manages deferred text input for dialogue responses
    /// </summary>
    public static class TextInputManager
    {
        private static bool _awaitingTextInput = false;
        private static string _inputTitle = "";
        private static NPC _currentNpc = null;
        private static string _currentDialogueKey = "";
        private static string _currentResponse = "";
        
        /// <summary>
        /// Initialize the text input manager with mod events
        /// </summary>
        public static void Initialize()
        {
            ModEntry.SHelper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        /// <summary>
        /// Request text input - this will be handled on the next frame
        /// </summary>
        public static void RequestTextInput(string title, NPC npc, string dialogueKey = "", string dialogueStringConcat = "")
        {
            _awaitingTextInput = true;
            _inputTitle = title ?? "Enter your response";
            _currentNpc = npc;
            _currentDialogueKey = dialogueKey;
            _currentResponse = dialogueStringConcat;
        }

        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // Only show the input menu when we're not in a dialogue and input is requested
            if (_awaitingTextInput && Game1.activeClickableMenu == null)
            {
                _awaitingTextInput = false;
                ShowTextInputMenu();
            }
        }

        private static void ShowTextInputMenu()
        {
            try
            {
                var textInputMenu = new DialogueTextInputMenu(_inputTitle, OnTextEntered);
                Game1.activeClickableMenu = new DialogueTextInputMenuWrapper(textInputMenu);
            }
            catch (Exception ex)
            {
                ModEntry.SMonitor?.Log($"Error showing text input menu: {ex.Message}", StardewModdingAPI.LogLevel.Error);
            }
        }

        private static void OnTextEntered(string enteredText)
        {
            Game1.exitActiveMenu();

            if (_currentNpc == null || string.IsNullOrWhiteSpace(enteredText))
            {
                // Reset state
                _currentNpc = null;
                _currentDialogueKey = "";
                _inputTitle = "";
                return;
            }
            try
            {
                DialogueBuilder.Instance.AddConversation(_currentNpc, enteredText, isPlayerLine: true);

                // Generate NPC response to the typed input
                AsyncBuilder.Instance.RequestNpcResponse(_currentNpc, new string[] { enteredText, _currentResponse });
            }
            catch (Exception ex)
            {
                ModEntry.SMonitor?.Log($"Error handling text input: {ex.Message}", StardewModdingAPI.LogLevel.Error);
            }
            finally
            {
                // Reset state
                _currentNpc = null;
                _currentDialogueKey = "";
                _inputTitle = "";
            }
        }
    }

}