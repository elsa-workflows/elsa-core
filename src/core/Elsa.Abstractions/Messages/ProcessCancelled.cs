using Elsa.Services.Models;

namespace Elsa.Messages
{
    /// <summary>
    /// Published when a workflow transitioned into the Cancelled state.
    /// </summary>
    public class ProcessCancelled : ProcessNotification
    {
        public ProcessCancelled(ProcessInstance process) : base(process)
        {
        }
    }
}