using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Contracts;

public interface IWorkflowSinkClient
{
    Task SaveAsync(WorkflowSinkDto dto, CancellationToken cancellationToken = default);
}