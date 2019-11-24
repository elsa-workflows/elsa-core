using Elsa.Persistence;
using Elsa.Server.GraphQL.Services;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Mutations
{
    public class DeleteWorkflowDefinition : IMutationProvider
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;

        public DeleteWorkflowDefinition(IWorkflowDefinitionStore workflowDefinitionStore)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
        }

        public void Setup(ElsaMutation mutation)
        {
            mutation.FieldAsync<IntGraphType>(
                "deleteWorkflowDefinition",
                "Delete a specified workflow definition.",
                new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "workflowDefinitionId", Description = "The ID of the workflow definition to delete." }
                ),
                async context =>
                {
                    var workflowDefinitionId = context.GetArgument<string>("workflowDefinitionId");

                    return await workflowDefinitionStore.DeleteAsync(workflowDefinitionId, context.CancellationToken);
                });
        }
    }
}