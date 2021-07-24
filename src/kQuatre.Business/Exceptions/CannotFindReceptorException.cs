using System;

namespace fr.guiet.kquatre.business.exceptions
{
    public class CannotFindReceptorException : Exception
    {
        public CannotFindReceptorException(string message) : base(message)
        {

        }
    }
}
