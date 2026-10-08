using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public sealed class OutputJsonConverterTests
{
    [Fact]
    public void When_SerializeAndDeserializeConfiguredOutput_Then_ConverterConfigurationRoundTrips()
    {
        // Arrange
        var options = CreateOptions();
        var output = new Output<string>(new Variable("Result"))
        {
            Converter = new OutputConverterConfiguration("sample.to-text", CreateSettings())
        };

        // Act
        var json = JsonSerializer.Serialize(output, options);
        var result = JsonSerializer.Deserialize<Output<string>>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("resultVariable", result.MemoryBlockReference().Id);
        var configuration = Assert.IsType<OutputConverterConfiguration>(result.Converter);
        Assert.Equal("sample.to-text", configuration.Id);
        Assert.True(configuration.Settings.HasValue);
        Assert.Equal("compact", configuration.Settings.Value.GetProperty("format").GetString());

        using var document = JsonDocument.Parse(json);
        var converter = document.RootElement.GetProperty("converter");
        Assert.Equal(new[] { "id", "settings" }, converter.EnumerateObject().Select(x => x.Name));
    }

    [Fact]
    public void When_SerializeAndDeserializeUnconfiguredOutput_Then_ConverterPropertyIsOmitted()
    {
        // Arrange
        var options = CreateOptions();
        var output = new Output<string>(new Variable("Result"));

        // Act
        var json = JsonSerializer.Serialize(output, options);
        var result = JsonSerializer.Deserialize<Output<string>>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Converter);

        using var document = JsonDocument.Parse(json);
        Assert.Equal(new[] { "typeName", "memoryReference" }, document.RootElement.EnumerateObject().Select(x => x.Name));
        Assert.Equal("String", document.RootElement.GetProperty("typeName").GetString());
        Assert.Equal("resultVariable", document.RootElement.GetProperty("memoryReference").GetProperty("id").GetString());
    }

    private static JsonSerializerOptions CreateOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new OutputJsonConverter<string>(SerializationTypeRegistry.CreateDefault())
        }
    };

    private static JsonElement CreateSettings()
    {
        using var document = JsonDocument.Parse("""{"format":"compact"}""");
        return document.RootElement.Clone();
    }
}
