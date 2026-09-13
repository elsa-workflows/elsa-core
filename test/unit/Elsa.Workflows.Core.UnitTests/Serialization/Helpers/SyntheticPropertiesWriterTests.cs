using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Helpers;

public sealed class SyntheticPropertiesWriterTests
{
    [Test]
    public async Task When_WriteConfiguredSyntheticOutput_Then_WritesConverterConfiguration()
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
        await Assert.That(syntheticOutput.GetProperty("typeName").GetString()).IsEqualTo("String");
        await Assert.That(syntheticOutput.GetProperty("memoryReference").GetProperty("id").GetString()).IsEqualTo("resultVariable");

        var converter = syntheticOutput.GetProperty("converter");
        await Assert.That(converter.GetProperty("id").GetString()).IsEqualTo("sample.to-text");
        await Assert.That(converter.GetProperty("settings").GetProperty("format").GetString()).IsEqualTo("compact");
        await Assert.That(converter.EnumerateObject().Select(x => x.Name))
            .IsEquivalentTo(new[] { "id", "settings" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task When_WriteUnconfiguredSyntheticOutput_Then_OmitsConverterProperty()
    {
        // Arrange
        var output = new Output<string>(new Variable("Result"));

        // Act
        using var document = WriteSyntheticOutput(output);

        // Assert
        var syntheticOutput = document.RootElement.GetProperty("result");
        await Assert.That(syntheticOutput.EnumerateObject().Select(x => x.Name))
            .IsEquivalentTo(new[] { "typeName", "memoryReference" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(syntheticOutput.GetProperty("typeName").GetString()).IsEqualTo("String");
        await Assert.That(syntheticOutput.GetProperty("memoryReference").GetProperty("id").GetString()).IsEqualTo("resultVariable");
    }

    [Test]
    public async Task When_RoundTripConfiguredSyntheticOutput_Then_PreservesConverterConfiguration()
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
        await Assert.That(result.HasExceptions).IsFalse();
        var roundTrippedOutput = (await Assert.That(result.Activity!.SyntheticProperties["Result"]).IsTypeOf<Output<string>>())!;
        var configuration = (await Assert.That(roundTrippedOutput.Converter).IsTypeOf<OutputConverterConfiguration>())!;
        await Assert.That(configuration.Id).IsEqualTo("sample.to-text");
        await Assert.That(configuration.Settings.HasValue).IsTrue();
        await Assert.That(configuration.Settings!.Value.GetProperty("format").GetString()).IsEqualTo("compact");
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
