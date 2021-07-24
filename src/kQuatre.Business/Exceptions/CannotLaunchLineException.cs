using System;

namespace fr.guiet.kquatre.business.exceptions
{
    public class CannotLaunchLineException : Exception
    {
        public CannotLaunchLineException(string message) : base(message)
        {

        }
    }
}
