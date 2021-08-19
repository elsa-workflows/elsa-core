using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowDefinitions;

namespace Elsa
{
    public static class WorkflowDefinitionStoreExtensions
    {
        public static Task<WorkflowDefinition?> FindByDefinitionIdAsync(
            this IWorkflowDefinitionStore store,
            string definitionId,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            store.FindAsync(new WorkflowDefinitionIdSpecification(definitionId, versionOptions), cancellationToken);
    }
}