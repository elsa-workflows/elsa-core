using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Data;
using Elsa.Extensions;
using Elsa.Indexes;
using Elsa.Models;
using YesSql;

namespace Elsa.Queries
{
    public class WorkflowDefinitionByIdAndVersionQuery : ICompiledQuery<WorkflowDefinition>
    {
        public string WorkflowDefinitionId { get; }
        public VersionOptions VersionOptions { get; }

        public WorkflowDefinitionByIdAndVersionQuery(string workflowDefinitionId, VersionOptions versionOptions)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            VersionOptions = versionOptions;
        }

        public Expression<Func<IQuery<WorkflowDefinition>, IQuery<WorkflowDefinition>>> Query()
        {
            return query => query
                .With<WorkflowDefinitionIndex>(x => x.WorkflowDefinitionId == WorkflowDefinitionId)
                .WithVersion(VersionOptions);
        }
    }

    public static class WorkflowDefinitionByIdAndVersionQuerySessionExtensions
    {
        public static IQuery<WorkflowDefinition> QueryWorkflowDefinitionByIdAndVersion(
            this ISession session,
            string workflowDefinitionId,
            VersionOptions versionOptions)
        {
            return session.ExecuteQuery(
                new WorkflowDefinitionByIdAndVersionQuery(workflowDefinitionId, versionOptions),
                CollectionNames.WorkflowDefinitions);
        }

        public static async Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(
            this ISession session,
            string workflowDefinitionId,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            await session.QueryWorkflowDefinitionByIdAndVersion(workflowDefinitionId, versionOptions)
                .FirstOrDefaultAsync();
    }
}