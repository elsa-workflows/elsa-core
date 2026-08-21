using Elsa.Resilience.Models;
using Elsa.Workflows;

namespace Elsa.Resilience.Core.UnitTests.TestHelpers;

/// <summary>
/// A resilient activity that reports one detail per retry attempt, plus one detail that is always null so that
/// tests can assert null details are dropped rather than recorded.
/// </summary>
internal class TestResilientActivity : CodeActivity, IResilientActivity
{
    public IDictionary<string, string?> CollectRetryDetails(ActivityExecutionContext context, RetryAttempt attempt) => new Dictionary<string, string?>
    {
        ["exception"] = attempt.Exception?.Message,
        ["never-recorded"] = null
    };

    protected override void Execute(ActivityExecutionContext context)
    {
    }
}
