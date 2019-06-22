using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRegistry
    {
        void RegisterWorkflow(IWorkflow workflow);
    }
}