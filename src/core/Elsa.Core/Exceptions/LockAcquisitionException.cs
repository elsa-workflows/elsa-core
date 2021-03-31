using System;

namespace Elsa.Exceptions
{
    public class LockAcquisitionException : Exception
    {
        public LockAcquisitionException(string message) : base(message)
        {
        }
        
        public LockAcquisitionException(string message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}