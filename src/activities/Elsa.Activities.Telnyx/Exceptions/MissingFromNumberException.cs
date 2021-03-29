using System;

namespace Elsa.Activities.Telnyx.Exceptions
{
    public class MissingFromNumberException : TelnyxException
    {
        public MissingFromNumberException(string message, Exception? innerException = default) : base(message, innerException)
        {
        }
    }
}