using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.GraphQL.Types;
using GraphQL.Types;

namespace Elsa.Server.GraphQL
{
    public class ElsaQuery : ObjectGraphType
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;

        public ElsaQuery(IWorkflowDefinitionStore workflowDefinitionStore)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;

            FieldAsync<ListGraphType<WorkflowDefinitionVersionType>>("workflowDefinitions", resolve: ResolveWorkflowDefinitions);
        }

        private async Task<object> ResolveWorkflowDefinitions(ResolveFieldContext<object> context)
        {
            var items = await workflowDefinitionStore.ListAsync(VersionOptions.All, context.CancellationToken);

            return items;
        }
    }
}