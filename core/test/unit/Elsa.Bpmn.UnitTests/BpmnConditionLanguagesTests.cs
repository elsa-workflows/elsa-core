using Elsa.Bpmn.Hosting;

namespace Elsa.Bpmn.UnitTests;

public class BpmnConditionLanguagesTests
{
    [Theory(DisplayName = "A supported language resolves to its Elsa expression type, by name or by a URI's last path segment")]
    [InlineData(null, "JavaScript")]
    [InlineData("", "JavaScript")]
    [InlineData("  ", "JavaScript")]
    [InlineData("javascript", "JavaScript")]
    [InlineData("JAVASCRIPT", "JavaScript")]
    [InlineData(" csharp ", "CSharp")]
    [InlineData("Liquid", "Liquid")]
    [InlineData("https://elsaworkflows.io/expressions/javascript", "JavaScript")]
    [InlineData("https://elsaworkflows.io/expressions/CSharp/", "CSharp")]
    public void ResolvesSupportedLanguages(string? language, string expectedExpressionType)
    {
        Assert.True(BpmnConditionLanguages.TryResolveExpressionType(language, out var expressionType));
        Assert.Equal(expectedExpressionType, expressionType);
    }

    [Theory(DisplayName = "Any other language is refused, never mapped onto a supported one")]
    [InlineData("http://www.w3.org/1999/XPath")]
    [InlineData("juel")]
    [InlineData("feel")]
    [InlineData("https://elsaworkflows.io/")]
    [InlineData("/javascript")]
    [InlineData("java script")]
    public void RefusesOtherLanguages(string language) =>
        Assert.False(BpmnConditionLanguages.TryResolveExpressionType(language, out _));
}
