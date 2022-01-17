using System.Collections.Generic;
using System.Threading;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Providers.Workflows;

/// <summary>
/// Represents the programmatic source of workflows <see cref="IWorkflowRegistry"/>
/// </summary>
public interface IProgrammaticWorkflowProvider
{
    IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowsAsync(CancellationToken cancellationToken);
}