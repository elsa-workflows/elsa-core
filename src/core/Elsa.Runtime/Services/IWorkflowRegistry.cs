using Elsa.Models;
using Elsa.Persistence.Models;

namespace Elsa.Runtime.Services;

public interface IWorkflowRegistry
{
    Task<Workflow?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    Task<Workflow?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default);
}