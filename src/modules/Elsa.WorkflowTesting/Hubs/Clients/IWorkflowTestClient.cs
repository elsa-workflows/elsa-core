namespace Elsa.WorkflowTesting.Hubs.Clients
{
    public interface IWorkflowTestClient
    {
        Task Connected(string connectionId);
    }
}
