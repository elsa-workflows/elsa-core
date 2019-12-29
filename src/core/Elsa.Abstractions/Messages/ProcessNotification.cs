using Elsa.Services.Models;
using MediatR;

namespace Elsa.Messages
{
    /// <summary>
    /// Common base for workflow-related events.
    /// </summary>
    public abstract class ProcessNotification : INotification
    {
        protected ProcessNotification(ProcessInstance process)
        {
            Process = process;
        }
        
        public ProcessInstance Process { get; }
    }
}