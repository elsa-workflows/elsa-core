using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Serializers;

public sealed class JsonActivitySerializerTests : IAsyncLifetime
{
    private readonly ServiceProvider _services;

    public JsonActivitySerializerTests()
    {
        var services = new ServiceCollection();
        services.AddElsa();
        _services = services.BuildServiceProvider();
    }
    private IActivitySerializer Serializer => _services.GetRequiredService<IActivitySerializer>();

    public async Task InitializeAsync()
    {
        var registry = _services.GetRequiredService<IActivityRegistry>();
        await registry.RegisterAsync<Sequence>();
        await registry.RegisterAsync<WriteLine>();
        registry.Register(new ActivityDescriptor
        {
            TypeName = "Test.Generated",
            ClrType = typeof(GeneratedActivity),
            Version = 2,
            Constructor = context => context.CreateActivity<GeneratedActivity>(),
            Inputs = { new InputDescriptor { Name = "Text", Type = typeof(string), IsSynthetic = true } },
            Outputs = { new OutputDescriptor { Name = "Result", Type = typeof(string), IsSynthetic = true } }
        });
    }

    public Task DisposeAsync() => _services.DisposeAsync().AsTask();

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void RoundTripPreservesGeneratedInputsAndOutputs(bool objectOverload, bool nested)
    {
        var generated = new GeneratedActivity { Id = "generated", Type = "Test.Generated", Version = 2 };
        generated.SyntheticProperties["Text"] = new Input<string>("keep this value");
        generated.SyntheticProperties["Result"] = new Output<string>(new Variable("result"));
        IActivity root = nested ? new Sequence { Id = "root", Activities = [generated] } : generated;

        var json = objectOverload ? Serializer.Serialize((object)root) : Serializer.Serialize(root);
        using var document = JsonDocument.Parse(json);
        var node = nested ? document.RootElement.GetProperty("activities")[0] : document.RootElement;
        Assert.Equal("keep this value", node.GetProperty("text").GetProperty("expression").GetProperty("value").GetString());
        var restoredRoot = Serializer.Deserialize(json);
        var restored = Assert.IsType<GeneratedActivity>(nested ? Assert.IsType<Sequence>(restoredRoot).Activities.Single() : restoredRoot);
        Assert.Equal(generated.Id, restored.Id);
        Assert.Equal(generated.Type, restored.Type);
        Assert.Equal(generated.Version, restored.Version);
        Assert.IsType<Input<string>>(restored.SyntheticProperties["Text"]);
        var output = Assert.IsType<Output<string>>(restored.SyntheticProperties["Result"]);
        Assert.Equal("resultVariable", output.MemoryBlockReference().Id);
        using var roundTrip = JsonDocument.Parse(Serializer.Serialize(restored));
        Assert.Equal("keep this value", roundTrip.RootElement.GetProperty("text").GetProperty("expression").GetProperty("value").GetString());
    }

    [Fact]
    public void StaticActivityRoundTripPreservesInputAndIdentity()
    {
        var activity = new WriteLine("hello") { Id = "write" };
        var restored = Assert.IsType<WriteLine>(Serializer.Deserialize(Serializer.Serialize(activity)));
        Assert.Equal(activity.Id, restored.Id);
        Assert.Equal(activity.Type, restored.Type);
        using var document = JsonDocument.Parse(Serializer.Serialize(restored));
        Assert.Equal("hello", document.RootElement.GetProperty("text").GetProperty("expression").GetProperty("value").GetString());
    }

