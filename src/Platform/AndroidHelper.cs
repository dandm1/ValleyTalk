using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace ValleyTalk.Platform
{
    /// <summary>
    /// Helper class for Android-specific input handling
    /// </summary>
    public static class AndroidHelper
    {
        /// <summary>
        /// Checks if the current platform is Android
        /// </summary>
        public static bool IsAndroid => Game1.game1?.GetType().Assembly.GetName().Name?.Contains("Android") == true;

        /// <summary>
        /// Gets the virtual keyboard height on Android
        /// </summary>
        public static int GetVirtualKeyboardHeight()
        {
            if (!IsAndroid) return 0;
            
            // Android virtual keyboard typically takes 1/3 of screen height
            return Game1.graphics.GraphicsDevice.Viewport.Height / 3;
        }

        /// <summary>
        /// Adjusts UI position for virtual keyboard on Android
        /// </summary>
        public static Vector2 AdjustPositionForKeyboard(Vector2 originalPosition)
        {
            if (!IsAndroid) return originalPosition;
            
            var keyboardHeight = GetVirtualKeyboardHeight();
            var screenHeight = Game1.graphics.GraphicsDevice.Viewport.Height;
            
            // Move UI up if it would be covered by keyboard
            if (originalPosition.Y > screenHeight - keyboardHeight)
            {
                return new Vector2(originalPosition.X, screenHeight - keyboardHeight - 100);
            }
            
            return originalPosition;
        }
    }
}
