using Elsa.Services.Models;

namespace Elsa.Messages
{
    /// <summary>
    /// Published when a workflow transitioned into the Suspended state.
    /// </summary>
    public class ProcessSuspended : ProcessNotification
    {
        public ProcessSuspended(ProcessInstance process) : base(process)
        {
        }
    }
}