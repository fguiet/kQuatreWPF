using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Exceptions
{
    class TimeoutPacketException : Exception
    {
        public TimeoutPacketException(string message) : base(message)
        {

        }
    }
}
