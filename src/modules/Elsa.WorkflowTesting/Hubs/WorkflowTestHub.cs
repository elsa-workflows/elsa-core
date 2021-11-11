using Elsa.WorkflowTesting.Hubs.Clients;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.WorkflowTesting.Hubs
{
    public class WorkflowTestHub : Hub<IWorkflowTestClient>
    {
        public async Task Connecting()
        {
            await Clients.Client(Context.ConnectionId).Connected(Context.ConnectionId);
        }
    }
}
