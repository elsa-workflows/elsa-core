using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Helpers;

public sealed class SyntheticPropertiesWriterTests
{
    [Fact]
    public void When_WriteConfiguredSyntheticOutput_Then_WritesConverterConfiguration()
    {
        // Arrange
        var output = new Output<string>(new Variable("Result"))
        {
            Converter = new OutputConverterConfiguration("sample.to-text", CreateSettings())
        };

        // Act
        using var document = WriteSyntheticOutput(output);

        // Assert
        var syntheticOutput = document.RootElement.GetProperty("result");
        Assert.Equal("String", syntheticOutput.GetProperty("typeName").GetString());
        Assert.Equal("resultVariable", syntheticOutput.GetProperty("memoryReference").GetProperty("id").GetString());

        var converter = syntheticOutput.GetProperty("converter");
        Assert.Equal("sample.to-text", converter.GetProperty("id").GetString());
        Assert.Equal("compact", converter.GetProperty("settings").GetProperty("format").GetString());
        Assert.Equal(new[] { "id", "settings" }, converter.EnumerateObject().Select(x => x.Name));
    }

    [Fact]
    public void When_WriteUnconfiguredSyntheticOutput_Then_OmitsConverterProperty()
    {
        // Arrange
        var output = new Output<string>(new Variable("Result"));

        // Act
        using var document = WriteSyntheticOutput(output);

        // Assert
        var syntheticOutput = document.RootElement.GetProperty("result");
        Assert.Equal(new[] { "typeName", "memoryReference" }, syntheticOutput.EnumerateObject().Select(x => x.Name));
        Assert.Equal("String", syntheticOutput.GetProperty("typeName").GetString());
        Assert.Equal("resultVariable", syntheticOutput.GetProperty("memoryReference").GetProperty("id").GetString());
    }

    [Fact]
    public void When_RoundTripConfiguredSyntheticOutput_Then_PreservesConverterConfiguration()
    {
        // Arrange
        var output = new Output<string>(new Variable("Result"))
        {
            Converter = new OutputConverterConfiguration("sample.to-text", CreateSettings())
        };
        using var document = WriteSyntheticOutput(output);

        // Act
        var result = JsonActivityConstructorContextHelper.CreateActivity<TestActivity>(
            CreateActivityDescriptor(),
            document.RootElement,
            CreateOptions());

        // Assert
        Assert.False(result.HasExceptions);
        var roundTrippedOutput = Assert.IsType<Output<string>>(result.Activity.SyntheticProperties["Result"]);
        var configuration = Assert.IsType<OutputConverterConfiguration>(roundTrippedOutput.Converter);
        Assert.Equal("sample.to-text", configuration.Id);
        Assert.True(configuration.Settings.HasValue);
        Assert.Equal("compact", configuration.Settings.Value.GetProperty("format").GetString());
    }

    private static JsonDocument WriteSyntheticOutput(Output<string> output)
    {
        var activity = new TestActivity();
        activity.SyntheticProperties["Result"] = output;

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            var sut = new SyntheticPropertiesWriter(Substitute.For<IExpressionDescriptorRegistry>());
            sut.WriteSyntheticProperties(writer, activity, CreateActivityDescriptor(), CreateOptions());
            writer.WriteEndObject();
        }

        return JsonDocument.Parse(buffer.ToArray());
    }

    private static ActivityDescriptor CreateActivityDescriptor() => new()
    {
        Outputs =
        {
            new OutputDescriptor
            {
                Name = "Result",
                Type = typeof(string),
                IsSynthetic = true
            }
        }
    };

    private static JsonSerializerOptions CreateOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new TypeJsonConverter(SerializationTypeRegistry.CreateDefault())
        }
    };

    private static JsonElement CreateSettings()
    {
        using var document = JsonDocument.Parse("""{"format":"compact"}""");
        return document.RootElement.Clone();
    }

    private sealed class TestActivity : Activity
    {
    }
}
