using Microsoft.Extensions.Logging;

namespace Grumpy.StatePatternFramework
{
    public class StateLibrary: Dictionary<StateIDBase, StateBase>
    {
        protected ILogger? _logger;

        public StateLibrary(ILogger logger = null!) : base() {

            _logger = logger;
        }

        public bool LoggerIsSet => _logger != null; 

        public bool Add(StateBase st)
        {
            if (!this.ContainsKey(st.ID)) {

                Add(st.ID, st);
                    return true;
            }
            else {

                return false;
            }
        }


    
        public StateBase? this[string name] {
            get {

                try {

                    var st =  this.Where((kv) => string.Equals(kv.Key.Name, 
                            name, System.StringComparison.OrdinalIgnoreCase)).First().Value;

                    return st;
                }
#if DEBUG
                catch (Exception ex) {

                    _logger?.LogDebug($"States Dictionary. State " +
                        $"{name} not found. Exception: {ex.Message}");

                    return null;
                }
#else
                catch { return null;}
#endif
            }
        }

        public StateBase? this[int id] {
            get {

                try {

                    return
                        this.Where((kv) => kv.Key.Id == id).First().Value;
                }
#if DEBUG
                catch (Exception ex) {

                    _logger?.LogDebug($"States Dictionary. State " +
                        $"{id} not found. Exception: {ex.Message}");

                    return null;
                }
#else
                catch { return null;}
#endif
            }
        }

        public bool HasSateWithName(string name)
            => string.IsNullOrEmpty(name) ? false : this[name] is not null;
    }
}
