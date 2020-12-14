using Elsa.Models;

namespace Elsa.Persistence
{
    public interface ISuspendedWorkflowStore : IStore<SuspendedWorkflowBlockingActivity>
    {
    }
}