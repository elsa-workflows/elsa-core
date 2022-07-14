using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Providers.Workflows
{
    public abstract class WorkflowProvider : IWorkflowProvider
    {
        public abstract IAsyncEnumerable<IWorkflowBlueprint> ListAsync(VersionOptions versionOptions, int? skip = default, int? take = default, string? tenantId = default, CancellationToken cancellationToken = default);
        public abstract ValueTask<int> CountAsync(VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default);

        public abstract ValueTask<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default);
        public abstract ValueTask<IWorkflowBlueprint?> FindByDefinitionVersionIdAsync(string definitionVersionId, string? tenantId = default, CancellationToken cancellationToken = default);

        public abstract ValueTask<IWorkflowBlueprint?> FindByNameAsync(string name, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default);
        public abstract ValueTask<IWorkflowBlueprint?> FindByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default);
        public abstract ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionIds(IEnumerable<string> definitionIds, VersionOptions versionOptions, CancellationToken cancellationToken = default);
        public abstract ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionVersionIds(IEnumerable<string> definitionVersionIds, CancellationToken cancellationToken = default);
        public abstract ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByNames(IEnumerable<string> names, CancellationToken cancellationToken = default);
        public abstract ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default);
    }
}