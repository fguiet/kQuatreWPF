using System;

namespace fr.guiet.lora.exceptions
{
    public class InvalidPacketReceivedException : Exception
    {
        public InvalidPacketReceivedException(string message) : base(message)
        {

        }
    }
}
