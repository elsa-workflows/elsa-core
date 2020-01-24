using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.GraphQL.Services;
using Elsa.Server.GraphQL.Types;
using Elsa.Services;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Mutations
{
    public class RunWorkflow : IMutationProvider
    {
        private readonly IWorkflowHost workflowHost;
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;

        public RunWorkflow(IWorkflowHost workflowHost, IWorkflowDefinitionStore workflowDefinitionStore)
        {
            this.workflowHost = workflowHost;
            this.workflowDefinitionStore = workflowDefinitionStore;
        }

        public void Setup(ElsaMutation mutation)
        {
            mutation.FieldAsync<WorkflowInstanceType>(
                "runWorkflow",
                "Run a specified workflow.",
                new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "workflowDefinitionId", Description = "The ID of the workflow definition to run." },
                    new QueryArgument<StringGraphType> { Name = "correlationId", Description = "The correlation ID to associate the workflow with." }
                ),
                async context =>
                {
                    var workflowDefinitionId = context.GetArgument<string>("workflowDefinitionId");
                    var correlationId = context.GetArgument<string>("correlationId");
                    var executionContext = await workflowHost.RunWorkflowInstanceAsync(workflowDefinitionId, correlationId, null, context.CancellationToken);
                    return executionContext.CreateWorkflowInstance();
                });
        }
    }
}