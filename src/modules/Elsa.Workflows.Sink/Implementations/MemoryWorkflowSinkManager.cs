using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Implementations;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Implementations;

public class MemoryWorkflowSinkManager : IWorkflowSinkManager
{
    private readonly MemoryStore<WorkflowSinkDto> _store;

    public MemoryWorkflowSinkManager(MemoryStore<WorkflowSinkDto> store)
    {
        _store = store;
    }
    
    public Task SaveAsync(WorkflowSinkDto dto, CancellationToken cancellationToken = default)
    {
        var existingEntity = _store.Find(e => e.Id == dto.Id);
        
        if (existingEntity?.LastExecutedAt == dto.LastExecutedAt) return Task.CompletedTask;
        
        dto.CreatedAt = existingEntity?.CreatedAt ?? dto.CreatedAt;

        _store.Save(dto, x => x.Id);
        return Task.CompletedTask;
    }
}