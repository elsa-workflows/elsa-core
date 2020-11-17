using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Signaling.Services
{
    public interface ISignaler
    {
        Task SendSignalAsync(string signal, object? input = default, string? correlationId = default, CancellationToken cancellationToken = default);
    }
}