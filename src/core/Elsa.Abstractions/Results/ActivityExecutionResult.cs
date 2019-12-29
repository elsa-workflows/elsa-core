using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public abstract class ActivityExecutionResult : IActivityExecutionResult
    {
        public virtual Task ExecuteAsync(IProcessRunner runner, ProcessExecutionContext processContext, CancellationToken cancellationToken)
        {
            Execute(runner, processContext);
            return Task.CompletedTask;
        }

        protected virtual void Execute(IProcessRunner runner, ProcessExecutionContext processContext)
        {
        }
    }
}
