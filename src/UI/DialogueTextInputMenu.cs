using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using System;

namespace ValleyTalk
{
    /// <summary>
    /// Menu for dialogue text input with larger interface
    /// </summary>
    public class DialogueTextInputMenu
    {
        public delegate void TextSubmittedDelegate(string input);

        private readonly string _title;
        private readonly DialogueTextInputBox _inputTextBox;
        private readonly ClickableTextureComponent _okButton;
        private readonly ClickableTextureComponent _cancelButton;
        private readonly TextSubmittedDelegate _onTextSubmitted;

        // Menu dimensions
        private const int MenuWidth = 1200;
        private const int MenuHeight = 600;
        private const int TextBoxHeight = 240;
        private const int ButtonSize = 64;
        private const int Margin = 24;

        // Positions
        private readonly Vector2 _menuPosition;
        private readonly Rectangle _menuBounds;

        public DialogueTextInputMenu(string title, TextSubmittedDelegate callback)
        {
            _title = title ?? "Enter your response";
            var titleSize = Game1.dialogueFont.MeasureString(_title);
            _onTextSubmitted = callback;

            var totalHeight = Margin * 8 + titleSize.Y + TextBoxHeight + ButtonSize * 2 ;
            // Center the menu
            _menuPosition = new Vector2(
                (Game1.viewport.Width - MenuWidth) / 2,
                (Game1.viewport.Height - totalHeight) / 2
            );
            
            _menuBounds = new Rectangle((int)_menuPosition.X, (int)_menuPosition.Y, MenuWidth, MenuHeight);

            // Create text input box
            _inputTextBox = new DialogueTextInputBox(500)
            {
                Position = new Vector2(_menuPosition.X + Margin * 2, _menuPosition.Y + titleSize.Y + Margin * 5),
                Extent = new Vector2(MenuWidth - 4 * Margin, TextBoxHeight),
                Font = Game1.dialogueFont,
                TextColor = Game1.textColor,
                Selected = true
            };
            _inputTextBox.OnSubmit += (sender) => Submit(sender.Text);

            // Set up keyboard input
            Game1.keyboardDispatcher.Subscriber = _inputTextBox;

            // Create OK button
            _okButton = new ClickableTextureComponent(
                new Rectangle(
                    (int)_menuPosition.X + MenuWidth - 2 * Margin - ButtonSize,
                    (int)_menuPosition.Y + MenuHeight - 2 * Margin - ButtonSize,
                    ButtonSize, ButtonSize),
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1),
                1f);

            // Create Cancel button
            _cancelButton = new ClickableTextureComponent(
                new Rectangle(
                    (int)_menuPosition.X + MenuWidth - 3 * Margin - 2 * ButtonSize,
                    (int)_menuPosition.Y + MenuHeight - 2 * Margin - ButtonSize,
                    ButtonSize, ButtonSize),
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47, -1, -1),
                1f);
        }

        public void Close()
        {
            Game1.keyboardDispatcher.Subscriber = null;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw semi-transparent background
            spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);

            // Draw menu background
            Game1.drawDialogueBox(_menuBounds.X, _menuBounds.Y, _menuBounds.Width, _menuBounds.Height, false, true);

            // Draw title
            var titleSize = Game1.dialogueFont.MeasureString(_title);
            var titlePos = new Vector2(
                _menuPosition.X + (MenuWidth - titleSize.X) / 2,
                _menuPosition.Y + 2 * Margin + titleSize.Y
            );
            spriteBatch.DrawString(Game1.dialogueFont, _title, titlePos, Game1.textColor);

            // Draw text input box
            _inputTextBox.Draw(spriteBatch);

            // Draw instruction text
            var instruction = "Press Enter to submit or click OK. Press Escape to cancel.";
            var instructionSize = Game1.smallFont.MeasureString(instruction);
            var instructionPos = new Vector2(
                _menuPosition.X + (MenuWidth - instructionSize.X) / 2,
                _inputTextBox.Position.Y + _inputTextBox.Extent.Y + Margin * 1.5f
            );
            spriteBatch.DrawString(Game1.smallFont, instruction, instructionPos, Color.Gray);

            // Draw buttons
            _okButton.draw(spriteBatch);
            _cancelButton.draw(spriteBatch);

            // Draw button labels
            //var okLabel = "OK";
            //var cancelLabel = "Cancel";
            //var okLabelSize = Game1.smallFont.MeasureString(okLabel);
            //var cancelLabelSize = Game1.smallFont.MeasureString(cancelLabel);

            //spriteBatch.DrawString(Game1.smallFont, okLabel,
            //    new Vector2(_okButton.bounds.X + (ButtonSize - okLabelSize.X) / 2,
            //               _okButton.bounds.Y + ButtonSize + 5), Game1.textColor);

            //spriteBatch.DrawString(Game1.smallFont, cancelLabel,
            //    new Vector2(_cancelButton.bounds.X + (ButtonSize - cancelLabelSize.X) / 2,
            //               _cancelButton.bounds.Y + ButtonSize + 5), Game1.textColor);

            // Draw mouse cursor
            if (!Game1.options.hardwareCursor)
            {
                spriteBatch.Draw(Game1.mouseCursors, new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                    Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16),
                    Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            }
        }

        public void ReceiveLeftClick(int x, int y)
        {
            if (_okButton.containsPoint(x, y))
            {
                Game1.playSound("coin");
                Submit(_inputTextBox.Text);
            }
            else if (_cancelButton.containsPoint(x, y))
            {
                Game1.playSound("cancel");
                Submit("");
            }
            else if (_inputTextBox.ContainsPoint(x, y))
            {
                Game1.keyboardDispatcher.Subscriber = _inputTextBox;
            }
        }

        public void ReceiveKeyPress(Keys key)
        {
            if (key == Keys.Escape)
            {
                Submit("");
            }
            else
            {
                _inputTextBox.RecieveSpecialInput(key);
            }
        }

        public bool ContainsPoint(int x, int y)
        {
            return _menuBounds.Contains(x, y);
        }

        private void Submit(string text)
        {
            _onTextSubmitted?.Invoke(text ?? "");
        }
    }
}