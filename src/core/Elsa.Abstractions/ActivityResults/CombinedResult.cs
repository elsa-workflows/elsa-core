using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class CombinedResult : ActivityExecutionResult
    {
        public CombinedResult(IEnumerable<IActivityExecutionResult> results)
        {
            Results = results.ToList();
        }

        public IReadOnlyCollection<IActivityExecutionResult> Results { get; }

        public override async Task ExecuteAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            foreach (var result in Results) 
                await result.ExecuteAsync(activityExecutionContext, cancellationToken);
        }
    }
}