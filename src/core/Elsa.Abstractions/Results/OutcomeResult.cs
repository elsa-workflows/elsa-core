using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class OutcomeResult : IActivityExecutionResult
    {
        public OutcomeResult(IEnumerable<string> outcomes = default, Variable output = default)
        {
            Outcomes = outcomes?.ToArray() ?? new string[0];
            Output = output;
        }

        public IReadOnlyCollection<string> Outcomes { get; }
        public Variable? Output { get; }

        public Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}