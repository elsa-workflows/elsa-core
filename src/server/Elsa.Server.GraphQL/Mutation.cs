using System.Collections.Generic;
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

namespace Elsa.Server.GraphQL
{
    public class Mutation 
    {
        private readonly IWorkflowDefinitionStore store;
        private readonly IWorkflowSerializer serializer;
        private readonly IWorkflowPublisher publisher;
        private readonly IMapper mapper;

        public Mutation(IWorkflowDefinitionStore store, IWorkflowSerializer serializer, IWorkflowPublisher publisher, IMapper mapper)
        {
            this.store = store;
            this.serializer = serializer;
            this.publisher = publisher;
            this.mapper = mapper;
        }
        
        public async Task<WorkflowDefinitionVersion> SaveWorkflowDefinition(
            string id,
            WorkflowSaveAction saveAction, 
            WorkflowInput workflowInput,
            CancellationToken cancellationToken)
        {
            var workflowDefinition = await store.GetByIdAsync(id, VersionOptions.Latest, cancellationToken);

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
            
            if (saveAction == WorkflowSaveAction.Publish)
                workflowDefinition = await publisher.PublishAsync(workflowDefinition, cancellationToken);
            else
                workflowDefinition = await publisher.SaveDraftAsync(workflowDefinition, cancellationToken);
            
            return workflowDefinition;
        }

        private ActivityDefinition ToActivityDefinition(ActivityDefinitionInput source) => mapper.Map<ActivityDefinition>(source);
    }
}