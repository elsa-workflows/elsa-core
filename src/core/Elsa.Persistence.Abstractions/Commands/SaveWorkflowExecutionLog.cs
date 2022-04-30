using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Commands;

public record SaveWorkflowExecutionLog : ICommand
{
    public SaveWorkflowExecutionLog(IEnumerable<WorkflowExecutionLogRecord> records) => Records = records.ToList();
    public IReadOnlyCollection<WorkflowExecutionLogRecord> Records { get; }
}