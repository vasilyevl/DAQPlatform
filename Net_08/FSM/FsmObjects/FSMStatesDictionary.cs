
using Serilog;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Xml.Linq;
using Tau.Common;

namespace FSM
{
    public class FSMStatesDictionary: Dictionary<EnumeratorBase, StateBase>
    {
        protected static ILogger _logger = Log.Logger;
        public bool Add( StateBase st)
        {
            if (!this.ContainsKey(st.ID)) {
                Add(st.ID, st);
                    return true;
            }
            else {
                return false;
            }
        }

        public StateBase this[string name] {
            get {
                try {

                    var st =  this.Where((kv) => string.Equals(kv.Key.Name, 
                            name, System.StringComparison.OrdinalIgnoreCase)).First().Value;

                    return st;
                }
#if DEBUG
                catch (Exception ex) {

                    _logger.Debug($"FSMStatesDictionary. State with name " +
                        $"{name} not found. Exception: {ex.Message}");

                    return null;
                }
#else
                catch { return null;}
#endif
            }
        }

        public StateBase this[int id] {
            get {
                try {
                    return
                        this.Where((kv) => kv.Key.Id == id).First().Value;
                }
#if DEBUG
                catch (Exception ex) {

                    _logger.Debug($"FSMStatesDictionary. State with ID " +
                        $"{id} not found. Exception: {ex.Message}");

                    return null;
                }
#else
                catch { return null;}

#endif
            }
        }

        public bool HasSateWithName(string name)
        {
            return this[name] != null;
        }
    }
}
