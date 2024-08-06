
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

    public class ClickHandlerException : Exception
    {
        public ClickHandlerException( string name,
                                      ClickErrorCode error,
                                      string? details = null) :
            base(string.IsNullOrEmpty(details) ? "N/A" : details) {
            MethodName = name;
            Error = error;
        }

        public string MethodName { get; private set; }
        public ClickErrorCode Error { get; private set; }
        public int ErrorID => (int)Error;
        public string ErrorDetails => base.Message;
        public bool IsError => Error != ((int) ClickErrorCode.NoError);
    }


    public static class ClickPlcHandlerErrors
    {
        public static string GetErrorDescription(ClickErrorCode errorCode) {
            if (_errorDescriptors.ContainsKey(errorCode)) {

                return (string)_errorDescriptors[errorCode].Clone();
            }

            return string.Empty;
        }

        private static readonly IReadOnlyDictionary<ClickErrorCode, string> _errorDescriptors =
            new Dictionary<ClickErrorCode, string>() {
                { ClickErrorCode.NoError,  "No Error."},
                { ClickErrorCode.GenericError,  "Invalid error code." },
                { ClickErrorCode.InvalidControlName,  "Invalid control name assigned." },
                { ClickErrorCode.InvalidControlNamePrefix,  "Invalid control name prefix." },
                { ClickErrorCode.InvalidControlAddress,  "Invalid control name address ( must be 1+)." }
            };
    }
}
