using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Metadata;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.GraphQL.Models;
using Elsa.Services;
using HotChocolate;

namespace Elsa.Server.GraphQL
{
    public class Query
    {
        public IEnumerable<ActivityDescriptor> GetActivityDescriptors(
            [Service] IActivityResolver activityResolver,
            [Service] IActivityDescriber describer) =>
            activityResolver.GetActivityTypes().Select(describer.Describe).ToList();

        public ActivityDescriptor? GetActivityDescriptor(
            [Service]IActivityResolver activityResolver,
            [Service] IActivityDescriber describer,
            string typeName)
        {
            var type = activityResolver.GetActivityType(typeName);

            return type == null ? default : describer.Describe(type);
        }

        public async Task<IEnumerable<WorkflowDefinition>> GetWorkflowDefinitions(
            VersionOptionsInput? version,
            [Service] IWorkflowDefinitionStore store,
            [Service] IMapper mapper,
            CancellationToken cancellationToken)
        {
            var mappedVersion = mapper.Map<VersionOptions?>(version);
            return await store.ListAsync(mappedVersion ?? VersionOptions.Latest, cancellationToken);
        }
        
        public async Task<WorkflowDefinition> GetWorkflowDefinition(
            string? id,
            string? definitionId,
            VersionOptionsInput? version,
            [Service] IWorkflowDefinitionStore store,
            [Service] IMapper mapper,
            CancellationToken cancellationToken)
        {
            if (id != null)
                return await store.GetByIdAsync(id, cancellationToken);
            
            var mappedVersion = mapper.Map<VersionOptions?>(version);
            return await store.GetByIdAsync(definitionId, mappedVersion ?? VersionOptions.Latest, cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(
            string definitionId, 
            WorkflowStatus? status,
            [Service] IWorkflowInstanceStore store,
            CancellationToken cancellationToken)
        {
            if(status == null)
                return await store.ListByDefinitionAsync(definitionId, cancellationToken);

            return await store.ListByStatusAsync(definitionId, status.Value, cancellationToken);
        }
        
        public async Task<WorkflowInstance> GetWorkflowInstance(
            string id,
            [Service] IWorkflowInstanceStore store,
            CancellationToken cancellationToken)
        {
            return await store.GetByIdAsync(id, cancellationToken);
        }
    }
}