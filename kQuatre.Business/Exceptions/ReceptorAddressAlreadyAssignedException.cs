using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Exceptions
{
    public class ReceptorAddressAlreadyAssignedException : Exception
    {
        public ReceptorAddressAlreadyAssignedException(string message) : base(message)
        {

        }
    }
}
