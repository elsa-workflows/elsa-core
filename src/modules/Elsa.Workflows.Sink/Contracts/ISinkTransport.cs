using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Contracts;

public interface ISinkTransport
{
    public Task SendAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken);
}