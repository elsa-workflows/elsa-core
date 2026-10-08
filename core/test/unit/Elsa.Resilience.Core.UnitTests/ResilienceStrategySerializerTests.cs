using System.Text.Json;
using Elsa.Resilience.Core.UnitTests.TestHelpers;
using Elsa.Resilience.Options;
using Elsa.Resilience.Serialization;

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

    [Fact(DisplayName = "Serializer should write a type discriminator named after the strategy type")]
    public void Serialize_RegisteredStrategy_WritesTypeDiscriminator()
    {
        var json = _serializer.Serialize(new TestRetryStrategy());

        using var document = JsonDocument.Parse(json);
        Assert.Equal(nameof(TestRetryStrategy), document.RootElement.GetProperty("$type").GetString());
    }

    [Fact(DisplayName = "Serializer should write property names in camel case")]
    public void Serialize_RegisteredStrategy_UsesCamelCasePropertyNames()
    {
        var json = _serializer.Serialize(new TestRetryStrategy
        {
            Id = "my-strategy",
            MaxRetryAttempts = 7
        });

        using var document = JsonDocument.Parse(json);
        Assert.Equal("my-strategy", document.RootElement.GetProperty("id").GetString());
        Assert.Equal(7, document.RootElement.GetProperty("maxRetryAttempts").GetInt32());
    }

    [Fact(DisplayName = "Serializer should write enums as strings")]
    public void Serialize_StrategyWithEnum_WritesEnumAsString()
    {
        var json = _serializer.Serialize(new TestNoopStrategy
        {
            Flavor = TestStrategyFlavor.Fancy
        });

        using var document = JsonDocument.Parse(json);
        Assert.Equal(nameof(TestStrategyFlavor.Fancy), document.RootElement.GetProperty("flavor").GetString());
    }

    [Fact(DisplayName = "Serializer should round-trip a strategy back into its concrete type")]
    public void Deserialize_SerializedStrategy_ReturnsConcreteType()
    {
        var json = _serializer.Serialize(new TestRetryStrategy
        {
            Id = "round-trip",
            DisplayName = "Round Trip",
            MaxRetryAttempts = 4
        });

        var strategy = Assert.IsType<TestRetryStrategy>(_serializer.Deserialize(json));

        Assert.Equal("round-trip", strategy.Id);
        Assert.Equal("Round Trip", strategy.DisplayName);
        Assert.Equal(4, strategy.MaxRetryAttempts);
    }

    [Fact(DisplayName = "Serializer should read property names case-insensitively")]
    public void Deserialize_PascalCasePropertyNames_ReadsValues()
    {
        var json = $$"""{"$type":"{{nameof(TestRetryStrategy)}}","Id":"pascal","MaxRetryAttempts":3}""";

        var strategy = Assert.IsType<TestRetryStrategy>(_serializer.Deserialize(json));

        Assert.Equal("pascal", strategy.Id);
        Assert.Equal(3, strategy.MaxRetryAttempts);
    }

    [Fact(DisplayName = "Serializer should read numbers written as strings")]
    public void Deserialize_NumberAsString_ReadsNumber()
    {
        var json = $$"""{"$type":"{{nameof(TestRetryStrategy)}}","maxRetryAttempts":"5"}""";

        var strategy = Assert.IsType<TestRetryStrategy>(_serializer.Deserialize(json));

        Assert.Equal(5, strategy.MaxRetryAttempts);
    }

    [Fact(DisplayName = "Serializer should round-trip a heterogeneous list of strategies")]
    public void SerializeMany_MixedStrategies_RoundTripsEachConcreteType()
    {
        var json = _serializer.SerializeMany([
            new TestRetryStrategy { Id = "first" },
            new TestNoopStrategy { Id = "second" }
        ]);

        var strategies = _serializer.DeserializeMany(json).ToList();

        Assert.Collection(strategies,
            s => Assert.Equal("first", Assert.IsType<TestRetryStrategy>(s).Id),
            s => Assert.Equal("second", Assert.IsType<TestNoopStrategy>(s).Id));
    }

    [Fact(DisplayName = "Serializer should reject strategy types that were not registered")]
    public void Serialize_UnregisteredStrategy_Throws()
    {
        var serializer = CreateSerializer(typeof(TestNoopStrategy));

        Assert.Throws<NotSupportedException>(() => serializer.Serialize(new TestRetryStrategy()));
    }
}
