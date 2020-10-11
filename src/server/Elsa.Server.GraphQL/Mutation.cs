using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Queries;
using Elsa.Serialization;
using Elsa.Server.GraphQL.Models;
using Elsa.Server.GraphQL.Types;
using Elsa.Services;
using HotChocolate;
using Open.Linq.AsyncExtensions;
using YesSql;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Server.GraphQL
{
    public class Mutation
    {
        private readonly IMapper _mapper;

        public Mutation(IMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task<WorkflowDefinition> SaveWorkflowDefinition(
            string? id,
            WorkflowSaveAction saveAction,
            WorkflowInput workflowInput,
            [Service] IWorkflowDefinitionManager manager,
            [Service] IIdGenerator idGenerator,
            [Service] ITokenSerializer serializer,
            [Service] IWorkflowPublisher publisher,
            CancellationToken cancellationToken)
        {
            var workflowDefinition = id != null
                ? await manager.GetAsync(id, VersionOptions.Latest, cancellationToken)
                : default;

            if (workflowDefinition == null)
            {
                workflowDefinition = new WorkflowDefinition
                {
                    WorkflowDefinitionId = idGenerator.Generate(),
                    WorkflowDefinitionVersionId = idGenerator.Generate(),
                    Version = 1,
                    IsLatest = true
                };
            }

            if (workflowInput.Activities != null)
                workflowDefinition.Activities = workflowInput.Activities.Select(ToActivityDefinition).ToList();

            if (workflowInput.Connections != null)
                workflowDefinition.Connections = workflowInput.Connections;

            if (workflowInput.Description != null)
                workflowDefinition.Description = workflowInput.Description.Trim();

            if (workflowInput.Name != null)
                workflowDefinition.Name = workflowInput.Name.Trim();

            if (workflowInput.IsEnabled != null)
                workflowDefinition.IsEnabled = workflowInput.IsEnabled.Value;

            if (workflowInput.IsSingleton != null)
                workflowDefinition.IsSingleton = workflowInput.IsSingleton.Value;

            if (workflowInput.DeleteCompletedInstances != null)
                workflowDefinition.DeleteCompletedInstances = workflowInput.DeleteCompletedInstances.Value;

            if (workflowInput.Variables != null)
                workflowDefinition.Variables = serializer.Deserialize<Variables>(workflowInput.Variables);

            if (workflowInput.PersistenceBehavior != null)
                workflowDefinition.PersistenceBehavior = workflowInput.PersistenceBehavior.Value;

            if (saveAction == WorkflowSaveAction.Publish)
                workflowDefinition = await publisher.PublishAsync(workflowDefinition, cancellationToken);
            else
                workflowDefinition = await publisher.SaveDraftAsync(workflowDefinition, cancellationToken);

            return workflowDefinition;
        }

        public async Task<int> DeleteWorkflowDefinition(
            string id,
            [Service] IWorkflowDefinitionManager manager,
            CancellationToken cancellationToken)
        {
            var workflowDefinitions = await manager.QueryByIdAndVersion(id, VersionOptions.All)
                .ListAsync()
                .ToList();

            foreach (var workflowDefinition in workflowDefinitions)
                await manager.DeleteAsync(workflowDefinition, cancellationToken);

            return workflowDefinitions.Count;
        }

        private ActivityDefinition ToActivityDefinition(ActivityDefinitionInput source) =>
            _mapper.Map<ActivityDefinition>(source);
    }
}