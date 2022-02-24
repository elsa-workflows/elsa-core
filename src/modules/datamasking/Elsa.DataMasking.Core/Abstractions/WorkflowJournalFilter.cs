using System.Threading.Tasks;
using Elsa.DataMasking.Core.Contracts;
using Elsa.DataMasking.Core.Models;

namespace Elsa.DataMasking.Core.Abstractions;

/// <summary>
/// A base class for workflow journal filters.
/// </summary>
public abstract class WorkflowJournalFilter : IWorkflowJournalFilter
{
    async ValueTask IWorkflowJournalFilter.ApplyAsync(WorkflowJournalFilterContext context) => await ApplyAsync(context);

    protected virtual Task ApplyAsync(WorkflowJournalFilterContext context)
    {
        Apply(context);
        return Task.CompletedTask;
    }

    protected virtual void Apply(WorkflowJournalFilterContext context)
    {
    }
}