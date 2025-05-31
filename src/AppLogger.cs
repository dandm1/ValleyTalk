using System;
using StardewModdingAPI;

namespace ValleyTalk
{
    /// <summary>
    /// Cross-platform compatible logger for ValleyTalk
    /// Provides a drop-in replacement for Serilog functionality
    /// </summary>
    public static class Log
    {
        private static IMonitor _monitor;
        private static string _logPrefix = "[ValleyTalk] ";
        
        /// <summary>
        /// Initialize the logger with SMAPI's monitor
        /// </summary>
        /// <param name="monitor">The SMAPI monitor instance</param>
        /// <param name="enableDebug">Whether debug logging is enabled</param>
        public static void Initialize(IMonitor monitor)
        {
            _monitor = monitor;
        }

        /// <summary>
        /// Logger class that mimics Serilog's LoggerConfiguration for compatibility
        /// </summary>
        public static class Logger
        {
            public static IMonitor Monitor => _monitor;
            
            // This is a no-op property to allow existing Serilog initialization code to compile
            public static LoggerConfiguration CreateLogger() => new LoggerConfiguration();
        }
        
        /// <summary>
        /// Configuration class that mimics Serilog's LoggerConfiguration for compatibility
        /// </summary>
        public class LoggerConfiguration 
        {
            // These methods return the instance to allow method chaining like Serilog does
            public LoggerConfiguration WriteTo => this;
            public LoggerConfiguration Console() => this;
            public LoggerConfiguration File(string path, object rollingInterval) => this;
            public LoggerConfiguration MinimumLevel => this;
            public LoggerConfiguration Debug() => this;
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void Debug(string message)
        {
                _monitor?.Log($"{_logPrefix}{message}", LogLevel.Debug);
        }

        /// <summary>
        /// Log a debug message with string formatting
        /// </summary>
        public static void Debug(string format, params object[] args)
        {
                _monitor?.Log($"{_logPrefix}{string.Format(format, args)}", LogLevel.Debug);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string message)
        {
            _monitor?.Log($"{_logPrefix}{message}", LogLevel.Error);
        }

        /// <summary>
        /// Log an error with an exception
        /// </summary>
        public static void Error(Exception ex, string message)
        {
            _monitor?.Log($"{_logPrefix}{message}: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public static void Information(string message)
        {
            _monitor?.Log($"{_logPrefix}{message}", LogLevel.Info);
        }

        /// <summary>
        /// Log an informational message with string formatting
        /// </summary>
        public static void Information(string format, params object[] args)
        {
            _monitor?.Log($"{_logPrefix}{string.Format(format, args)}", LogLevel.Info);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warning(string message)
        {
            _monitor?.Log($"{_logPrefix}{message}", LogLevel.Warn);
        }

        /// <summary>
        /// Log a warning message with string formatting
        /// </summary>
        public static void Warning(string format, params object[] args)
        {
            _monitor?.Log($"{_logPrefix}{string.Format(format, args)}", LogLevel.Warn);
        }
    }
}