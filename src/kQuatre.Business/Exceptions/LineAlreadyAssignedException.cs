using System;

namespace fr.guiet.kquatre.business.exceptions
{
    public class LineAlreadyAssignedException : Exception
    {
        public LineAlreadyAssignedException(string message) : base(message)
        {

        }
    }
}
