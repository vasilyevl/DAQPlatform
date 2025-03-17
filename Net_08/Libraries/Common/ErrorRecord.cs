using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Grumpy.Common
{
    public class ErrorRecord
    {
        public string Caller { get; }
        public string CallerMethod { get; }
        public Exception Error { get; }

        public DateTime TimeStamp { get; }
        public string Message => Error.Message;

        public ErrorRecord(string caller, 
            string callerMethod, 
            Exception error,
            DateTime timeStamep = default) {
            
            TimeStamp = (timeStamep == default) ? 
                DateTime.Now : timeStamep;
            Caller = caller;
            CallerMethod = callerMethod;
            Error = error;
        }

        public ErrorRecord(string caller,
            string callerMethod,
            string error,
            DateTime timeStamep = default) {

            TimeStamp = (timeStamep == default) ?
                DateTime.Now : timeStamep;
            Caller = caller;
            CallerMethod = callerMethod;
            Error = new Exception(error);
        }

        public static ErrorRecord CreateFromException(
            Exception exception,
            [CallerMemberName] string callerMethod = "",
             DateTime timeStamep = default) {

            string caller = GetCallerClassName();
            Exception error = exception;
            return new ErrorRecord(caller, 
                callerMethod, error, timeStamep);
        }

        public static ErrorRecord CreateFromMessage(
            string errorMessage,
            [CallerMemberName] string callerMethod = "",
            DateTime timeStamep = default ) {

            string caller = GetCallerClassName();
            Exception error = new Exception(errorMessage);
            return new ErrorRecord(caller, 
                callerMethod, error, timeStamep);
        }

        private static string GetCallerClassName() {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(2); // Get the calling method frame
            var method = frame.GetMethod();
            var declaringType = method.DeclaringType;
            return declaringType.Name;
        }
    }

    public class ErrorHistory
    {
        private readonly LifoBase<ErrorRecord> errorStack;

        public ErrorHistory(uint capacity = 32) {
            errorStack = new LifoBase<ErrorRecord>(capacity);
        }

        public bool Push(Exception exception, 
            [CallerMemberName] string callerMethod = "") {
            
            var errorRecord = new ErrorRecord( 
                GetCallerClassName(), 
                callerMethod, 
                exception, 
                DateTime.Now);
            return errorStack.Push(errorRecord);
        }

        public bool Push( string errorMessage, 
            [CallerMemberName] string callerMethod = "") {

            var errorRecord = new ErrorRecord( 
                GetCallerClassName(),
                 callerMethod,
                 errorMessage, 
                 DateTime.Now);
            
            return errorStack.Push(errorRecord);
        }

        public bool Pop(out ErrorRecord errorRecord) {
            return errorStack.Pop(out errorRecord);
        }

        public bool Peek(out ErrorRecord errorRecord) {
            return errorStack.Peek(out errorRecord);
        }

        public bool Purge() {
            return errorStack.Purge();
        }

        public ErrorRecord[] PeekAllAsArray(bool recentFirst = true) {
            return errorStack.PeekAllAsArray(recentFirst);
        }

        public System.Collections.Generic.List<ErrorRecord> PeekAllAsList(bool recentFirst = true) {
            return errorStack.PeekAllAsList(recentFirst);
        }

        private static string GetCallerClassName(string callerFilePath) {
            return System.IO.Path.GetFileNameWithoutExtension(callerFilePath);
        }

        private static string GetCallerClassName() {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(3); // Get the calling method frame
            var method = frame.GetMethod();
            var declaringType = method.DeclaringType;
            return declaringType.Name;
        }
    }

}