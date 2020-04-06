using System;
using System.Collections.Generic;
using System.Text;

namespace fr.guiet.lora.exceptions
{
    public class AckNotReceivedTimeoutException : Exception
    {
        public AckNotReceivedTimeoutException(string message) : base(message)
        {

        }
    }
}
