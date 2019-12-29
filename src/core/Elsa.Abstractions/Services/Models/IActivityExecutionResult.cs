using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Models
{
    public interface IActivityExecutionResult
    {
        Task ExecuteAsync(IProcessRunner runner, ProcessExecutionContext processContext, CancellationToken cancellationToken);
    }
}
