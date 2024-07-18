using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.Common
{
    public enum LogLevel {
        Debug, 
        Info,
        Warning,
        Error, 
        Critical
    }

    public interface ILogRecord
    {
        public string MethodName { get; }
        public DateTime Time { get; }
        public LogLevel Level { get; }
        public int ErrorCode { get; }
        public string Details { get; }
        public bool IsError { get; }
        public string ToString();
        public ILogRecord? InternalRecord { get; }    
    }

    public class LogRecord: ILogRecord {


        public static ILogRecord CreateRecord(LogLevel level, string methodName, string message, int errorCode, DateTime time = default, ILogRecord nestedRecord = null) {

            return new LogRecord(level, methodName, message, errorCode, time, nestedRecord);
        }


        public static ILogRecord CreateRecord(string methodName, Exception ex, int errorCode = -1, DateTime time = default(DateTime)) {

            return new LogRecord(methodName, ex, errorCode, time);
        }


        internal LogRecord(LogLevel level, string methodName, string message, int errorCode, DateTime time = default, ILogRecord? nestedRecord = null) {

            MethodName = methodName;
            ErrorCode = errorCode;
            Details = message;
            Level = level;
            InternalRecord = nestedRecord;
        }

        internal  LogRecord(string methodName,  Exception ex, int errorCode = -1, DateTime time = default(DateTime)) {

            MethodName = methodName;
            ErrorCode = errorCode;
            Details = ex.Message;
            Level = LogLevel.Error;

            StringBuilder sb = new StringBuilder();
            sb.Append(ex.Message);

            if (ex.InnerException != null) {

                var innerException = ex.InnerException;

                while (innerException.InnerException != null) {
                    sb.Append($"\n\t{innerException.Message}");
                    innerException = innerException.InnerException;
                }              
            }
            
            Details = sb.ToString();
        }



        public  DateTime Time { get; private set; }
        public LogLevel Level { get; private set; }
        public  string MethodName { get; private set; }
        public int ErrorCode { get; private set; }
        public string Details { get; private set; }
        public bool IsError => Level == LogLevel.Error || Level == LogLevel.Critical;
        public bool IsInfo => Level == LogLevel.Info;
        public bool IsWarning => Level == LogLevel.Warning;
        public bool IsDebugInfo => Level == LogLevel.Debug;


        public ILogRecord? InternalRecord { get; private set; }

        public override string ToString() {

            StringBuilder sb = new StringBuilder();
            sb.Append($"[{Time.ToString("yy/MM/dd HH:mm:ss:fff")} {Level}] {MethodName}: Error code {(ErrorCode == -1 ? "N/A" : ErrorCode)}.Details: {Details}");

            var nested = InternalRecord;
            while (nested != null) {
                sb.Append($"\n\t{nested}");
                nested = nested.InternalRecord;
            }
            return sb.ToString();
        }
    } 
}
