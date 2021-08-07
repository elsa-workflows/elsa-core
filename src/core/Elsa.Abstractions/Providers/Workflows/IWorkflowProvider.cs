using System.Collections.Generic;
using System.Threading;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Providers.Workflows
{
    /// <summary>
    /// Represents a source of workflows for the <see cref="IWorkflowRegistry"/>
    /// </summary>
    public interface IWorkflowProvider
    {
        IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowsAsync(CancellationToken cancellationToken);
    }
}