using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;

namespace Elsa.Extensions
{
    public static class WorkflowDefinitionStoreExtensions
    {
        public static Task<WorkflowDefinition?> FindByIdAsync(
            this IWorkflowDefinitionStore store,
            string id,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            store.FindAsync(new WorkflowDefinitionIdSpecification(id).And(new VersionOptionsSpecification(versionOptions)), cancellationToken);
    }
}