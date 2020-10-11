using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Represents a source of workflows for the <see cref="IWorkflowRegistry"/>
    /// </summary>
    public interface IWorkflowProvider
    {
        Task<IEnumerable<WorkflowBlueprint>> GetWorkflowsAsync(CancellationToken cancellationToken);
    }
}