using Elsa.Services.Models;

namespace Elsa.Messages
{
    public class ExecutingProcess : ProcessNotification
    {
        public ExecutingProcess(ProcessInstance process) : base(process)
        {
        }
    }
}