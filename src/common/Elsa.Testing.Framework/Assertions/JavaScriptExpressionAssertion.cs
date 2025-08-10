using Elsa.Testing.Framework.Abstractions;

namespace Elsa.Testing.Framework.Assertions;

public class JavaScriptExpressionAssertion : Assertion
{
    public string ActivityId { get; set; } = null!;
    public string Expression { get; set; } = null!;
    public object ExpectedValue { get; set; } = null!;
}