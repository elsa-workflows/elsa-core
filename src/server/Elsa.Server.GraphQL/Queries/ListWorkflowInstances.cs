using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.GraphQL.Services;
using Elsa.Server.GraphQL.Types;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Queries
{
    public class ListWorkflowInstances : IQueryProvider
    {
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public ListWorkflowInstances(IWorkflowInstanceStore workflowInstanceStore)
        {
            this.workflowInstanceStore = workflowInstanceStore;
        }
        
        public void Setup(ElsaQuery query)
        {
            query.FieldAsync<ListGraphType<WorkflowInstanceType>>(
                "workflowInstances",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType>{ Name = "definitionId", Description = "Filter workflow instances by the specified workflow definition ID."},
                    new QueryArgument<EnumerationGraphType<WorkflowStatus>>{ Name = "status", Description = "Filter workflow instances by the specified workflow status."}
                ),
                resolve: async context => await workflowInstanceStore.ListAllAsync(context.CancellationToken));
        }
    }
}