using System.Text.Json;
using Elsa.Resilience.Core.UnitTests.TestHelpers;
using Elsa.Resilience.Options;
using Elsa.Resilience.Serialization;
using System.Threading.Tasks;

namespace Elsa.Resilience.Core.UnitTests;

public class ResilienceStrategySerializerTests
{
    private readonly ResilienceStrategySerializer _serializer = CreateSerializer(typeof(TestRetryStrategy), typeof(TestNoopStrategy));

    private static ResilienceStrategySerializer CreateSerializer(params Type[] strategyTypes)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ResilienceOptions
        {
            StrategyTypes = strategyTypes.ToList()
        });

        return new(options);
    }

    [Test]
    [DisplayName("Serializer should write a type discriminator named after the strategy type")]
    public async Task Serialize_RegisteredStrategy_WritesTypeDiscriminator()
    {
        var json = _serializer.Serialize(new TestRetryStrategy());

        using var document = JsonDocument.Parse(json);
        await Assert.That(document.RootElement.GetProperty("$type").GetString()).IsEqualTo(nameof(TestRetryStrategy));
    }

    [Test]
    [DisplayName("Serializer should write property names in camel case")]
    public async Task Serialize_RegisteredStrategy_UsesCamelCasePropertyNames()
    {
        var json = _serializer.Serialize(new TestRetryStrategy
        {
            Id = "my-strategy",
            MaxRetryAttempts = 7
        });

        using var document = JsonDocument.Parse(json);
        await Assert.That(document.RootElement.GetProperty("id").GetString()).IsEqualTo("my-strategy");
        await Assert.That(document.RootElement.GetProperty("maxRetryAttempts").GetInt32()).IsEqualTo(7);
    }

    [Test]
    [DisplayName("Serializer should write enums as strings")]
    public async Task Serialize_StrategyWithEnum_WritesEnumAsString()
    {
        var json = _serializer.Serialize(new TestNoopStrategy
        {
            Flavor = TestStrategyFlavor.Fancy
        });

        using var document = JsonDocument.Parse(json);
        await Assert.That(document.RootElement.GetProperty("flavor").GetString()).IsEqualTo(nameof(TestStrategyFlavor.Fancy));
    }

    [Test]
    [DisplayName("Serializer should round-trip a strategy back into its concrete type")]
    public async Task Deserialize_SerializedStrategy_ReturnsConcreteType()
    {
        var json = _serializer.Serialize(new TestRetryStrategy
        {
            Id = "round-trip",
            DisplayName = "Round Trip",
            MaxRetryAttempts = 4
        });

        var result = _serializer.Deserialize(json);
        await Assert.That(result).IsOfType(typeof(TestRetryStrategy));
        var strategy = (TestRetryStrategy)result;

        await Assert.That(strategy.Id).IsEqualTo("round-trip");
        await Assert.That(strategy.DisplayName).IsEqualTo("Round Trip");
        await Assert.That(strategy.MaxRetryAttempts).IsEqualTo(4);
    }

    [Test]
    [DisplayName("Serializer should read property names case-insensitively")]
    public async Task Deserialize_PascalCasePropertyNames_ReadsValues()
    {
        var json = $$"""{"$type":"{{nameof(TestRetryStrategy)}}","Id":"pascal","MaxRetryAttempts":3}""";

        var result = _serializer.Deserialize(json);
        await Assert.That(result).IsOfType(typeof(TestRetryStrategy));
        var strategy = (TestRetryStrategy)result;

        await Assert.That(strategy.Id).IsEqualTo("pascal");
        await Assert.That(strategy.MaxRetryAttempts).IsEqualTo(3);
    }

    [Test]
    [DisplayName("Serializer should read numbers written as strings")]
    public async Task Deserialize_NumberAsString_ReadsNumber()
    {
        var json = $$"""{"$type":"{{nameof(TestRetryStrategy)}}","maxRetryAttempts":"5"}""";

        var result = _serializer.Deserialize(json);
        await Assert.That(result).IsOfType(typeof(TestRetryStrategy));
        var strategy = (TestRetryStrategy)result;

        await Assert.That(strategy.MaxRetryAttempts).IsEqualTo(5);
    }

    [Test]
    [DisplayName("Serializer should round-trip a heterogeneous list of strategies")]
    public async Task SerializeMany_MixedStrategies_RoundTripsEachConcreteType()
    {
        var json = _serializer.SerializeMany([
            new TestRetryStrategy { Id = "first" },
            new TestNoopStrategy { Id = "second" }
        ]);

        var strategies = _serializer.DeserializeMany(json).ToList();
        await Assert.That(strategies).Count().IsEqualTo(2);
        await Assert.That(strategies[0]).IsOfType(typeof(TestRetryStrategy));
        await Assert.That(strategies[1]).IsOfType(typeof(TestNoopStrategy));
        var first = (TestRetryStrategy)strategies[0];
        var second = (TestNoopStrategy)strategies[1];
        await Assert.That(first.Id).IsEqualTo("first");
        await Assert.That(second.Id).IsEqualTo("second");
    }

    [Test]
    [DisplayName("Serializer should reject strategy types that were not registered")]
    public void Serialize_UnregisteredStrategy_Throws()
    {
        var serializer = CreateSerializer(typeof(TestNoopStrategy));

        Assert.ThrowsExactly<NotSupportedException>(() => serializer.Serialize(new TestRetryStrategy()));
    }
}
