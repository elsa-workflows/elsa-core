using System.Threading.Tasks;

namespace Elsa.Server.Api.Hubs.Clients
{
    public interface IWorkflowTestClient
    {
        Task Connected(string connectionId);
    }
}
