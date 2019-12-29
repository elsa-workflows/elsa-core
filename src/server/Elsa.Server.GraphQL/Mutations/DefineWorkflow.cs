using AutoMapper;
using Elsa.Models;
using Elsa.Server.GraphQL.Models;
using Elsa.Server.GraphQL.Services;
using Elsa.Server.GraphQL.Types;
using Elsa.Server.GraphQL.Types.Input;
using Elsa.Services;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Mutations
{
    public class DefineWorkflow : IMutationProvider
    {
        private readonly IWorkflowPublisher publisher;
        private readonly IIdGenerator idGenerator;
        private readonly IMapper mapper;

        public DefineWorkflow(IWorkflowPublisher publisher, IIdGenerator idGenerator, IMapper mapper)
        {
            this.publisher = publisher;
            this.idGenerator = idGenerator;
            this.mapper = mapper;
        }
        
        public void Setup(ElsaMutation mutation)
        {
            mutation.FieldAsync<WorkflowDefinitionVersionType>(
                "defineWorkflow",
                "Create a new workflow definitions",
                new QueryArguments(new QueryArgument<NonNullGraphType<DefineWorkflowDefinitionInputType>> { Name = "workflowDefinition" }),
                async context =>
                {
                    var model = context.GetArgument<DefineWorkflowDefinitionInputModel>("workflowDefinition");
                    var publish = model.Publish;
                    var workflowDefinitionVersion = mapper.Map<ProcessDefinitionVersion>(model);

                    workflowDefinitionVersion.Id = idGenerator.Generate();

                    if (string.IsNullOrWhiteSpace(workflowDefinitionVersion.DefinitionId))
                        workflowDefinitionVersion.DefinitionId = idGenerator.Generate();

                    if (publish)
                        await publisher.PublishAsync(workflowDefinitionVersion, context.CancellationToken);
                    else
                        await publisher.SaveDraftAsync(workflowDefinitionVersion, context.CancellationToken);

                    return workflowDefinitionVersion;
                });
        }
    }
}