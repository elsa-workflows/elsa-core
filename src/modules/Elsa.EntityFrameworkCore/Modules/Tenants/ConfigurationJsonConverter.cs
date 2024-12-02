using System.Text.Json;
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

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        return JsonSerializer.Serialize(configuration.AsEnumerable(), jsonOptions);
    }

    private static IConfiguration DeserializeConfiguration(string json)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));

        var data = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(data);
        return configurationBuilder.Build();
    }
}