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
