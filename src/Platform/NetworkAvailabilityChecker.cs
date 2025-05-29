using System.Threading.Tasks;
using ValleyTalk.Platform;

namespace ValleyTalk
{
    /// <summary>
    /// Helper class for network availability checking in patches
    /// </summary>
    public static class NetworkAvailabilityChecker
    {
        /// <summary>
        /// Checks network availability on Android, with retry logic
        /// </summary>
        /// <returns>True if network is available or not on Android, false if Android and no network after retry</returns>
        public static async Task<bool> IsNetworkAvailableWithRetryAsync()
        {
            // If not on Android, always return true (assume network is available)
            if (!AndroidHelper.IsAndroid)
                return true;

            // First check
            if (NetworkHelper.IsNetworkAvailable())
                return true;

            ModEntry.SMonitor.Log("Network not available, retrying once per second for 5 seconds...", StardewModdingAPI.LogLevel.Warn);
            
            // Retry once per second for 5 seconds
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(1000);
                
                if (NetworkHelper.IsNetworkAvailable())
                    return true;
            }

            ModEntry.SMonitor.Log("Network still not available after retrying for 5 seconds, disabling AI dialogue generation", StardewModdingAPI.LogLevel.Warn);
            return false;
        }

        /// <summary>
        /// Synchronous version that blocks for the network check
        /// </summary>
        public static bool IsNetworkAvailableWithRetry()
        {
            return IsNetworkAvailableWithRetryAsync().Result;
        }
    }

}