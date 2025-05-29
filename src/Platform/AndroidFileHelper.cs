using System;
using System.IO;
using StardewModdingAPI;

namespace ValleyTalk.Platform
{
    /// <summary>
    /// Helper class for Android-compatible file system operations
    /// </summary>
    public static class AndroidFileHelper
    {
        private static IModHelper _modHelper;

        /// <summary>
        /// Initialize with SMAPI mod helper
        /// </summary>
        public static void Initialize(IModHelper modHelper)
        {
            _modHelper = modHelper;
        }

        /// <summary>
        /// Gets a safe file path using SMAPI's data directory
        /// </summary>
        public static string GetSafeFilePath(string fileName)
        {
            if (_modHelper == null)
                throw new InvalidOperationException("AndroidFileHelper not initialized");

            return Path.Combine(_modHelper.DirectoryPath, fileName);
        }

        /// <summary>
        /// Reads file content with Android-compatible error handling
        /// </summary>
        public static string ReadFileContent(string fileName)
        {
            try
            {
                var filePath = GetSafeFilePath(fileName);
                
                if (!File.Exists(filePath))
                    return null;

                return File.ReadAllText(filePath);
            }
            catch (UnauthorizedAccessException)
            {
                // Handle Android permission issues
                ModEntry.SMonitor.Log($"Permission denied accessing file: {fileName}", LogLevel.Warn);
                return null;
            }
            catch (IOException ex)
            {
                ModEntry.SMonitor.Log($"IO error reading file {fileName}: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// Writes file content with Android-compatible error handling
        /// </summary>
        public static bool WriteFileContent(string fileName, string content)
        {
            try
            {
                var filePath = GetSafeFilePath(fileName);
                var directory = Path.GetDirectoryName(filePath);
                
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, content);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                ModEntry.SMonitor.Log($"Permission denied writing file: {fileName}", LogLevel.Warn);
                return false;
            }
            catch (IOException ex)
            {
                ModEntry.SMonitor.Log($"IO error writing file {fileName}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Checks if file exists with Android-compatible error handling
        /// </summary>
        public static bool FileExists(string fileName)
        {
            try
            {
                var filePath = GetSafeFilePath(fileName);
                return File.Exists(filePath);
            }
            catch
            {
                return false;
            }
        }
    }
}
