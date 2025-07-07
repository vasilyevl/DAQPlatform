/*
Copyright (c) 2025 vasilyevl (Grumpy). Permission is hereby granted, 
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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Grumpy.Common.BaseObjects.Collections;

namespace Grumpy.Common.BaseObjects
{
    /// <summary>
    /// Represents a record of an error, including details about the caller and the error itself.
    /// </summary>
    public class ErrorRecord: DisposableBase
    {
        /// <summary>
        /// Gets the name of the caller class.
        /// </summary>
        public string Caller { get; }

        /// <summary>
        /// Gets the name of the caller method.
        /// </summary>
        public string CallerMethod { get; }

        /// <summary>
        /// Gets the exception representing the error.
        /// </summary>
        public Exception Error { get;
            private set;
        }

        /// <summary>
        /// Gets the timestamp when the error occurred.
        /// </summary>
        public DateTime TimeStamp { get;
            private set;
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message => Error?.Message ?? string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorRecord"/> class with the specified details.
        /// </summary>
        /// <param name="caller">The name of the caller class.</param>
        /// <param name="callerMethod">The name of the caller method.</param>
        /// <param name="error">The exception representing the error.</param>
        /// <param name="timeStamep">The timestamp when the error occurred.</param>
        public ErrorRecord(string caller, string callerMethod, Exception error, DateTime timeStamep = default) {
            TimeStamp = (timeStamep == default) ? DateTime.Now : timeStamep;
            Caller = caller;
            CallerMethod = callerMethod;
            Error = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorRecord"/> class with the specified details.
        /// </summary>
        /// <param name="caller">The name of the caller class.</param>
        /// <param name="callerMethod">The name of the caller method.</param>
        /// <param name="error">The error message.</param>
        /// <param name="timeStamep">The timestamp when the error occurred.</param>
        public ErrorRecord(string caller, string callerMethod, string error, DateTime timeStamep = default) {
            TimeStamp = (timeStamep == default) ? DateTime.Now : timeStamep;
            Caller = caller;
            CallerMethod = callerMethod;
            Error = new Exception(error);
        }

        /// <summary>
        /// Creates an <see cref="ErrorRecord"/> from an exception.
        /// </summary>
        /// <param name="exception">The exception representing the error.</param>
        /// <param name="callerMethod">The name of the caller method.</param>
        /// <param name="timeStamep">The timestamp when the error occurred.</param>
        /// <returns>An <see cref="ErrorRecord"/> instance.</returns>
        public static ErrorRecord CreateFromException(Exception exception, [CallerMemberName] string callerMethod = "", DateTime timeStamep = default) {
            string caller = GetCallerMethodName();
            Exception error = exception;
            return new ErrorRecord(caller, callerMethod, error, timeStamep);
        }

        /// <summary>
        /// Creates an <see cref="ErrorRecord"/> from an error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="callerMethod">The name of the caller method.</param>
        /// <param name="timeStamep">The timestamp when the error occurred.</param>
        /// <returns>An <see cref="ErrorRecord"/> instance.</returns>
        public static ErrorRecord CreateFromMessage(string errorMessage, [CallerMemberName] string callerMethod = "", DateTime timeStamep = default) {
            string caller = GetCallerMethodName();
            Exception error = new Exception(errorMessage);
            return new ErrorRecord(caller, callerMethod, error, timeStamep);
        }

        /// <summary>
        /// Gets the name of the caller method.
        /// </summary>
        /// <returns>The name of the caller method.</returns>
        private static string GetCallerMethodName() {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(2); // Get the calling method frame
            var method = frame?.GetMethod();
            var declaringType = method?.DeclaringType;
            return declaringType?.Name ?? string.Empty;
        }

        protected override void DisposeManagedResources() {
            Error = null!;
        }
    }

    /// <summary>
    /// Manages a history of error records using a Last-In-First-Out (LIFO) collection.
    /// </summary>
    public class ErrorHistory:DisposableBase
    {
        private readonly LIFOBase<ErrorRecord> errorStack;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHistory"/> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The maximum capacity of the error history.</param>
        public ErrorHistory(int capacity = 32) {
            errorStack = new LIFOBase<ErrorRecord>(capacity);
        }

        /// <summary>
        /// Pushes an exception onto the error history.
        /// </summary>
        /// <param name="exception">The exception to push.</param>
        /// <param name="callerMethod">The name of the caller method.</param>
        /// <returns>True if the exception was successfully pushed; otherwise, false.</returns>
        public bool Push(Exception exception, [CallerMemberName] string callerMethod = "") {
            var errorRecord = new ErrorRecord(GetCallerClassName(), callerMethod, exception, DateTime.Now);
            return errorStack.TryAdd(errorRecord, out string error);
        }

        /// <summary>
        /// Pushes an error message onto the error history.
        /// </summary>
        /// <param name="errorMessage">The error message to push.</param>
        /// <param name="callerMethod">The name of the caller method.</param>
        /// <returns>True if the error message was successfully pushed; otherwise, false.</returns>
        public bool Push(string errorMessage, [CallerMemberName] string callerMethod = "") {
            var errorRecord = new ErrorRecord(GetCallerClassName(), callerMethod, errorMessage, DateTime.Now);
            return errorStack.TryAdd(errorRecord, out string error);
        }

        /// <summary>
        /// Pops the most recent error record from the error history.
        /// </summary>
        /// <param name="errorRecord">The popped error record.</param>
        /// <returns>True if an error record was successfully popped; otherwise, false.</returns>
        public bool Pop(out ErrorRecord errorRecord) {
            return errorStack.Pop(out errorRecord, out string error);
        }

        /// <summary>
        /// Peeks at the most recent error record without removing it.
        /// </summary>
        /// <param name="errorRecord">The most recent error record.</param>
        /// <returns>True if an error record was successfully peeked; otherwise, false.</returns>
        public bool Peek(out ErrorRecord errorRecord) {
            return errorStack.Peek(out errorRecord, out string error);
        }

        /// <summary>
        /// Removes all error records from the error history.
        /// </summary>
        /// <returns>True if the error history was successfully purged; otherwise, false.</returns>
        public bool Purge() {
            return errorStack.TryClear(out string error);
        }

        /// <summary>
        /// Peeks at all error records as an array.
        /// </summary>
        /// <param name="recentFirst">If true, the most recent error records are first in the array.</param>
        /// <returns>An array of all error records.</returns>
        public ErrorRecord[] PeekAllAsArray(bool recentFirst = true) {
            return errorStack.PeekAllAsArray(out string error, recentFirst: true);
        }

        /// <summary>
        /// Peeks at all error records as a list.
        /// </summary>
        /// <param name="recentFirst">If true, the most recent error records are first in the list.</param>
        /// <returns>A list of all error records.</returns>
        public System.Collections.Generic.List<ErrorRecord> PeekAllAsList(bool recentFirst = true) {
            return errorStack.PeekAllAsList(out string error, recentFirst: true);
        }

        /// <summary>
        /// Gets the name of the caller class from the file path.
        /// </summary>
        /// <param name="callerFilePath">The file path of the caller.</param>
        /// <returns>The name of the caller class.</returns>
        private static string GetCallerClassName(string callerFilePath) {
            return System.IO.Path.GetFileNameWithoutExtension(callerFilePath);
        }

        /// <summary>
        /// Gets the name of the caller class.
        /// </summary>
        /// <returns>The name of the caller class.</returns>
        private static string GetCallerClassName() {
            var stackTrace = new StackTrace();
            StackFrame? frame = stackTrace.GetFrame(3); // Get the calling method frame
            System.Reflection.MethodBase? method = frame?.GetMethod();
            Type? declaringType = method?.DeclaringType;
            return declaringType?.Name ?? string.Empty;
        }

        protected override void DisposeManagedResources() {
            errorStack.Dispose();
        }
    }
}