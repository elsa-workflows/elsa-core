using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Common.Converters;
using Elsa.Resilience.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Resilience.Serialization;

public class ResilienceStrategySerializer
{
    private readonly JsonSerializerOptions _serializerOptions;

    public ResilienceStrategySerializer(IOptions<ResilienceOptions> options)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(typeInfo =>
            {
                if (typeInfo.Type != typeof(IResilienceStrategy))
                    return;

                if (typeInfo.Kind != JsonTypeInfoKind.Object)
                    return;
            
                var polymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$type"
                };

                foreach (var type in options.Value.StrategyTypes.ToList())
                    polymorphismOptions.DerivedTypes.Add(new(type, type.Name));

                typeInfo.PolymorphismOptions = polymorphismOptions;
            }),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        
        _serializerOptions = serializerOptions;
    }

    public JsonSerializerOptions SerializerOptions => _serializerOptions;
    public string Serialize(IResilienceStrategy strategy) => JsonSerializer.Serialize(strategy, _serializerOptions);
    public string SerializeMany(IEnumerable<IResilienceStrategy> strategies) => JsonSerializer.Serialize(strategies, _serializerOptions);
    public IResilienceStrategy Deserialize(string json) => JsonSerializer.Deserialize<IResilienceStrategy>(json, _serializerOptions)!;
    public IEnumerable<IResilienceStrategy> DeserializeMany(string json) => JsonSerializer.Deserialize<IEnumerable<IResilienceStrategy>>(json, _serializerOptions)!;
}