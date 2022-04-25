using Elsa.Models;
using Elsa.Persistence.Models;

namespace Elsa.Runtime.Services;

/// <summary>
/// Represents a source of workflows.
/// </summary>
public interface IWorkflowProvider
{
    ValueTask<Workflow?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    ValueTask<Workflow?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Workflow> StreamAllAsync(CancellationToken cancellationToken = default);
}