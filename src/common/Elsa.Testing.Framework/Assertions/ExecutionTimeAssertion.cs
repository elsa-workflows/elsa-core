using Elsa.Testing.Framework.Abstractions;

namespace Elsa.Testing.Framework.Assertions;

public class ExecutionTimeAssertion : Assertion
{
    public string ActivityId { get; set; } = null!;
    public TimeSpan MaxExecutionTime { get; set; }
}