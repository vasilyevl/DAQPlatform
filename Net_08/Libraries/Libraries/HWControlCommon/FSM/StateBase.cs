using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.FSM
{
    public enum StateExitValue {
        None = 0,
        Success = 1,
        Idling = 2,
        Error = 3,
    } 

    public abstract class StateBase : IEquatable<StateBase> 
    {
        private const int _NoIdleTime = 0;

        private FSMStateIdBase _id;
        private int _idleTimeMs;  

        protected StateBase(FSMStateIdBase id, int idleTimeMs = _NoIdleTime) {
            
            _id = id;
            _idleTimeMs = idleTimeMs <= _NoIdleTime ? _NoIdleTime : idleTimeMs;
        }

        public virtual void OnEntry() { }

        public virtual void StateFunction() { }

        public virtual StateExitValue OnExit() {

            return StateExitValue.None;
        }

        public string Name => (string)(_id.Name?.Clone() ?? string.Empty);

        public int IdValue => _id.Id;

        public virtual bool CanProcessCommand  => false;
        
        private bool _CanIdle =>  _idleTimeMs > 0;

        #region IEquatable
        public bool Equals(StateBase? other) {
            
            if (other is null) {
                return false;
            }

            return _id == other._id;
        }

        public override bool Equals(object? obj) {
            
            if (obj is StateBase other) {
            
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode() {

            return _id.GetHashCode();
        }

        public static bool operator == (StateBase? a, StateBase? b) {

            if (a is null) {
                return b is null;
            }

            return a.Equals(b);
        }

        public static bool operator !=(StateBase? a, StateBase? b) {
            return !(a == b);
        }

        public override string ToString() {
            return $"{_id.Name} ({_id.Id})";
        }

        #endregion
    }
}
