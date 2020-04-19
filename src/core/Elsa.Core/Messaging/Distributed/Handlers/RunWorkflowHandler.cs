using System.Threading.Tasks;
using Elsa.Services;
using Rebus.Handlers;

namespace Elsa.Messaging.Distributed.Handlers
{
    public class RunWorkflowHandler : IHandleMessages<RunWorkflow>
    {
        private readonly IWorkflowHost workflowHost;

        public RunWorkflowHandler(IWorkflowHost workflowHost) => this.workflowHost = workflowHost;

        public async Task Handle(RunWorkflow message) => await workflowHost.RunWorkflowInstanceAsync(
            message.InstanceId,
            message.ActivityId,
            message.Input);
    }
}