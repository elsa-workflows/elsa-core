using Elsa.Testing.Framework.Abstractions;

namespace Elsa.Testing.Framework.Assertions;

public class ActivityOutputAssertion : Assertion
{
    public string ActivityId { get; set; } = null!;
    public string OutputName { get; set; } = null!;
    public object ExpectedValue { get; set; } = null!;
}