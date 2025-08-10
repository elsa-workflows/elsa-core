using Elsa.Testing.Framework.Abstractions;

namespace Elsa.Testing.Framework.Assertions;

public class WorkflowOutputAssertion : Assertion
{
    public string OutputName { get; set; } = null!;
    public object ExpectedValue { get; set; } = null!;
}