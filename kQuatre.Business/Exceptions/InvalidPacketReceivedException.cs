using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Exceptions
{

    public class InvalidPacketReceivedException : Exception
    {
        public InvalidPacketReceivedException(string message) : base(message)
        {

        }
    }

}
