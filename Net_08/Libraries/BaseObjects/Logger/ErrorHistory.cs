using Grumpy.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.Common
{
    public class ErrorHistory: StackBase<LogRecord> {

        public static ErrorHistory Create(string? name = null,
            uint maxCapacity = _MaxCapacity) {

            return new ErrorHistory(name, maxCapacity);
        }

        private static int _objectCounter = 1;
        private const int _MaxCapacity = 16;
        public ErrorHistory() : base($"ErrorStack_{_objectCounter}") {

            _objectCounter++;
        }

        public ErrorHistory(string? name = null, 
            uint maxCapacity = _MaxCapacity ) : 
            base( string.IsNullOrEmpty(name) ? $"ErrorStack_{_objectCounter}" : name,
                maxCapacity) {
            _objectCounter++;
        }

    }
}
