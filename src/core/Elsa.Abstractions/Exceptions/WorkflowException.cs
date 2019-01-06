using System;

namespace Elsa.Exceptions
{
    public class WorkflowException : Exception
    {
        public WorkflowException(string message) : base(message)
        {
        }
    }
}