using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class CompleteWorkflowResult : ActivityExecutionResult
    {
        protected override void Execute(IProcessRunner runner, ProcessExecutionContext processContext)
        {
            processContext.Complete();
        }
    }
}