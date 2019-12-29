using Elsa.Services.Models;

namespace Elsa.Messages
{
    /// <summary>
    /// Published when a burst of execution finished.
    /// </summary>
    public class ProcessExecuted  : ProcessNotification
    {
        public ProcessExecuted(ProcessInstance process) : base(process)
        {
        }
    }
}