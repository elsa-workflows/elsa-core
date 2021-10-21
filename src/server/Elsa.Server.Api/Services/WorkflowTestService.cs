using System.Threading.Tasks;
using Elsa.Server.Api.Hubs;
using Elsa.Server.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Server.Api.Services
{
    public class WorkflowTestService : Hub, IWorkflowTestService
    {
        private readonly IHubContext<WorkflowTestHub> _hubContext;

        public WorkflowTestService(IHubContext<WorkflowTestHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task DispatchMessage(string signalRConnectionId, WorkflowTestMessage message)
        {
            await _hubContext.Clients.Client(signalRConnectionId).SendAsync("DispatchMessage", message);
        }
    }
}
