using Elsa.Models;

namespace Elsa.Providers.WorkflowStorage
{
    public record WorkflowStorageContext(WorkflowInstance WorkflowInstance, string ActivityId);
}