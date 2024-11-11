/* 
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using DAQFramework.Utilities;

using Serilog;
using System.Reflection;



namespace Grumpy.DAQFramework.Utilities
{

    public interface ILogger
    {
        bool IsConfigured { get; }
        void AddLogRecord(LogLevel level, string message);
    }


    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public static class Logger
    {
        private const string _Preffix = "Logger:";
        private const long DefaultMaxLogFileSizeInBytes = 2097152;

        private static bool _configured = false;    
        private static LogLevel _logLevel = LogLevel.Info;

        public static LogLevel Level {
            get {
                LogLevel res;
                Thread.MemoryBarrier();
                res = _logLevel;
                Thread.MemoryBarrier();
                return res;
            } 
            private set {
                Thread.MemoryBarrier();
                _logLevel = value;
                Thread.MemoryBarrier();
            }
        }

        private static object _lock = new object();
        public static bool IsConfigured {

            get {
                lock (_lock) {
                    return _configured;
                }
            }

            private set {
                lock (_lock) {
                    _configured = value;
                }
            }
        }

        public static bool ConfigureLogger( string logFolder, bool cleanupFolder,
            LogLevel level,  out string lastError)
        {
            string appName = Assembly.GetEntryAssembly()?.GetName().Name!;

            string assemblyLocation = Assembly.GetExecutingAssembly()?.Location!;

            UriBuilder uri = new UriBuilder(assemblyLocation);

            string assemblyPath = Uri.UnescapeDataString(uri.Path);

            if (string.IsNullOrEmpty(logFolder)) {

                logFolder = Path.Combine(Directory.GetParent(assemblyPath)?.FullName!, "Logs");
            }

            if (!FileUtilities.DirectoryExists(logFolder)
             && !FileUtilities.CreateFolder(logFolder, out lastError, 10000)) {

                lastError = $"{_Preffix} Failed to find and create Log folder \"{logFolder}\".";
            }

            if (cleanupFolder) {

                FileUtilities.FolderCleanup(logFolder, out string error);
            }

            LoggerConfiguration loggerConfig = new LoggerConfiguration();

            Level = level;

            string logFile = Path.Combine(logFolder, $"{appName}.Log").ToString();

            _ConfigureAsyncFileSink(logFile, ref loggerConfig);


            Log.Logger = loggerConfig.CreateLogger();

            IsConfigured = true;

            lastError = null!;
            return true;
        }

        private static void _ConfigureAsyncFileSink(string logFile,
                                                 ref LoggerConfiguration loggerConfig)
        {
            loggerConfig.WriteTo.Async(a =>
            {
                a.File(logFile,
                       outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                                            "{Message:lj}{NewLine}{Exception}",
                       rollingInterval: RollingInterval.Day,
                       buffered: true,
                       retainedFileCountLimit: null,
                       rollOnFileSizeLimit: true,
                       fileSizeLimitBytes: DefaultMaxLogFileSizeInBytes);
            });
        }

        public static void AddRecord( LogLevel level, string message)
        {
            if ( IsConfigured ) {

                switch ( level ) {

                    case LogLevel.Error:
                        Log.Error(message); 
                        break;

                    case LogLevel.Info:
                        Log.Information(message); 
                        break;

                    case LogLevel.Debug:
                        Log.Debug(message); 
                        break;

                    case LogLevel.Warning:
                        Log.Warning(message); 
                        break;

                    case (LogLevel.Critical):
                        Log.Error("Critical: " +message); 
                        break;
                }
            }
        }
    }
}
