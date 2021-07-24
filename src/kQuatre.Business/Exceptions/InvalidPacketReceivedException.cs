using System;

namespace fr.guiet.kquatre.business.exceptions
{

    public class InvalidPacketReceivedException : Exception
    {
        public InvalidPacketReceivedException(string message) : base(message)
        {

        }
    }

}
