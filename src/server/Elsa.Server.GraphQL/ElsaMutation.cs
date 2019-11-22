using System.Linq;
using Elsa.Models;
using Elsa.Server.GraphQL.Models;
using Elsa.Server.GraphQL.Types;
using Elsa.Services;
using GraphQL.Types;

namespace Elsa.Server.GraphQL
{
    public class ElsaMutation : ObjectGraphType
    {
        public ElsaMutation(IWorkflowPublisher publisher, IIdGenerator idGenerator)
        {
            FieldAsync<WorkflowDefinitionVersionType>(
                "defineWorkflow",
                "Create a new workflow definitions",
                new QueryArguments(new QueryArgument<NonNullGraphType<WorkflowDefinitionInputType>> { Name = "workflowDefinition" }),
                async context =>
                {
                    var model = context.GetArgument<WorkflowDefinitionInputModel>("workflowDefinition");
                    var publish = model.Publish;

                    var workflowDefinition = new WorkflowDefinitionVersion(
                        idGenerator.Generate(),
                        model.Id ?? idGenerator.Generate(),
                        1,
                        model.Name,
                        model.Description,
                        model.Activities.Select(x => x.ToActivityDefinition()),
                        model.Connections,
                        model.IsSingleton,
                        model.IsDisabled,
                        new Variables()
                    );

                    if (publish)
                        await publisher.PublishAsync(workflowDefinition, context.CancellationToken);
                    else
                        await publisher.SaveDraftAsync(workflowDefinition, context.CancellationToken);

                    return workflowDefinition;
                });
        }
    }
}