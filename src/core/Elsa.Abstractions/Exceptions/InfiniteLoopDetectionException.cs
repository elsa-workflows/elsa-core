using System;

namespace Elsa.Exceptions
{
    public class InfiniteLoopDetectionException : WorkflowException
    {
        public InfiniteLoopDetectionException(string message) : base(message)
        {
        }

        public InfiniteLoopDetectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}