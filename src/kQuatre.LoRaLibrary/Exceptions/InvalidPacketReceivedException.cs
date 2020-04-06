using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.exceptions
{
    public class InvalidPacketReceivedException : Exception
    {
        public InvalidPacketReceivedException(string message) : base(message)
        {

        }
    }
}
