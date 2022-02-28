using System.Threading.Tasks;
using Elsa.DataMasking.Core.Models;

namespace Elsa.DataMasking.Core.Contracts;

/// <summary>
/// Implement this interface to filter or mask sensitive data that is about to be stored in the workflow journal. 
/// </summary>
public interface IWorkflowJournalFilter
{
    ValueTask ApplyAsync(WorkflowJournalFilterContext context);
}