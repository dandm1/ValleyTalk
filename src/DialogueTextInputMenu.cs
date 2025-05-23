using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using System;

namespace ValleyTalk
{
    /// <summary>
    /// A larger text input box specifically designed for dialogue responses
    /// </summary>
    public class DialogueTextInputBox : IKeyboardSubscriber
    {
        public delegate void TextBoxEvent(DialogueTextInputBox sender);
        public event TextBoxEvent OnSubmit;

        public Vector2 Position { get; set; }
        public Vector2 Extent { get; set; }
        public Color TextColor { get; set; } = Game1.textColor;
        public SpriteFont Font { get; set; } = Game1.dialogueFont;
        public bool Selected { get; set; } = true;
        public string Text { get; private set; } = "";

        private readonly int _characterLimit;
        private int _caretPosition = 0;
        private readonly Texture2D _backgroundTexture;

        public DialogueTextInputBox(int characterLimit = 500)
        {
            _characterLimit = characterLimit;
            
            // Create background texture
            _backgroundTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            _backgroundTexture.SetData(new Color[] { Color.White });
        }

        public bool ContainsPoint(float x, float y)
        {
            return x >= Position.X && y >= Position.Y && 
                   x <= Position.X + Extent.X && y <= Position.Y + Extent.Y;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw textbox background using the game's standard texture box
            IClickableMenu.drawTextureBox(spriteBatch, (int)Position.X, (int)Position.Y, 
                                         (int)Extent.X, (int)Extent.Y, Color.White);
            
            // Calculate text area with padding
            var textArea = new Rectangle((int)Position.X + 16, (int)Position.Y + 16, 
                                       (int)Extent.X - 32, (int)Extent.Y - 32);
            
            // Draw text with word wrapping
            if (!string.IsNullOrEmpty(Text))
            {
                DrawWrappedText(spriteBatch, Text, textArea, Font, TextColor);
            }
            
            // Draw caret if selected
            if (Selected)
            {
                DrawCaret(spriteBatch, textArea);
            }
        }

        private void DrawWrappedText(SpriteBatch spriteBatch, string text, Rectangle area, SpriteFont font, Color color)
        {
            var lines = WrapText(text, area.Width, font);
            var lineHeight = (int)font.MeasureString("A").Y;
            var y = area.Y;
            
            foreach (var line in lines)
            {
                if (y + lineHeight > area.Bottom) break; // Don't draw outside the box
                
                spriteBatch.DrawString(font, line, new Vector2(area.X, y), color);
                y += lineHeight;
            }
        }

        private string[] WrapText(string text, int maxWidth, SpriteFont font)
        {
            var lines = new System.Collections.Generic.List<string>();
            var words = text.Split(' ');
            var currentLine = "";
            
            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                if (font.MeasureString(testLine).X <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        lines.Add(word); // Word is too long, add it anyway
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }
            
            return lines.ToArray();
        }

        private void DrawCaret(SpriteBatch spriteBatch, Rectangle textArea)
        {
            // Simple caret at the end of text for now
            var textSize = Font.MeasureString(Text);
            var caretX = textArea.X + (int)textSize.X;
            var caretY = textArea.Y;
            
            // Wrap caret position if needed
            if (caretX > textArea.Right - 20)
            {
                var lines = WrapText(Text, textArea.Width, Font);
                var lineHeight = (int)Font.MeasureString("A").Y;
                caretY = textArea.Y + (lines.Length - 1) * lineHeight;
                
                if (lines.Length > 0)
                {
                    var lastLineWidth = Font.MeasureString(lines[lines.Length - 1]).X;
                    caretX = textArea.X + (int)lastLineWidth;
                }
            }
            
            var caretRect = new Rectangle(caretX, caretY, 2, (int)Font.MeasureString("A").Y);
            spriteBatch.Draw(Game1.staminaRect, caretRect, TextColor);
        }

        public void RecieveTextInput(char inputChar)
        {
            if (char.IsControl(inputChar) && inputChar != '\b' && inputChar != '\r' && inputChar != '\n')
                return;
                
            if (inputChar == '\b') // Backspace
            {
                if (_caretPosition > 0)
                {
                    Text = Text.Remove(_caretPosition - 1, 1);
                    _caretPosition--;
                }
            }
            else if (inputChar == '\r' || inputChar == '\n') // Enter
            {
                if (Text.Length < _characterLimit - 1)
                {
                    Text = Text.Insert(_caretPosition, " ");
                    _caretPosition++;
                }
            }
            else if (Text.Length < _characterLimit)
            {
                Text = Text.Insert(_caretPosition, inputChar.ToString());
                _caretPosition++;
            }
        }

