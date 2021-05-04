using System;

namespace Elsa.Activities.Telnyx.Exceptions
{
    public class TelnyxException : Exception
    {
        public TelnyxException(string message, Exception? innerException = default) : base(message, innerException)
        {
        }
    }
}