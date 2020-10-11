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
    public class WorkflowDefinitionByVersionIdQuery : ICompiledQuery<WorkflowDefinition>
    {
        public string WorkflowDefinitionVersionId { get; }

        public WorkflowDefinitionByVersionIdQuery(string workflowDefinitionVersionId) =>
            WorkflowDefinitionVersionId = workflowDefinitionVersionId;

        public Expression<Func<IQuery<WorkflowDefinition>, IQuery<WorkflowDefinition>>> Query()
        {
            return query => query
                .With<WorkflowDefinitionIndex>(x => x.WorkflowDefinitionVersionId == WorkflowDefinitionVersionId);
        }
    }

    public static class WorkflowDefinitionByVersionIdQueryWorkflowDefinitionManagerExtensions
    {
        public static IQuery<WorkflowDefinition> QueryByVersionId(this IWorkflowDefinitionManager manager,
            string workflowDefinitionVersionId) =>
            manager.ExecuteQuery(new WorkflowDefinitionByVersionIdQuery(workflowDefinitionVersionId));

        public static async Task<WorkflowDefinition?> GetByVersionIdAsync(
            this IWorkflowDefinitionManager manager,
            string workflowDefinitionVersionId,
            CancellationToken cancellationToken = default) =>
            await manager.QueryByVersionId(workflowDefinitionVersionId).FirstOrDefaultAsync();
    }
}