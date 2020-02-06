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
            [Service] IActivityResolver activityResolver) =>
            activityResolver.GetActivityTypes().Select(ActivityDescriber.Describe).ToList();

        public ActivityDescriptor? GetActivityDescriptor([Service]IActivityResolver activityResolver, string typeName)
        {
            var type = activityResolver.GetActivityType(typeName);

            return type == null ? default : ActivityDescriber.Describe(type);
        }

        public async Task<IEnumerable<WorkflowDefinitionVersion>> GetWorkflowDefinitions(
            VersionOptionsInput? version,
            [Service] IWorkflowDefinitionStore store,
            [Service] IMapper mapper,
            CancellationToken cancellationToken)
        {
            var mappedVersion = mapper.Map<VersionOptions?>(version);
            return await store.ListAsync(mappedVersion ?? VersionOptions.Latest, cancellationToken);
        }
        
        public async Task<WorkflowDefinitionVersion> GetWorkflowDefinition(
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
    }
}