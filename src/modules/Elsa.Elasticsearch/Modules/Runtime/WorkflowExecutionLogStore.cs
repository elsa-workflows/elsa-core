using Elsa.Common.Models;
using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Elasticsearch.Modules.Runtime;

/// <summary>
/// Store and retrieves <see cref="WorkflowExecutionLogRecord"/> objects from an Elasticsearch.
/// </summary>
public class ElasticWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly ElasticStore<WorkflowExecutionLogRecord> _store;
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public ElasticWorkflowExecutionLogStore(ElasticStore<WorkflowExecutionLogRecord> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => 
        await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default) => 
        await _store.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, PageArgs? pageArgs = default, CancellationToken cancellationToken = default) =>
        await _store
            .SearchAsync(desc => desc
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.WorkflowInstanceId)
                        .Query(workflowInstanceId))), pageArgs, cancellationToken);
}