    [Fact]
    public void OrdinaryObjectsRetainTheirSerialization()
    {
        using var document = JsonDocument.Parse(Serializer.Serialize(new { Message = "hello", Count = 3 }));
        Assert.Equal("hello", document.RootElement.GetProperty("message").GetString());
        Assert.Equal(3, document.RootElement.GetProperty("count").GetInt32());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RootSerializationPreservesClrJsonMetadata(bool generated)
    {
        var activity = new GeneratedActivity { Id = "metadata", Type = generated ? "Test.Generated" : "Test.Unregistered", Version = 2 };
        if (generated)
        {
            activity.SyntheticProperties["Text"] = new Input<string>("synthetic");
        }
        using var document = JsonDocument.Parse(Serializer.Serialize(activity));
        Assert.Equal("named", document.RootElement.GetProperty("external_name").GetString());
        Assert.False(document.RootElement.TryGetProperty("alias", out _));
        Assert.False(document.RootElement.TryGetProperty("omitted", out _));
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("retainedNull").ValueKind);
        Assert.Equal(42, document.RootElement.GetProperty("number").GetInt32());
    }

    [Fact]
    public void DescriptorConverterDoesNotLeakIntoLaterDeserializationOrDuplicateSyntheticInputs()
    {
        var registry = _services.GetRequiredService<IActivityRegistry>();
        var descriptor = registry.Find("Test.Generated", 2)!;
        descriptor.ConfigureSerializerOptions = options =>
        {
            options.Converters.Insert(0, new JsonIgnoreCompositeRootConverterFactory(_services.GetRequiredService<ActivityWriter>()));
            return options;
        };
        var activity = new GeneratedActivity { Id = "configured", Type = descriptor.TypeName, Version = descriptor.Version };
        activity.SyntheticProperties["Text"] = new Input<string>("configured value");
        var serializer = Serializer;
        var json = serializer.Serialize(activity);
        using var document = JsonDocument.Parse(json);
        Assert.Single(document.RootElement.EnumerateObject(), property => property.Name == "text");
        var restored = Assert.IsType<GeneratedActivity>(serializer.Deserialize(json));
        Assert.IsType<Input<string>>(restored.SyntheticProperties["Text"]);
        Assert.IsType<WriteLine>(serializer.Deserialize("{\"type\":\"Elsa.WriteLine\",\"version\":1,\"id\":\"later\"}"));
    }

    [Fact]
    public void GeneratedRootHonorsConfiguredDepthDuringValidation()
    {
        var descriptor = _services.GetRequiredService<IActivityRegistry>().Find("Test.Generated", 2)!;
        descriptor.ConfigureSerializerOptions = options =>
        {
            options.MaxDepth = 128;
            return options;
        };
        descriptor.Inputs.Single().Type = typeof(object);
        object value = "deep value";
        for (var depth = 0; depth < 70; depth++)
        {
            value = new Dictionary<string, object> { ["child"] = value };
        }
        var activity = new GeneratedActivity { Type = descriptor.TypeName, Version = descriptor.Version };
        activity.SyntheticProperties["Text"] = new Input<object>(value);
        using var document = JsonDocument.Parse(Serializer.Serialize(activity), new JsonDocumentOptions { MaxDepth = 128 });
        Assert.True(document.RootElement.TryGetProperty("text", out _));
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("ID")]
    [InlineData("ExternalName")]
    [InlineData("result")]
    public void RootRejectsAmbiguousSyntheticPropertyNames(string name)
    {
        var descriptor = _services.GetRequiredService<IActivityRegistry>().Find("Test.Generated", 2)!;
        descriptor.Inputs.Add(new InputDescriptor { Name = name, Type = typeof(string), IsSynthetic = true });
        var activity = new GeneratedActivity { Id = "collision", Type = descriptor.TypeName, Version = descriptor.Version };
        activity.SyntheticProperties[name] = new Input<string>("collision");
        activity.SyntheticProperties["Result"] = new Output<string>(new Variable("result"));
        Assert.Throws<JsonException>(() => Serializer.Serialize(activity));
    }

    public sealed class GeneratedActivity : Activity
    {
        [JsonPropertyName("external_name")]
        public string Alias { get; set; } = "named";

        [JsonPropertyName("externalName")]
        public string CollisionAlias { get; set; } = "alias";

        public string? Omitted { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string? RetainedNull { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Number { get; set; } = 42;
    }
}
