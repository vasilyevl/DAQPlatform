using Grumpy.DaqFramework.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.StatePatternFramework
{
    public class StateIDBase : EnumBase
    {
        public static StateIDBase NA =
                              new StateIDBase(nameof(NA), 0);
        public static StateIDBase Start =
                              new StateIDBase(nameof(Start), 1);
        public static StateIDBase Stop =
                              new StateIDBase(nameof(Stop), 2);
        public static StateIDBase End =
                              new StateIDBase(nameof(End), 3);
        public static StateIDBase GenericError =
                              new StateIDBase(nameof(GenericError), 4);
        public static StateIDBase Loaded =
                              new StateIDBase(nameof(Loaded), 5);
        public static StateIDBase TransitionError =
                              new StateIDBase(nameof(TransitionError), 6);
        public StateIDBase(string name, int id) : base(name, id) { }
    }
}
