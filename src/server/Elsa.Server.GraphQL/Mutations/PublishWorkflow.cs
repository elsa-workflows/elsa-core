using Elsa.Server.GraphQL.Services;
using Elsa.Server.GraphQL.Types;
using Elsa.Services;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Mutations
{
    public class PublishWorkflow : IMutationProvider
    {
        private readonly IWorkflowPublisher publisher;

        public PublishWorkflow(IWorkflowPublisher publisher)
        {
            this.publisher = publisher;
        }

        public void Setup(ElsaMutation mutation)
        {
            mutation.FieldAsync<WorkflowDefinitionVersionType>(
                "publishWorkflow",
                "Publish a specified workflow.",
                new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "workflowDefinitionId", Description = "The ID of the workflow definition to publish." }
                ),
                async context =>
                {
                    var workflowDefinitionId = context.GetArgument<string>("workflowDefinitionId");

                    return await publisher.PublishAsync(workflowDefinitionId, context.CancellationToken);
                });
        }
    }
}