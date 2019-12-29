using Elsa.Services.Models;

namespace Elsa.Messages
{
    /// <summary>
    /// Published when a workflow transitioned into the Faulted state.
    /// </summary>
    public class ProcessFaulted : ProcessNotification
    {
        public ProcessFaulted(ProcessInstance process) : base(process)
        {
        }
    }
}