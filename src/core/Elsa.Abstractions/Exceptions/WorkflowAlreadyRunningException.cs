using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Exceptions
{
    public class WorkflowAlreadyRunningException : WorkflowException
    {
        public WorkflowAlreadyRunningException(string message) 
            : base(message)
        {
        }
        public WorkflowAlreadyRunningException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
