using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class CombinedResult : ActivityExecutionResult
    {
        public CombinedResult(params IActivityExecutionResult[] results)
        {
            Results = results;
        }
        
        public CombinedResult(IEnumerable<IActivityExecutionResult> results)
        {
            Results = results.ToList();
        }
        
        public IReadOnlyCollection<IActivityExecutionResult> Results { get; }

        public override async Task ExecuteAsync(IWorkflowRunner runner, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            foreach (var result in Results)
            {
                await result.ExecuteAsync(runner, workflowContext, cancellationToken);
            }
        }
    }
}