using System;

namespace fr.guiet.kquatre.business.exceptions
{
    public class CannotFindReceptorAddressException : Exception
    {
        public CannotFindReceptorAddressException(string message) : base(message)
        {

        }
    }
}
