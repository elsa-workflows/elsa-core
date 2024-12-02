using Elsa.Common.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;

namespace Elsa.EntityFrameworkCore.Modules.Tenants;

public class ConfigurationJsonConverter() : ValueConverter<IConfiguration, string>(
    config => SerializeConfiguration(config),
    json => DeserializeConfiguration(json))
{
    private static string SerializeConfiguration(IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        return Serializers.SerializeConfiguration(configuration);
    }

    private static IConfiguration DeserializeConfiguration(string json)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));
        return Serializers.DeserializeConfiguration(json);
    }
}