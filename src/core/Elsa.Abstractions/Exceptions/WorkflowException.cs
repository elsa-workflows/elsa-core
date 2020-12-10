using System;

namespace Elsa.Exceptions
{
    public class WorkflowException : Exception
    {
        public WorkflowException() : base()
        {
        }

        public WorkflowException(string message) : base(message)
        {
        }

        public WorkflowException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
