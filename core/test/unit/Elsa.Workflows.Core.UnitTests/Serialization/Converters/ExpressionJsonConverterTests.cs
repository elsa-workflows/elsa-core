using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Serialization.Converters;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public sealed class ExpressionJsonConverterTests
{
    [Fact]
    public void When_DeserializeUnknownExpressionType_Then_ThrowsJsonException()
    {
        // Arrange
        var options = CreateOptions();
        const string json = """{"type":"JavaScript","value":"getVariable('x') > 0"}""";

        // Act
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Expression>(json, options));

        // Assert
        Assert.Contains("JavaScript", exception.Message);
    }

    [Fact]
    public void When_SerializeUnknownExpressionType_Then_ThrowsJsonException()
    {
        // Arrange
        var options = CreateOptions();
        var expression = new Expression("JavaScript", "getVariable('x') > 0");

        // Act
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(expression, options));

        // Assert
        Assert.Contains("JavaScript", exception.Message);
    }

    [Fact]
    public void When_DeserializeKnownExpressionType_Then_ReturnsExpression()
    {
        // Arrange
        var options = CreateOptions(CreateLiteralDescriptor());
        const string json = """{"type":"Literal","value":"hello"}""";

        // Act
        var result = JsonSerializer.Deserialize<Expression>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Literal", result.Type);
        Assert.Equal("hello", result.Value);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"type\":\"\"}")]
    public void When_DeserializeExpressionWithoutOrWithEmptyType_Then_ReturnsNull(string json)
    {
        // Arrange
        var options = CreateOptions();

        // Act
        var result = JsonSerializer.Deserialize<Expression>(json, options);

        // Assert
        Assert.Null(result);
    }

    private static JsonSerializerOptions CreateOptions(ExpressionDescriptor? descriptor = null)
    {
        var registry = Substitute.For<IExpressionDescriptorRegistry>();

        if (descriptor != null)
            registry.Find(descriptor.Type).Returns(descriptor);

        return new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new ExpressionJsonConverterFactory(registry)
            }
        };
    }

    private static ExpressionDescriptor CreateLiteralDescriptor() => new()
    {
        Type = "Literal"
    };
}
