using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Manages materialization of <see cref="WorkflowDefinition"/> to <see cref="Workflow"/> objects. 
/// </summary>
public interface IWorkflowDefinitionService
{
    Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> FindAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
}