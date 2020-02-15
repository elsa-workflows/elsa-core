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
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly ITokenSerializer serializer;
        private readonly IWorkflowPublisher publisher;
        private readonly IMapper mapper;

        public Mutation(
            IWorkflowDefinitionStore workflowDefinitionStore,
            IWorkflowInstanceStore workflowInstanceStore,
            ITokenSerializer serializer, 
            IWorkflowPublisher publisher, 
            IMapper mapper)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.workflowInstanceStore = workflowInstanceStore;
            this.serializer = serializer;
            this.publisher = publisher;
            this.mapper = mapper;
        }

        public async Task<WorkflowDefinitionVersion> SaveWorkflowDefinition(
            string? id,
            WorkflowSaveAction saveAction,
            WorkflowInput workflowInput,
            [Service] IIdGenerator idGenerator,
            CancellationToken cancellationToken)
        {
            var workflowDefinition = id != null ? await workflowDefinitionStore.GetByIdAsync(id, cancellationToken) : default;

            if (workflowDefinition == null)
            {
                workflowDefinition = new WorkflowDefinitionVersion
                {
                    Id = idGenerator.Generate(),
                    DefinitionId = idGenerator.Generate(),
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

            if (workflowInput.IsDisabled != null)
                workflowDefinition.IsDisabled = workflowInput.IsDisabled.Value;

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

        public async Task<int> DeleteWorkflowDefinition(string id, CancellationToken cancellationToken)
        {
            return await workflowDefinitionStore.DeleteAsync(id, cancellationToken);
        }

        private ActivityDefinition ToActivityDefinition(ActivityDefinitionInput source) => mapper.Map<ActivityDefinition>(source);
    }
}