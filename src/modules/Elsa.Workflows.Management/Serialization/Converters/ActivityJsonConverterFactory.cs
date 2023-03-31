using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// Creates instances of <see cref="ActivityJsonConverter"/>.
/// </summary>
public class ActivityJsonConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc />
    public ActivityJsonConverterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Notice that this factory only creates converters when the type to convert is IActivity.
    // The ActivityJsonConverter will create concrete activity objects, which then uses regular serialization
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(IActivity);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => ActivatorUtilities.CreateInstance<ActivityJsonConverter>(_serviceProvider);
}