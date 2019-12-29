using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityInvoker
    {
        Task<IActivityExecutionResult> ExecuteAsync(ProcessExecutionContext processContext, IActivity activity, Variable? input = null, CancellationToken cancellationToken = default);
        Task<IActivityExecutionResult> ResumeAsync(ProcessExecutionContext processContext, IActivity activity, Variable? input = null, CancellationToken cancellationToken = default);
    }
}