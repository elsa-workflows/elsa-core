using Elsa.Models;
using Elsa.Persistence.Entities;

namespace Elsa.Management.Services;

public interface IWorkflowMaterializer
{
    string Name { get; }
    ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
}