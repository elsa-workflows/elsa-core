using System.Text.Json;
using Elsa.Workflows.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Serialization;

internal class SerializationConfigurator : SerializationOptionsConfiguratorBase
{
    private readonly IServiceProvider _serviceProvider;

    public SerializationConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public override void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(ActivatorUtilities.GetServiceOrCreateInstance<ArgumentJsonConverterFactory>(_serviceProvider));
    }
}