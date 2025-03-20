using Microsoft.Extensions.Configuration;

namespace Elsa.Framework.Shells.Extensions;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddConfiguration(this IConfigurationBuilder configurationBuilder, IConfiguration config)
    {
        return configurationBuilder.Add(new ChainedConfigurationSource
        {
            Configuration = config
        });
    }
}