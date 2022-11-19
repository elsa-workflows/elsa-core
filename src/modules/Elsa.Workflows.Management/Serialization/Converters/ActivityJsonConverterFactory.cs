using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Serialization.Converters;

public class ActivityJsonConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ActivityJsonConverterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Notice that this factory only creates converters when the type to convert is IActivity.
    // The ActivityJsonConverter will create concrete activity objects, which then uses regular serialization
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IActivity);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => ActivatorUtilities.CreateInstance<ActivityJsonConverter>(_serviceProvider);
}