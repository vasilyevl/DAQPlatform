using Tau.ControlBase;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM
{
    public class StateQueue : QueueBase<StateBase>    {
        public const int DefaultMaxDepth = 128;



        public StateQueue(int maxDepth = DefaultMaxDepth) : base(maxDepth)
        { }


        public bool StatePending => base.ItemPending;

        public int StateSwequenceDepth => base.Count;

        public int StateQueueRoomLeft => base.RoomLeft;

        public void Append(StateBase state) => base.TryEnqueue(state);
    }

}
