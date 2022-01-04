using Elsa.Models;
using Elsa.Persistence.Models;

namespace Elsa.Runtime.Contracts;

/// <summary>
/// Represents a source of workflows.
/// </summary>
public interface IWorkflowProvider
{
    ValueTask<Workflow?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Workflow> StreamAllAsync(CancellationToken cancellationToken = default);
}