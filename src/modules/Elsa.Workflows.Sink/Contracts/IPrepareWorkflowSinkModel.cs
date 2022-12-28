using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Contracts;

public interface IPrepareWorkflowSinkModel
{
    public Task<WorkflowSinkDto> ExecuteAsync(WorkflowState state, CancellationToken cancellationToken);
}