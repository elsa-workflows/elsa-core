using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Implementations;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Implementations;

public class MemoryWorkflowSink : IMemoryWorkflowSink
{
    private readonly MemoryStore<WorkflowSinkDto> _store;

    public MemoryWorkflowSink(MemoryStore<WorkflowSinkDto> store)
    {
        _store = store;
    }
    
    public Task SaveAsync(WorkflowSinkDto record, CancellationToken cancellationToken = default)
    {
        AdjustWithExistingEntity(record);
        
        _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }
    
    private void AdjustWithExistingEntity(WorkflowSinkDto record)
    {
        var existingEntity = _store.Find(e => e.Id == record.Id);
        record.CreatedAt = existingEntity?.CreatedAt ?? record.CreatedAt;
    }
}