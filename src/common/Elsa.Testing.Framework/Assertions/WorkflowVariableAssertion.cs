using Elsa.Testing.Framework.Abstractions;

namespace Elsa.Testing.Framework.Assertions;

public class WorkflowVariableAssertion : Assertion
{
    public string VariableName { get; set; } = null!;
    public object ExpectedValue { get; set; } = null!;
}