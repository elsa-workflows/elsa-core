using System.Dynamic;
using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Serialization.Serializers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.UnitTests.Extensions;

public class StoredTriggerExtensionsTests
{
    [Fact]
    public void GetPayload_ReturnsTypedPayload_AfterRoundTripThroughPayloadSerializer()
    {
        // Same round trip a persistent trigger store does: serialize on save, deserialize as object on load.
        var serializer = CreatePayloadSerializer();
        var json = serializer.Serialize(new TestPayload("orders", TestKind.Topic));
        var reloaded = new StoredTrigger { Payload = serializer.Deserialize(json) };

        var payload = reloaded.GetPayload<TestPayload>();

        Assert.Equal(new TestPayload("orders", TestKind.Topic), payload);
    }

    [Fact]
    public void GetPayload_ConvertsExpandoObjectPayload()
    {
        dynamic expando = new ExpandoObject();
        expando.name = "orders";
        expando.kind = "Topic";
        var trigger = new StoredTrigger { Payload = (object)expando };

        Assert.Equal(new TestPayload("orders", TestKind.Topic), trigger.GetPayload<TestPayload>());
    }

    [Fact]
    public void GetPayload_ConvertsJsonElementPayload()
    {
        var trigger = new StoredTrigger { Payload = JsonSerializer.SerializeToElement(new { name = "orders", kind = "Topic" }) };

        Assert.Equal(new TestPayload("orders", TestKind.Topic), trigger.GetPayload<TestPayload>());
    }

    [Fact]
    public void GetPayload_ReturnsTypedPayloadAsIs()
    {
        var expected = new TestPayload("orders", TestKind.Queue);
        var trigger = new StoredTrigger { Payload = expected };

        Assert.Same(expected, trigger.GetPayload<TestPayload>());
    }

    private static JsonPayloadSerializer CreatePayloadSerializer()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ISerializationTypeRegistry, SerializationTypeRegistry>()
            .BuildServiceProvider();
        return new(services);
    }

    public enum TestKind { Queue, Topic }

    public record TestPayload(string Name, TestKind Kind);
}
