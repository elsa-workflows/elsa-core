using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Options;
using Elsa.Workflows;
using Microsoft.Extensions.Options;

namespace Elsa.Alterations.Core.Serialization;

/// <summary>
/// Add additional <see cref="JsonConverter"/> objects.
/// </summary>
public class AlterationSerializationOptionConfigurator : SerializationOptionsConfiguratorBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<AlterationOptions> _options;

    /// <inheritdoc />
    public AlterationSerializationOptionConfigurator(IServiceProvider serviceProvider, IOptions<AlterationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    // public override void Configure(JsonSerializerOptions options)
    // {
    //     options.Converters.Add(new AlterationJsonConverterFactory(_serviceProvider));
    // }

    /// <inheritdoc />
    public override IEnumerable<Action<JsonTypeInfo>> GetModifiers()
    {
        var alterationTypes = _options.Value.AlterationTypes;
    
        yield return typeInfo =>
        {
            if (typeInfo.Type != typeof(IAlteration))
                return;
            
            if(typeInfo.Kind != JsonTypeInfoKind.Object)
                return;
    
            var polymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "type"
            };
    
            foreach (var alterationType in alterationTypes.ToList())
            {
                polymorphismOptions.DerivedTypes.Add(new JsonDerivedType(alterationType, alterationType.Name));
            }
            
            typeInfo.PolymorphismOptions = polymorphismOptions;
        };
    }
}