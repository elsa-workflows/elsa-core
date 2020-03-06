using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Server.GraphQL.Models;
using Elsa.Server.GraphQL.Types;
using Elsa.Services;
using HotChocolate;

namespace Elsa.Server.GraphQL
{
    public class Mutation
    {
        private readonly IMapper mapper;

        public Mutation(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public async Task<WorkflowDefinitionVersion> SaveWorkflowDefinitionVersion(
            string? id,
            WorkflowSaveAction saveAction,
            WorkflowInput workflowInput,
            [Service] IWorkflowDefinitionVersionStore store,
            [Service] IIdGenerator idGenerator,
            [Service] ITokenSerializer serializer,
            [Service] IWorkflowPublisher publisher,
            CancellationToken cancellationToken)
        {
            var workflowDefinitionVersion = id != null ? await store.GetByIdAsync(id, cancellationToken) : default;

            if (workflowDefinitionVersion == null)
            {
                workflowDefinitionVersion = new WorkflowDefinitionVersion
                {
                    Id = idGenerator.Generate(),
                    DefinitionId = idGenerator.Generate(),
                    Version = 1,
                    IsLatest = true
                };
            }

            if (workflowInput.Activities != null)
                workflowDefinitionVersion.Activities = workflowInput.Activities.Select(ToActivityDefinition).ToList();

            if (workflowInput.Connections != null)
                workflowDefinitionVersion.Connections = workflowInput.Connections;

            if (workflowInput.Description != null)
                workflowDefinitionVersion.Description = workflowInput.Description.Trim();

            if (workflowInput.Name != null)
                workflowDefinitionVersion.Name = workflowInput.Name.Trim();

            if (workflowInput.IsDisabled != null)
                workflowDefinitionVersion.IsDisabled = workflowInput.IsDisabled.Value;

            if (workflowInput.IsSingleton != null)
                workflowDefinitionVersion.IsSingleton = workflowInput.IsSingleton.Value;

            if (workflowInput.DeleteCompletedInstances != null)
                workflowDefinitionVersion.DeleteCompletedInstances = workflowInput.DeleteCompletedInstances.Value;

            if (workflowInput.Variables != null)
                workflowDefinitionVersion.Variables = serializer.Deserialize<Variables>(workflowInput.Variables);

            if (workflowInput.PersistenceBehavior != null)
                workflowDefinitionVersion.PersistenceBehavior = workflowInput.PersistenceBehavior.Value;

            if (saveAction == WorkflowSaveAction.Publish)
                workflowDefinitionVersion = await publisher.PublishAsync(workflowDefinitionVersion, cancellationToken);
            else
                workflowDefinitionVersion = await publisher.SaveDraftAsync(workflowDefinitionVersion, cancellationToken);

            return workflowDefinitionVersion;
        }

        public async Task<int> DeleteWorkflowDefinitionVersion(
            string id,
            [Service] IWorkflowDefinitionVersionStore store,
            CancellationToken cancellationToken)
        {
            return await store.DeleteAsync(id, cancellationToken);
        }

        private ActivityDefinition ToActivityDefinition(ActivityDefinitionInput source) => mapper.Map<ActivityDefinition>(source);
    }
}