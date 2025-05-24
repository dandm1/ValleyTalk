using StardewValley.Menus;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ValleyTalk
{
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

}