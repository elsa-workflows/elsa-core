using Elsa.Expressions.JavaScript.Helpers;

namespace Elsa.JavaScript.IntegrationTests;

public class VariableNameValidatorTests
{
    [Theory(DisplayName = "The obsolete HasVariableAccessors still accepts only letters-and-digits names")]
    [InlineData("fooBar", true)]
    [InlineData("1234", true)]
    [InlineData("foo_bar", false)]
    [InlineData("$amount", false)]
    [InlineData("", false)]
    public void HasVariableAccessorsKeepsItsBehavior(string name, bool expected)
    {
#pragma warning disable CS0618 // Covers the deprecated API until it is removed.
        Assert.Equal(expected, VariableNameValidator.HasVariableAccessors(name));
#pragma warning restore CS0618
    }
}
