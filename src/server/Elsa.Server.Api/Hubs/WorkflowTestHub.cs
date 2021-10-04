using System.Threading.Tasks;
using Elsa.Server.Api.Hubs.Clients;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Server.Api.Hubs
{
    public class WorkflowTestHub : Hub<IWorkflowTestClient>
    {
        public async Task Connecting()
        {
            await Clients.Client(Context.ConnectionId).Connected(Context.ConnectionId);
        }
    }
}
