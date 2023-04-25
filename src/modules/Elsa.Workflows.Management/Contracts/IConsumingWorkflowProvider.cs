using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Contracts;

public interface IConsumingWorkflowProvider
{
    Task<IEnumerable<WorkflowDefinition>> FindWorkflowsWithOutdatedCompositeActivitiesAsync(WorkflowDefinition compositeActivity, CancellationToken cancellationToken = default);
}