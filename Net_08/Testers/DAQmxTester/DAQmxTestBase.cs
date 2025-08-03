using Grumpy.DAQmxTester;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAQmxTester
{
    public abstract class DAQmxTestBase
    {
        protected readonly DAQmxTestHelper _helper;

        protected DAQmxTestBase(DAQmxTestHelperConfig? config = null) {
            _helper = new DAQmxTestHelper(config ?? new DAQmxTestHelperConfig());
        }
    }
}
