using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Contracts;
using Elsa.Management.Contracts;

namespace Elsa.Management.Serialization.Converters;

public class TriggerJsonConverterFactory : JsonConverterFactory
{
    private readonly ITriggerRegistry _triggerRegistry;
    private readonly IServiceProvider _serviceProvider;
    
    public TriggerJsonConverterFactory(ITriggerRegistry triggerRegistry, IServiceProvider serviceProvider)
    {
        _triggerRegistry = triggerRegistry;
        _serviceProvider = serviceProvider;
    }

    // Notice that this factory only creates converters when the type to convert is ITrigger.
    // The TriggerJsonConverter will create concrete trigger objects, which then uses regular serialization
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(ITrigger);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new TriggerJsonConverter(_triggerRegistry, _serviceProvider);
}