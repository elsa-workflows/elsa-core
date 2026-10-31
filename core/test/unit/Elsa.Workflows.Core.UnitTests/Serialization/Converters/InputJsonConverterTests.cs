using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public sealed class InputJsonConverterTests
{
    [Fact]
    public void When_DeserializeUnknownExpressionType_Then_ThrowsJsonException()
    {
        // Arrange
        var options = CreateOptions();
        const string json = """{"typeName":"System.Boolean","expression":{"type":"JavaScript","value":"getVariable('x') > 0"}}""";

        // Act
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Input<bool>>(json, options));

        // Assert
        Assert.Contains("JavaScript", exception.Message);
    }

    [Fact]
    public void When_SerializeUnknownExpressionType_Then_ThrowsJsonException()
    {
        // Arrange
        var options = CreateOptions();
        var input = new Input<bool>(new Expression("JavaScript", "getVariable('x') > 0"));

        // Act
        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(input, options));

        // Assert
        Assert.Contains("JavaScript", exception.Message);
    }

    [Fact]
    public void When_DeserializeKnownExpressionType_Then_ReturnsInput()
    {
        // Arrange
        var options = CreateOptions(CreateLiteralDescriptor());
        const string json = """{"typeName":"System.Boolean","expression":{"type":"Literal","value":true}}""";

        // Act
        var result = JsonSerializer.Deserialize<Input<bool>>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Expression);
        Assert.Equal("Literal", result.Expression.Type);
        Assert.Equal(true, result.Expression.Value);
    }

    [Fact]
    public void When_DeserializeInputObjectWithoutExpressionType_Then_ReturnsNull()
    {
        // Arrange
        var options = CreateOptions();
        const string json = """{"typeName":"System.Boolean"}""";

        // Act
        var result = JsonSerializer.Deserialize<Input<bool>>(json, options);

        // Assert
        Assert.Null(result);
    }

    private static JsonSerializerOptions CreateOptions(ExpressionDescriptor? descriptor = null)
    {
        var registry = Substitute.For<IExpressionDescriptorRegistry>();

        if (descriptor != null)
            registry.Find(descriptor.Type).Returns(descriptor);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(registry)
            .BuildServiceProvider();

        return new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new ExpressionJsonConverterFactory(registry),
                new InputJsonConverterFactory(serviceProvider)
            }
        };
    }

    private static ExpressionDescriptor CreateLiteralDescriptor() => new()
    {
        Type = "Literal"
    };
}
