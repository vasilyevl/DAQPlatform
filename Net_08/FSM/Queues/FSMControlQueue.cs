
using System.Collections.Concurrent;

namespace FSM
{
    public class ControlQueue<T> : ConcurrentQueue<T> where T : new()
    {
        public const int DEFAULT_LENGTH = 1024;
        public const int INFINIT_LENGTH = -1;

        protected int _maxLength;
        protected T _lastDequeValue;

        public ControlQueue(T startUpValue) : base()
        {
            _maxLength = INFINIT_LENGTH;
            _lastDequeValue = startUpValue;
        }

        public T Value
        {
            get
            {
                T vdq;
                if (TryDequeue(out vdq))
                    System.Threading.Thread.MemoryBarrier();
                _lastDequeValue = vdq;
                System.Threading.Thread.MemoryBarrier();
                return _lastDequeValue;
            }
        }
    }
}
