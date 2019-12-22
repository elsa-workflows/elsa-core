using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Represents a source of workflow definitions for <see cref="IWorkflowRegistry"/>
    /// </summary>
    public interface IWorkflowProvider
    {
        Task<IEnumerable<WorkflowBlueprint>> GetWorkflowDefinitionsAsync(CancellationToken cancellationToken);
    }
}