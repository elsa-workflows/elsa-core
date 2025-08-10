using Elsa.Testing.Framework.Abstractions;
using Elsa.Testing.Framework.Models;

namespace Elsa.Testing.Framework.Assertions;

public class ActivityOutcomeAssertion : Assertion
{
    public string ActivityId { get; set; } = null!;
    public bool ExpectedOutcome { get; set; }
    public override Task<AssertionResult> RunAsync(AssertionContext context)
    {
        throw new NotImplementedException();
    }
}