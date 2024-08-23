using System;
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


namespace Grumpy.ClickPLCHandler
{
    public enum ClickErrorCode
    {
        NoError = 0,
        GenericError = -1,
        ConfigurationNotSet = -2,
        InvalidControlName = -3,
        InvalidControlNamePrefix = -4,
        InvalidControlAddress = -5,
        OpenFailed = -6,
        CloseFailed = -7,
        ProhibitedWhenControllerIsConnected = -8,
        ProhibitedWhenControllerIsNotConnected = -9,
        ConfigDeserializationError = -10,
        IoNotSupported = -11,
        InvalidSwitchState = -12,
        NotConnected = -13,
        SingleIoWriteFailed = -14,
        NoDataProvided = -15,
        GroupIoWriteFailed = -16,
        ConfigurationIsNotProvided = -17,
        NotWritableControl = -18,
        FailedTConvertRegistersToFloat = -19,
    }
}
