using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grumpy.Common.BaseObjects;

namespace Grumpy.Common.Inerfaces
{

    public interface ICalibration<TV, TC>
    {
        public bool Apply(TV value, out TV result);

        public bool Init(string configuration);

        public bool Init(TC configuration);

        public TC Configuration();

        string ConfigurationAsString();

        ErrorRecord GetLastError();
    }
}
