using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Sinks.Models;

namespace Elsa.Workflows.Sinks.Contracts;

public interface IPrepareWorkflowSinkModel
{
    public Task<WorkflowInstanceDto> ExecuteAsync(WorkflowState state, CancellationToken cancellationToken);
}