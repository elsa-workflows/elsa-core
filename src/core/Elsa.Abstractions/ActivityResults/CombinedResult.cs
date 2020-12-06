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

        public CombinedResult(params IActivityExecutionResult[] results) : this(results.AsEnumerable())
        {
        }

        public IReadOnlyCollection<IActivityExecutionResult> Results { get; }

        public override async ValueTask ExecuteAsync(
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken)
        {
            foreach (var result in Results)
                await result.ExecuteAsync(activityExecutionContext, cancellationToken);
        }
    }
}