        public void RecieveTextInput(string text)
        {
            foreach (char c in text)
            {
                RecieveTextInput(c);
            }
        }

        public void RecieveCommandInput(char command)
        {
            Keys key = (Keys)command;
            switch (key)
            {
                case Keys.Enter:
                    OnSubmit?.Invoke(this);
                    break;
            }
        }

        public void RecieveSpecialInput(Keys key)
        {
            switch (key)
            {
                case Keys.Left:
                    if (_caretPosition > 0) _caretPosition--;
                    break;
                case Keys.Right:
                    if (_caretPosition < Text.Length) _caretPosition++;
                    break;
                case Keys.Home:
                    _caretPosition = 0;
                    break;
                case Keys.End:
                    _caretPosition = Text.Length;
                    break;
                case Keys.Delete:
                    if (_caretPosition < Text.Length)
                    {
                        Text = Text.Remove(_caretPosition, 1);
                    }
                    break;
                case Keys.Enter:
                    OnSubmit?.Invoke(this);
                    break;
            }
        }
    }

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
        private const int Margin = 32;

        // Positions
        private readonly Vector2 _menuPosition;
        private readonly Rectangle _menuBounds;

        public DialogueTextInputMenu(string title, TextSubmittedDelegate callback)
        {
            _title = title ?? "Enter your response";
            _onTextSubmitted = callback;

            // Center the menu
            _menuPosition = new Vector2(
                (Game1.viewport.Width - MenuWidth) / 2,
                (Game1.viewport.Height - MenuHeight) / 2
            );
            
            _menuBounds = new Rectangle((int)_menuPosition.X, (int)_menuPosition.Y, MenuWidth, MenuHeight);

            // Create text input box
            _inputTextBox = new DialogueTextInputBox(500)
            {
                Position = new Vector2(_menuPosition.X + Margin, _menuPosition.Y + 80),
                Extent = new Vector2(MenuWidth - 2 * Margin, TextBoxHeight),
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
                    (int)_menuPosition.X + MenuWidth - Margin - ButtonSize,
                    (int)_menuPosition.Y + MenuHeight - Margin - ButtonSize,
                    ButtonSize, ButtonSize),
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1),
                1f);

            // Create Cancel button
            _cancelButton = new ClickableTextureComponent(
                new Rectangle(
                    (int)_menuPosition.X + MenuWidth - Margin - 2 * ButtonSize - 10,
                    (int)_menuPosition.Y + MenuHeight - Margin - ButtonSize,
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
                _menuPosition.Y + Margin
            );
            spriteBatch.DrawString(Game1.dialogueFont, _title, titlePos, Game1.textColor);

            // Draw text input box
            _inputTextBox.Draw(spriteBatch);

            // Draw instruction text
            var instruction = "Press Enter to submit or click OK. Press Escape to cancel.";
            var instructionSize = Game1.smallFont.MeasureString(instruction);
            var instructionPos = new Vector2(
                _menuPosition.X + (MenuWidth - instructionSize.X) / 2,
                _inputTextBox.Position.Y + _inputTextBox.Extent.Y + 10
            );
            spriteBatch.DrawString(Game1.smallFont, instruction, instructionPos, Color.Gray);

            // Draw buttons
            _okButton.draw(spriteBatch);
            _cancelButton.draw(spriteBatch);

            // Draw button labels
            var okLabel = "OK";
            var cancelLabel = "Cancel";
            var okLabelSize = Game1.smallFont.MeasureString(okLabel);
            var cancelLabelSize = Game1.smallFont.MeasureString(cancelLabel);

            spriteBatch.DrawString(Game1.smallFont, okLabel,
                new Vector2(_okButton.bounds.X + (ButtonSize - okLabelSize.X) / 2,
                           _okButton.bounds.Y + ButtonSize + 5), Game1.textColor);

            spriteBatch.DrawString(Game1.smallFont, cancelLabel,
                new Vector2(_cancelButton.bounds.X + (ButtonSize - cancelLabelSize.X) / 2,
                           _cancelButton.bounds.Y + ButtonSize + 5), Game1.textColor);

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