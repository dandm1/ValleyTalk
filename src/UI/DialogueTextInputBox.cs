using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

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
            if (_caretPosition < 0) _caretPosition = 0;
            if (_caretPosition > Text.Length) _caretPosition = Text.Length;
            
            // Calculate caret position based on actual cursor position in text
            var textBeforeCaret = Text.Substring(0, _caretPosition);
            var lines = WrapText(textBeforeCaret, textArea.Width, Font);
            var lineHeight = (int)Font.MeasureString("A").Y;
            
            int caretX, caretY;
            
            if (lines.Length == 0)
            {
                // No text, caret at start
                caretX = textArea.X;
                caretY = textArea.Y;
            }
            else
            {
                // Caret is at the end of the last line of text before cursor
                var lastLine = lines[lines.Length - 1];
                var lastLineWidth = Font.MeasureString(lastLine).X;
                
                caretX = textArea.X + (int)lastLineWidth;
                caretY = textArea.Y + (lines.Length - 1) * lineHeight;
                
                // If we're at the very end and the line is full, move to next line
                if (_caretPosition < Text.Length)
                {
                    var fullTextLines = WrapText(Text, textArea.Width, Font);
                    if (lines.Length < fullTextLines.Length && lastLineWidth + Font.MeasureString("A").X > textArea.Width)
                    {
                        caretX = textArea.X;
                        caretY += lineHeight;
                    }
                }
            }
            
            var caretRect = new Rectangle(caretX, caretY, 2, lineHeight);
            spriteBatch.Draw(Game1.staminaRect, caretRect, TextColor);
        }

        public void RecieveTextInput(char inputChar)
        {
            // Handle backspace
            if (inputChar == '\b')
            {
                if (_caretPosition > 0 && Text.Length > 0)
                {
                    Text = Text.Remove(_caretPosition - 1, 1);
                    _caretPosition--;
                }
                return;
            }
            
            // Handle Enter - submit the text
            if (inputChar == '\r' || inputChar == '\n')
            {
                OnSubmit?.Invoke(this);
                return;
            }
            
            // Skip other control characters
            if (char.IsControl(inputChar))
                return;
            
            // Insert printable character at cursor position
            if (Text.Length < _characterLimit)
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
                    if (_caretPosition > 0) 
                        _caretPosition--;
                    break;
                    
                case Keys.Right:
                    if (_caretPosition < Text.Length) 
                        _caretPosition++;
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
                    
                case Keys.Back:
                    if (_caretPosition > 0 && Text.Length > 0)
                    {
                        Text = Text.Remove(_caretPosition - 1, 1);
                        _caretPosition--;
                    }
                    break;
                    
                case Keys.Enter:
                    OnSubmit?.Invoke(this);
                    break;
                    
                // Add Ctrl+A for select all (though we don't have selection yet)
                case Keys.A:
                    if (Game1.oldKBState.IsKeyDown(Keys.LeftControl) || Game1.oldKBState.IsKeyDown(Keys.RightControl))
                    {
                        // Could implement select all here if needed
                        _caretPosition = Text.Length;
                    }
                    break;
            }
        }
    }
}