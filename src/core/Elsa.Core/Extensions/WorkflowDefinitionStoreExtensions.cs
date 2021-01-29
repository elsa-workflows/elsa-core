using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowDefinitions;

namespace Elsa
{
    public static class WorkflowDefinitionStoreExtensions
    {
        public static Task<WorkflowDefinition?> FindByIdAsync(
            this IWorkflowDefinitionStore store,
            string id,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            store.FindAsync(new WorkflowDefinitionIdSpecification(id, versionOptions), cancellationToken);
    }
}