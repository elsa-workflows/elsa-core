using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Implement this interface or <see cref="WorkflowBase"/> when implementing workflows using code so that they become available to the system.
/// </summary>
public interface IWorkflow
{
    ValueTask BuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default);
}