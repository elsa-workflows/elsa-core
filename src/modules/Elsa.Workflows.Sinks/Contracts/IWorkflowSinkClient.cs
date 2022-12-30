using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Sinks.Models;

namespace Elsa.Workflows.Sinks.Contracts;

public interface IWorkflowSinkClient
{
    Task SaveAsync(WorkflowInstanceDto dto, CancellationToken cancellationToken = default);
}