using StardewValley;
using StardewValley.Menus;
using System;
using System.Threading.Tasks;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
                GenerateNpcResponse(enteredText);
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

        private static async void GenerateNpcResponse(string playerInput,string translationKey = "")
        {
            try
            {
                var npc = _currentNpc;
                var newDialogueTask = DialogueBuilder.Instance.GenerateResponse(_currentNpc, new[] { _currentResponse, playerInput }, true);
                var newDialogue = await newDialogueTask;

                if (!string.IsNullOrEmpty(newDialogue))
                {
                    DialogueBuilder.Instance.AddConversation(npc, newDialogue);
                    
                    // Create a new dialogue with the response and add it to the NPC's dialogue stack
                    var dialogue = new Dialogue(npc, _currentDialogueKey, newDialogue);
                    npc.CurrentDialogue.Push(dialogue);

                    Game1.DrawDialogue(dialogue);
                }
            }
            catch (Exception ex)
            {
                ModEntry.SMonitor?.Log($"Error generating NPC response: {ex.Message}", StardewModdingAPI.LogLevel.Error);
            }
        }
    }

    /// <summary>
    /// Wrapper to integrate DialogueTextInputMenu with Stardew Valley's menu system
    /// </summary>
    internal class DialogueTextInputMenuWrapper : IClickableMenu
    {
        private readonly DialogueTextInputMenu _innerMenu;

        public DialogueTextInputMenuWrapper(DialogueTextInputMenu innerMenu) : base()
        {
            _innerMenu = innerMenu;
        }

        public override void draw(SpriteBatch b)
        {
            _innerMenu.Draw(b);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            _innerMenu.ReceiveLeftClick(x, y);
        }

        public override void receiveKeyPress(Keys key)
        {
            _innerMenu.ReceiveKeyPress(key);
        }

        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }

        protected override void cleanupBeforeExit()
        {
            _innerMenu.Close();
            base.cleanupBeforeExit();
        }
    }

    /// <summary>
    /// Simple text input handler that uses the deferred system
    /// </summary>
    public class TextInputHandler
    {
        private readonly string _title;
        
        public TextInputHandler(string title)
        {
            _title = title ?? "Enter your response";
        }

        /// <summary>
        /// Request text input - returns a special marker since actual input is deferred
        /// </summary>
        public string GetString()
        {
            // We can't return the actual text synchronously, so we'll need to 
            // handle this differently in the dialogue patch
            return "__TEXT_INPUT_DEFERRED__";
        }
    }
}