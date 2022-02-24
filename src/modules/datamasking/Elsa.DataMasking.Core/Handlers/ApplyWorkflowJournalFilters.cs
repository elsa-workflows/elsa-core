using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.DataMasking.Core.Contracts;
using Elsa.DataMasking.Core.Models;
using Elsa.Events;
using MediatR;

namespace Elsa.DataMasking.Core.Handlers;

public class ApplyWorkflowJournalFilters : INotificationHandler<SavingWorkflowExecutionLog>
{
    private readonly IEnumerable<IWorkflowJournalFilter> _filters;

    public ApplyWorkflowJournalFilters(IEnumerable<IWorkflowJournalFilter> filters)
    {
        _filters = filters;
    }
    
    public async Task Handle(SavingWorkflowExecutionLog notification, CancellationToken cancellationToken)
    {
        var records = notification.Records;

        foreach (var filter in _filters)
        {
            foreach (var executionLogRecord in records)
            {
                var context = new WorkflowJournalFilterContext(executionLogRecord);
                await filter.ApplyAsync(context);
            }    
        }
        
    }
}