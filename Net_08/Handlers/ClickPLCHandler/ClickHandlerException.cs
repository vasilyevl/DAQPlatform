using System;
using System.Collections.Generic;

namespace Grumpy.ClickPLC
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
                                      string? details = null) :
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
