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
        private readonly IProcessRunner processRunner;
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;

        public RunWorkflow(IProcessRunner processRunner, IWorkflowDefinitionStore workflowDefinitionStore)
        {
            this.processRunner = processRunner;
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
                    var workflowDefinition = await workflowDefinitionStore.GetByIdAsync(workflowDefinitionId, VersionOptions.Published, context.CancellationToken);

                    if (workflowDefinition == null)
                        return null;

                    var correlationId = context.GetArgument<string>("correlationId");
                    var executionContext = await processRunner.RunAsync(workflowDefinition, correlationId: correlationId, cancellationToken: context.CancellationToken);
                    return executionContext.ProcessInstance.ToInstance();
                });
        }
    }
}