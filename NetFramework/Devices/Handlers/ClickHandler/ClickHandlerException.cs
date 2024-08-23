/*
Copyright (c) 2024 LV-PissedEngineer Permission is hereby granted, 
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
using System;
using System.Collections.Generic;

namespace PissedEngineer.ClickPLCHandler
{
    public enum ErrorCode
    {
        NoError = 0,
        CodeNotDefined = -1,
        ConfigurationNotSet = -2,
        InvalidControlName = -3,
        InvalidControlNamePreffix = -4,
        InvalidControlAddress = -5,
        OpenFailed = -6,
        CloseFailed = -7,
        ProhibitedWhenControllerIsConnected = -8,
        ProhibitedWhenControllerIsNotConnected = -9,
        ConfigDeserialisationError = -10,
        IoNotSupported = -11,
        InvalidSwitchState = -12,
        NotConnected = -13,
        SingleIoWriteFailed = -14,
        NoDataProvided = -15,
        GroupIoWriteFailed = -16,
        ConfigurationIsNotProvided = -17,
        NotWritableControl = -18,
    }

    public class ClickHandlerException : Exception
    {
        public ClickHandlerException(string name,
                                      ErrorCode error,
                                      string details = null) :
            base(string.IsNullOrEmpty(details) ? "N/A" : details)
        {
            MethodName = name;
            ErrorCode = error;
        }

        public string MethodName { get; private set; }
        public ErrorCode ErrorCode { get; private set; }
        public string ErrorDetails => base.Message;
    }


    public static class ClickPlcHandlerErrors
    {
        public static string GetErrorDescription(ErrorCode errorCode)
        {
            if (_errorDescriptors.ContainsKey(errorCode))
            {

                return (string)_errorDescriptors[errorCode].Clone();
            }

            return string.Empty;
        }

        private static readonly IReadOnlyDictionary<ErrorCode, string> _errorDescriptors =
            new Dictionary<ErrorCode, string>() {
                { ErrorCode.NoError,  "No Error."},
                { ErrorCode.CodeNotDefined,  "Invalid error code." },
                { ErrorCode.InvalidControlName,  "Invalid control name assigned." },
                { ErrorCode.InvalidControlNamePreffix,  "Invalid control name preffix." },
                { ErrorCode.InvalidControlAddress,  "Invalid control name addres ( must be 1+)." }
            };
    }
}
