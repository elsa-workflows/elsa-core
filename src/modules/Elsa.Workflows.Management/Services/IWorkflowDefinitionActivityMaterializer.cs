using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Implementations;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Constructs an <see cref="IActivity"/> from the specified <see cref="WorkflowDefinition"/>
/// </summary>
public interface IWorkflowDefinitionActivityMaterializer
{
    Task<IActivity> MaterializeAsync(WorkflowDefinitionActivity activity, CancellationToken cancellationToken = default);
}