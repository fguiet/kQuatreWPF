using System;

namespace fr.guiet.lora.exceptions
{
    public class AckNotReceivedTimeoutException : Exception
    {
        public AckNotReceivedTimeoutException(string message) : base(message)
        {

        }
    }
}
