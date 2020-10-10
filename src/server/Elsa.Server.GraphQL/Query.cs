using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Extensions;
using Elsa.Indexes;
using Elsa.Metadata;
using Elsa.Models;
using Elsa.Queries;
using Elsa.Server.GraphQL.Models;
using Elsa.Services;
using HotChocolate;
using YesSql;

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
            [Service] ISession session,
            [Service] IMapper mapper,
            CancellationToken cancellationToken)
        {
            var mappedVersion = mapper.Map<VersionOptions?>(version);
            return await session.QueryWorkflowDefinitions().WithVersion(mappedVersion ?? VersionOptions.Latest)
                .ListAsync();
        }

        public async Task<WorkflowDefinition?> GetWorkflowDefinition(
            string? workflowDefinitionVersionId,
            string? workflowDefinitionId,
            VersionOptionsInput? version,
            [Service] ISession session,
            [Service] IMapper mapper,
            CancellationToken cancellationToken)
        {
            if (workflowDefinitionVersionId != null)
                return await session.QueryWorkflowDefinitions<WorkflowDefinitionIndex>(
                    x => x.WorkflowDefinitionVersionId == workflowDefinitionVersionId).FirstOrDefaultAsync();

            var mappedVersion = mapper.Map<VersionOptions?>(version);

            return await session.GetWorkflowDefinitionAsync(
                workflowDefinitionId!,
                mappedVersion ?? VersionOptions.Latest,
                cancellationToken);
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(
            string definitionId,
            WorkflowStatus? status,
            [Service] ISession session,
            CancellationToken cancellationToken)
        {
            Expression<Func<WorkflowInstanceIndex, bool>> predicate;

            if (status == null)
                predicate = x => x.WorkflowDefinitionId == definitionId;
            else
                predicate = x => x.WorkflowDefinitionId == definitionId && x.WorkflowStatus == status;

            return await session.QueryWorkflowInstances(predicate).ListAsync();
        }

        public async Task<WorkflowInstance?> GetWorkflowInstance(
            string id,
            [Service] ISession session,
            CancellationToken cancellationToken)
        {
            return await session.GetWorkflowInstanceByIdAsync(id, cancellationToken);
        }
    }
}