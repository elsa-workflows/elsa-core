namespace Elsa.WorkflowTesting.Api.Hubs.Clients
{
    public interface IWorkflowTestClient
    {
        Task Connected(string connectionId);
    }
}
