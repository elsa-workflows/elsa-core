using System.Threading.Tasks;
using Elsa.Services;
using Rebus.Handlers;

namespace Elsa.Messages.Distributed.Handlers
{
    public class RunWorkflowHandler : IHandleMessages<RunWorkflow>
    {
        private readonly IWorkflowHost workflowHost;

        public RunWorkflowHandler(IWorkflowHost workflowHost) => this.workflowHost = workflowHost;

        public Task Handle(RunWorkflow message) => workflowHost.RunWorkflowInstanceAsync(
            message.InstanceId,
            message.ActivityId,
            message.Input);
    }
}