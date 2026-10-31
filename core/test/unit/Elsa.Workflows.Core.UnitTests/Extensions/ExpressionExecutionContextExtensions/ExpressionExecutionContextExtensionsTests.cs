using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common;
using Elsa.Expressions.JavaScript.Activities;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests;

public class ExpressionExecutionContextExtensionsTests
{
    [Fact]
    public void GetVariable_ReturnsVariable_WhenVariableExists()
    {
        // Arrange
        var variable = new Variable("test", 5);
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>
        {
            { variable.Id, new(variable.Value, new VariableBlockMetadata(variable, typeof(object), true)) }
        });

        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        var result = context.GetVariable<int>("test");

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void GetVariable_ReturnsNull_WhenVariableDoesNotExist()
    {
        // Arrange
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>());
        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        var result = context.GetVariable<string>("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateVariable_ThrowsException_WhenVariableExists()
    {
        // Arrange
        var variable = new Variable("test", 5);
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>
        {
            { variable.Id, new(variable.Value, new VariableBlockMetadata(variable, typeof(object), true)) }
        });

        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act & Assert
        Assert.Throws<Exception>(() => context.CreateVariable("test", 10));
    }

    [Fact]
    public void CreateVariable_CreatesVariable_WhenVariableDoesNotExist()
    {
        // Arrange
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>());
        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        context.CreateVariable("newVariable", 10);

        // Assert
        var variable = context.GetVariable<int>("newVariable");
        Assert.Equal(10, variable);
    }

    [Fact]
    public void SetVariable_CreatesVariable_WhenVariableDoesNotExist()
    {
        // Arrange
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>());
        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        context.SetVariable("newVariable", 10);

        // Assert
        var variable = context.GetVariable<int>("newVariable");
        Assert.Equal(10, variable);
    }

    [Fact]
    public void SetVariable_SetsValue_WhenVariableExists()
    {
        // Arrange
        var variable = new Variable("test", 5);
        var memoryRegister = new MemoryRegister(new Dictionary<string, MemoryBlock>
        {
            { variable.Id, new(variable.Value, new VariableBlockMetadata(variable, typeof(object), true)) }
        });

        var context = new ExpressionExecutionContext(null!, memoryRegister);

        // Act
        context.SetVariable("test", 10);

        // Assert
        var updatedVariable = context.GetVariable<int>("test");
        Assert.Equal(10, updatedVariable);
    }

    [Fact]
    public async Task GetInput_UsesSecondHostConverters_AfterFirstHostAlreadyConverted()
    {
        // Arrange
        var firstContext = await CreateContextWithSerializerAsync("first");
        firstContext.WorkflowExecutionContext.Input["payload"] = """{"token":"x"}""";

        // Act
        var first = firstContext.ExpressionExecutionContext.GetInput<TaggedValue>("payload");

        var secondContext = await CreateContextWithSerializerAsync("second");
        secondContext.WorkflowExecutionContext.Input["payload"] = """{"token":"x"}""";
        var second = secondContext.ExpressionExecutionContext.GetInput<TaggedValue>("payload");

        // Assert
        Assert.Equal("first", first?.Tag);
        Assert.Equal("second", second?.Tag);
    }

    [Fact]
    public async Task GetInput_ResolvesSerializerOptionsFromCurrentHostOnEachCall()
    {
        // Arrange
        var serializer = Substitute.For<IJsonSerializer>();
        serializer.GetOptions().Returns(CreateTaggedOptions("first"), CreateTaggedOptions("second"));

        var context = await CreateContextAsync(serializer);
        context.WorkflowExecutionContext.Input["payload"] = """{"token":"x"}""";

        // Act
        var first = context.ExpressionExecutionContext.GetInput<TaggedValue>("payload");
        var second = context.ExpressionExecutionContext.GetInput<TaggedValue>("payload");

        // Assert
        Assert.Equal("first", first?.Tag);
        Assert.Equal("second", second?.Tag);
    }

    private static Task<ActivityExecutionContext> CreateContextWithSerializerAsync(string converterTag) =>
        CreateContextAsync(new StubJsonSerializer(CreateTaggedOptions(converterTag)));

    private static Task<ActivityExecutionContext> CreateContextAsync(IJsonSerializer serializer)
    {
        var fixture = new ActivityTestFixture(new WriteLine("test"))
            .ConfigureServices(services => services.AddSingleton(serializer));
        return fixture.BuildAsync();
    }

    private static JsonSerializerOptions CreateTaggedOptions(string tag)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TaggedValueConverter(tag));
        return options;
    }

    private sealed class TaggedValue
    {
        public string Tag { get; set; } = "";
    }

    private sealed class TaggedValueConverter(string tag) : JsonConverter<TaggedValue>
    {
        public override TaggedValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var _ = JsonDocument.ParseValue(ref reader);
            return new TaggedValue { Tag = tag };
        }

        public override void Write(Utf8JsonWriter writer, TaggedValue value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Tag);
    }

    private sealed class StubJsonSerializer(JsonSerializerOptions options) : IJsonSerializer
    {
        public JsonSerializerOptions GetOptions() => options;
        public void ApplyOptions(JsonSerializerOptions _) { }
        public string Serialize(object value) => throw new NotSupportedException();
        public string Serialize(object value, Type type) => throw new NotSupportedException();
        public string Serialize<T>(T value) => throw new NotSupportedException();
        public object Deserialize(string json) => throw new NotSupportedException();
        public object Deserialize(string json, Type type) => throw new NotSupportedException();
        public T Deserialize<T>(string json) => throw new NotSupportedException();
    }
}
