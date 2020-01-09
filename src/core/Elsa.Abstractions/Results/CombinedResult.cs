using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class CombinedResult : ActivityExecutionResult
    {
        public CombinedResult(IEnumerable<IActivityExecutionResult> results)
        {
            Results = results.ToList();
        }

        public IReadOnlyCollection<IActivityExecutionResult> Results { get; }

        public override async Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            foreach (var result in Results) 
                await result.ExecuteAsync(workflowExecutionContext, activityExecutionContext, cancellationToken);
        }
    }
}