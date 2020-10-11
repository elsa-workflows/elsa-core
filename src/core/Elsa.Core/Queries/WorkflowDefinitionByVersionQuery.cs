using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using YesSql;

namespace Elsa.Queries
{
    public class WorkflowDefinitionByVersionQuery : ICompiledQuery<WorkflowDefinition>
    {
        public VersionOptions VersionOptions { get; }
        public WorkflowDefinitionByVersionQuery(VersionOptions versionOptions) => VersionOptions = versionOptions;

        public Expression<Func<IQuery<WorkflowDefinition>, IQuery<WorkflowDefinition>>> Query() =>
            query => query.WithVersion(VersionOptions);
    }

    public static class WorkflowDefinitionByVersionQueryWorkflowDefinitionManagerExtensions
    {
        public static IQuery<WorkflowDefinition> QueryByVersion(this IWorkflowDefinitionManager manager,
            VersionOptions versionOptions) =>
            manager.ExecuteQuery(
                new WorkflowDefinitionByVersionQuery(versionOptions));

        public static async Task<IEnumerable<WorkflowDefinition>> ListAsync(
            this IWorkflowDefinitionManager manager,
            VersionOptions versionOptions,
            CancellationToken cancellationToken = default) =>
            await manager.QueryByVersion(versionOptions).ListAsync();
    }
}