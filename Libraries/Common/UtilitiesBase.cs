using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSE.Common
{
    /*
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs( object o, string message ) { 
  
            ObjectTypeName = o.GetType().FullName;
            Description = (string)( message?.Clone() ?? null );
        }

        public String ObjectTypeName { get; private set; }
        public string Description { get; private set; }
    }

    public delegate void ErrorEventHandler(object source, ErrorEventArgs args);


    public  class UtilitiesBase
    {
        private string _lastError;
        private object _lastErrorLock;
        public UtilitiesBase() {
            _lastError = null;
            _lastErrorLock = new object();
        }


        public event ErrorEventHandler ErrorEvent;

        public string LastError {
            get {
                lock (_lastErrorLock) {
                    return (string)(_lastError?.Clone() ?? null);
                }
            }

            private set {
                lock (_lastErrorLock) {
                    if ( !string.IsNullOrEmpty(value) )
                    {
                        if ((ErrorEvent?.GetInvocationList().Length ?? 0) > 0) {
                            ErrorEvent.Invoke(this, new ErrorEventArgs(this, (string)value.Clone()));
                        }                        
                    }
                    _lastError = value;
                }
            }
        }

    }
    */
}
