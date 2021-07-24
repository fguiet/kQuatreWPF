using System;

namespace fr.guiet.kquatre.business.exceptions
{
    public class ReceptorAddressAlreadyAssignedException : Exception
    {
        public ReceptorAddressAlreadyAssignedException(string message) : base(message)
        {

        }
    }
}
