using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Contracts;

public interface IPrepareWorkflowSinkModel
{
    public Task<WorkflowSinkDto> ExecuteAsync(string definitionId, int definitionVersion, string stateId, CancellationToken cancellationToken);
}