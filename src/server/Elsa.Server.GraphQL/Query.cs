using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Metadata;
using Elsa.Models;
using Elsa.Queries;
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
            [Service] IActivityResolver activityResolver,
            [Service] IActivityDescriber describer,
            string typeName)
        {
            var type = activityResolver.GetActivityType(typeName);

            return type == null ? default : describer.Describe(type);
        }

        public async Task<IEnumerable<WorkflowDefinition>> GetWorkflowDefinitions(
            VersionOptionsInput? version,
            [Service] IWorkflowDefinitionManager manager,
            [Service] IMapper mapper,
            CancellationToken cancellationToken)
        {
            var mappedVersion = mapper.Map<VersionOptions?>(version);
            return await manager.ListAsync(mappedVersion ?? VersionOptions.Latest, cancellationToken);
        }

        public async Task<WorkflowDefinition?> GetWorkflowDefinition(
            string? workflowDefinitionVersionId,
            string? workflowDefinitionId,
            VersionOptionsInput? version,
            [Service] IWorkflowDefinitionManager manager,
            [Service] IMapper mapper,
            CancellationToken cancellationToken)
        {
            if (workflowDefinitionVersionId != null)
                return await manager.GetByVersionIdAsync(workflowDefinitionVersionId, cancellationToken);

            var mappedVersion = mapper.Map<VersionOptions?>(version);

            return await manager.GetAsync(
                workflowDefinitionId!,
                mappedVersion ?? VersionOptions.Latest,
                cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(
            string definitionId,
            WorkflowStatus? status,
            [Service] IWorkflowInstanceManager manager,
            CancellationToken cancellationToken) =>
            status == null
                ? await manager.ListByDefinitionAsync(definitionId, cancellationToken)
                : await manager.ListByDefinitionAndStatusAsync(definitionId, status.Value, cancellationToken);

        public async Task<WorkflowInstance?> GetWorkflowInstance(
            string id,
            [Service] IWorkflowInstanceManager manager,
            CancellationToken cancellationToken) =>
            await manager.GetByWorkflowInstanceIdAsync(id, cancellationToken);
    }
}