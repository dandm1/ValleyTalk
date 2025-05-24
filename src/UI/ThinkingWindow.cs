using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace ValleyTalk
{
    /// <summary>
    /// A simple "Thinking..." window to show during AI generation
    /// </summary>
    internal class ThinkingWindow : IClickableMenu
    {
        private readonly string _message;
        private int _animationFrame;
        private float _animationTimer;
        
        // Margin dimensions
        private const int Margin = 24;
        
        public ThinkingWindow(string message = "Thinking") : base()
        {
            _message = message ?? "Thinking";
            var messageSize = Game1.dialogueFont.MeasureString(_message+"...");
            _animationFrame = 0;
            _animationTimer = 0f;
            
            // Center the window
            this.width = (int)messageSize.X + 6 * Margin;
            this.height = (int)messageSize.Y + 6 * Margin;
            this.xPositionOnScreen = (Game1.viewport.Width - this.width) / 2;
            this.yPositionOnScreen = (Game1.viewport.Height - this.height) / 2;
        }

        public override void update(GameTime time)
        {
            base.update(time);
            
            // Update animation
            _animationTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
            if (_animationTimer >= 500f) // Change dots every 500ms
            {
                _animationFrame = (_animationFrame + 1) % 4; // 0, 1, 2, 3 dots
                _animationTimer = 0f;
            }
        }

        public override void draw(SpriteBatch b)
        {
            // Draw semi-transparent background
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.3f);
            
            // Draw window background
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
            
            // Create animated message with dots
            string dots = new string('.', _animationFrame);
            string animatedMessage = _message + dots;
            
            // Draw the message centered
            var messageSize = Game1.dialogueFont.MeasureString(animatedMessage);
            var messagePos = new Vector2(
                this.xPositionOnScreen + (this.width - messageSize.X) / 2,
                this.yPositionOnScreen + this.height / 2 + Margin
            );
            
            b.DrawString(Game1.dialogueFont, animatedMessage, messagePos, Game1.textColor);
            
            // Draw mouse cursor
            if (!Game1.options.hardwareCursor)
            {
                b.Draw(Game1.mouseCursors, new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                    Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16),
                    Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
            }
        }

        // Prevent any input interaction with this window
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Do nothing - this window should not be interactive
        }

        public override void receiveKeyPress(Microsoft.Xna.Framework.Input.Keys key)
        {
            // Do nothing - this window should not be interactive
        }

        public override bool overrideSnappyMenuCursorMovementBan()
        {
            return true;
        }
    }
}