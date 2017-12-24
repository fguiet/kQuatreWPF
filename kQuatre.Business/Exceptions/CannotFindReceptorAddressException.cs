using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Exceptions
{
    public class CannotFindReceptorAddressException : Exception
    {
        public CannotFindReceptorAddressException(string message) : base(message)
        {

        }
    }
}
