using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Sinks.Models;

namespace Elsa.Workflows.Sinks.Contracts;

public interface ISinkTransport
{
    public Task SendAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken);
}