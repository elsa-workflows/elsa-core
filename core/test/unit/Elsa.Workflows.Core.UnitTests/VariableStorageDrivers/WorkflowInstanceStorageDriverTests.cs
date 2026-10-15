using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Common.Serialization;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Serialization.Converters;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.VariableStorageDrivers;

[Collection(nameof(WorkflowInstanceStorageDriverTestsCollection))]
public class WorkflowInstanceStorageDriverTests
{
    [Fact]
    public async Task WriteAsync_WhenSerializeFails_PreservesPreviousValue()
    {
        var harness = CreateHarness(new Variable<string>("name", "kept"));
        const string id = "nameVariable";

        await harness.Driver.WriteAsync(id, "kept", harness.Context);

        var storedBefore = GetVariables(harness.Properties)[id].ToJsonString();

        await harness.Driver.WriteAsync(id, CreateUnserializableValue(), harness.Context);

        var dictionary = GetVariables(harness.Properties);
        Assert.True(dictionary.ContainsKey(id));
        Assert.Equal(storedBefore, dictionary[id].ToJsonString());

        var read = await harness.Driver.ReadAsync(id, harness.Context);
        Assert.Equal("kept", read);
    }

    [Fact]
    public async Task WriteAsync_WhenSerializeFailsWithNoPriorValue_DoesNotCreateEntry()
    {
        var harness = CreateHarness(new Variable<string>("name", ""));
        const string id = "nameVariable";

        await harness.Driver.WriteAsync(id, CreateUnserializableValue(), harness.Context);

        Assert.False(GetVariables(harness.Properties).ContainsKey(id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesStoredValue()
    {
        var harness = CreateHarness(new Variable<string>("name", "kept"));
        const string id = "nameVariable";

        await harness.Driver.WriteAsync(id, "kept", harness.Context);
        await harness.Driver.DeleteAsync(id, harness.Context);

        Assert.False(GetVariables(harness.Properties).ContainsKey(id));
    }

    [Fact]
    public async Task WriteThenRead_WhenValueHasOrdinaryDollarRefProperty_RoundTrips()
    {
        // Arrange
        var harness = CreateHarness(new Variable<RefPayload>("payload", new()));
        const string id = "payloadVariable";
        var value = new RefPayload { Ref = "ordinary" };

        // Act
        await harness.Driver.WriteAsync(id, value, harness.Context);
        var read = await harness.Driver.ReadAsync(id, harness.Context);

        // Assert
        var restored = Assert.IsType<RefPayload>(read);
        Assert.Equal("ordinary", restored.Ref);
    }

    [Fact]
    public async Task WriteThenRead_WhenObjectVariableHoldsAliasedType_RestoresConcreteClrType()
    {
        // Arrange
        var registry = SerializationTypeRegistry.CreateDefault();
        registry.RegisterType(typeof(AliasedPerson), nameof(AliasedPerson));
        var harness = CreateHarness(new Variable<object>("payload", new()), registry);
        const string id = "payloadVariable";
        var value = new AliasedPerson { Name = "Ada" };

        // Act
        await harness.Driver.WriteAsync(id, value, harness.Context);
        var stored = GetVariables(harness.Properties)[id].AsObject();
        var read = await harness.Driver.ReadAsync(id, harness.Context);

        // Assert
        Assert.Equal(nameof(AliasedPerson), stored["_type"]?.GetValue<string>());
        var restored = Assert.IsType<AliasedPerson>(read);
        Assert.Equal("Ada", restored.Name);
    }

    [Fact]
    public async Task WriteAsync_WhenValueIsArray_StoresJsonArrayReadableWithDefaultConverter()
    {
        // Arrange
        var harness = CreateHarness(new Variable<string[]>("elements", []));
        const string id = "elementsVariable";

        // Act
        await harness.Driver.WriteAsync(id, new[] { "Element 1", "Element 2" }, harness.Context);
        var node = GetVariables(harness.Properties)[id];
        var actual = node.ConvertTo<string[]>();

        // Assert
        Assert.Equal(JsonValueKind.Array, node.GetValueKind());
        Assert.NotNull(actual);
        Assert.Equal(["Element 1", "Element 2"], actual);
    }

    [Fact]
    public async Task WriteAsync_WhenValueIsProjectedEnumerable_StoresJsonArrayReadableWithDefaultConverter()
    {
        // Arrange
        var harness = CreateHarness(new Variable<string[]>("messages", []));
        const string id = "messagesVariable";
        var value = new[] { "a", "b", "c" }.Select(x => x.ToUpperInvariant());

        // Act
        await harness.Driver.WriteAsync(id, value, harness.Context);
        var node = GetVariables(harness.Properties)[id];
        var actual = node.ConvertTo<string[]>();

        // Assert
        Assert.Equal(JsonValueKind.Array, node.GetValueKind());
        Assert.NotNull(actual);
        Assert.Equal(["A", "B", "C"], actual);
    }

    [Fact]
    public async Task ReadAsync_WhenConvertFails_DoesNotReturnUntypedJsonNode()
    {
        var harness = CreateHarness(new Variable<int>("count", 0));
        const string id = "countVariable";
        SeedIncompatibleNode(harness.Properties, id);

        var read = await harness.Driver.ReadAsync(id, harness.Context);

        Assert.Null(read);
        Assert.False(read is JsonNode);
        Assert.True(GetVariables(harness.Properties).ContainsKey(id));
    }

    [Fact]
    public async Task ReadAsync_WhenConvertFailsAndStrictMode_Throws()
    {
        var harness = CreateHarness(new Variable<int>("count", 0));
        const string id = "countVariable";
        SeedIncompatibleNode(harness.Properties, id);

        var originalStrictMode = ObjectConverter.StrictMode;
        try
        {
            ObjectConverter.StrictMode = true;

            await Assert.ThrowsAnyAsync<Exception>(() => harness.Driver.ReadAsync(id, harness.Context).AsTask());

            Assert.True(GetVariables(harness.Properties).ContainsKey(id));
        }
        finally
        {
            ObjectConverter.StrictMode = originalStrictMode;
        }
    }

    private static Harness CreateHarness(Variable variable, ISerializationTypeRegistry? typeRegistry = null)
    {
        var properties = new Dictionary<string, object>();
        var executionContext = Substitute.For<IExecutionContext>();
        executionContext.Properties.Returns(properties);

        var payloadSerializer = Substitute.For<IPayloadSerializer>();
        payloadSerializer.GetOptions().Returns(CreatePayloadSerializerOptions(typeRegistry));

        var driver = new WorkflowInstanceStorageDriver(payloadSerializer, NullLogger<WorkflowInstanceStorageDriver>.Instance);
        var context = new StorageDriverContext(executionContext, variable, CancellationToken.None);

        return new(driver, context, properties);
    }

    private static JsonSerializerOptions CreatePayloadSerializerOptions(ISerializationTypeRegistry? typeRegistry = null)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(typeRegistry is null
            ? new PolymorphicObjectConverterFactory()
            : new PolymorphicObjectConverterFactory(typeRegistry));
        return options;
    }

    private static VariablesDictionary GetVariables(IDictionary<string, object> properties) =>
        (VariablesDictionary)properties[WorkflowInstanceStorageDriver.VariablesDictionaryStateKey];

    private static void SeedIncompatibleNode(IDictionary<string, object> properties, string id)
    {
        properties[WorkflowInstanceStorageDriver.VariablesDictionaryStateKey] = new VariablesDictionary
        {
            [id] = JsonNode.Parse("""{"foo":"bar"}""")!
        };
    }

    private static CyclicValue CreateUnserializableValue()
    {
        var value = new CyclicValue();
        value.Self = value;
        return value;
    }

    private sealed record Harness(
        WorkflowInstanceStorageDriver Driver,
        StorageDriverContext Context,
        IDictionary<string, object> Properties);

    private sealed class CyclicValue
    {
        public CyclicValue Self { get; set; } = null!;
    }

    private sealed class RefPayload
    {
        [JsonPropertyName("$ref")]
        public string Ref { get; set; } = "";
    }

    private sealed class AliasedPerson
    {
        public string Name { get; set; } = "";
    }
}

[CollectionDefinition(nameof(WorkflowInstanceStorageDriverTestsCollection), DisableParallelization = true)]
public sealed class WorkflowInstanceStorageDriverTestsCollection;
