using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.business.exceptions
{
    public class CannotFindReceptorException : Exception
    {
        public CannotFindReceptorException(string message) : base(message)
        {

        }
    }
}
