using PissedEngineer.Primitives.Stacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PissedEngineer.Primitives.Utility
{
    public enum LogLevel {

        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public class  LogRecord: ICloneable
    {
        private const string _TimeStampFormat = "yy/MM/dd HH:mm:ss:fff";
        public LogRecord(LogLevel level, string message, 
            DateTime timeStamp = default)           
        {
            Level = level;
            Message = message;
            TimeStamp = timeStamp == DateTime.MinValue ? DateTime.Now : timeStamp;
        }

        public LogLevel Level { get; private set; }
        public string Message { get; private set; }
        public DateTime TimeStamp { get; private set; }

        public object Clone() => new LogRecord(Level, Message, TimeStamp);
        
        public LogRecord Copy() => (LogRecord)Clone();
        
        public override string ToString() => $"{TimeStamp.ToString(_TimeStampFormat)} [{LevelToString(Level)}]{Message}";
        
        private string LevelToString(LogLevel level) {
            switch (level) {
                case LogLevel.Debug: return "DBG";
                case LogLevel.Info: return "INF";
                case LogLevel.Warning: return "WRN";
                case LogLevel.Error: return "ERR";
                case LogLevel.Critical: return "CRT";
                default: return "???";
            }
        }
    }
    public class LogHistory: StackBase<LogRecord>
    {
        private const uint _DefaultHistoryDepth = 4096;
        public LogHistory(uint historyDepth = _DefaultHistoryDepth)
            : base(historyDepth) 
        { }

        public void Log(LogLevel level, string message, DateTime timeStamp = default(DateTime)) {
            Push(new LogRecord(level, message, timeStamp));
        }   
        public void Debug(string message, DateTime timeStamp = default(DateTime)) {
            Log(LogLevel.Debug, message, timeStamp);
        }
        public void Info(string message, DateTime timeStamp = default(DateTime)) {
            Log(LogLevel.Info, message, timeStamp);
        }
        public void Warning(string message, DateTime timeStamp = default(DateTime)) {
            Log(LogLevel.Warning, message, timeStamp);
        }
        public void Error(string message, DateTime timeStamp = default(DateTime)) {
            Log(LogLevel.Error, message, timeStamp);
        }
        public void Critical(string message, DateTime timeStamp = default(DateTime)) {
            Log(LogLevel.Critical, message, timeStamp);
        }
    }
}
