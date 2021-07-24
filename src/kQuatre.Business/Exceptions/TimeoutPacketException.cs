using System;

namespace fr.guiet.kquatre.business.exceptions
{
    class TimeoutPacketException : Exception
    {
        public TimeoutPacketException(string message) : base(message)
        {

        }
    }
}
