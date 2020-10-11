using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Indexes;
using Elsa.Models;
using Elsa.Services;
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

    public static class WorkflowDefinitionByIdAndVersionQueryWorkflowDefinitionManagerExtensions
    {
        public static IQuery<WorkflowDefinition> QueryByIdAndVersion(
            this IWorkflowDefinitionManager manager,
            string workflowDefinitionId,
            VersionOptions versionOptions)
        {
            return manager.ExecuteQuery(
                new WorkflowDefinitionByIdAndVersionQuery(workflowDefinitionId, versionOptions));
        }

        public static async Task<WorkflowDefinition?> GetAsync(
            this IWorkflowDefinitionManager manager,
            string workflowDefinitionId,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            await manager.QueryByIdAndVersion(workflowDefinitionId, versionOptions)
                .FirstOrDefaultAsync();
    }
}