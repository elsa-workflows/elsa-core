using Elsa.Resilience.Models;
using Elsa.Workflows;

namespace Elsa.Resilience;

public interface IResilientActivity : IActivity
{
    IDictionary<string, string?> CollectRetryDetails(ActivityExecutionContext context, RetryAttempt attempt);
}