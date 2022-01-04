using Elsa.Models;
using Elsa.Persistence.Models;

namespace Elsa.Runtime.Contracts;

public interface IWorkflowRegistry
{
    Task<Workflow?> FindByIdAsync(string id, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Workflow> StreamAllAsync(CancellationToken cancellationToken = default);
}